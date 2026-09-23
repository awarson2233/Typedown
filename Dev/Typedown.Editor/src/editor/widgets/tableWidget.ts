import { WidgetType, type EditorView } from '@codemirror/view';
import { undo, redo } from '@codemirror/commands';
import { cellEdit, parseTable, tableRows, unescapeCell, type TableModel } from '../commands/tableCells';
import { cachedHeight, heightKey, measureInto } from './heightCache';

/**
 * 表格网格 widget（C1 原型，webview-wysiwyg-engine.md 第 6 节）：
 * - 每个单元格是一个 contenteditable 的 span，显示去转义后的源码文本（C1 不渲染行内格式）；
 * - 单元格内输入只替换该单元格的源码区间（`|` 转义为 `\|`），组字期间不提交，compositionend 时提交；
 * - 行列数不变时 updateDOM 原地更新，保留 DOM 与焦点；行列数变化时重建。
 * 模型里的区间以表格起点为 0（相对偏移），提交时用 posAtDOM 取表格当前在文档中的位置。
 */

interface TableDom extends HTMLElement {
  tdWidget?: TableWidget;
}

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

  private cellText(from: number, to: number) {
    return unescapeCell(this.src.slice(from, to));
  }

  private shape() {
    return tableRows(this.model).map(r => r.cells.length).join(',') + '/' + this.model.cols;
  }

  toDOM(view: EditorView): HTMLElement {
    const wrap = document.createElement('div') as TableDom;
    wrap.className = 'cm-td-table-wrap';
    wrap.contentEditable = 'false';
    wrap.tdWidget = this;
    wrap.dataset.shape = this.shape();
    const table = document.createElement('table');
    table.className = 'cm-td-table';
    wrap.appendChild(table);
    tableRows(this.model).forEach((row, r) => {
      const tr = document.createElement('tr');
      for (let c = 0; c < this.model.cols; c++) {
        const cellEl = document.createElement(r === 0 ? 'th' : 'td');
        const align = this.model.aligns[c];
        if (align) cellEl.style.textAlign = align;
        const span = document.createElement('span');
        span.className = 'cm-td-cell';
        span.contentEditable = 'true';
        span.spellcheck = false;
        span.dataset.row = String(r);
        span.dataset.col = String(c);
        const cell = row.cells[c];
        span.textContent = cell ? this.cellText(cell.from, cell.to) : '';
        if (!cell) span.dataset.missing = '1';
        cellEl.appendChild(span);
        tr.appendChild(cellEl);
      }
      (r === 0 ? (table.createTHead()) : (table.tBodies[0] ?? table.createTBody())).appendChild(tr);
    });
    attachCellHandlers(wrap, view);
    measureInto(this.key, wrap);
    return wrap;
  }

  /** 行列结构相同：原地更新非焦点单元格的文本，保留 DOM 与焦点。 */
  updateDOM(dom: HTMLElement): boolean {
    const wrap = dom as TableDom;
    if (wrap.dataset.shape !== this.shape()) return false;
    wrap.tdWidget = this;
    const rows = tableRows(this.model);
    for (const span of wrap.querySelectorAll<HTMLElement>('.cm-td-cell')) {
      const r = Number(span.dataset.row), c = Number(span.dataset.col);
      const cell = rows[r]?.cells[c];
      const text = cell ? this.cellText(cell.from, cell.to) : '';
      // 由这个单元格自己的输入产生的事务，模型文本与 DOM 已一致，不动它（保留光标）；撤销等外部变化才改写
      if (span.textContent !== text) span.textContent = text;
    }
    measureInto(this.key, wrap);
    return true;
  }

  ignoreEvent(e: Event) {
    const t = e.target as HTMLElement | null;
    return !!t?.closest?.('.cm-td-cell');
  }
}

function attachCellHandlers(wrap: TableDom, view: EditorView) {
  let composing = false;
  const commit = (span: HTMLElement) => {
    const widget = wrap.tdWidget;
    if (!widget || !wrap.isConnected) return;
    const tableFrom = view.posAtDOM(wrap);
    const r = Number(span.dataset.row), c = Number(span.dataset.col);
    const change = cellEdit((f, t) => widget.src.slice(f, t), widget.model, r, c, span.textContent ?? '');
    if (!change) return;
    view.dispatch({
      changes: { from: tableFrom + change.from, to: tableFrom + change.to, insert: change.insert },
      userEvent: 'input.table',
    });
  };
  wrap.addEventListener('compositionstart', () => { composing = true; });
  wrap.addEventListener('compositionend', e => {
    composing = false;
    const span = (e.target as HTMLElement).closest<HTMLElement>('.cm-td-cell');
    if (span) commit(span);
  });
  wrap.addEventListener('input', e => {
    if (composing || (e as InputEvent).isComposing) return;
    const span = (e.target as HTMLElement).closest<HTMLElement>('.cm-td-cell');
    if (span) commit(span);
  });
  wrap.addEventListener('keydown', e => {
    const span = (e.target as HTMLElement).closest<HTMLElement>('.cm-td-cell');
    if (!span || e.isComposing) return;
    const mod = e.ctrlKey || e.metaKey;
    if (mod && (e.key === 'z' || e.key === 'Z')) { e.preventDefault(); (e.shiftKey ? redo : undo)(view); return; }
    if (mod && (e.key === 'y' || e.key === 'Y')) { e.preventDefault(); redo(view); return; }
    if (e.key === 'Enter') { e.preventDefault(); return; }
    if (e.key === 'Tab') {
      e.preventDefault();
      const cells = [...wrap.querySelectorAll<HTMLElement>('.cm-td-cell')];
      const next = cells[cells.indexOf(span) + (e.shiftKey ? -1 : 1)];
      if (next) focusEnd(next);
      return;
    }
    if (e.key === 'Escape') {
      e.preventDefault();
      const pos = view.posAtDOM(wrap) + (wrap.tdWidget?.src.length ?? 0);
      view.focus();
      view.dispatch({ selection: { anchor: Math.min(pos, view.state.doc.length) } });
    }
  });
  // 粘贴只取纯文本，避免富文本进入单元格
  wrap.addEventListener('paste', e => {
    const span = (e.target as HTMLElement).closest<HTMLElement>('.cm-td-cell');
    if (!span) return;
    e.preventDefault();
    const text = e.clipboardData?.getData('text/plain') ?? '';
    document.execCommand('insertText', false, text.replace(/\r?\n/g, ' '));
  });
}

function focusEnd(el: HTMLElement) {
  el.focus();
  const sel = el.ownerDocument.getSelection();
  if (!sel) return;
  const range = el.ownerDocument.createRange();
  range.selectNodeContents(el);
  range.collapse(false);
  sel.removeAllRanges();
  sel.addRange(range);
}
