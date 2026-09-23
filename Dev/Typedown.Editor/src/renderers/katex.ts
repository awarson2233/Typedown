/**
 * KaTeX 按需加载：首个公式进入视口时才 import()，连同 mhchem 与样式（字体随 CSS 由 Vite 打进懒块）。
 * 加载完成后 `katexNow()` 同步可用，widget 的 toDOM 可以直接同步渲染，避免高度跳动。
 */
type Katex = typeof import('katex')['default'];

let katex: Katex | null = null;
let loading: Promise<Katex> | null = null;
const cache = new Map<string, string>();

export function katexNow(): Katex | null {
  return katex;
}

export function loadKatex(): Promise<Katex> {
  return (loading ??= (async () => {
    const [mod] = await Promise.all([import('katex'), import('katex/dist/katex.min.css')]);
    await import('katex/contrib/mhchem');
    katex = mod.default;
    return katex;
  })());
}

/** 同步渲染（KaTeX 已加载时）；返回 false 表示尚未加载。 */
export function renderMathSync(el: HTMLElement, src: string, displayMode: boolean): boolean {
  if (!katex) return false;
  const key = (displayMode ? 'D' : 'I') + src;
  let html = cache.get(key);
  if (html === undefined) {
    try {
      html = katex.renderToString(src, { displayMode, throwOnError: true, strict: 'ignore' });
    } catch (err) {
      html = `<span class="cm-td-render-error" title="${escapeAttr(String((err as Error).message ?? err))}">${escapeHtml(src || '（空公式）')}</span>`;
    }
    if (cache.size > 2000) cache.clear();
    cache.set(key, html);
  }
  el.innerHTML = html;
  return true;
}

export async function renderMath(el: HTMLElement, src: string, displayMode: boolean): Promise<void> {
  if (!renderMathSync(el, src, displayMode)) {
    await loadKatex();
    renderMathSync(el, src, displayMode);
  }
}

/**
 * 公式块的同步渲染：出错与空公式用 Muya 的占位（`div.ag-math-error` / `div.ag-empty` 的写法，文字取 shared/strings）；
 * 返回 false 表示 KaTeX 尚未加载。
 */
export function renderBlockMathSync(el: HTMLElement, src: string, text: { empty: string; invalid: string }): boolean {
  if (!src.trim()) {
    el.innerHTML = `<div class="cm-td-render-empty">${escapeHtml(text.empty)}</div>`;
    return true;
  }
  if (!katex) return false;
  const key = 'B' + src;
  let html = cache.get(key);
  if (html === undefined) {
    try {
      html = katex.renderToString(src, { displayMode: true, throwOnError: true, strict: 'ignore' });
    } catch (err) {
      html = `<div class="cm-td-render-error" title="${escapeAttr(String((err as Error).message ?? err))}">${escapeHtml(text.invalid)}</div>`;
    }
    if (cache.size > 2000) cache.clear();
    cache.set(key, html);
  }
  el.innerHTML = html;
  return true;
}

export const escapeHtml =(s: string) => s.replace(/[&<>]/g, c => (c === '&' ? '&amp;' : c === '<' ? '&lt;' : '&gt;'));
const escapeAttr = (s: string) => escapeHtml(s).replace(/"/g, '&quot;');
