/**
 * mermaid 按需加载与渲染。渲染调用的要点照 Muya 的 render/index.js（renderMermaid）：
 * securityLevel strict、按明暗主题初始化、先 parse 预检、出错时占位。mermaid 的 render 不可重入，这里串行排队。
 */
import { escapeHtml } from './katex';

type Mermaid = typeof import('mermaid')['default'];

let mermaid: Mermaid | null = null;
let loading: Promise<Mermaid> | null = null;
let initializedTheme: string | null = null;
let queue: Promise<unknown> = Promise.resolve();
let seq = 0;
const cache = new Map<string, string>();

const currentTheme = () => (document.documentElement.dataset.theme === 'dark' ? 'dark' : 'default');

function load(): Promise<Mermaid> {
  return (loading ??= import('mermaid').then(m => (mermaid = m.default)));
}

export function mermaidCached(src: string): string | undefined {
  return cache.get(currentTheme() + '\n' + src);
}

export function renderMermaid(el: HTMLElement, src: string): Promise<void> {
  const key = currentTheme() + '\n' + src;
  const hit = cache.get(key);
  if (hit !== undefined) { el.innerHTML = hit; return Promise.resolve(); }
  const job = queue.then(async () => {
    const m = mermaid ?? (await load());
    const theme = currentTheme();
    if (initializedTheme !== theme) {
      m.initialize({ startOnLoad: false, securityLevel: 'strict', theme });
      initializedTheme = theme;
    }
    let html: string;
    try {
      await m.parse(src);
      const { svg } = await m.render(`td-mermaid-${++seq}`, src);
      html = svg;
    } catch (err) {
      html = `<div class="cm-td-render-error">&lt; Invalid Mermaid Codes &gt; ${escapeHtml(String((err as Error)?.message ?? err)).slice(0, 300)}</div>`;
      // mermaid 出错时会在 body 上留下临时节点
      document.getElementById(`dtd-mermaid-${seq}`)?.remove();
    }
    if (cache.size > 500) cache.clear();
    cache.set(key, html);
    el.innerHTML = html;
  });
  queue = job.catch(() => undefined);
  return job;
}
