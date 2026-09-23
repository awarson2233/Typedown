import type { Line, Text } from '@codemirror/state';
import type { Tree } from '@lezer/common';

/**
 * 规范位置（wysiwyg-engine-survey.md 第 6.3 节）：隐藏标记两侧视觉上重合的两个源偏移里，
 * 光标与输入实际落在哪一个。全部是纯函数，由 transactionFilter / inputHandler 调用。
 *
 * - 行首常隐藏前缀（列表符号、任务框、引用 `>` 及其前面的缩进）：光标不停在前缀内，一律落到前缀末尾；
 *   从前缀末尾向左移动时越过前缀到上一行行尾。
 * - ATX 标题：从别的行移入标题行行首时，光标落在标题文字前而不是 `#` 前（Typora 的行为）。
 * - 行内强调：在强调内容边缘键入空白时落到标记外，避免产生 CommonMark 不认的 `**文字 **`。
 * - 行内 span 的显形规则取「端点落在 [from, to] 含两端就显形」，所以光标从外侧移到边界时
 *   标记已经变灰可见、光标显示在标记外侧，输入落在外侧——「从外侧移入保持外侧」由显形规则直接保证。
 */

const PREFIX_MARKS = new Set(['QuoteMark', 'ListMark', 'TaskMarker']);
const isSpace = (c: number) => c === 32 || c === 9;

/** 行首常隐藏前缀的末尾；该行没有这类前缀时返回 line.from。与 inlineSpecs 的隐藏范围一致（标记后吃一个空格）。 */
export function hiddenPrefixEnd(tree: Tree, line: Line): number {
  const marks: { from: number; to: number }[] = [];
  tree.iterate({
    from: line.from,
    to: Math.min(line.to, line.from + 200),
    enter(n) {
      if (PREFIX_MARKS.has(n.name) && n.from >= line.from && n.to <= line.to) marks.push({ from: n.from, to: n.to });
    },
  });
  if (!marks.length) return line.from;
  marks.sort((a, b) => a.from - b.from);
  const text = line.text;
  let pos = 0, found = false;
  for (const m of marks) {
    let p = pos;
    while (p < text.length && isSpace(text.charCodeAt(p))) p++;
    if (m.from - line.from !== p) break;
    pos = m.to - line.from;
    if (pos < text.length && isSpace(text.charCodeAt(pos))) pos++;
    found = true;
  }
  return found ? line.from + pos : line.from;
}

/** ATX 标题行的文字起点（`#` 与其后空白之后）；不是 ATX 标题行时返回 -1。 */
export function headingTextStart(tree: Tree, line: Line): number {
  let start = -1;
  tree.iterate({
    from: line.from,
    to: line.from,
    enter(n) {
      if (start >= 0) return false;
      if (/^ATXHeading\d$/.test(n.name) && n.from >= line.from) {
        const mark = n.node.getChild('HeaderMark');
        if (mark && mark.from === n.from) {
          let p = mark.to;
          while (p < line.to && isSpace(line.text.charCodeAt(p - line.from))) p++;
          start = p;
        }
        return false;
      }
      return true;
    },
  });
  return start;
}

/**
 * 选区头的规范化。`prevHead` 是事务前的光标头，`head` 是事务给出的新光标头。
 * 只处理「只动选区、不改文本」的事务。
 */
export function canonicalHead(doc: Text, tree: Tree, prevHead: number, head: number): number {
  const line = doc.lineAt(head);
  const prefixEnd = hiddenPrefixEnd(tree, line);
  if (prefixEnd > line.from && head < prefixEnd) {
    // 从前缀末尾往左：越过隐藏前缀到上一行行尾，否则会卡在原地
    if (prevHead === prefixEnd && head < prevHead && line.from > 0) return line.from - 1;
    return prefixEnd;
  }
  if (head === line.from && (prevHead < line.from || prevHead > line.to)) {
    const textStart = headingTextStart(tree, line);
    if (textStart > line.from) return textStart;
  }
  return head;
}

const EMPHASIS_SPANS = new Set(['Emphasis', 'StrongEmphasis', 'Strikethrough', 'Highlight']);

/**
 * 在强调内容边缘插入空白时的实际插入位置：紧贴闭合标记内侧 → 挪到闭合标记外；
 * 紧贴开标记内侧 → 挪到开标记外。嵌套时逐层向外挪。
 */
export function whitespaceInsertPos(tree: Tree, pos: number): number {
  for (let guard = 0; guard < 8; guard++) {
    const after = tree.resolveInner(pos, 1);
    const parentA = after.parent;
    if (after.from === pos && parentA && EMPHASIS_SPANS.has(parentA.name) && parentA.lastChild && parentA.lastChild.from === after.from && after.name.endsWith('Mark') && after.from > parentA.from) {
      pos = after.to;
      continue;
    }
    const before = tree.resolveInner(pos, -1);
    const parentB = before.parent;
    if (before.to === pos && parentB && EMPHASIS_SPANS.has(parentB.name) && parentB.firstChild && parentB.firstChild.to === before.to && before.name.endsWith('Mark') && before.to < parentB.to) {
      pos = before.from;
      continue;
    }
    break;
  }
  return pos;
}
