import { Annotation, EditorSelection, EditorState, Prec, StateEffect, StateField, type Extension } from '@codemirror/state';
import { EditorView, keymap, type Command } from '@codemirror/view';
import { syntaxTree } from '@codemirror/language';
import { iterateTopBlocks } from '../syntax';
import { parseTable, tableRows } from '../commands/tableCells';
import { clearCells, normalizeRect, subTableMarkdown, tableDeletion, type CellPos } from '../commands/tableSelect';
import { CELL_CLASS, TABLE_WRAP_CLASS, cellAddress, type TableDom } from './tableCellRender';
import { Leave, cellSession, findWrap, openCell } from './cellEditor';

/**
 * 表格的块选择（webview-wysiwyg-engine.md 第 6 节「选择」；交互照 Muya 的 tableSelectCellsCtrl.js）：
 * - 整表选中：光标从表格外用方向键移入时先选中整表，再按一次进入单元格；单元格内 Ctrl+A 先选格内内容，再按选中整表。
 *   选中后 Backspace / Delete 删除整表，复制得到整表 markdown（源码切片）。
 * - 矩形多选：在单元格之间拖动时按矩形选中；复制得到所选区域的 markdown 子表，Backspace / Delete 清空所选单元格。
 * 选中期间主视图有焦点，主选区覆盖整张表格（键盘与剪贴板事件落在主视图上）；哪些格被选中记在 tableSelectionField，
 * 视觉样式（Muya 的 ag-cell-selected 与四边框线）由更新监听器画到表格 widget 的格子上。
 */

export interface TableSelection {
  /** 表格首行行首、末行行尾 */
  readonly from: number;
  readonly to: number;
  readonly anchor: CellPos;
  readonly head: CellPos;
  /** 整表选中（方向键移入、Ctrl+A）；拖出来的矩形即使覆盖全表也按多选处理（Delete 清空而不是删表） */
  readonly whole: boolean;
}

const setTableSelection = StateEffect.define<TableSelection | null>();
/** 清空所选单元格的事务：块选择保持 */
const keepSelection = Annotation.define<boolean>();

export const tableSelectionField = StateField.define<TableSelection | null>({
  create: () => null,
  update(value, tr) {
    for (const e of tr.effects) if (e.is(setTableSelection)) return e.value;
    if (!value) return value;
    if (tr.annotation(keepSelection)) return { ...value, from: tr.changes.mapPos(value.from, -1), to: tr.changes.mapPos(value.to, 1) };
    return tr.docChanged || tr.selection ? null : value;
  },
});

/**
 * 块选择期间主视图从 DOM 读回的选区（拖选时浏览器还在扩展原生选区、选区跨过不可编辑的 widget 被浏览器改写）
 * 落在表格范围内时丢掉，块选择不被它清掉。
 */
const keepAgainstDom = EditorState.transactionFilter.of(tr => {
  const ts = tr.startState.field(tableSelectionField, false);
  if (!ts || tr.docChanged || !tr.selection || tr.effects.length || !tr.isUserEvent('select')) return tr;
  return tr.selection.ranges.every(r => r.from >= ts.from && r.to <= ts.to) ? {} : tr;
});

/** 光标从 from 移到 to 途经（含落点）的第一张顶层表格；起点所在的表格不算 */
export function tableBetween(state: EditorState, from: number, to: number): { from: number; to: number } | null {
  if (from === to) return null;
  const forward = to > from;
  let found: { from: number; to: number } | null = null;
  iterateTopBlocks(syntaxTree(state), Math.min(from, to), Math.max(from, to), n => {
    if (n.name !== 'Table') return;
    const t = { from: state.doc.lineAt(n.from).from, to: state.doc.lineAt(n.to).to };
    if (from >= t.from && from <= t.to) return;
    if (!found || (forward ? t.from < found.from : t.from > found.from)) found = t;
  });
  return found;
}

/** 选中表格 table 的 anchor..head 矩形；whole 为整表选中（此时 anchor、head 取两个角）。拖选时 scroll 为 false：滚动会让指针下的格子变掉 */
export function selectTable(view: EditorView, table: { from: number; to: number }, whole: boolean, anchor?: CellPos, head?: CellPos, scroll = true) {
  let a = anchor, h = head;
  if (whole || !a || !h) {
    const model = parseTable(view.state.sliceDoc(table.from, table.to), 0);
    a = { row: 0, col: 0 };
    h = { row: tableRows(model).length - 1, col: model.cols - 1 };
  }
  view.dispatch({
    selection: EditorSelection.range(table.from, table.to),
    effects: setTableSelection.of({ from: table.from, to: table.to, anchor: a, head: h, whole }),
    scrollIntoView: scroll,
  });
}

