import type { EditorView } from '@codemirror/view';
import { syntaxTree } from '@codemirror/language';
import type { SyntaxNode } from '@lezer/common';
import { linkAt } from '../src/editor/decorations/links';
import { UiText, uiText } from '../src/shared/strings';
import { elementNode, htmlNodes, imageSource } from './renderExpected';
import { normalizeBlocks, wrapFlow, type Align, type Block, type ExtKind, type Inline, type Item } from './renderTree';

/**
 * 编辑器侧：渲染态的 contentDOM → 语义树（renderTree.ts）。只看 DOM（编辑器自己的行装饰类、mark 类与 widget 类），
 * 不读语法树——看不见的东西（例如 `- - foo` 外层列表项没有画出的符号）就是渲染缺了它。例外两处，都是 DOM 里没有的信息：
 * - 链接地址（`](url)` 整段隐藏）：取 Ctrl+单击会打开的地址（links.ts 的 linkAt）；
 * - 链接引用定义（`[foo]: /url`）：编辑器按源码显示，规范里不渲染；它的行与段落行在 DOM 里无从区分，
 *   按语法树认出后跳过，否则所有引用式链接的用例都只能报出这一处有意差异。
 *
 * 做法：contentDOM 的每个子元素（行或块 widget）先抽成一行 Row——行的容器层次（引用 / 列表项，来自行装饰的
 * `--td-indent` 与引用竖线的 background-position）、列表符号 widget、行的种类与行内内容；再按容器层次逐层分组，
 * 还原引用、列表与列表项，最内层的行按种类合成段落、标题、代码块等。
 */

// ── 类名映射（全部集中在这里） ─────────────────────────────────────────

/** 行的种类 */
enum LineKind {
  /** 普通文字行：段落（或尚未做成块组件的内容，按源码显示） */
  Text,
  /** 块间空行（段距），只起分隔作用 */
  Gap,
  /** 标题行，级别见 cm-td-h1…h6 */
  Heading,
  /** Setext 标题的下划线行：按设计以灰色源码显示，不算内容 */
  SetextUnderline,
  /** 分隔线：`---` 隐藏，由行装饰画线 */
  Rule,
  /** 代码块首行：``` 隐藏，只剩可编辑的语言名（.cm-td-code-lang） */
  FenceOpen,
  /** 围栏块的内容行（代码、front matter；编辑态的公式与 mermaid） */
  FenceBody,
  /** HTML 块的源码框：渲染态下只有只含不可见内容（注释、script 等）的块这样显示 */
  HtmlSource,
  /** 编辑态的 `[TOC]` 源码行 */
  TocSource,
  /** 脚注定义区的行 */
  Footnote,
  /** 链接引用定义的行（按语法树认出，见文件头） */
  LinkDefinition,
}

/** 行装饰类 → 行的种类，按顺序取第一个命中的；都不命中是普通文字行 */
const LINE_CLASSES: readonly (readonly [string, LineKind])[] = [
  ['cm-td-gap', LineKind.Gap],
  ['cm-td-setext-underline', LineKind.SetextUnderline],
  ['cm-td-hr', LineKind.Rule],
  ['cm-td-fence-lang-line', LineKind.FenceOpen],
  ['cm-td-fence-body', LineKind.FenceBody],
  ['cm-td-html-src', LineKind.HtmlSource],
  ['cm-td-toc-src', LineKind.TocSource],
  ['cm-td-footnote', LineKind.Footnote],
  ['cm-td-h', LineKind.Heading],
];
/** 标题级别：cm-td-h1 … cm-td-h6 */
const HEADING_LEVEL = /(?:^|\s)cm-td-h([1-6])(?:\s|$)/;
/** 围栏块的内容行属于哪种块：cm-td-fence-code / -frontmatter / -math / -mermaid */
const FENCE_KIND: readonly (readonly [string, FenceKind])[] = [
  ['cm-td-fence-code', 'code'], ['cm-td-fence-frontmatter', 'frontmatter'], ['cm-td-fence-math', 'math'], ['cm-td-fence-mermaid', 'mermaid'],
];
type FenceKind = 'code' | 'frontmatter' | 'math' | 'mermaid';
/** 行装饰：有这个类的行在引用或列表项里，层数与引用所在层见行内样式 */
const NEST_CLASS = 'cm-td-nest';

