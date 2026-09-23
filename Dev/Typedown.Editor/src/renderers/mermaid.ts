/**
 * mermaid 按需加载与渲染。渲染调用的要点照 Muya 的 render/index.js（renderMermaid）：
 * securityLevel strict、按明暗主题初始化、先 parse 预检、出错时占位。mermaid 的 render 不可重入，这里串行排队。
 *
 * 所有图按同一比例绘制：各图类型的 useMaxWidth 默认开（SVG 宽 100%、以自身宽度为 max-width），全部关掉，
 * SVG 以自身尺寸写 width / height（1 个 SVG 单位 = 1 CSS px）；宽于编辑区时由样式表等比缩小，不按容器放大（blocks.css）。
 */
import type { MermaidConfig } from 'mermaid';
import { escapeHtml } from './katex';
import { UiText, uiText } from '../shared/strings';

type Mermaid = typeof import('mermaid')['default'];

let mermaid: Mermaid | null = null;
let loading: Promise<Mermaid> | null = null;
let initializedTheme: string | null = null;
let queue: Promise<unknown> = Promise.resolve();
let seq = 0;
const cache = new Map<string, string>();

/**
 * 带 useMaxWidth 的图类型（mermaid 12 的 MermaidConfig；看板读的是 mindmap 的这一项；treemap 在默认配置里有、类型里漏了）。
 * 升级 mermaid 后由单测对照它的默认配置检查有没有漏掉新的图类型。
 */
export const DIAGRAM_TYPES = [
  'flowchart', 'swimlane', 'agentflow', 'sequence', 'gantt', 'journey', 'timeline', 'class', 'state', 'er', 'pie',
  'quadrantChart', 'xyChart', 'requirement', 'architecture', 'mindmap', 'ishikawa', 'kanban', 'gitGraph', 'c4', 'sankey',
  'packet', 'block', 'eventmodeling', 'treeView', 'radar', 'usecase', 'venn', 'wardley-beta', 'cynefin', 'railroad', 'treemap',
] as const satisfies readonly (keyof MermaidConfig | 'treemap')[];

const NATURAL_SIZE = Object.fromEntries(DIAGRAM_TYPES.map(t => [t, { useMaxWidth: false }])) as MermaidConfig;

/**
 * 按 viewBox 写 SVG 的 width / height：时序图、旅程图等写出的 height 与 viewBox 的高不一致，
 * 按 preserveAspectRatio 整张图会被等比缩小（时序图约 0.91）或上下留白；未关掉 useMaxWidth 的图类型的 100% 宽与 max-width 也在这里去掉。
 */
export function naturalSize(svg: string): string {
  const t = document.createElement('template');
  t.innerHTML = svg;
  const el = t.content.firstElementChild;
  const box = el?.localName === 'svg' ? el.getAttribute('viewBox')?.trim().split(/[\s,]+/).map(Number) : undefined;
  if (!el || !box || box.length !== 4 || !(box[2] > 0 && box[3] > 0)) return svg;
  el.setAttribute('width', String(box[2]));
  el.setAttribute('height', String(box[3]));
  (el as SVGElement).style.removeProperty('max-width');
  if (el.getAttribute('style') === '') el.removeAttribute('style');
  return t.innerHTML;
}

const currentTheme = () => (document.documentElement.dataset.theme === 'dark' ? 'dark' : 'default');

function load(): Promise<Mermaid> {
  return (loading ??= import('mermaid').then(m => (mermaid = m.default)));
}

export function mermaidCached(src: string): string | undefined {
  return cache.get(currentTheme() + '\n' + src);
}

/**
 * 渲染到 el。frame 是编辑区（CM6 的 contentDOM）：mermaid 在一个与它同宽的隐藏容器里排版，
 * 按容器定宽的图（甘特图取父元素宽度）正好铺满编辑区，其余图的尺寸只由内容决定。宽度在排队轮到时才读，不在 toDOM 里强制排版。
 * 缓存只按主题与源码区分，编辑区宽度变了甘特图保持首次渲染的宽度（更宽时不放大，更窄时由样式表缩小）。
 */
export function renderMermaid(el: HTMLElement, src: string, frame: HTMLElement): Promise<void> {
  const key = currentTheme() + '\n' + src;
  const hit = cache.get(key);
  if (hit !== undefined) { el.innerHTML = hit; return Promise.resolve(); }
  const job = queue.then(async () => {
    const m = mermaid ?? (await load());
    const theme = currentTheme();
    if (initializedTheme !== theme) {
      m.initialize({ startOnLoad: false, securityLevel: 'strict', theme, ...NATURAL_SIZE });
      initializedTheme = theme;
    }
    // mermaid 在容器里建临时节点，出错时也留在容器里，随容器一起移除
    const host = document.createElement('div');
    host.style.cssText = `position: absolute; left: -100000px; top: 0; visibility: hidden; width: ${Math.max(1, frame.clientWidth)}px`;
    document.body.appendChild(host);
    let html: string;
    try {
      await m.parse(src);
      const { svg } = await m.render(`td-mermaid-${++seq}`, src, host);
      html = naturalSize(svg);
    } catch (err) {
      const detail = escapeHtml(String((err as Error)?.message ?? err)).slice(0, 300).replace(/"/g, '&quot;');
      html = `<div class="cm-td-render-error" title="${detail}">${escapeHtml(uiText(UiText.InvalidMermaid))}</div>`;
    } finally {
      host.remove();
    }
    if (cache.size > 500) cache.clear();
    cache.set(key, html);
    el.innerHTML = html;
  });
  queue = job.catch(() => undefined);
  return job;
}
