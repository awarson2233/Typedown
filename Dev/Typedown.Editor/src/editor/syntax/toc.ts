import type { BlockContext, Line, MarkdownConfig } from '@lezer/markdown';
import { tags } from '@lezer/highlight';

/**
 * `[TOC]`：顶层独占一行（大小写不限，允许行尾空白）时是目录块 TableOfContents，子节点 TableOfContentsMark 覆盖 `[TOC]`。
 * 与 Muya 的判定相同（lexer.js 的 `/^\[toc\]\n?$/i`）；不打断段落，段落中间的 `[TOC]` 仍是正文。
 */
const TOC = /^\[toc\][ \t]*$/i;

export function parseToc(cx: BlockContext, line: Line): boolean {
  if (cx.depth !== 1 || line.indent - line.baseIndent >= 4 || !TOC.test(line.text.slice(line.pos))) return false;
  const from = cx.lineStart + line.pos;
  cx.addElement(cx.elt('TableOfContents', from, from + 5, [cx.elt('TableOfContentsMark', from, from + 5)]));
  cx.nextLine();
  return true;
}

export const TocExtension: MarkdownConfig = {
  defineNodes: [
    { name: 'TableOfContents', block: true },
    { name: 'TableOfContentsMark', style: tags.processingInstruction },
  ],
  parseBlock: [{ name: 'TableOfContents', parse: parseToc, before: 'HTMLBlock' }],
};
