import type { InlineContext, MarkdownConfig } from '@lezer/markdown';
import { tags } from '@lezer/highlight';

const EQ = 61;
const HighlightDelim = { resolve: 'Highlight', mark: 'HighlightMark' };
const Punctuation = /[\p{P}\p{S}]/u;

/** `==高亮==`：写法与 @lezer/markdown 的 GFM Strikethrough 相同，只是定界符换成两个等号。 */
export function parseHighlight(cx: InlineContext, next: number, pos: number): number {
  if (next !== EQ || cx.char(pos + 1) !== EQ || cx.char(pos + 2) === EQ) return -1;
  const before = cx.slice(pos - 1, pos), after = cx.slice(pos + 2, pos + 3);
  const sBefore = /\s|^$/.test(before), sAfter = /\s|^$/.test(after);
  const pBefore = Punctuation.test(before), pAfter = Punctuation.test(after);
  return cx.addDelimiter(HighlightDelim, pos, pos + 2,
    !sAfter && (!pAfter || sBefore || pBefore),
    !sBefore && (!pBefore || sAfter || pAfter));
}

export const HighlightExtension: MarkdownConfig = {
  defineNodes: [
    { name: 'Highlight', style: tags.special(tags.emphasis) },
    { name: 'HighlightMark', style: tags.processingInstruction },
  ],
  parseInline: [{ name: 'Highlight', parse: parseHighlight, after: 'Emphasis' }],
};
