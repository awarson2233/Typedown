import type { Text } from '@codemirror/state';
import type { SyntaxNode, Tree } from '@lezer/common';

/**
 * 位置处的链接：行内链接 `[text](url)`、引用式链接 `[text][ref]`、尖括号自动链接 `<url>`、GFM 裸链接。
 * 选区规范化（从右侧移入落在文字末尾）与 Ctrl+单击打开共用。纯函数。
 */
export interface LinkInfo {
  from: number;
  to: number;
  /** 链接文字的末尾（`]` 之前）；自动链接与裸链接没有文字部分，为 null */
  textEnd: number | null;
  /** 可打开的地址；引用式链接要查定义，这里为 null */
  url: string | null;
}

/** GFM 裸链接按 GFM 规范补协议：`www.` 开头补 http://，邮箱补 mailto: */
export function bareUrlHref(text: string): string {
  if (/^www\./i.test(text)) return 'http://' + text;
  if (!/^[a-z][a-z0-9+.-]*:/i.test(text) && text.includes('@')) return 'mailto:' + text;
  return text;
}

export function linkAt(tree: Tree, doc: Text, pos: number, side: -1 | 1): LinkInfo | null {
  for (let n: SyntaxNode | null = tree.resolveInner(pos, side); n; n = n.parent) {
    if (n.name === 'Link') {
      const marks = n.getChildren('LinkMark');
      const url = n.getChild('URL');
      return { from: n.from, to: n.to, textEnd: marks.length >= 2 ? marks[1].from : null, url: url ? doc.sliceString(url.from, url.to) : null };
    }
    if (n.name === 'Autolink') {
      const url = n.getChild('URL');
      const text = url ? doc.sliceString(url.from, url.to) : '';
      return { from: n.from, to: n.to, textEnd: null, url: url ? (/^[a-z][a-z0-9+.-]*:/i.test(text) ? text : 'mailto:' + text) : null };
    }
    if (n.name === 'URL' && !['Link', 'Autolink', 'Image'].includes(n.parent?.name ?? '')) {
      return { from: n.from, to: n.to, textEnd: null, url: bareUrlHref(doc.sliceString(n.from, n.to)) };
    }
    if (n.name === 'Image' || n.name === 'Paragraph') return null;
  }
  return null;
}
