/** 明暗主题：C5 接宿主的 theme 消息，C1 只提供切换入口（dev 页用）。 */
export type Theme = 'light' | 'dark';

export function applyTheme(theme: Theme) {
  document.documentElement.dataset.theme = theme;
}
