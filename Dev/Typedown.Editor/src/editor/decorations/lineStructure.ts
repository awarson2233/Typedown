import type { Line, Text } from '@codemirror/state';
import type { SyntaxNode, Tree } from '@lezer/common';

/**
 * 块结构（列表、任务、引用）在一行上的形态：行首常隐藏前缀的范围、要画的列表符号、缩进层数与引用竖线。
 * 行内显形（inlineSpecs）与选区规范化（canonical）共用这一个纯函数，两边对「前缀到哪为止」的判断才一致。
 *
 * 对照旧编辑器的 DOM：每层引用与每层列表项在 Muya 里各是 30px 的左内边距（`blockquote { padding: 0 30px }`、
 * `ul, ol { padding-left: 30px }`），列表符号画在内容左侧（list-style-position: outside），引用竖线画在该层的 15px 处。
 * 这里把这些翻成按行的缩进层数与竖线所在的层，由行装饰的内联样式施加（typography.css）。
 */

/**
 * 列表符号。shift：符号所属列表项之内、本行还有几层容器（通常为 0）；
 * task 的 offset：`[` 相对前缀起点（行首）的偏移，任务框点击时据此找到要改的那个字符。
 */
export type MarkerSpec =
  | { type: 'bullet'; depth: number; shift: number }
  | { type: 'ordered'; number: number; shift: number }
  | { type: 'task'; checked: boolean; offset: number; shift: number };

export interface LinePrefix {
  /** 行首常隐藏前缀的终点；没有前缀时为 line.from */
  end: number;
  /** 前缀换成的列表符号；null 表示前缀整段隐藏（引用 `>`、续行缩进、外层列表的符号） */
  marker: MarkerSpec | null;
  /** 行所在的容器层数：引用与列表项各算一层 */
  indent: number;
  /** 引用竖线所在的层（从 0 起），按由外到内的顺序 */
  quoteLevels: number[];
  /** 行属于某个已勾选的任务项（旧编辑器把整个任务项的内容变淡） */
  taskDone: boolean;
}

const PREFIX_MARKS = new Set(['QuoteMark', 'ListMark', 'TaskMarker']);
const LIST_TYPES = new Set(['BulletList', 'OrderedList']);
/** 前缀之后是这些块时，续行缩进只隐藏到列表内容列，更深的缩进属于内容本身 */
const CODE_LIKE = new Set(['FencedCode', 'CodeBlock']);
/** 前缀只可能出现在行首附近，扫描语法树时不必走完整行 */
const SCAN_LIMIT = 200;

const isSpace = (c: number) => c === 32 || c === 9;

/** 行内偏移 i 处的列号（制表符按 4 列的制表位展开） */
function columnAt(text: string, i: number): number {
  let col = 0;
  for (let k = 0; k < i && k < text.length; k++) col = text.charCodeAt(k) === 9 ? col + 4 - (col % 4) : col + 1;
  return col;
}

/** 列表项内容的起始列（CommonMark：符号后 1–4 个空格算进前缀，5 个及以上或空项只算 1 个） */
function contentColumn(doc: Text, item: SyntaxNode): number {
  const mark = item.getChild('ListMark');
  if (!mark) return 0;
  const line = doc.lineAt(mark.from);
  const text = line.text;
  const end = mark.to - line.from;
  let n = 0;
  while (end + n < text.length && isSpace(text.charCodeAt(end + n))) n++;
  const spaces = n === 0 || n > 4 || end + n >= text.length ? 1 : n;
  return columnAt(text, end) + spaces;
}

function listDepth(item: SyntaxNode): number {
  let depth = 0;
  for (let p: SyntaxNode | null = item.parent; p; p = p.parent) if (LIST_TYPES.has(p.name)) depth++;
  return depth;
}

