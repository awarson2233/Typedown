/**
 * 渲染对照（render.test.ts）两侧共用的语义树：期望 HTML（renderExpected.ts）与编辑器渲染态 DOM（renderActual.ts）
 * 各自抽成这里的节点，经同一套 normalize 归一化后逐节点比对。
 *
 * 粒度：块（标题、段落、代码块、列表与列表项、任务框、引用、分隔线、表格、HTML 元素）与行内（强调、加粗、删除线、高亮、
 * 行内代码、链接、图片、换行、行内 HTML 元素）；文本按空白归一化，属性只比对语义相关的（级别、语言、起始号、勾选、对齐、href、src、alt）。
 *
 * 换行：编辑器按源码行显示，软换行与硬换行在渲染态里都是另起一行（Typora 式），两侧的软换行、硬换行都归一为 br。
 */

export type Align = 'left' | 'center' | 'right' | null;

export type Inline =
  | { t: 'text'; v: string }
  | { t: 'em' | 'strong' | 'del' | 'mark'; c: Inline[] }
  | { t: 'icode'; v: string }
  | { t: 'link'; href: string | null; c: Inline[] }
  | { t: 'image'; src: string; alt: string }
  | { t: 'br' }
  /** 行内公式（KaTeX 渲染结果，v 为 TeX 源码）：规范里没有，编辑器的扩展语法才会产生 */
  | { t: 'math'; v: string }
  /** 脚注引用上标（编辑器的扩展语法） */
  | { t: 'fnref'; label: string }
  /** 其余行内 HTML 元素：只比标签名与内容 */
  | { t: 'el'; tag: string; c: Node[] };

export interface Item { t: 'item'; task: boolean | null; c: Block[] }

export type Block =
  | { t: 'heading'; level: number; c: Inline[] }
  | { t: 'paragraph'; c: Inline[] }
  | { t: 'code'; lang: string; v: string }
  | { t: 'list'; ordered: boolean; start: number; c: Item[] }
  | { t: 'quote'; c: Block[] }
  | { t: 'hr' }
  | { t: 'table'; align: Align[]; rows: Inline[][][] }
  /** 块级公式、mermaid、front matter、目录、脚注定义：编辑器的扩展块，规范里没有，出现即不同 */
  | { t: 'ext'; kind: ExtKind; v: string }
  /** 其余块级 HTML 元素（div、自定义标签等）：只比标签名与内容 */
  | { t: 'el'; tag: string; c: Node[] };

export type ExtKind = 'math' | 'mermaid' | 'frontmatter' | 'toc' | 'footnote';

export type Node = Block | Inline | Item;

/** 块级 HTML 标签：流内容里遇到它们时结束当前的行内片段（片段包成段落） */
export const BLOCK_TAGS = new Set([
  'address', 'article', 'aside', 'blockquote', 'body', 'center', 'dd', 'details', 'dialog', 'dir', 'div', 'dl', 'dt', 'fieldset',
  'figcaption', 'figure', 'footer', 'form', 'h1', 'h2', 'h3', 'h4', 'h5', 'h6', 'header', 'hr', 'li', 'main', 'menu', 'nav', 'ol',
  'p', 'pre', 'section', 'summary', 'table', 'tbody', 'td', 'tfoot', 'th', 'thead', 'tr', 'ul', 'caption', 'colgroup', 'col',
]);

/** 渲染出来不可见的元素：两侧都丢弃（编辑器把只含这些内容的 HTML 块显示成源码框，见 renderActual.ts） */
export const INVISIBLE_TAGS = new Set(['script', 'style', 'template', 'noscript', 'title', 'meta', 'link', 'base']);

const isInline = (n: Node) => n.t === 'text' || n.t === 'em' || n.t === 'strong' || n.t === 'del' || n.t === 'mark' || n.t === 'icode'
  || n.t === 'link' || n.t === 'image' || n.t === 'br' || n.t === 'math' || n.t === 'fnref' || (n.t === 'el' && !BLOCK_TAGS.has(n.tag));