/** 块选择对应的剪贴板文本：整表是源码切片，矩形是子表 */
export function selectionText(state: EditorState, ts: TableSelection): string {
  const src = state.sliceDoc(ts.from, ts.to);
  if (ts.whole) return src;
  return subTableMarkdown(src, parseTable(src, 0), ts.anchor, ts.head);
}

/** 删除块选择的内容：整表删除表格，矩形清空所选单元格（只改这些格的字节） */
export function deleteTableSelection(view: EditorView, ts: TableSelection) {
  if (ts.whole) {
    const { change, cursor } = tableDeletion(view.state.doc, ts.from, ts.to);
    view.dispatch({ changes: change, selection: { anchor: cursor }, userEvent: 'delete', scrollIntoView: true });
    return;
  }
  const src = view.state.sliceDoc(ts.from, ts.to);
  const changes = clearCells(src, parseTable(src, 0), ts.anchor, ts.head, ts.from);
  if (changes.length) view.dispatch({ changes, userEvent: 'delete', annotations: keepSelection.of(true) });
}

// ── 键盘 ─────────────────────────────────────────────────────────────

enum Dir { Up, Down, Left, Right }

const arrow = (dir: Dir): Command => view => {
  const ts = view.state.field(tableSelectionField, false);
  if (ts) return enterCells(view, ts, dir);
  const sel = view.state.selection.main;
  if (!sel.empty) return false;
  const forward = dir === Dir.Down || dir === Dir.Right;
  const target = dir === Dir.Up || dir === Dir.Down ? view.moveVertically(sel, forward) : view.moveByChar(sel, forward);
  // 上下键会整块跳过块 widget（落到表格另一侧的行上），所以找移动途经的第一张表格，而不只看落点
  const table = tableBetween(view.state, sel.head, target.head);
  if (!table || (sel.head >= table.from && sel.head <= table.to)) return false;
  selectTable(view, table, true);
  return true;
};

/** 块选择之后再按方向键：向下、向右进入首格；向上进入末行首格，向左进入末格；矩形多选进入 head 所在格 */
function enterCells(view: EditorView, ts: TableSelection, dir: Dir): boolean {
  const wrap = findWrap(view, ts.from);
  const t = wrap?.tdTable;
  if (!wrap || !t) return true;
  const last = tableRows(t.model).length - 1;
  if (!ts.whole) openCell(view, wrap, ts.head.row, ts.head.col, Infinity);
  else if (dir === Dir.Down || dir === Dir.Right) openCell(view, wrap, 0, 0, 0);
  else if (dir === Dir.Up) openCell(view, wrap, last, 0, 0);
  else openCell(view, wrap, last, t.model.cols - 1, Infinity);
  return true;
}

const whenSelected = (f: (view: EditorView, ts: TableSelection) => void): Command => view => {
  const ts = view.state.field(tableSelectionField, false);
  if (!ts) return false;
  f(view, ts);
  return true;
};

const tableSelectionKeymap = Prec.high(keymap.of([
  { key: 'ArrowUp', run: arrow(Dir.Up) },
  { key: 'ArrowDown', run: arrow(Dir.Down) },
  { key: 'ArrowLeft', run: arrow(Dir.Left) },
  { key: 'ArrowRight', run: arrow(Dir.Right) },
  { key: 'Backspace', run: whenSelected(deleteTableSelection) },
  { key: 'Delete', run: whenSelected(deleteTableSelection) },
  { key: 'Escape', run: whenSelected((view, ts) => view.dispatch({ selection: { anchor: ts.to }, scrollIntoView: true })) },
  // 块选择时回车不做事（不把选中的表格换成换行）
  { key: 'Enter', run: whenSelected(() => undefined) },
]));

const clipboard = EditorView.domEventHandlers({
  copy(e, view) { return toClipboard(e, view, false); },
  cut(e, view) { return toClipboard(e, view, true); },
});

function toClipboard(e: ClipboardEvent, view: EditorView, cut: boolean): boolean {
  const ts = view.state.field(tableSelectionField, false);
  if (!ts || !e.clipboardData) return false;
  e.preventDefault();
  e.clipboardData.clearData();
  e.clipboardData.setData('text/plain', selectionText(view.state, ts));
  if (cut) deleteTableSelection(view, ts);
  return true;
}

