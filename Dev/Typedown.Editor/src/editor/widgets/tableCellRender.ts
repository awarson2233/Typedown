import { Text } from '@codemirror/state';
import type { EditorView } from '@codemirror/view';
import { cellParser } from '../syntax';
import { normalizeFootnoteLabel } from '../syntax/footnote';
import { buildInlineSpecs, type InlineSpec } from '../decorations/inlineSpecs';
import { footnoteNumberSource } from '../decorations/inlinePlugin';
import { renderInlineStatic, staticOffset } from '../decorations/staticInline';
import type { TableModel } from '../commands/tableCells';

/**
 * 表格网格的 DOM 约定与非焦点单元格的渲染（webview-wysiwyg-engine.md 第 6 节「单元格渲染」）。
 * 非焦点单元格与正文共用同一套行内规则：单元格源码用 cellParser（GFM 表格对每格做的行内解析）解析，
 * 交给 buildInlineSpecs 得出显形决策（没有光标，全部是渲染态），再由 staticInline 画成 DOM。
 * 焦点单元格换成嵌套视图（cellEditor.ts），挂同一套 InlineReveal 插件，所以两者的外观一致。
 */

export const TABLE_WRAP_CLASS = 'cm-td-table-wrap';
export const CELL_CLASS = 'cm-td-cell';
/** 挂着嵌套视图的单元格（非焦点渲染跳过它，内容由嵌套视图维护） */
export const ACTIVE_CELL_CLASS = 'cm-td-cell-active';

/** 表格 widget 的外框元素：挂着当前的表格模型（区间以表格起点为 0）与单元格元素网格 */
export interface TableDom extends HTMLElement {
  tdTable?: { readonly src: string; readonly model: TableModel };
  tdCells?: HTMLElement[][];
}

export const cellElement = (wrap: TableDom, row: number, col: number): HTMLElement | null => wrap.tdCells?.[row]?.[col] ?? null;

/** 单元格元素在网格里的位置 */
export function cellAddress(el: HTMLElement): { row: number; col: number } {
  return { row: Number(el.dataset.row), col: Number(el.dataset.col) };
}

/** 单元格源码 → 行内显形决策（渲染态）。 */
export function cellSpecs(src: string, footnoteNumber?: (label: string) => number | undefined): InlineSpec[] {
  const tree = cellParser().parse(src);
  return buildInlineSpecs(Text.of([src]), tree, { from: 0, to: src.length }, [], { tableCell: true, footnoteNumber });
}

/** 按主视图的脚注编号（与正文的上标一致）查标签 */
export function footnoteLookup(view: EditorView): ((label: string) => number | undefined) | undefined {
  const numbers = view.state.facet(footnoteNumberSource)?.(view.state);
  return numbers ? label => numbers.get(normalizeFootnoteLabel(label)) : undefined;
}

const rendered = new WeakMap<HTMLElement, string>();

/** 上次渲染进该单元格的源码 */
export const renderedSource = (span: HTMLElement) => rendered.get(span);

/** 把单元格源码渲染进 span（清掉原有内容） */
export function renderCell(span: HTMLElement, src: string, view: EditorView) {
  span.replaceChildren();
  if (src) renderInlineStatic(span, src, cellSpecs(src, footnoteLookup(view)), view);
  rendered.set(span, src);
}

/** 单击位置 → 单元格源码偏移；点在单元格内容之外（格子内边距）时为 null */
export function offsetAtPoint(span: HTMLElement, x: number, y: number): number | null {
  const doc = span.ownerDocument as Document & {
    caretPositionFromPoint?: (x: number, y: number) => { offsetNode: Node; offset: number } | null;
    caretRangeFromPoint?: (x: number, y: number) => Range | null;
  };
  const p = doc.caretPositionFromPoint?.(x, y);
  if (p) return staticOffset(span, p.offsetNode, p.offset);
  const r = doc.caretRangeFromPoint?.(x, y);
  return r ? staticOffset(span, r.startContainer, r.startOffset) : null;
}
