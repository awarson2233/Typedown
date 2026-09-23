/**
 * HTML 块的预览（webview-wysiwyg-engine.md 第 1 节 D7）：DOMPurify 消毒后交给浏览器渲染，配置照 Muya 的
 * PREVIEW_DOMPURIFY_CONFIG（禁 style 属性与 contenteditable、不允许 data-*、html + svg 配置档），另外禁掉 <style>
 * （它会作用到整个页面）。DOMPurify 按需加载，加载完成后同步可用。
 * 图片的 src 按文档目录改写成可加载的地址（renderers/imageUrl.ts）。
 */
import { imageUrlOf } from './imageUrl';

type Purify = typeof import('dompurify')['default'];

let purify: Purify | null = null;
let loading: Promise<Purify> | null = null;
const cache = new Map<string, string>();

export function loadPurify(): Promise<Purify> {
  return (loading ??= import('dompurify').then(m => (purify = m.default)));
}

const config = () => ({
  FORBID_ATTR: ['style', 'contenteditable'],
  FORBID_TAGS: ['style'],
  ALLOW_DATA_ATTR: false,
  USE_PROFILES: { html: true, svg: true, svgFilters: true, mathMl: false },
  RETURN_TRUSTED_TYPE: false,
  RETURN_DOM_FRAGMENT: true as const,
});

/**
 * 只含不可见内容的 HTML 块（注释、script、style、meta、link、title 之类）：渲染出来是空白，
 * 这种块不进入渲染态，始终显示源码（与 Typora 相同）。
 */
export function isInvisibleHtml(src: string): boolean {
  const rest = src
    .replace(/<!--[\s\S]*?(?:-->|$)/g, '')
    .replace(/<(script|style|template|noscript|title|textarea)\b[\s\S]*?(?:<\/\1\s*>|$)/gi, '')
    .replace(/<\/?(?:meta|link|base|script|style|template|noscript|title|html|head|body|!doctype)\b[^>]*>/gi, '')
    .replace(/<\?[\s\S]*?(?:\?>|$)/g, '');
  return !rest.trim();
}

/** 同步消毒（DOMPurify 已加载时）；返回 null 表示尚未加载 */
export function sanitizeHtmlSync(src: string, basePath: string): string | null {
  if (!purify) return null;
  const key = basePath + '\n' + src;
  let html = cache.get(key);
  if (html === undefined) {
    const frag = purify.sanitize(src, config());
    for (const img of Array.from(frag.querySelectorAll('img'))) {
      const s = img.getAttribute('src');
      if (s === null) continue;
      const url = imageUrlOf(s, basePath);
      if (url) img.setAttribute('src', url); else img.removeAttribute('src');
    }
    const box = document.createElement('div');
    box.appendChild(frag);
    html = box.innerHTML;
    if (cache.size > 500) cache.clear();
    cache.set(key, html);
  }
  return html;
}

export async function sanitizeHtml(src: string, basePath: string): Promise<string> {
  const now = sanitizeHtmlSync(src, basePath);
  if (now !== null) return now;
  await loadPurify();
  return sanitizeHtmlSync(src, basePath)!;
}
