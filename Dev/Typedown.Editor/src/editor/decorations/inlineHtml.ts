import type { Text } from '@codemirror/state';
import type { SyntaxNode } from '@lezer/common';

/**
 * 行内 HTML 的配对。Lezer 只把每个标签各自解析成一个 HTMLTag 节点，不成对；
 * 这里在同一个父节点的子节点里按栈配对开闭标签，供显形（inlineSpecs）与选区标记（selectionInfo 的下划线）共用。纯函数。
 */

export interface HtmlTag {
  from: number;
  to: number;
  /** 小写标签名 */
  name: string;
  kind: 'open' | 'close' | 'void';
  /** 标签名之后、`>` 之前的属性原文（不含自闭合的 `/`） */
  attrs: string;
}

export interface HtmlPair {
  open: HtmlTag;
  close: HtmlTag;
}

/** 渲染成真实元素的行内标签（短语内容元素）；其余标签只显示源码。消毒另由 DOMPurify 做（inlinePlugin）。 */
export const RENDERED_TAGS = new Set([
  'a', 'abbr', 'b', 'bdi', 'bdo', 'big', 'cite', 'code', 'del', 'dfn', 'em', 'font', 'i', 'ins', 'kbd', 'mark',
  'q', 's', 'samp', 'small', 'span', 'strike', 'strong', 'sub', 'sup', 'tt', 'u', 'var',
]);

const VOID_TAGS = new Set(['area', 'base', 'br', 'col', 'embed', 'hr', 'img', 'input', 'link', 'meta', 'source', 'track', 'wbr']);
const TAG_RE = /^<(\/?)([a-zA-Z][a-zA-Z0-9-]*)([\s\S]*?)(\/?)>$/;

export function parseHtmlTag(src: string, from: number): HtmlTag | null {
  const m = TAG_RE.exec(src);
  if (!m) return null;
  const name = m[2].toLowerCase();
  const kind = m[1] ? 'close' : m[4] || VOID_TAGS.has(name) ? 'void' : 'open';
  return { from, to: from + src.length, name, kind, attrs: m[3] };
}

/** 父节点下直接子节点里的行内标签：成对的与落单的（落单的包括空元素、找不到另一半的开闭标签）。 */
export function pairHtmlTags(doc: Text, parent: SyntaxNode): { pairs: HtmlPair[]; single: HtmlTag[] } {
  const pairs: HtmlPair[] = [];
  const single: HtmlTag[] = [];
  const stack: HtmlTag[] = [];
  for (let c = parent.firstChild; c; c = c.nextSibling) {
    if (c.name !== 'HTMLTag') continue;
    const tag = parseHtmlTag(doc.sliceString(c.from, c.to), c.from);
    if (!tag) continue;
    if (tag.kind === 'open') { stack.push(tag); continue; }
    if (tag.kind === 'void') { single.push(tag); continue; }
    let i = stack.length - 1;
    while (i >= 0 && stack[i].name !== tag.name) i--;
    if (i < 0) { single.push(tag); continue; }
    // 中间没闭合的开标签落单
    single.push(...stack.splice(i + 1));
    pairs.push({ open: stack.pop()!, close: tag });
  }
  single.push(...stack);
  pairs.sort((a, b) => a.open.from - b.open.from);
  single.sort((a, b) => a.from - b.from);
  return { pairs, single };
}

/** 从属性原文里取一个属性值（img 的 src、alt、title 用）；没有该属性时返回 null。 */
export function htmlAttr(attrs: string, name: string): string | null {
  const re = new RegExp(`(?:^|\\s)${name}\\s*=\\s*(?:"([^"]*)"|'([^']*)'|([^\\s"'=<>\`]+))`, 'i');
  const m = re.exec(attrs);
  if (!m) return null;
  return m[1] ?? m[2] ?? m[3] ?? '';
}
