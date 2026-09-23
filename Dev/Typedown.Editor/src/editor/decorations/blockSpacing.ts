import type { Line, Text } from '@codemirror/state';
import type { SyntaxNode, Tree } from '@lezer/common';

/**
 * 顶层块之间的纵向间距：按旧编辑器（Muya）各块的外边距与 CSS 外边距折叠规则，算出 CM6 里块间空行的高度
 * 与相邻块（中间没有空行）之间补的内边距。取值只写成 CSS 表达式，字号、行高由宿主设置的 CSS 变量决定（typography.css）。
 *
 * Muya 的外边距（F 为宿主设置的字号，块内 em 都按它算）：
 * - 段落、列表、引用、分隔线、[TOC]：.5em（F × .5），可折叠；
 * - 标题：1rem（恒为 16px，不随字号设置变），可折叠；
 * - figure（表格、公式块、mermaid、HTML 块）：1.4em（F × 1.4），可折叠；脚注区的字号是 .8em，外边距是 F × 1.12；
 * - front matter 的 pre：.5em × 90% 字号（F × .45），块级，可折叠；
 * - 代码块 pre.ag-fence-code：同样 F × .45，但它是 inline-flex，外边距不与任何相邻外边距折叠，而是相加。
 * 两块之间的间距 = 两者都可折叠时取大者，否则相加。
 *
 * 连续多个空行：Muya 的词法分析把 n 个连续换行（b = n − 1 个空行）变成 ⌊n/2⌋ − 1 个空段落（lexer.js「修复无法显示空行的问题」），
 * 每个空段落高一行（F × 行高），上下 .5em 外边距。CM6 里这 b 行空行平分这段总高度，每行都保留可点击的高度。
 */

export interface Margin {
  /** CSS 长度表达式 */
  readonly v: string;
  /** 能否与相邻外边距折叠 */
  readonly collapse: boolean;
}

export const MARGIN = {
  paragraph: { v: 'var(--td-m-p)', collapse: true },
  heading: { v: 'var(--td-m-h)', collapse: true },
  figure: { v: 'var(--td-m-fig)', collapse: true },
  footnote: { v: 'var(--td-m-fn)', collapse: true },
  frontmatter: { v: 'var(--td-m-code)', collapse: true },
  code: { v: 'var(--td-m-code)', collapse: false },
} as const satisfies Record<string, Margin>;

/** 文档边界：外边距 0，可折叠（Muya 的编辑区有内边距，首块的上外边距原样保留） */
const EDGE: Margin = { v: '0px', collapse: true };

/** 两个相邻外边距之间的实际间距 */
export function joinMargins(a: Margin, b: Margin): string {
  if (a === EDGE) return b.v;
  if (b === EDGE) return a.v;
  if (a.collapse && b.collapse) return a.v === b.v ? a.v : `max(${a.v}, ${b.v})`;
  return `calc(${a.v} + ${b.v})`;
}

const HEADING = /Heading\d$/;

/** 顶层块节点在 Muya 里的外边距 */
export function marginOf(doc: Text, node: SyntaxNode): Margin {
  const name = node.name;
  if (HEADING.test(name)) return MARGIN.heading;
  switch (name) {
    case 'FencedCode': {
      const info = node.getChild('CodeInfo');
      return info && /^mermaid$/i.test(doc.sliceString(info.from, info.to).trim()) ? MARGIN.figure : MARGIN.code;
    }
    case 'CodeBlock': return MARGIN.code;
    case 'Table': case 'BlockMath': case 'HTMLBlock': case 'CommentBlock': case 'ProcessingInstructionBlock': return MARGIN.figure;
    case 'FootnoteDefinition': return MARGIN.footnote;
    case 'Frontmatter': return MARGIN.frontmatter;
    default: return MARGIN.paragraph;
  }
}

/** 顶层块：Document（及 YAML front matter 包装层的 Body）的直接子节点 */
function topBlockAt(tree: Tree, pos: number, side: -1 | 1): SyntaxNode | null {
  let n: SyntaxNode | null = tree.resolveInner(pos, side);
  while (n && n.parent && !n.parent.type.isTop && n.parent.name !== 'Body') n = n.parent;
  return n && !n.type.isTop && n.name !== 'Body' ? n : null;
}

/** 块的最后一行：HTML 块的节点终点带着结尾换行（落在下一行行首），退回上一行 */
export function lastLineOf(doc: Text, node: SyntaxNode): Line {
  const end = node.to > node.from && doc.lineAt(node.to).from === node.to ? node.to - 1 : node.to;
  return doc.lineAt(end);
}

/**
 * 连续 lines 个空行夹在外边距 a、b 之间时每一行的高度表达式；段落间的普通空行（.5em）返回 null，用样式表的默认值。
 * a 为 null 表示空行从文首开始（Muya 先在正文前补两个换行再做词法分析）。
 */
function gapRunHeight(a: Margin | null, b: Margin, lines: number): string | null {
  const newlines = a ? lines + 1 : lines + 2;
  const top = a ?? EDGE;
  const empty = Math.max(0, Math.floor(newlines / 2) - 1);
  let total: string;
  if (!empty) total = joinMargins(top, b);
  else {
    const p = MARGIN.paragraph;
    total = `calc(${joinMargins(top, p)} + ${empty} * var(--td-lh-px) + ${empty - 1} * var(--td-m-p) + ${joinMargins(p, b)})`;
  }
  if (lines === 1 && total === MARGIN.paragraph.v) return null;
  return lines === 1 ? total : `calc((${total}) / ${lines})`;
}