/** 块 widget（contentDOM 的直接子元素，不是 .cm-line） */
enum BlockWidget { Table, Html, Math, Mermaid, Toc, Skip }
/** 块 widget 类 → 种类，按顺序取第一个命中的（编辑态的浮动预览同时带 cm-td-math 等类，所以 Skip 排在前面） */
const BLOCK_CLASSES: readonly (readonly [string, BlockWidget])[] = [
  ['cm-td-preview', BlockWidget.Skip], // 编辑态公式 / mermaid 的浮动预览：0 高，浮在下文之上
  ['cm-td-fence', BlockWidget.Skip], // 收起的围栏行（块首、块尾的内边距条）
  ['cm-td-table-wrap', BlockWidget.Table],
  ['cm-td-html-preview', BlockWidget.Html],
  ['cm-td-math', BlockWidget.Math],
  ['cm-td-mermaid', BlockWidget.Mermaid],
  ['cm-td-toc', BlockWidget.Toc],
];

/** 行内元素的角色 */
enum InlineRole { Skip, Em, Strong, Del, Mark, Code, Link, HtmlElement, Image, Math, Emoji, FootnoteRef, Break }
/**
 * 行内 mark 类与 widget 类 → 角色。不在表里的元素（`cm-td-syntax` 灰色标记、`cm-td-html-tag` 未渲染的标签源码、
 * `cm-td-url`、prism 高亮等）只是给文字上色，内容照常算作文字；隐藏的源码区间在 DOM 里是空的 span，没有文字。
 */
const INLINE_CLASSES: readonly (readonly [string, InlineRole])[] = [
  ['cm-widgetBuffer', InlineRole.Skip], // CM6 在 widget 两侧插的光标缓冲 <img>
  ['cm-td-marker', InlineRole.Skip], // 列表符号、任务框：在行一级读取
  ['cm-td-fence-prefix', InlineRole.Skip], // 代码块首行被隐藏的 ```
  ['cm-td-code-lang-placeholder', InlineRole.Skip], // 没写语言时的「输入语言」占位
  ['cm-td-math-preview', InlineRole.Skip], // 显形态行内公式的浮出预览（aria-hidden）
  ['cm-td-footnote-label', InlineRole.Skip], // 脚注定义区的编号标签
  ['cm-td-footnote-back', InlineRole.Skip], // 脚注定义区的「↩」
  ['katex-mathml', InlineRole.Skip], // KaTeX 给读屏的 MathML 副本
  ['cm-td-em', InlineRole.Em],
  ['cm-td-strong', InlineRole.Strong],
  ['cm-td-del', InlineRole.Del],
  ['cm-td-highlight', InlineRole.Mark],
  ['cm-td-code-inline', InlineRole.Code],
  ['cm-td-link', InlineRole.Link], // 行内链接的文字、自动链接、GFM 裸链接
  ['cm-td-html-inline', InlineRole.HtmlElement], // 行内 HTML 渲染成的真实元素（标签名即语义）
  ['cm-td-image', InlineRole.Image], // 图片 widget（含加载中、失败、空三种占位）
  ['cm-td-math-inline', InlineRole.Math],
  ['cm-td-emoji', InlineRole.Emoji],
  ['cm-td-footnote-ref', InlineRole.FootnoteRef],
  ['cm-td-html-br', InlineRole.Break], // 行内 `<br>` 的渲染态
];

// ── 行 ────────────────────────────────────────────────────────────────

