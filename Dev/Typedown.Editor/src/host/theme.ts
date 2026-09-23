import type { EditorColor, EditorSettings, EditorTheme } from '../bridge/protocol';

/**
 * 宿主下发的主题与设置落到页面上（初始态、view.theme、view.settings）。
 * 视觉部分只写根元素的 data-theme 与 CSS 变量（styles/editor.css 读取）；编辑器行为部分（源码模式、制表符、拼写检查）交给 EditorControls。
 */

/** 明暗主题 */
export type Theme = 'light' | 'dark';

const css = (c: EditorColor) => `rgba(${c.r}, ${c.g}, ${c.b}, ${c.a})`;
const isColor = (c: unknown): c is EditorColor =>
  !!c && ['r', 'g', 'b', 'a'].every(k => typeof (c as Record<string, unknown>)[k] === 'number');

export function applyTheme(theme: Theme | EditorTheme, root: HTMLElement = document.documentElement) {
  if (typeof theme === 'string') { root.dataset.theme = theme; return; }
  root.dataset.theme = theme.isDark ? 'dark' : 'light';
  // 强调色与背景缺失或畸形时保留样式表里的默认值（rgba(undefined, …) 会被浏览器整条丢弃）
  if (isColor(theme.accent)) root.style.setProperty('--td-accent', css(theme.accent));
  else root.style.removeProperty('--td-accent');
  if (isColor(theme.background)) root.style.setProperty('--td-bg', css(theme.background));
  else root.style.removeProperty('--td-bg');
}

/** 设置里由编辑器实例执行的部分 */
export interface EditorControls {
  setSourceMode(on: boolean): void;
  setTabSize(size: number): void;
  setSpellcheck(on: boolean): void;
  /** 字号、行高、版心宽度变化后让 CM6 重新测量行高 */
  remeasure(): void;
}

/** 当前生效的全量设置；apply 只处理非空字段（null 与缺失同义） */
export class SettingsApplier {
  readonly current: EditorSettings = {};

  constructor(private readonly editor: EditorControls, private readonly root: HTMLElement = document.documentElement) {}

  /** 合并变更，落到 CSS 变量与编辑器上 */
  apply(changes: EditorSettings | null | undefined) {
    if (!changes) return;
    let layout = false;
    const s = this.root.style;
    for (const [key, value] of Object.entries(changes) as [keyof EditorSettings, unknown][]) {
      if (value === null || value === undefined) continue;
      (this.current as Record<string, unknown>)[key] = value;
      switch (key) {
        case 'fontSize':
          if (typeof value === 'number' && value > 0) { s.setProperty('--td-font-size', `${value}px`); layout = true; }
          break;
        case 'lineHeight':
          if (typeof value === 'number' && value > 0) { s.setProperty('--td-line-height', String(value)); layout = true; }
          break;
        case 'editorAreaWidth':
          // 任意 CSS 长度（设置页是自由文本，默认 1200px）；不合法的值退回样式表默认
          if (typeof value === 'string' && value.trim() && (typeof CSS === 'undefined' || CSS.supports('max-width', value.trim()))) s.setProperty('--td-area-width', value.trim());
          else s.removeProperty('--td-area-width');
          layout = true;
          break;
        case 'tabSize':
          if (typeof value === 'number' && value > 0) this.editor.setTabSize(value);
          break;
        case 'spellcheckEnabled':
          if (typeof value === 'boolean') this.editor.setSpellcheck(value);
          break;
        case 'sourceCode':
          if (typeof value === 'boolean') this.editor.setSourceMode(value);
          break;
        // 其余字段（打字机、专注、自动配对、查找选项等）属于后续阶段的功能，这里只记下当前值
      }
    }
    if (layout) this.editor.remeasure();
  }
}
