import { parser as commonmarkParser } from '@lezer/markdown';
import type { InlineContext, MarkdownConfig } from '@lezer/markdown';

/**
 * CJK 强调边界放宽：CommonMark 的左右侧定界规则把中文标点当作标点，于是 `**中文，**后面`、
 * `前面**「引用」**后面` 这类写法不成粗体，而 Typora 用户习惯它成立。
 *
 * 做法：仍由 @lezer/markdown 内置的 Emphasis 解析器产出定界符（它的定界符类型没有导出，
 * 强/弱强调的配对逻辑依赖这两个类型对象），然后在定界符两侧任一字符是 CJK 时，
 * 把 CJK 标点当作普通字母重新计算一次可开/可闭，与内置结果取并集。只影响渲染，不改文件。
 */

const CJK = /[⺀-⿿、-〿぀-ヿ㄀-ㄯ㆐-ㇿ㐀-䶿一-鿿ꥠ-꥿가-힯豈-﫿︰-﹏＀-￯‘’“”…—·]/;
const Punctuation = /[\p{P}\p{S}]/u;

export const isCjk = (ch: string) => ch.length > 0 && CJK.test(ch);

export interface Flanking { canOpen: boolean; canClose: boolean }

/** CommonMark 的定界符规则；`cjkAsLetter` 为真时 CJK 标点按字母处理。 */
export function flanking(before: string, after: string, marker: '*' | '_', cjkAsLetter: boolean): Flanking {
  const punct = (c: string) => Punctuation.test(c) && !(cjkAsLetter && isCjk(c));
  const sBefore = /\s|^$/.test(before), sAfter = /\s|^$/.test(after);
  const pBefore = punct(before), pAfter = punct(after);
  const left = !sAfter && (!pAfter || sBefore || pBefore);
  const right = !sBefore && (!pBefore || sAfter || pAfter);
  return {
    canOpen: left && (marker === '*' || !right || pBefore),
    canClose: right && (marker === '*' || !left || pAfter),
  };
}

/** 两侧有 CJK 字符时放宽后的结果（总是包含标准结果）。 */
export function cjkFlanking(before: string, after: string, marker: '*' | '_'): Flanking {
  const std = flanking(before, after, marker, false);
  if (!isCjk(before) && !isCjk(after)) return std;
  const relaxed = flanking(before, after, marker, true);
  return { canOpen: std.canOpen || relaxed.canOpen, canClose: std.canClose || relaxed.canClose };
}

type InlineParseFn = (cx: InlineContext, next: number, pos: number) => number;
interface ParserInternals { inlineParsers: (InlineParseFn | undefined)[]; inlineNames: string[] }
interface DelimiterInternals { from: number; to: number; side: number }

const internals = commonmarkParser as unknown as ParserInternals;
const builtinEmphasis = internals.inlineParsers[internals.inlineNames.indexOf('Emphasis')]!;

const MARK_OPEN = 1, MARK_CLOSE = 2;

function parseCjkEmphasis(cx: InlineContext, next: number, start: number): number {
  const end = builtinEmphasis(cx, next, start);
  if (end < 0) return end;
  const parts = (cx as unknown as { parts: unknown[] }).parts;
  const d = parts[parts.length - 1] as DelimiterInternals | null;
  if (!d || d.from !== start || typeof d.side !== 'number') return end;
  const before = cx.slice(start - 1, start), after = cx.slice(d.to, d.to + 1);
  if (!isCjk(before) && !isCjk(after)) return end;
  const f = cjkFlanking(before, after, next === 42 ? '*' : '_');
  d.side |= (f.canOpen ? MARK_OPEN : 0) | (f.canClose ? MARK_CLOSE : 0);
  return end;
}

export const CjkEmphasisExtension: MarkdownConfig = {
  // 同名替换内置的 Emphasis 解析器（MarkdownParser.configure 对同名 parseInline 就地替换）
  parseInline: [{ name: 'Emphasis', parse: parseCjkEmphasis }],
};