/** 有序列表项的显示序号：首项的数字加上本项在列表里的序位（与 `<ol start>` 相同，不看本项自己写的数字） */
function orderedNumber(doc: Text, item: SyntaxNode): number {
  let index = 0, first: SyntaxNode = item;
  for (let s = item.prevSibling; s; s = s.prevSibling) if (s.name === 'ListItem') { index++; first = s; }
  const mark = first.getChild('ListMark');
  const start = mark ? parseInt(doc.sliceString(mark.from, mark.to), 10) : 1;
  return (Number.isFinite(start) ? start : 1) + index;
}

function taskChecked(doc: Text, item: SyntaxNode): boolean | null {
  const marker = item.getChild('Task')?.getChild('TaskMarker');
  if (!marker) return null;
  return /x/i.test(doc.sliceString(marker.from, marker.to));
}

/** 行的块结构；行不在任何引用或列表项里时返回 null。 */
export function linePrefix(tree: Tree, doc: Text, line: Line): LinePrefix | null {
  const containers: SyntaxNode[] = [];
  const marks: SyntaxNode[] = [];
  let codeLike = false;
  tree.iterate({
    from: line.from,
    to: Math.min(line.to, line.from + SCAN_LIMIT),
    enter(n) {
      // 只结束在本行行首的节点属于前面的行
      if (n.to === line.from && n.from < line.from) return false;
      if (PREFIX_MARKS.has(n.name)) {
        if (n.from >= line.from && n.to <= line.to) marks.push(n.node);
        return false;
      }
      if (n.name === 'Blockquote' || n.name === 'ListItem') containers.push(n.node);
      else if (CODE_LIKE.has(n.name)) codeLike = true;
      return true;
    },
  });
  if (!containers.length && !marks.length) return null;

  const text = line.text;
  // 依次吃掉「空白 + 标记 + 一个空格」，直到遇到别的字符
  let pos = 0;
  let lastList: SyntaxNode | null = null, lastTask: SyntaxNode | null = null;
  marks.sort((a, b) => a.from - b.from);
  for (const m of marks) {
    let p = pos;
    while (p < text.length && isSpace(text.charCodeAt(p))) p++;
    if (m.from - line.from !== p) break;
    pos = m.to - line.from;
    if (pos < text.length && isSpace(text.charCodeAt(pos))) pos++;
    if (m.name === 'ListMark') { lastList = m; lastTask = null; }
    else if (m.name === 'TaskMarker') lastTask = m;
  }

  // 列表项的续行（本行没有最内层列表项的符号）：隐藏到该项的内容列为止；
  // 段落续行的更深缩进在渲染里本来就不出现，一并隐藏；代码块里更深的缩进是代码本身，保留
  const items = containers.filter(c => c.name === 'ListItem');
  const inner = items[items.length - 1];
  if (inner && !(lastList && lastList.parent && lastList.parent.from === inner.from)) {
    const col = contentColumn(doc, inner);
    while (pos < text.length && isSpace(text.charCodeAt(pos)) && (!codeLike || columnAt(text, pos) < col)) pos++;
  }

  let marker: MarkerSpec | null = null;
  const item = lastList?.parent;
  if (item && item.name === 'ListItem') {
    // 符号画在内容起点左侧；本行之后还有更内层的容器（`- > 引用`）时，往左挪回列表项自己那一层
    const shift = containers.length - 1 - containers.findIndex(c => c.from === item.from && c.name === 'ListItem');
    const checked = lastTask && lastTask.parent?.parent?.from === item.from ? taskChecked(doc, item) : null;
    if (checked !== null && lastTask) marker = { type: 'task', checked, offset: lastTask.from - line.from, shift };
    else if (item.parent?.name === 'OrderedList') marker = { type: 'ordered', number: orderedNumber(doc, item), shift };
    else marker = { type: 'bullet', depth: listDepth(item), shift };
  }

  const quoteLevels: number[] = [];
  containers.forEach((c, i) => { if (c.name === 'Blockquote') quoteLevels.push(i); });
  return {
    end: line.from + pos,
    marker,
    indent: containers.length,
    quoteLevels,
    taskDone: items.some(i => taskChecked(doc, i) === true),
  };
}
