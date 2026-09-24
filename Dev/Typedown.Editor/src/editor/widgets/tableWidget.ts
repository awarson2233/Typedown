import { WidgetType, type EditorView } from '@codemirror/view';
import { parseTable, tableRows, type CellRange, type TableModel } from '../commands/tableCells';
import { cachedHeight, heightKey, measureInto } from './heightCache';
import { ACTIVE_CELL_CLASS, CELL_CLASS, TABLE_WRAP_CLASS, cellAddress, offsetAtPoint, renderCell, renderedSource, type TableDom } from './tableCellRender';
import { cellSession, openCell, tableDomDestroyed } from './cellEditor';
import { trackCellDrag } from './tableSelection';

/**
 * 表格网格 widget（webview-wysiwyg-engine.md 第 6 节）：
 * - 非焦点单元格显示渲染后的行内格式（tableCellRender.ts，与正文同一套行内规则）；
 * - 单击或 Tab 进入的单元格换成嵌套视图（cellEditor.ts），输入只替换该单元格的源码区间，经主视图提交；
 * - 行列数不变时 updateDOM 原地更新，只重画源码变了的非焦点单元格，保留 DOM 与焦点；行列数变化时重建，
 *   焦点单元格的嵌套视图由会话挪进新表格。
 * 模型里的区间以表格起点为 0（相对偏移），要文档位置时用 posAtDOM 取表格当前的起点。
 */

export class TableWidget extends WidgetType {
  readonly model: TableModel;
  readonly key: string;
  constructor(readonly src: string) {
    super();
    this.model = parseTable(src, 0);
    this.key = heightKey('table', src);
  }

  eq(o: TableWidget) { return o.src === this.src; }

  get estimatedHeight() {
    return cachedHeight(this.key) ?? 8 + 34 * (1 + this.model.body.length);
  }

  private cellSrc(cell: CellRange | undefined) {
    return cell ? this.src.slice(cell.from, cell.to) : '';
  }

  private shape() {
    return tableRows(this.model).map(r => r.cells.length).join(',') + '/' + this.model.cols;
  }

  toDOM(view: EditorView): HTMLElement {
    const wrap = document.createElement('div') as TableDom;
    wrap.className = TABLE_WRAP_CLASS;
    wrap.contentEditable = 'false';
    wrap.tdTable = this;
    wrap.tdCells = [];
    wrap.dataset.shape = this.shape();
    const table = document.createElement('table');
    table.className = 'cm-td-table';
    wrap.appendChild(table);
    tableRows(this.model).forEach((row, r) => {
      const tr = document.createElement('tr');
      const line: HTMLElement[] = [];
      for (let c = 0; c < this.model.cols; c++) {
        const cellEl = document.createElement(r === 0 ? 'th' : 'td');
        const align = this.model.aligns[c];
        if (align) cellEl.style.textAlign = align;
        const span = document.createElement('span');
        span.className = CELL_CLASS;
        span.dataset.row = String(r);
        span.dataset.col = String(c);
        const cell = row.cells[c];
        if (!cell) span.dataset.missing = '1';
        renderCell(span, this.cellSrc(cell), view);
        cellEl.appendChild(span);
        tr.appendChild(cellEl);
        line.push(span);
      }
      wrap.tdCells!.push(line);
      (r === 0 ? (table.createTHead()) : (table.tBodies[0] ?? table.createTBody())).appendChild(tr);
    });
    attachCellHandlers(wrap, view);
    measureInto(this.key, wrap);
    return wrap;
  }

  /** 行列结构相同：原地重画源码变了的非焦点单元格，保留 DOM 与焦点。 */
  updateDOM(dom: HTMLElement, view: EditorView): boolean {
    const wrap = dom as TableDom;
    if (wrap.dataset.shape !== this.shape() || !wrap.tdCells) return false;
    wrap.tdTable = this;
    tableRows(this.model).forEach((row, r) => {
      for (let c = 0; c < this.model.cols; c++) {
        const span = wrap.tdCells![r][c];
        // 焦点单元格的内容由嵌套视图维护（会话在主视图更新后核对）
        if (span.classList.contains(ACTIVE_CELL_CLASS)) continue;
        const src = this.cellSrc(row.cells[c]);
        if (renderedSource(span) !== src) renderCell(span, src, view);
      }
    });
    measureInto(this.key, wrap);
    return true;
  }

  destroy(dom: HTMLElement) {
    tableDomDestroyed(dom);
  }

  /** 单元格里的事件（包括嵌套视图的）由表格自己处理，主视图不管 */
  ignoreEvent(e: Event) {
    const t = e.target as HTMLElement | null;
    return !!t?.closest?.('th, td');
  }
}

function attachCellHandlers(wrap: TableDom, view: EditorView) {
  // 捕获阶段处理：静态渲染里的 widget（图片等）自己的 mousedown 不再触发
  wrap.addEventListener('mousedown', e => {
    if (e.button !== 0) return;
    const cellEl = (e.target as HTMLElement).closest?.('th, td');
    const span = cellEl?.querySelector<HTMLElement>(`.${CELL_CLASS}`);
    if (!span || !wrap.contains(span)) return;
    const cell = cellAddress(span);
    // 已经是焦点单元格：格内的按下交给嵌套视图，这里只跟踪是否拖到别的格子（矩形多选）
    if (cellSession(view)?.host === span) { trackCellDrag(view, wrap, cell, null); return; }
    e.preventDefault();
    e.stopPropagation();
    const offset = offsetAtPoint(span, e.clientX, e.clientY) ?? Infinity;
    const s = openCell(view, wrap, cell.row, cell.col, offset);
    trackCellDrag(view, wrap, cell, s ? s.nested.state.selection.main.head : null);
  }, true);
}