/** 块选择时键入的文字不替换表格 */
const blockTyping = EditorView.inputHandler.of(view => !!view.state.field(tableSelectionField, false));

// ── 视觉 ─────────────────────────────────────────────────────────────

const SELECTED = 'cm-td-cell-selected';
const EDGES = ['cm-td-cell-border-top', 'cm-td-cell-border-right', 'cm-td-cell-border-bottom', 'cm-td-cell-border-left'];
export const TABLE_SELECTED_CLASS = 'cm-td-table-selected';

/** 把块选择画到表格外框上（ts 为 null 时清掉） */
export function paintSelection(wrap: TableDom, ts: TableSelection | null) {
  if (!ts && !wrap.classList.contains(TABLE_SELECTED_CLASS)) return;
  wrap.classList.toggle(TABLE_SELECTED_CLASS, !!ts);
  const rect = ts && normalizeRect(ts.anchor, ts.head);
  (wrap.tdCells ?? []).forEach((line, r) => line.forEach((span, c) => {
    const cell = span.parentElement!;
    const on = !!rect && r >= rect.top && r <= rect.bottom && c >= rect.left && c <= rect.right;
    cell.classList.toggle(SELECTED, on);
    cell.classList.toggle(EDGES[0], on && r === rect!.top);
    cell.classList.toggle(EDGES[1], on && c === rect!.right);
    cell.classList.toggle(EDGES[2], on && r === rect!.bottom);
    cell.classList.toggle(EDGES[3], on && c === rect!.left);
  }));
}

const painter = EditorView.updateListener.of(u => {
  const ts = u.state.field(tableSelectionField, false) ?? null;
  const prev = u.startState.field(tableSelectionField, false) ?? null;
  if (!ts && !prev) return;
  for (const wrap of u.view.contentDOM.querySelectorAll<TableDom>(`.${TABLE_WRAP_CLASS}`)) {
    paintSelection(wrap, ts && u.view.posAtDOM(wrap) === ts.from ? ts : null);
  }
});

// ── 鼠标：单元格之间拖动选矩形 ────────────────────────────────────────

/**
 * 在表格里按下鼠标后跟踪拖动（Muya 的 handleCellMouseDown / MouseMove / MouseUp）：
 * 移到另一个单元格时开始矩形多选（结束格内编辑，焦点交给主视图）；还在起始格里时，manualFrom 不为 null
 * 表示这一格是按下时才打开的（按下的默认行为已阻止），由这里按指针位置扩展格内选区。
 */
export function trackCellDrag(view: EditorView, wrap: TableDom, start: CellPos, manualFrom: number | null) {
  const doc = wrap.ownerDocument;
  let rect = false;
  const cellAt = (x: number, y: number): { span: HTMLElement; pos: CellPos } | null => {
    const el = doc.elementFromPoint(x, y)?.closest('th, td');
    const span = el && wrap.contains(el) ? el.querySelector<HTMLElement>(`.${CELL_CLASS}`) : null;
    return span ? { span, pos: cellAddress(span) } : null;
  };
  const move = (e: MouseEvent) => {
    if (!(e.buttons & 1)) { stop(); return; }
    const hit = cellAt(e.clientX, e.clientY);
    if (!hit) return;
    if (!rect && hit.pos.row === start.row && hit.pos.col === start.col) {
      const s = cellSession(view);
      if (manualFrom !== null && s && s.host === hit.span) {
        const h = s.nested.posAtCoords({ x: e.clientX, y: e.clientY });
        if (h !== null) s.select(manualFrom, h);
      }
      return;
    }
    e.preventDefault();
    const tsPrev = view.state.field(tableSelectionField, false);
    if (rect && tsPrev && tsPrev.head.row === hit.pos.row && tsPrev.head.col === hit.pos.col) return;
    if (!rect) {
      rect = true;
      cellSession(view)?.close(Leave.Stay, true);
      doc.getSelection()?.removeAllRanges();
      view.focus();
    }
    const from = view.posAtDOM(wrap), to = from + (wrap.tdTable?.src.length ?? 0);
    selectTable(view, { from, to }, false, start, hit.pos, false);
  };
  const stop = () => {
    doc.removeEventListener('mousemove', move, true);
    doc.removeEventListener('mouseup', stop, true);
  };
  doc.addEventListener('mousemove', move, true);
  doc.addEventListener('mouseup', stop, true);
}

export const tableSelectionExtension: Extension = [tableSelectionField, keepAgainstDom, tableSelectionKeymap, clipboard, blockTyping, painter];
