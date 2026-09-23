import type { Line, Text } from '@codemirror/state';
import type { Tree } from '@lezer/common';
import { linePrefix } from './lineStructure';
import { linkAt } from './links';

/**
 * 规范位置（wysiwyg-engine-survey.md 第 6.3 节）：隐藏标记两侧视觉上重合的两个源偏移里，
 * 光标与输入实际落在哪一个。全部是纯函数，由 transactionFilter / inputHandler 调用。
 *
 * - 行首常隐藏前缀（列表符号、任务框、引用 `>`、前面的缩进与列表项续行的缩进）：光标不停在前缀内，
 *   一律落到前缀末尾（方向键、Home、单击符号都停在符号之后）；从前缀末尾用方向键向左时越过前缀到上一行行尾。
 * - ATX 标题：从别的行移入标题行行首时，光标落在标题文字前而不是 `#` 前（Typora 的行为）。
 * - 链接：从右侧用方向键移入时，光标落在链接文字末尾而不是 `)` 之后（不照抄 Typora 的这个毛病）。
 * - 行内强调：在强调内容边缘键入空白时落到标记外，避免产生 CommonMark 不认的 `**文字 **`。
 * - 行内 span 的显形规则取「端点落在 [from, to] 含两端就显形」，所以光标从外侧移到边界时
 *   标记已经变灰可见、光标显示在标记外侧，输入落在外侧——「从外侧移入保持外侧」由显形规则直接保证。
 */

/** 选区变化的来源：只有方向键这类逐字移动才适用「越过前缀到上一行」与「链接从右侧移入」 */
export type SelectionOrigin = 'keyboard' | 'pointer' | 'lineBoundary';

const isSpace = (c: number) => c === 32 || c === 9;

/** 行首常隐藏前缀的末尾；该行没有这类前缀时返回 line.from。与 inlineSpecs 的隐藏范围一致（同一个 linePrefix）。 */
export function hiddenPrefixEnd(tree: Tree, doc: Text, line: Line): number {
  return linePrefix(tree, doc, line)?.end ?? line.from;
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
export function canonicalHead(doc: Text, tree: Tree, prevHead: number, head: number, origin: SelectionOrigin = 'keyboard', empty = true): number {
  const line = doc.lineAt(head);
  const prefixEnd = hiddenPrefixEnd(tree, doc, line);
  if (prefixEnd > line.from && head < prefixEnd) {
    // 方向键从前缀末尾往左：越过隐藏前缀到上一行行尾，否则会卡在原地（Home、单击符号则停在前缀末尾）
    if (origin === 'keyboard' && prevHead === prefixEnd && head < prevHead && line.from > 0) return line.from - 1;
    return prefixEnd;
  }
  if (origin === 'keyboard' && empty && prevHead > head) {
    const link = linkAt(tree, doc, head, -1);
    if (link && link.to === head && link.textEnd !== null && prevHead > link.to) return link.textEnd;
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