/** 一段连续空行 [first, last] 前后的顶层块；空行落在某个顶层块内部（列表项之间、引用里）时返回 null */
function gapNeighbours(doc: Text, tree: Tree, first: number, last: number): { prev: SyntaxNode | null; next: SyntaxNode | null } | null {
  const start = doc.line(first), end = doc.line(last);
  const prev = start.from > 0 ? topBlockAt(tree, start.from - 1, -1) : null;
  const next = end.to < doc.length ? topBlockAt(tree, end.to + 1, 1) : null;
  if (prev && prev.to > start.from) return null;
  if (next && next.from < end.to) return null;
  if (prev && next && prev.from === next.from) return null;
  return { prev, next };
}

/**
 * 一段连续空行 [first, last]（行号）里每一行的高度表达式；段落间的普通空行（.5em）返回 null，用样式表的默认值。
 * 空行落在某个顶层块内部（列表项之间、引用里）时也返回 null：那里的间距就是段落的 .5em。
 */
export function gapLineHeight(doc: Text, tree: Tree, first: number, last: number): string | null {
  const n = gapNeighbours(doc, tree, first, last);
  if (!n) return null;
  return gapRunHeight(n.prev ? marginOf(doc, n.prev) : null, n.next ? marginOf(doc, n.next) : EDGE, last - first + 1);
}

/** 光标落在一段连续空行里时的排版：光标行按输入第一个字符后会成为的那一行排，其余空行按变成的新块间距重新分摊 */
export interface CaretGap {
  /** 光标行之上、之下两段空行每一行的高度表达式（null 用默认 .5em）；该段没有空行时不用 */
  above: string | null;
  below: string | null;
  /** 光标行（正文行高）的上、下内边距；null 表示不加 */
  paddingTop: string | null;
  paddingBottom: string | null;
}

/** 行尾所在的最内层块是段落：下一行输入文字会成为它的惰性续行（段落本身、以段落结尾的列表与引用） */
function endsInParagraph(tree: Tree, line: Line): boolean {
  if (line.length === 0) return false;
  for (let n: SyntaxNode | null = tree.resolveInner(line.to, -1); n; n = n.parent) {
    if (n.name === 'Paragraph') return n.to >= line.to;
    if (n.type.isTop) return false;
  }
  return false;
}

/** 不能打断段落的块：紧跟在新段落之后时成为它的续行（缩进代码块），或本来就是段落 */
const JOINS_PARAGRAPH = new Set(['Paragraph', 'CodeBlock']);

/**
 * 光标所在的块间空行（第 caret 行，位于连续空行 [first, last] 内）的排版。回车新起的一行在语法上是块间空行，
 * 若照常压成段距，光标会先画在矮行里，输入文字后才跳到正文行的位置；这里让光标行一开始就处在输入后的位置：
 * - 光标行紧接在段落（或以段落结尾的列表、引用）之后：输入后是该段落的续行，光标行就是一行正文，不加间距；
 * - 否则输入后是一个新段落 P：光标行上方的空行按「前一块 | P」重新分摊，没有空行时把两者的间距加成光标行的上内边距；
 *   下方同理按「P | 后一块」，后一块是段落时输入后会并进 P，不加间距。
 * 空行落在顶层块内部时返回 null（光标行按普通正文行排，其余空行不变）。
 */
export function caretGapLayout(doc: Text, tree: Tree, first: number, last: number, caret: number): CaretGap | null {
  const n = gapNeighbours(doc, tree, first, last);
  if (!n) return null;
  const a = n.prev ? marginOf(doc, n.prev) : null, b = n.next ? marginOf(doc, n.next) : EDGE;
  const p = MARGIN.paragraph;
  const continues = caret === first && !!n.prev && endsInParagraph(tree, doc.line(caret - 1));
  const own = continues ? a! : p;
  const out: CaretGap = { above: null, below: null, paddingTop: null, paddingBottom: null };
  if (caret > first) out.above = gapRunHeight(a, p, caret - first);
  else if (!continues) out.paddingTop = joinMargins(a ?? EDGE, p);
  if (caret < last) out.below = gapRunHeight(own, b, last - caret);
  else if (n.next && !JOINS_PARAGRAPH.has(n.next.name)) out.paddingBottom = joinMargins(own, b);
  return out;
}

/** 块首行就是文字行、可以直接加内边距的块（其余是整块 widget 或带竖线、边框的行） */
const PLAIN = /^(Paragraph|HorizontalRule|LinkReference|TableOfContents|BulletList|OrderedList|(ATX|Setext)Heading\d)$/;

export interface AdjacentPadding {
  /** 加内边距的行起点 */
  at: number;
  /** 'top' 加在后一块首行，'bottom' 加在前一块末行 */
  side: 'top' | 'bottom';
  value: string;
}

/**
 * 两个顶层块之间没有空行（例如标题下直接接正文、段落下直接接列表或代码块）时，把 Muya 的外边距间距加成文字行的内边距。
 * 优先加在前一块的末行（段落、标题、分隔线），否则加在后一块的首行；两边都是整块 widget 时没有可加的行，返回 null。
 * 文档首块（prev 为 null）的上外边距加在首行上（front matter 由它自己的围栏条处理）。
 */
export function adjacentPadding(doc: Text, prev: SyntaxNode | null, next: SyntaxNode): AdjacentPadding | null {
  const gap = joinMargins(prev ? marginOf(doc, prev) : EDGE, marginOf(doc, next));
  if (prev && PLAIN.test(prev.name) && !/List$/.test(prev.name)) return { at: lastLineOf(doc, prev).from, side: 'bottom', value: gap };
  if (PLAIN.test(next.name)) return { at: doc.lineAt(next.from).from, side: 'top', value: gap };
  return null;
}
