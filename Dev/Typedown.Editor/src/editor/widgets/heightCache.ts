import { hashString } from '../../shared/hash';

/**
 * widget 高度按内容哈希缓存（SilverBullet widgetCache 的思路）：公式与图表的真实高度要渲染后才知道，
 * 视口外的 widget 用缓存高度给 CM6 估算文档高度，滚动回来时不再跳动。
 */
const heights = new Map<string, number>();

export const heightKey = (kind: string, src: string) => kind + ':' + hashString(src);

export function cachedHeight(key: string): number | undefined {
  return heights.get(key);
}

export function rememberHeight(key: string, h: number) {
  if (h > 0) {
    if (heights.size > 5000) heights.clear();
    // 不取整：图表块高多带小数（svg 行框），取整后每块差零点几像素，长文档里累积成可见的滚动跳动
    heights.set(key, h);
  }
}

/** 渲染完成后量一次高度写进缓存；不在文档里（已被替换）时跳过。 */
export function measureInto(key: string, el: HTMLElement) {
  requestAnimationFrame(() => {
    if (el.isConnected) rememberHeight(key, el.getBoundingClientRect().height);
  });
}