/** 流内容（根、列表项、引用、块级 HTML 元素）：连续的行内节点包成一个段落（紧凑与松散列表因此一致） */
export function wrapFlow(nodes: Node[]): Block[] {
  const out: Block[] = [];
  let run: Inline[] = [];
  const flush = () => { if (run.length) out.push({ t: 'paragraph', c: run }); run = []; };
  for (const n of nodes) {
    if (isInline(n)) run.push(n as Inline);
    else { flush(); if (n.t !== 'item') out.push(n as Block); }
  }
  flush();
  return out;
}

// ── 归一化 ─────────────────────────────────────────────────────────────

const WS = /[ \t\n\r\f]+/g;

/** 地址按百分号转义解码后比较（规范渲染器会把非 ASCII 与特殊字符编码成 %XX，源码里写的是原字符） */
function normalizeUrl(s: string): string {
  try { return decodeURIComponent(s); } catch { return s; }
}

/**
 * 图片的 alt：编辑器照 Muya 去掉 markdown 标记字符（imageWidget.ts 的 plainAlt），期望侧的纯文本 alt 做同样的处理后再比；
 * 编辑器侧已经没有这些字符，两侧都做一遍不影响它。
 */
const ALT_MARKUP = /[`*{}[\]()#+\-.!_>~:|<>$]/g;

/** 行内序列：换行提到元素外、同类相邻元素合并、文本空白折叠、块首尾与换行两侧去空白、去掉空文本 */
export function normalizeInlines(list: Inline[]): Inline[] {
  let seq = hoistBreaks(list.map(normalizeInline));
  seq = mergeAdjacent(seq);
  collapseSpaces(seq, { prevSpace: true });
  trimEdges(seq);
  return prune(seq);
}

function normalizeInline(n: Inline): Inline {
  switch (n.t) {
    case 'em': case 'strong': case 'del': case 'mark': {
      // 同类嵌套（`****foo****` 的 strong 套 strong）看起来与一层相同，拆掉内层
      const c = n.c.map(normalizeInline).flatMap(k => (k.t === n.t ? (k as typeof n).c : [k]));
      // 只包着一个链接时把链接提到外面：`[*a*](u)` 的强调与链接文字范围相同，CM6 的嵌套顺序与规范渲染器相反，看起来一样
      const only = c.length === 1 ? c[0] : null;
      if (only?.t === 'link') return { ...only, c: [{ t: n.t, c: only.c }] };
      return { t: n.t, c };
    }
    case 'link': return { ...n, href: n.href === null ? null : normalizeUrl(n.href), c: n.c.map(normalizeInline) };
    case 'icode': return { t: 'icode', v: n.v.replace(WS, ' ').trim() };
    case 'image': return { t: 'image', src: normalizeUrl(n.src), alt: n.alt.replace(ALT_MARKUP, '').replace(WS, ' ').trim() };
    case 'el': return { ...n, c: n.c.every(isInline) ? n.c.map(x => normalizeInline(x as Inline)) : normalizeBlocks(wrapFlow(n.c)) };
    default: return n;
  }
}

const hasChildren = (n: Inline): n is Extract<Inline, { c: Inline[] }> => n.t === 'em' || n.t === 'strong' || n.t === 'del' || n.t === 'mark' || n.t === 'link';

/**
 * 元素里的换行拆到元素外：`em[a br b]` → `em[a] br em[b]`。编辑器的标记装饰按行拆成几段，
 * 跨行的强调、链接在 DOM 里本来就是每行一段，期望侧同样拆开后两侧才可比。
 */
function hoistBreaks(list: Inline[]): Inline[] {
  const out: Inline[] = [];
  for (const n of list) {
    if (!hasChildren(n)) { out.push(n); continue; }
    const kids = hoistBreaks(n.c);
    if (!kids.some(k => k.t === 'br')) { out.push({ ...n, c: kids } as Inline); continue; }
    let part: Inline[] = [];
    const flush = () => { if (part.length) out.push({ ...n, c: part } as Inline); part = []; };
    for (const k of kids) {
      if (k.t === 'br') { flush(); out.push(k); } else part.push(k);
    }
    flush();
  }
  return out;
}

const sameShell = (a: Inline, b: Inline) =>
  a.t === b.t && (a.t !== 'link' || a.href === (b as typeof a).href) && (a.t !== 'el' || a.tag === (b as typeof a).tag);

/** 相邻的同类元素合并（CM6 会把一个标记拆成相邻几段），相邻文本合并；连续的换行只留一个 */
function mergeAdjacent(list: Inline[]): Inline[] {
  const out: Inline[] = [];
  for (const raw of list) {
    const n = hasChildren(raw) ? { ...raw, c: mergeAdjacent(raw.c) } as Inline : raw;
    const prev = out[out.length - 1];
    if (prev && n.t === 'text' && prev.t === 'text') out[out.length - 1] = { t: 'text', v: prev.v + n.v };
    else if (prev && hasChildren(n) && hasChildren(prev) && sameShell(prev, n)) out[out.length - 1] = { ...prev, c: mergeAdjacent([...prev.c, ...n.c]) } as Inline;
    else if (prev && n.t === 'icode' && prev.t === 'icode') out[out.length - 1] = { t: 'icode', v: prev.v + ' ' + n.v };
    else out.push(n);
  }
  return out;
}

/** 空白折叠成一个空格，跨元素边界也只留一个（前一个叶子以空白结尾时，后一个叶子去掉开头的空白） */
function collapseSpaces(list: Inline[], st: { prevSpace: boolean }) {
  for (let i = 0; i < list.length; i++) {
    const n = list[i];
    if (n.t === 'text') {
      let v = n.v.replace(WS, ' ');
      if (st.prevSpace) v = v.replace(/^ /, '');
      if (v) st.prevSpace = v.endsWith(' ');
      list[i] = { t: 'text', v };
    } else if (n.t === 'br') st.prevSpace = true;
    else if (hasChildren(n)) collapseSpaces(n.c, st);
    else st.prevSpace = false;
  }
}

/** 去掉块首尾与换行两侧的空白（行首缩进、行尾硬换行的空格都不显示） */
function trimEdges(list: Inline[]) {
  const trimSide = (seq: Inline[], end: boolean): boolean => {
    for (let k = 0; k < seq.length; k++) {
      const i = end ? seq.length - 1 - k : k;
      const n = seq[i];
      if (n.t === 'text') {
        const v = end ? n.v.replace(/ +$/, '') : n.v.replace(/^ +/, '');
        seq[i] = { t: 'text', v };
        if (v) return true;
      } else if (hasChildren(n)) {
        if (trimSide(n.c, end)) return true;
      } else return true;
    }
    return false;
  };
  // 按换行切段，每段两头去空白
  let start = 0;
  for (let i = 0; i <= list.length; i++) {
    if (i === list.length || list[i].t === 'br') {
      const seg = list.slice(start, i);
      trimSide(seg, false);
      trimSide(seg, true);
      list.splice(start, seg.length, ...seg);
      start = i + 1;
    }
  }
}

function prune(list: Inline[]): Inline[] {
  const out: Inline[] = [];
  for (const n of list) {
    if (n.t === 'text' && !n.v) continue;
    if (hasChildren(n)) {
      const c = prune(n.c);
      if (!c.length) continue; // 没有内容的元素（空链接等）看不见
      out.push({ ...n, c } as Inline);
    } else out.push(n);
  }
  // 行首、行尾的换行（期望侧的 `<br />\n` 已在抽取时合并）不可见
  while (out.length && out[0].t === 'br') out.shift();
  while (out.length && out[out.length - 1].t === 'br') out.pop();
  return out;
}

export function normalizeBlocks(list: Block[]): Block[] {
  const out: Block[] = [];
  for (const b of list) {
    const n = normalizeBlock(b);
    if (n) out.push(n);
  }
  return out;
}

function normalizeBlock(b: Block): Block | null {
  switch (b.t) {
    case 'heading': return { ...b, c: normalizeInlines(b.c) };
    case 'paragraph': {
      const c = normalizeInlines(b.c);
      return c.length ? { t: 'paragraph', c } : null;
    }
    case 'code': return { ...b, lang: b.lang.trim().split(/\s+/)[0] ?? '', v: b.v.replace(/\n$/, '') };
    case 'list': return { ...b, c: b.c.map(i => ({ ...i, c: normalizeBlocks(i.c) })) };
    case 'quote': return { t: 'quote', c: normalizeBlocks(b.c) };
    case 'table': return { ...b, rows: b.rows.map(r => r.map(normalizeInlines)) };
    case 'el': {
      // 没有内容的块级元素（`<div id="foo">` 这种）渲染出来什么都没有，编辑器显示「空 HTML」占位，两侧都不计
      const c = b.c.every(isInline) ? normalizeInlines(b.c as Inline[]) : normalizeBlocks(wrapFlow(b.c));
      return c.length ? { ...b, c } : null;
    }
    default: return b;
  }
}

// ── 比对与显示 ─────────────────────────────────────────────────────────

export interface Difference {
  /** 从根到差异处的路径：`序号.类型` 用 `/` 连接 */
  path: string;
  expected: string;
  actual: string;
}

const kidsOf = (n: Node): Node[] | null => {
  switch (n.t) {
    case 'heading': case 'paragraph': case 'quote': case 'item': case 'em': case 'strong': case 'del': case 'mark': case 'link': case 'el': case 'list':
      return n.c;
    default: return null;
  }
};

/** 节点自身（不含子节点）的属性签名 */
function shell(n: Node): string {
  switch (n.t) {
    case 'heading': return `h${n.level}`;
    case 'paragraph': return 'p';
    case 'code': return `code(${n.lang})${JSON.stringify(n.v)}`;
    case 'list': return n.ordered ? `ol(${n.start})` : 'ul';
    case 'item': return n.task === null ? 'li' : `li[${n.task ? 'x' : ' '}]`;
    case 'quote': return 'quote';
    case 'hr': return 'hr';
    case 'table': return `table(${n.align.map(a => a ?? '-').join(',')})`;
    case 'ext': return `${n.kind}${JSON.stringify(n.v)}`;
    case 'el': return `<${n.tag}>`;
    case 'text': return JSON.stringify(n.v);
    case 'icode': return `icode${JSON.stringify(n.v)}`;
    case 'link': return `link(${n.href ?? '?'})`;
    case 'image': return `img(${n.src}|${n.alt})`;
    case 'br': return 'br';
    case 'math': return `math${JSON.stringify(n.v)}`;
    case 'fnref': return `fnref(${n.label})`;
    case 'em': case 'strong': case 'del': case 'mark': return n.t;
  }
}

/** 紧凑的单行显示，超长截断 */
export function show(n: Node | Node[] | undefined, max = 160): string {
  const s = n === undefined ? '∅' : Array.isArray(n) ? `[${n.map(x => show(x, Infinity)).join(' ')}]` : showOne(n);
  return s.length > max ? s.slice(0, max - 1) + '…' : s;
}

function showOne(n: Node): string {
  if (n.t === 'table') return `${shell(n)}{${n.rows.map(r => r.map(c => show(c, Infinity)).join(' | ')).join(' / ')}}`;
  const kids = kidsOf(n);
  return kids ? `${shell(n)}${show(kids, Infinity)}` : shell(n);
}

/** 第一处差异；相同时返回 null */
export function firstDifference(expected: Block[], actual: Block[]): Difference | null {
  return diffList(expected, actual, '');
}

function diffList(a: readonly Node[], b: readonly Node[], path: string): Difference | null {
  for (let i = 0; i < Math.max(a.length, b.length); i++) {
    const x = a[i], y = b[i];
    const p = `${path}/${i}.${(x ?? y).t}`;
    if (!x || !y) return { path: p, expected: show(x), actual: show(y) };
    const d = diffNode(x, y, p);
    if (d) return d;
  }
  return null;
}

function diffNode(x: Node, y: Node, path: string): Difference | null {
  if (shell(x) !== shell(y)) return { path, expected: show(x), actual: show(y) };
  if (x.t === 'table' && y.t === 'table') {
    for (let r = 0; r < Math.max(x.rows.length, y.rows.length); r++) {
      for (let c = 0; c < Math.max(x.rows[r]?.length ?? 0, y.rows[r]?.length ?? 0); c++) {
        const ec = x.rows[r]?.[c], ac = y.rows[r]?.[c];
        const p = `${path}/r${r}c${c}`;
        if (!ec || !ac) return { path: p, expected: ec ? show(ec) : '∅', actual: ac ? show(ac) : '∅' };
        const d = diffList(ec, ac, p);
        if (d) return d;
      }
    }
    return null;
  }
  const kx = kidsOf(x), ky = kidsOf(y);
  return kx && ky ? diffList(kx, ky, path) : null;
}
