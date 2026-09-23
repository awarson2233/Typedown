import { StateField, type ChangeDesc, type EditorState, type Text } from '@codemirror/state';

/**
 * 大纲与标题 id：只看行首的扫描器，不依赖 syntaxTree。
 *
 * 语法树只可信到 parsedLength（同步解析只推进到视口末尾，后台解析也只推进到视口后 10 万字符），
 * 大纲却要覆盖全文，所以这里单独扫行首：只认第 0 列起头的结构（ATX 标题、setext 标题、围栏代码、
 * 公式块、HTML 块、front matter），缩进的行一律当作容器内容或续行，列表与引用里的标题不进大纲。
 *
 * 跨行状态只有两类：
 * - 区域（围栏代码、`$$` 公式块、HTML 块 1–5 类、front matter）：可以跨空行，作为条目记下起止；
 * - 段落 / 容器段落 / HTML 块 6–7 类：遇空行就结束。
 * 所以「空行之后且不在区域里」「某个条目刚结束」的行首都是默认状态，增量维护从改动前最近的这种行首
 * 开始重扫，扫到改动之后新旧两边都回到默认状态的行首为止，其余条目按变化映射。
 */

export interface OutlineItem {
  readonly from: number;
  /** 最后一行的行尾 */
  readonly to: number;
  /** 1–6 是标题级别；0 是区域（围栏代码、公式块、HTML 块、front matter） */
  readonly level: number;
  /** 标题的源码文本（去掉 # 与闭合序列；setext 标题多行以空格连接）；区域为空串 */
  readonly text: string;
}

export interface Outline {
  /** 按位置排序、互不重叠 */
  readonly items: readonly OutlineItem[];
  readonly headings: readonly OutlineItem[];
  /** 标题的级别、文本或顺序变化时 +1；只是位置随编辑移动时不变（outline.changed 据此决定是否发送） */
  readonly revision: number;
}

type Open =
  | { kind: 'fence'; ch: string; len: number }
  | { kind: 'math' }
  | { kind: 'html'; end: RegExp }
  | { kind: 'front' };
/**
 * none：默认；para：第 0 列起头的段落（可接 setext 下划线）；other：列表 / 引用 / 缩进 1–3 格 / 表格起头的段落；
 * code：缩进代码块（没有惰性续行，第 0 列的行重新起头）；html：6–7 类 HTML 块
 */
type Block = 'none' | 'para' | 'other' | 'code' | 'html';

