// @vitest-environment jsdom
import { describe, expect, it } from 'vitest';
import { DIAGRAM_TYPES, naturalSize } from '../src/renderers/mermaid';

/** mermaid 统一比例：各图类型关掉 useMaxWidth，SVG 的 width / height 按 viewBox 写（1 个 SVG 单位 = 1 CSS px） */
describe('mermaid 统一比例', () => {
  const attrs = (svg: string) => {
    const t = document.createElement('template');
    t.innerHTML = svg;
    const el = t.content.firstElementChild!;
    return { width: el.getAttribute('width'), height: el.getAttribute('height'), style: el.getAttribute('style') };
  };

  it('height 与 viewBox 不一致（时序图）时按 viewBox 写，否则整张图会被等比缩小', () => {
    expect(attrs(naturalSize('<svg viewBox="-50 -10 450 343" width="450" height="313"><g></g></svg>'))).toEqual({ width: '450', height: '343', style: null });
  });
  it('去掉 useMaxWidth 留下的 100% 宽与 max-width，其余内联样式保留', () => {
    expect(attrs(naturalSize('<svg viewBox="0 0 360 64" width="100%" style="max-width: 360px; background-color: white;"></svg>')))
      .toEqual({ width: '360', height: '64', style: 'background-color: white;' });
  });
  it('没有 viewBox 或不是 SVG 时原样返回', () => {
    const plain = '<svg width="10" height="10"></svg>';
    expect(naturalSize(plain)).toBe(plain);
    expect(naturalSize('<div>x</div>')).toBe('<div>x</div>');
  });
  it('mermaid 默认配置里凡有 useMaxWidth 的图类型都在关闭清单里', async () => {
    const m = (await import('mermaid')).default;
    const defaults = (m as unknown as { mermaidAPI: { defaultConfig: Record<string, unknown> } }).mermaidAPI.defaultConfig;
    const withMaxWidth = Object.entries(defaults).filter(([, v]) => !!v && typeof v === 'object' && 'useMaxWidth' in v).map(([k]) => k);
    expect(withMaxWidth.length).toBeGreaterThan(20);
    expect(withMaxWidth.filter(k => !(DIAGRAM_TYPES as readonly string[]).includes(k))).toEqual([]);
  });
});
