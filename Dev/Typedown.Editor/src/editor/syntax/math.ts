import type { BlockContext, InlineContext, Line, MarkdownConfig, Element } from '@lezer/markdown';
import { tags } from '@lezer/highlight';

const DOLLAR = 36;
const BACKSLASH = 92;

/**
 * 行内公式 `$…$` 与 `$$…$$`（同一行内闭合）。规则取 Typora / pandoc 的常用约定：
 * 单个 `$` 的内容不能以空白开头或结尾（避免把 “$5 and $6” 识别成公式），闭合符不能被反斜杠转义，
 * 单 `$` 的闭合符后面不能紧跟数字。
 */
export function parseInlineMath(cx: InlineContext, next: number, pos: number): number {
  if (next !== DOLLAR) return -1;
  if (pos > cx.offset && cx.char(pos - 1) === BACKSLASH) return -1;
  const n = cx.char(pos + 1) === DOLLAR ? 2 : 1;
  if (n === 1 && cx.char(pos + 1) === DOLLAR) return -1;
  const contentStart = pos + n;
  if (n === 1) {
    const first = cx.char(contentStart);
    if (first === -1 || first === 32 || first === 9 || first === DOLLAR) return -1;
  }
  for (let i = contentStart; i < cx.end; i++) {
    const ch = cx.char(i);
    if (ch === BACKSLASH) { i++; continue; }
    if (ch !== DOLLAR) continue;
    if (n === 2) {
      if (cx.char(i + 1) !== DOLLAR) continue;
      if (i === contentStart) return -1;
      return cx.addElement(cx.elt('InlineMath', pos, i + 2, [
        cx.elt('InlineMathMark', pos, pos + 2),
        cx.elt('InlineMathMark', i, i + 2),
      ]));
    }
    const prev = cx.char(i - 1);
    if (prev === 32 || prev === 9) return -1;
    const after = cx.char(i + 1);
    if (after >= 48 && after <= 57) return -1;
    return cx.addElement(cx.elt('InlineMath', pos, i + 1, [
      cx.elt('InlineMathMark', pos, pos + 1),
      cx.elt('InlineMathMark', i, i + 1),
    ]));
  }
  return -1;
}

const isMathFence = (line: Line) =>
  line.next === DOLLAR && line.text.charCodeAt(line.pos + 1) === DOLLAR && /^\s*$/.test(line.text.slice(line.pos + 2));

/**
 * 块级公式：独占一行的 `$$` 开始，到下一个独占一行的 `$$` 结束（没有闭合时延续到容器结束，与围栏代码一致）。
 * 结构照 @lezer/markdown 的 FencedCode：容器标记（如引用的 `>`）按行收进子节点。
 */
export function parseBlockMath(cx: BlockContext, line: Line): boolean {
  if (line.indent - line.baseIndent >= 4 || !isMathFence(line)) return false;
  const from = cx.lineStart + line.pos;
  const children: Element[] = [cx.elt('BlockMathMark', from, from + 2)];
  let closed = false;
  // line.depth（本行匹配上的容器层数）是内部字段；与内置 FencedCode 的循环条件相同
  while (cx.nextLine() && (line as Line & { depth: number }).depth >= cx.depth) {
    for (const m of line.markers) children.push(m);
    if (line.indent - line.baseIndent < 4 && isMathFence(line)) {
      const at = cx.lineStart + line.pos;
      children.push(cx.elt('BlockMathMark', at, at + 2));
      cx.nextLine();
      closed = true;
      break;
    }
    // 与 CodeText 一样按行给内容节点，容器标记（引用的 `>`）夹在行与行之间，子节点保持有序不重叠
    const textFrom = cx.lineStart + line.basePos, textTo = cx.lineStart + line.text.length;
    if (textTo > textFrom) children.push(cx.elt('BlockMathContent', textFrom, textTo));
  }
  const end = closed ? children[children.length - 1].to : cx.prevLineEnd();
  cx.addElement(cx.elt('BlockMath', from, Math.max(end, from + 2), children));
  return true;
}

export const MathExtension: MarkdownConfig = {
  defineNodes: [
    { name: 'InlineMath', style: tags.special(tags.content) },
    { name: 'InlineMathMark', style: tags.processingInstruction },
    { name: 'BlockMath', block: true },
    { name: 'BlockMathMark', style: tags.processingInstruction },
    { name: 'BlockMathContent', style: tags.special(tags.content) },
  ],
  parseInline: [{ name: 'InlineMath', parse: parseInlineMath, before: 'Escape' }],
  parseBlock: [{ name: 'BlockMath', parse: parseBlockMath, before: 'FencedCode' }],
};