type Container = 'quote' | 'item';
enum MarkerKind { Bullet, Ordered, Task }
interface Marker { kind: MarkerKind; /** 符号所属列表项在容器层次里的下标 */ level: number; number: number; checked: boolean }

type Row =
  | { k: 'line'; kind: LineKind; level: number; fence: FenceKind; containers: Container[]; marker: Marker | null; el: HTMLElement }
  | { k: 'block'; containers: Container[]; blocks: Block[] };

function classOf<T>(el: Element, table: readonly (readonly [string, T])[]): T | undefined {
  for (const [cls, v] of table) if (el.classList.contains(cls)) return v;
  return undefined;
}

/** 容器层次：`--td-indent` 层，其中引用竖线所在的层（background-position 里的 `calc(L * …)`）是引用，其余是列表项 */
function containersOf(el: HTMLElement): Container[] {
  if (!el.classList.contains(NEST_CLASS)) return [];
  const style = el.getAttribute('style') ?? '';
  const depth = Number(/--td-indent:\s*(\d+)/.exec(style)?.[1] ?? '0');
  const out: Container[] = Array(depth).fill('item');
  const pos = /background-position:([^;]*)/.exec(style)?.[1] ?? '';
  for (const m of pos.matchAll(/calc\((\d+) \*/g)) out[Number(m[1])] = 'quote';
  return out;
}

function markerOf(el: HTMLElement, depth: number): Marker | null {
  const box = el.querySelector<HTMLElement>(':scope > .cm-td-marker');
  if (!box) return null;
  const shift = Number(box.style.getPropertyValue('--td-marker-shift') || '0');
  const level = depth - 1 - shift;
  const task = box.querySelector<HTMLInputElement>('input.cm-td-task');
  if (task) return { kind: MarkerKind.Task, level, number: 0, checked: task.checked };
  const list = box.querySelector('.cm-td-marker-list');
  if (list?.localName === 'ol') return { kind: MarkerKind.Ordered, level, number: (list as HTMLOListElement).start, checked: false };
  return { kind: MarkerKind.Bullet, level, number: 0, checked: false };
}

function toRow(el: HTMLElement, view: EditorView): Row | null {
  if (!el.classList.contains('cm-line')) {
    const kind = classOf(el, BLOCK_CLASSES);
    if (kind === undefined || kind === BlockWidget.Skip) return null;
    return { k: 'block', containers: [], blocks: blockWidget(el, kind, view) };
  }
  const containers = containersOf(el);
  let kind = classOf(el, LINE_CLASSES) ?? LineKind.Text;
  if (kind === LineKind.Text && inLinkDefinition(view, el)) kind = LineKind.LinkDefinition;
  const level = Number(HEADING_LEVEL.exec(el.className)?.[1] ?? '0');
  return { k: 'line', kind, level, fence: classOf(el, FENCE_KIND) ?? 'code', containers, marker: markerOf(el, containers.length), el };
}

function inLinkDefinition(view: EditorView, el: HTMLElement): boolean {
  const line = view.state.doc.lineAt(view.posAtDOM(el, 0));
  for (let n: SyntaxNode | null = syntaxTree(view.state).resolveInner(line.to, -1); n; n = n.parent) if (n.name === 'LinkReference') return true;
  return false;
}

// ── 块 widget ─────────────────────────────────────────────────────────

function blockWidget(el: HTMLElement, kind: BlockWidget, view: EditorView): Block[] {
  switch (kind) {
    case BlockWidget.Table: return [tableWidget(el, view)];
    // HTML 块：消毒后的真实 HTML，按期望侧同一套规则抽取；「空 HTML」占位表示渲染出来什么都没有
    case BlockWidget.Html: return el.querySelector(':scope > .cm-td-render-empty') ? [] : wrapFlow(htmlNodes(el));
    case BlockWidget.Math: return [ext('math', el)];
    case BlockWidget.Mermaid: return [ext('mermaid', el)];
    case BlockWidget.Toc: return [ext('toc', el)];
    case BlockWidget.Skip: return [];
  }
}

const ext = (kind: ExtKind, el: HTMLElement): Block => ({ t: 'ext', kind, v: (el.textContent ?? '').replace(/\s+/g, ' ').trim() });

function tableWidget(el: HTMLElement, view: EditorView): Block {
  const rows = Array.from(el.querySelectorAll('tr'));
  const align = (c: HTMLElement): Align => {
    const a = c.style.textAlign;
    return a === 'left' || a === 'center' || a === 'right' ? a : null;
  };
  const cells = (r: Element) => Array.from(r.children).filter((c): c is HTMLElement => c.localName === 'td' || c.localName === 'th');
  return {
    t: 'table',
    align: rows[0] ? cells(rows[0]).map(align) : [],
    // 单元格内容：B1 之前是 .cm-td-cell 里的纯文本；之后无论渲染成行内元素还是嵌套视图，都按行内规则抽取
    rows: rows.map(r => cells(r).map(c => inlineNodes(c.querySelector('.cm-td-cell') ?? c, view))),
  };
}

// ── 行内 ──────────────────────────────────────────────────────────────

/** 元素的行内内容。嵌套的 .cm-line（单元格里的嵌套视图）之间记为换行 */
function inlineNodes(parent: Element, view: EditorView): Inline[] {
  const out: Inline[] = [];
  for (const n of Array.from(parent.childNodes)) {
    if (n.nodeType === 3) { out.push({ t: 'text', v: (n as Text).data }); continue; }
    if (n.nodeType !== 1) continue;
    const el = n as HTMLElement;
    if (el.classList.contains('cm-line') && out.length) out.push({ t: 'br' });
    out.push(...inlineElement(el, view));
  }
  return out;
}

function inlineElement(el: HTMLElement, view: EditorView): Inline[] {
  const roles = INLINE_CLASSES.filter(([cls]) => el.classList.contains(cls)).map(([, r]) => r);
  if (roles.includes(InlineRole.Skip)) return [];
  if (!roles.length) return inlineNodes(el, view);
  // 同一个元素带几个角色时（CM6 合并了同范围的 mark）按表里的顺序由外到内套
  let inner: Inline[] | null = null;
  for (const role of [...roles].reverse()) inner = [roleNode(role, el, view, inner)].filter((x): x is Inline => x !== null);
  return inner ?? [];
}

function roleNode(role: InlineRole, el: HTMLElement, view: EditorView, inner: Inline[] | null): Inline | null {
  const kids = () => inner ?? inlineNodes(el, view);
  switch (role) {
    case InlineRole.Em: return { t: 'em', c: kids() };
    case InlineRole.Strong: return { t: 'strong', c: kids() };
    case InlineRole.Del: return { t: 'del', c: kids() };
    case InlineRole.Mark: return { t: 'mark', c: kids() };
    case InlineRole.Code: return { t: 'icode', v: el.textContent ?? '' };
    case InlineRole.Link: {
      // 地址不在 DOM 里：取 Ctrl+单击会打开的地址（文字起点处的链接）
      const pos = view.posAtDOM(el, 0);
      const link = linkAt(syntaxTree(view.state), view.state.doc, pos, 1);
      return { t: 'link', href: link?.url ?? null, c: kids() };
    }
    case InlineRole.HtmlElement: return elementNode(el, () => inner ?? inlineNodes(el, view)) as Inline | null;
    case InlineRole.Image: return image(el);
    case InlineRole.Math: return { t: 'math', v: el.querySelector('annotation')?.textContent ?? el.textContent ?? '' };
    case InlineRole.Emoji: return { t: 'text', v: el.textContent ?? '' };
    case InlineRole.FootnoteRef: return { t: 'fnref', label: el.dataset.label ?? '' };
    case InlineRole.Break: return { t: 'br' };
    case InlineRole.Skip: return null;
  }
}

/** 图片 widget：成功与加载中是 <img>（地址还原成源码里的路径），失败占位的文字里带着源码地址，空占位没有地址 */
function image(el: HTMLElement): Inline {
  const img = el.querySelector('img');
  if (img) return { t: 'image', src: imageSource(img.getAttribute('src') ?? ''), alt: img.getAttribute('alt') ?? '' };
  if (el.classList.contains('cm-td-image-fail')) {
    const text = el.querySelector('.cm-td-image-text')?.textContent ?? '';
    const prefix = `${uiText(UiText.ImageLoadFailed)}：`;
    return { t: 'image', src: text.startsWith(prefix) ? text.slice(prefix.length) : text, alt: '' };
  }
  return { t: 'image', src: '', alt: '' };
}

/** 代码行的文字（跳过 widget 与隐藏的元素） */
function lineText(el: Element): string {
  let s = '';
  for (const n of Array.from(el.childNodes)) {
    if (n.nodeType === 3) s += (n as Text).data;
    else if (n.nodeType === 1 && !INLINE_CLASSES.some(([cls, r]) => r === InlineRole.Skip && (n as Element).classList.contains(cls))) s += lineText(n as Element);
  }
  return s;
}

// ── 按容器层次还原块结构 ───────────────────────────────────────────────

type LineRow = Extract<Row, { k: 'line' }>;
const isGap = (r: Row) => r.k === 'line' && r.kind === LineKind.Gap;
const markerAt = (r: Row, d: number): Marker | null => (r.k === 'line' && r.marker && r.marker.level === d ? r.marker : null);

function build(rows: Row[], d: number, view: EditorView): Block[] {
  const out: Block[] = [];
  let i = 0;
  while (i < rows.length) {
    const c = rows[i].containers[d];
    if (c === undefined) i = leaf(rows, i, d, out, view);
    else if (c === 'quote') {
      let j = i + 1;
      while (j < rows.length && rows[j].containers[d] === 'quote') j++;
      out.push({ t: 'quote', c: build(rows.slice(i, j), d + 1, view) });
      i = j;
    } else i = list(rows, i, d, out, view);
  }
  return out;
}

const listKind = (m: Marker | null) => (m?.kind === MarkerKind.Ordered ? 'ol' : 'ul');

/** 从 rows[i] 起的一个列表（同层连续的列表项；中间的块间空行之后若还是同类列表项则延续） */
function list(rows: Row[], i: number, d: number, out: Block[], view: EditorView): number {
  const first = markerAt(rows[i], d);
  const kind = listKind(first);
  const items: { marker: Marker | null; rows: Row[] }[] = [];
  // 下一个列表项的符号接得上本列表：同为有序或无序；有序列表的序号是首项加序位，接不上说明是另起的列表（`3)` 换了分隔符）
  const continues = (m: Marker) => listKind(m) === kind && (m.kind !== MarkerKind.Ordered || m.number === (first?.number ?? 1) + items.length);
  let j = i;
  while (j < rows.length) {
    const r = rows[j];
    if (r.containers[d] === 'item') {
      const m = markerAt(r, d);
      if (m && items.length && !continues(m)) break;
      if (m || !items.length) items.push({ marker: m, rows: [] });
      items[items.length - 1].rows.push(r);
      j++;
      continue;
    }
    if (!isGap(r)) break;
    // 块间空行：之后第一行仍是本层列表项才延续
    let k = j;
    while (k < rows.length && isGap(rows[k]) && rows[k].containers[d] !== 'item') k++;
    const next = rows[k];
    if (!next || next.containers[d] !== 'item') break;
    const m = markerAt(next, d);
    if (m && !continues(m)) break;
    j = k;
  }
  const c: Item[] = items.map(it => ({ t: 'item', task: it.marker?.kind === MarkerKind.Task ? it.marker.checked : null, c: build(it.rows, d + 1, view) }));
  out.push({ t: 'list', ordered: kind === 'ol', start: first?.kind === MarkerKind.Ordered ? first.number : 1, c });
  return j;
}

/** rows[i] 起、容器层次恰为 d 的一个叶子块 */
function leaf(rows: Row[], i: number, d: number, out: Block[], view: EditorView): number {
  const r = rows[i];
  if (r.k === 'block') { out.push(...r.blocks); return i + 1; }
  const here = (k: number): LineRow | null => {
    const x = rows[k];
    return x && x.k === 'line' && x.containers.length === d ? x : null;
  };
  const run = (pred: (x: LineRow) => boolean) => {
    let j = i;
    while (here(j) && pred(here(j)!)) j++;
    return j;
  };
  const joinLines = (from: number, to: number): Inline[] => {
    const c: Inline[] = [];
    for (let k = from; k < to; k++) {
      const line = inlineNodes((rows[k] as LineRow).el, view);
      if (k > from && c[c.length - 1]?.t !== 'br') c.push({ t: 'br' });
      c.push(...line);
    }
    return c;
  };
  switch (r.kind) {
    case LineKind.Gap:
    case LineKind.SetextUnderline:
      return i + 1;
    case LineKind.Rule:
      out.push({ t: 'hr' });
      return i + 1;
    case LineKind.Heading: {
      // Setext 标题可以有几行（下划线行结束）；ATX 标题只有一行
      const j = run(x => x.kind === LineKind.Heading && x.level === r.level);
      const multi = j - i > 1 && here(j)?.kind === LineKind.SetextUnderline;
      if (multi) { out.push({ t: 'heading', level: r.level, c: joinLines(i, j) }); return j; }
      out.push({ t: 'heading', level: r.level, c: joinLines(i, i + 1) });
      return i + 1;
    }
    case LineKind.Text: {
      const j = run(x => x.kind === LineKind.Text);
      out.push({ t: 'paragraph', c: joinLines(i, j) });
      return j;
    }
    case LineKind.FenceOpen: {
      const lang = r.el.querySelector('.cm-td-code-lang')?.textContent ?? '';
      const j = run(x => x === r || (x.kind === LineKind.FenceBody && x.fence === r.fence));
      out.push({ t: 'code', lang, v: rows.slice(i + 1, j).map(x => lineText((x as LineRow).el)).join('\n') });
      return j;
    }
    case LineKind.FenceBody: {
      const j = run(x => x.kind === LineKind.FenceBody && x.fence === r.fence);
      const v = rows.slice(i, j).map(x => lineText((x as LineRow).el)).join('\n');
      out.push(r.fence === 'code' ? { t: 'code', lang: '', v } : { t: 'ext', kind: r.fence, v });
      return j;
    }
    case LineKind.HtmlSource:
      // 只含不可见内容的 HTML 块（期望侧同样丢弃注释、script 等）
      return run(x => x.kind === LineKind.HtmlSource);
    case LineKind.LinkDefinition:
      return run(x => x.kind === LineKind.LinkDefinition);
    case LineKind.TocSource:
      out.push({ t: 'ext', kind: 'toc', v: lineText(r.el) });
      return i + 1;
    case LineKind.Footnote: {
      const j = run(x => x.kind === LineKind.Footnote);
      out.push({ t: 'ext', kind: 'footnote', v: rows.slice(i, j).map(x => lineText((x as LineRow).el)).join(' ') });
      return j;
    }
  }
}

/** 渲染态 DOM → 归一化的语义树（含文末的哨兵段落，由调用方去掉） */
export function actualTree(view: EditorView): Block[] {
  const rows: Row[] = [];
  for (const el of Array.from(view.contentDOM.children) as HTMLElement[]) {
    const r = toRow(el, view);
    if (r) rows.push(r);
  }
  return normalizeBlocks(build(rows, 0, view));
}