const indentWidth = (t: string) => {
  let w = 0;
  for (let i = 0; i < t.length; i++) {
    const c = t.charCodeAt(i);
    if (c === 32) w++;
    else if (c === 9) w += 4 - (w % 4);
    else break;
  }
  return w;
};
/** 链接引用定义（目标写在同一行、可带标题）：它不起段落，所以后面的 `===` 不是 setext 下划线 */
const LINK_REF_DEF = /^\[(?:[^\]\\]|\\.)+\]:[ \t]*(?:<[^>\n]*>|[^\s<]\S*)(?:[ \t]+(?:"[^"]*"|'[^']*'|\([^)]*\)))?[ \t]*$/;

const FRONT_LOOKAHEAD = 64 * 1024; // 与 syntax/frontmatter.ts 相同
const HTML_BLOCK_6 = new Set(('address article aside base basefont blockquote body caption center col colgroup dd details dialog dir div dl dt ' +
  'fieldset figcaption figure footer form frame frameset h1 h2 h3 h4 h5 h6 head header hr html iframe legend li link main menu menuitem ' +
  'nav noframes ol optgroup option p param search section summary table tbody td tfoot th thead title tr track ul').split(' '));
const HTML_7 = /^(?:<[A-Za-z][A-Za-z0-9-]*(?:\s+[A-Za-z_:][\w.:-]*(?:\s*=\s*(?:[^\s"'=<>`]+|'[^']*'|"[^"]*"))?)*\s*\/?>|<\/[A-Za-z][A-Za-z0-9-]*\s*>)\s*$/;

const isBlank = (t: string) => /^[ \t]*$/.test(t);

/** HTML 块起始（CommonMark 4.6）：返回 1–5 类的结束条件、6 或 7，不是则 0 */
function htmlStart(t: string, inPara: boolean): RegExp | 6 | 7 | 0 {
  if (t.charCodeAt(0) !== 60 /* < */) return 0;
  if (/^<(?:script|pre|style|textarea)(?:\s|>|$)/i.test(t)) return /<\/(?:script|pre|style|textarea)>/i;
  if (t.startsWith('<!--')) return /-->/;
  if (t.startsWith('<?')) return /\?>/;
  if (t.startsWith('<![CDATA[')) return /\]\]>/;
  if (/^<![A-Za-z]/.test(t)) return />/;
  const m = /^<\/?([A-Za-z][A-Za-z0-9-]*)(?:[\s>]|\/>|$)/.exec(t);
  if (m && HTML_BLOCK_6.has(m[1].toLowerCase())) return 6;
  if (!inPara && HTML_7.test(t)) return 7;
  return 0;
}

/** 表格行的单元格数（忽略首尾竖线与转义竖线），与 @lezer/markdown 的 parseRow 计数口径一致 */
function cellCount(t: string): number {
  const s = t.trim();
  let n = 0, cell = false;
  for (let i = 0; i < s.length; i++) {
    const c = s[i];
    if (c === '\\') { i++; cell = true; continue; }
    if (c === '|') { if (cell || n === 0 && i > 0) n++; cell = false; continue; }
    if (c !== ' ' && c !== '\t') cell = true;
  }
  return cell ? n + 1 : n;
}
const DELIMITER_ROW = /^\|?(\s*:?-+:?\s*\|)+(\s*:?-+:?\s*)?$/;

function atxText(t: string): string {
  const s = t.replace(/^#{1,6}/, '');
  if (/^[ \t]*#*[ \t]*$/.test(s)) return '';
  return s.replace(/[ \t]+#+[ \t]*$/, '').trim();
}

function fenceClose(t: string, o: { ch: string; len: number }): boolean {
  const m = /^ {0,3}(`+|~+)[ \t]*$/.exec(t);
  return !!m && m[1][0] === o.ch && m[1].length >= o.len;
}

class Scanner {
  items: OutlineItem[] = [];
  open: Open | null = null;
  openFrom = 0;
  block: Block = 'none';
  paraStart = 0;
  paraLines = 0;
  paraFirst = '';

  constructor(readonly doc: Text) {}

  get isDefault(): boolean { return this.open === null && this.block === 'none'; }

  /** 文首：只有第一行是 `---` 且 64 KB 内有闭合行时才是 front matter（与 syntax/frontmatter.ts 相同） */
  startDocument(first: string): boolean {
    if (!/^---\s*$/.test(first)) return false;
    if (!/\n(---|\.\.\.)[ \t]*(\n|$)/.test(this.doc.sliceString(0, Math.min(this.doc.length, FRONT_LOOKAHEAD)))) return false;
    this.open = { kind: 'front' };
    this.openFrom = 0;
    return true;
  }

  line(from: number, to: number, t: string): void {
    const o = this.open;
    if (o) {
      const closed = o.kind === 'fence' ? fenceClose(t, o)
        : o.kind === 'math' ? /^ {0,3}\$\$\s*$/.test(t)
        : o.kind === 'html' ? o.end.test(t)
        : /^(---|\.\.\.)\s*$/.test(t);
      if (closed) this.closeRegion(to);
      return;
    }
    if (isBlank(t)) { this.block = 'none'; return; }
    if (this.block === 'html') return;
    const c0 = t.charCodeAt(0);
    if (c0 === 32 || c0 === 9) {
      // 缩进行：只处理段落的 setext 下划线与分隔线（CommonMark 允许 1–3 个空格），其余是续行或容器内容
      if (this.block === 'para' && /^ {1,3}(=+|-+)[ \t]*$/.test(t)) this.setext(to, t.trim()[0] === '=' ? 1 : 2);
      else if (/^ {1,3}(?:(?:\*[ \t]*){3,}|(?:-[ \t]*){3,}|(?:_[ \t]*){3,})$/.test(t)) this.block = 'none';
      else if (this.block === 'none') this.block = indentWidth(t) >= 4 ? 'code' : 'other';
      else if (this.block === 'para') this.paraLines++;
      return;
    }
    if (this.block === 'code') this.block = 'none';
    let m: RegExpExecArray | null;
    if ((m = /^(`{3,}|~{3,})(.*)$/.exec(t)) && !(m[1][0] === '`' && m[2].includes('`'))) {
      this.openRegion({ kind: 'fence', ch: m[1][0], len: m[1].length }, from);
      return;
    }
    // # 后面只认空格（@lezer/markdown 的 isAtxHeading 如此；CommonMark 还允许制表符）
    if ((m = /^(#{1,6})(?: |$)/.exec(t))) {
      this.items.push({ from, to, level: m[1].length, text: atxText(t) });
      this.block = 'none';
      return;
    }
    if (this.block === 'para' && (m = /^(=+|-+)[ \t]*$/.exec(t))) { this.setext(to, m[1][0] === '=' ? 1 : 2); return; }
    if (this.block === 'none' && /^\$\$\s*$/.test(t)) { this.openRegion({ kind: 'math' }, from); return; }
    const html = htmlStart(t, this.block === 'para');
    if (html instanceof RegExp) {
      if (html.test(t.slice(1))) this.block = 'none';
      else this.openRegion({ kind: 'html', end: html }, from);
      return;
    }
    if (html) { this.block = 'html'; return; }
    if (/^(?:(?:\*[ \t]*){3,}|(?:-[ \t]*){3,}|(?:_[ \t]*){3,})$/.test(t)) { this.block = 'none'; return; }
    if ((m = /^(?:([-+*])|(\d{1,9})[.)])([ \t]|$)/.exec(t)) || c0 === 62 /* > */) {
      // 列表项 / 引用：容器段落。空列表项与非 1 起头的有序列表不能打断段落（CommonMark 5.2）
      const interrupts = !m || (m[3] !== '' && (m[2] === undefined || m[2] === '1'));
      if (this.block !== 'para' || interrupts) { this.block = 'other'; return; }
    }
    if (this.block === 'none') {
      if (LINK_REF_DEF.test(t)) return;
      this.block = 'para';
      this.paraStart = from;
      this.paraLines = 1;
      this.paraFirst = t;
    } else if (this.block === 'para') {
      // 表格：第二行是分隔行、首行带竖线且列数相同（GFM 4.10），此后的续行都是表格行，不再接 setext 下划线
      if (this.paraLines === 1 && this.paraFirst.includes('|') && DELIMITER_ROW.test(t.trim()) && cellCount(this.paraFirst) === cellCount(t)) this.block = 'other';
      else this.paraLines++;
    }
  }

  finish(): void {
    if (this.open) this.closeRegion(this.doc.length);
  }

  private openRegion(open: Open, from: number) {
    this.open = open;
    this.openFrom = from;
    this.block = 'none';
  }

  private closeRegion(to: number) {
    this.items.push({ from: this.openFrom, to, level: 0, text: '' });
    this.open = null;
    this.block = 'none';
  }

  private setext(to: number, level: number) {
    const underline = this.doc.lineAt(to);
    const text = this.doc.sliceString(this.paraStart, underline.from - 1).split('\n').map(l => l.trim()).join(' ');
    this.items.push({ from: this.paraStart, to, level, text });
    this.block = 'none';
  }
}

function makeOutline(items: OutlineItem[], revision: number): Outline {
  return { items, headings: items.filter(i => i.level > 0), revision };
}

/** 全量扫描 */
export function scanOutline(doc: Text): Outline {
  const sc = new Scanner(doc);
  for (let n = 1; n <= doc.lines; n++) {
    const line = doc.line(n);
    if (n === 1 && sc.startDocument(line.text)) continue;
    sc.line(line.from, line.to, line.text);
  }
  sc.finish();
  return makeOutline(sc.items, 0);
}

/** items 里第一个 from >= pos 的下标 */
function lowerBound(items: readonly OutlineItem[], pos: number): number {
  let lo = 0, hi = items.length;
  while (lo < hi) { const mid = (lo + hi) >> 1; if (items[mid].from < pos) lo = mid + 1; else hi = mid; }
  return lo;
}

/** 旧文档里 pos（行首）处扫描器是否处于默认状态：不在任何条目内部，且上一行是空行或某个条目的最后一行 */
function defaultAt(doc: Text, items: readonly OutlineItem[], pos: number): boolean {
  if (pos === 0) return true;
  const i = lowerBound(items, pos);
  const prevItem = i > 0 ? items[i - 1] : null;
  if (prevItem && prevItem.to >= pos) return false;
  const prev = doc.lineAt(pos - 1);
  return isBlank(prev.text) || (!!prevItem && prevItem.to === prev.to);
}

const sameHeadings = (a: readonly OutlineItem[], b: readonly OutlineItem[]) => {
  const ha = a.filter(i => i.level > 0), hb = b.filter(i => i.level > 0);
  return ha.length === hb.length && ha.every((h, k) => h.level === hb[k].level && h.text === hb[k].text);
};

/** 增量维护：oldDoc/newDoc 是改动前后的全文，changes 是这次的变化 */
export function updateOutline(value: Outline, oldDoc: Text, newDoc: Text, changes: ChangeDesc): Outline {
  if (changes.empty) return value;
  const old = value.items;
  let firstA = Infinity, lastA = -1, firstB = Infinity, lastB = -1;
  const ranges: [number, number][] = [];
  changes.iterChangedRanges((fA, tA, fB, tB) => {
    ranges.push([fA, tA]);
    firstA = Math.min(firstA, fA); lastA = Math.max(lastA, tA);
    firstB = Math.min(firstB, fB); lastB = Math.max(lastB, tB);
  });

  // 与改动相接或重叠的条目作废；重扫起点退到它们之前
  let start = firstB;
  const touched = (it: OutlineItem) => ranges.some(([f, t]) => f <= it.to && t >= it.from);
  const kept: OutlineItem[] = [];
  for (const it of old) {
    if (it.to >= firstA && it.from <= lastA && touched(it)) { start = Math.min(start, changes.mapPos(it.from, -1)); continue; }
    kept.push(it.to < firstA ? it : { ...it, from: changes.mapPos(it.from, 1), to: changes.mapPos(it.to, 1) });
  }
  // front matter 取决于文首 64 KB 内的闭合行：文首是 `---` 时，前 64 KB 内的改动从文首重扫
  if (firstB < FRONT_LOOKAHEAD + 1 && (/^---\s*$/.test(newDoc.line(1).text) || /^---\s*$/.test(oldDoc.line(1).text))) start = 0;
  // 退到默认状态的行首：文首、空行之后、某个保留条目刚结束的行之后
  start = newDoc.lineAt(start).from;
  while (start > 0) {
    const prev = newDoc.lineAt(start - 1);
    const i = lowerBound(kept, start);
    const prevItem = i > 0 ? kept[i - 1] : null;
    if (isBlank(prev.text) || (prevItem && prevItem.to === prev.to)) break;
    start = prev.from;
  }

  const sc = new Scanner(newDoc);
  const minEnd = newDoc.lineAt(lastB).to; // 最后一处改动所在行的行尾
  const delta = newDoc.length - oldDoc.length;
  let stop = -1;
  for (let line = newDoc.lineAt(start); ; line = newDoc.line(line.number + 1)) {
    if (line.from === 0 && sc.startDocument(line.text)) { /* front matter 起头 */ }
    else sc.line(line.from, line.to, line.text);
    if (line.to >= newDoc.length) break;
    const next = line.to + 1;
    if (line.to >= minEnd && sc.isDefault && defaultAt(oldDoc, old, next - delta)) { stop = next; break; }
  }
  if (stop < 0) sc.finish();

  const before = kept.slice(0, lowerBound(kept, start)).filter(i => i.to < start);
  const after = stop < 0 ? [] : kept.slice(lowerBound(kept, stop));
  const items = [...before, ...sc.items, ...after];
  const oldWindow = old.slice(before.length, old.length - after.length);
  return makeOutline(items, sameHeadings(oldWindow, sc.items) ? value.revision : value.revision + 1);
}

export const outlineField = StateField.define<Outline>({
  create: state => scanOutline(state.doc),
  update: (value, tr) => tr.docChanged ? updateOutline(value, tr.startState.doc, tr.state.doc, tr.changes) : value,
});

/** 光标所在的标题（最后一个起点不晚于 pos 的标题）的下标，没有时为 -1 */
export function headingIndexAt(outline: Outline, pos: number): number {
  const hs = outline.headings;
  let lo = 0, hi = hs.length;
  while (lo < hi) { const mid = (lo + hi) >> 1; if (hs[mid].from <= pos) lo = mid + 1; else hi = mid; }
  return lo - 1;
}

/** 标题文本去掉行内标记，给大纲显示与 id 用 */
export function headingPlainText(text: string): string {
  return text
    .replace(/!?\[([^\]]*)\]\([^)]*\)/g, '$1')
    .replace(/\[([^\]]*)\]\[[^\]]*\]/g, '$1')
    .replace(/\*\*|__|~~|==|`/g, '')
    .replace(/(^|[^\\])\*/g, '$1')
    .replace(/\\([!-/:-@[-`{-~])/g, '$1')
    .trim();
}

const idCache = new WeakMap<readonly OutlineItem[], string[]>();

/**
 * 标题 id：纯文本转 slug（小写、去标点、空白换成 -），重名依次加 -1、-2（与 GitHub 的锚点规则相同）。
 * 同一份 headings 数组只算一次。协议里 HeadingId 对宿主是不透明字符串。
 */
export function headingIds(headings: readonly OutlineItem[]): string[] {
  const cached = idCache.get(headings);
  if (cached) return cached;
  const seen = new Map<string, number>();
  const ids = headings.map(h => {
    const base = headingPlainText(h.text).toLowerCase().replace(/[^\p{L}\p{N}\s_-]/gu, '').trim().replace(/\s+/g, '-') || 'section';
    const n = seen.get(base);
    seen.set(base, (n ?? 0) + 1);
    return n === undefined ? base : `${base}-${n}`;
  });
  idCache.set(headings, ids);
  return ids;
}

export const outlineOf = (state: EditorState): Outline => state.field(outlineField);
