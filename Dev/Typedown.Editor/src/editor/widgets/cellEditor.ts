import { Annotation, EditorSelection, EditorState, Prec, StateEffect, StateField, Transaction, type ChangeDesc, type Extension } from '@codemirror/state';
import { EditorView, keymap, type KeyBinding } from '@codemirror/view';
import { defaultKeymap, redo, undo } from '@codemirror/commands';
import { cellLanguage } from '../syntax';
import { footnoteNumberSource, tableCellReveal, inlineRevealExtension, openLinkHandler } from '../decorations/inlinePlugin';
import { revealState } from '../decorations/revealState';
import { documentLocation } from '../state/documentLocation';
import { cellReplace, cellSourceFixes, minimalChange, tableRows } from '../commands/tableCells';
import { ACTIVE_CELL_CLASS, TABLE_WRAP_CLASS, cellElement, renderCell, type TableDom } from './tableCellRender';

/**
 * 焦点单元格的嵌套视图（webview-wysiwyg-engine.md 第 6 节「单元格渲染」）。
 *
 * - 任一时刻每个主视图最多一个会话：获得焦点的单元格里放一个嵌套 EditorView，文档就是该格的源码（`\|` 原样），
 *   挂与正文相同的行内显形插件（tableCellReveal：不给块级排版），所以格内也按 span 显形；
 * - 嵌套视图没有 history。它的每次改动（组字期间除外）按「新旧格文本的最小替换」经主视图 dispatch 提交，
 *   带上改动前后的选区（主视图的撤销据此把光标还回格内）；格内的 Ctrl+Z / Ctrl+Y 直接调主视图的 undo / redo；
 * - 主视图每次更新后（updateListener，此时表格 widget 的 DOM 已更新）核对：主文档里该格位置上的文字仍与嵌套文档相同 → 不动；
 *   否则（撤销、别处的编辑）按表格模型重新取该格源码，最小替换进嵌套视图；表格被重建（行列数变化）时把嵌套视图挪进新的单元格元素。
 * - 嵌套文档里不允许出现未转义的竖线与换行（cellSourceFixes），所以提交给主文档的总是合法的单元格源码，其余单元格与对齐空格不动。
 */

/** 主视图上由单元格会话派发的事务 */
export const cellCommit = Annotation.define<boolean>();
/** 嵌套视图上由主文档同步过来的事务（不回写主文档） */
const fromMain = Annotation.define<boolean>();

interface TableRange { readonly from: number; readonly to: number }
const setActiveTable = StateEffect.define<TableRange | null>();

/**
 * 有焦点单元格的表格在主文档里的范围。嵌套视图的 DOM 选区落在主视图的 contentDOM 里，
 * 主视图的 DOM 观察器在变动刷新时会把它读成「选区移到表格 widget 边上」，这类选区事务在会话期间丢掉。
 */
export const activeTableField = StateField.define<TableRange | null>({
  create: () => null,
  update(value, tr) {
    for (const e of tr.effects) if (e.is(setActiveTable)) return e.value;
    if (value && tr.docChanged) return { from: tr.changes.mapPos(value.from, -1), to: tr.changes.mapPos(value.to, 1) };
    return value;
  },
});

const dropDomSelection = EditorState.transactionFilter.of(tr => {
  const active = tr.startState.field(activeTableField, false);
  if (!active || tr.docChanged || !tr.selection || tr.annotation(cellCommit) || tr.effects.length) return tr;
  const inside = tr.selection.ranges.every(r => r.from >= active.from && r.to <= active.to);
  return inside ? {} : tr;
});

// ── 定位 ─────────────────────────────────────────────────────────────

/** 单元格在主文档中的位置；缺的单元格（表体行比表头短）exists 为 false，区间是行尾的插入点 */
export interface CellLocation {
  readonly wrap: TableDom;
  readonly host: HTMLElement;
  readonly tableFrom: number;
  readonly tableTo: number;
  readonly from: number;
  readonly to: number;
  readonly src: string;
  readonly exists: boolean;
}

export function locateCell(main: EditorView, wrap: TableDom, row: number, col: number): CellLocation | null {
  const t = wrap.tdTable;
  if (!t || !wrap.isConnected) return null;
  const r = tableRows(t.model)[row];
  const host = cellElement(wrap, row, col);
  if (!r || col >= t.model.cols || !host) return null;
  const tableFrom = main.posAtDOM(wrap), tableTo = tableFrom + t.src.length;
  const c = r.cells[col];
  return c
    ? { wrap, host, tableFrom, tableTo, from: tableFrom + c.from, to: tableFrom + c.to, src: t.src.slice(c.from, c.to), exists: true }
    : { wrap, host, tableFrom, tableTo, from: tableFrom + r.to, to: tableFrom + r.to, src: '', exists: false };
}

/** 主视图里起点为 pos 的表格外框（只在已渲染的 DOM 里找） */
export function findWrap(main: EditorView, pos: number): TableDom | null {
  for (const w of main.contentDOM.querySelectorAll<TableDom>(`.${TABLE_WRAP_CLASS}`)) {
    if (w.tdTable && main.posAtDOM(w) === pos) return w;
  }
  return null;
}

// ── 会话 ─────────────────────────────────────────────────────────────

const sessions = new WeakMap<EditorView, CellSession>();
const live = new Set<CellSession>();

/** 主视图当前的单元格会话 */
export const cellSession = (main: EditorView): CellSession | null => sessions.get(main) ?? null;

/** 离开单元格时主视图光标的去处 */
export enum Leave {
  /** 不动主视图的焦点与选区（失焦、点到别处） */
  Stay,
  /** 聚焦主视图，选区保持（撤销把选区带到了格外） */
  KeepSelection,
  /** 表格前一行的行尾 */
  Before,
  /** 表格后一行的行首 */
  After,
  /** 表格末尾（Escape） */
  TableEnd,
}

export class CellSession {
  readonly nested: EditorView;
  host: HTMLElement;
  row: number;
  col: number;
  /** 表格起点（按主文档的变化映射；单元格元素被重建时据此找回表格） */
  private tableFrom: number;
  private tableTo: number;
  /** 嵌套文档在主文档里的起点：会话期间 nested.doc ≡ main.sliceDoc(start, start + base.length)（提交之间） */
  private start: number;
  private base: string;
  /** 有未提交的改动；before 是第一笔未提交改动之前的嵌套选区 */
  private dirty = false;
  private before: EditorSelection | null = null;
  private userEvent = 'input.type';
  closed = false;

  constructor(readonly main: EditorView, loc: CellLocation, row: number, col: number, anchor: number, head: number) {
    this.row = row;
    this.col = col;
    this.host = loc.host;
    this.tableFrom = loc.tableFrom;
    this.tableTo = loc.tableTo;
    this.start = loc.from;
    this.base = loc.src;
    this.nested = new EditorView({
      state: EditorState.create({ doc: loc.src, selection: EditorSelection.single(anchor, head), extensions: nestedExtensions(this) }),
      dispatchTransactions: (trs, view) => this.onNested(trs, view),
    });
    this.nested.dom.addEventListener('focusout', () => setTimeout(() => this.checkBlur(), 0));
    // 组字结束后的最后一笔文本事务可能仍被视为组字中：稍后在非组字状态下补一次提交
    this.nested.dom.addEventListener('compositionend', () => setTimeout(() => this.commit(), 20));
    this.mount(loc.host, false);
    live.add(this);
  }

  get text() { return this.nested.state.doc.toString(); }

  /** 把嵌套视图放进单元格元素（换下静态渲染） */
  private mount(host: HTMLElement, refocus: boolean) {
    const old = this.host;
    if (old !== host && old.isConnected && old.classList.contains(ACTIVE_CELL_CLASS)) {
      old.classList.remove(ACTIVE_CELL_CLASS);
      renderCell(old, this.base, this.main);
    }
    host.replaceChildren(this.nested.dom);
    host.classList.add(ACTIVE_CELL_CLASS);
    this.host = host;
    if (refocus) this.nested.focus();
  }

  select(anchor: number, head = anchor) {
    const len = this.nested.state.doc.length;
    this.nested.dispatch({ selection: EditorSelection.single(Math.min(anchor, len), Math.min(head, len)), scrollIntoView: true });
  }

  private onNested(trs: readonly Transaction[], view: EditorView) {
    view.update(trs);
    let changed = false;
    for (const tr of trs) {
      if (!tr.docChanged || tr.annotation(fromMain)) continue;
      if (!this.dirty && !changed) this.before = tr.startState.selection;
      changed = true;
      const ev = tr.annotation(Transaction.userEvent);
      // 组字的结果按普通输入进主文档的历史（与正文里输入法打字的撤销粒度相同）
      if (ev) this.userEvent = ev.startsWith('input.type') ? 'input.type' : ev;
    }
    if (!changed) return;
    this.dirty = true;
    if (!view.composing) this.commit();
  }

  /** 把嵌套文档的改动提交到主文档。组字期间不提交（force 除外：会话结束时）。 */
  commit(force = false) {
    if (!this.dirty || (this.closed && !force) || (this.nested.composing && !force)) return;
    // 组字里输入的竖线、换行没经过输入过滤器，这里补上转义
    const fixes = cellSourceFixes(this.text);
    if (fixes.length) this.nested.dispatch({ changes: fixes, annotations: fromMain.of(true) });
    this.dirty = false;
    const text = this.text;
    const main = this.main;
    const loc = this.locate();
    if (!loc) { this.close(Leave.Stay); return; }
    if (loc.exists && main.state.sliceDoc(this.start, this.start + this.base.length) !== this.base) {
      // 主文档里该格已不是上次同步的文字（不应发生）：以主文档为准重新载入
      this.reload(loc, false);
      return;
    }
    let change, cellStart;
    if (loc.exists) {
      change = minimalChange(this.base, text, this.start);
      cellStart = this.start;
    } else {
      const t = loc.wrap.tdTable!;
      const c = cellReplace((f, to) => t.src.slice(f, to), t.model, this.row, this.col, text);
      change = c && { from: c.from + loc.tableFrom, to: c.to + loc.tableFrom, insert: c.insert };
      // 补齐的单元格写成 ` 文本 |`：文本在插入串末尾的 ` |` 之前
      cellStart = c ? c.from + loc.tableFrom + c.insert.length - text.length - 2 : this.start;
    }
    const before = this.before;
    this.before = null;
    if (!change) return;
    if (before && loc.exists) {
      // 改动之前的选区先落到主文档：撤销这一步时光标回到这里
      const want = shiftSelection(before, this.start);
      if (!main.state.selection.eq(want)) main.dispatch({ selection: want, annotations: cellCommit.of(true) });
    }
    main.dispatch({
      changes: change,
      selection: shiftSelection(this.nested.state.selection, cellStart),
      userEvent: this.userEvent,
      annotations: cellCommit.of(true),
    });
  }

  private locate(): CellLocation | null {
    const wrap = (this.host.isConnected ? this.host.closest(`.${TABLE_WRAP_CLASS}`) : findWrap(this.main, this.tableFrom)) as TableDom | null;
    return wrap ? locateCell(this.main, wrap, this.row, this.col) : null;
  }

  /** 主视图更新之后（表格 widget 的 DOM 已更新）；changes 为本次更新的文档变化，没有时为 null */
  onMainUpdate(changes: ChangeDesc | null, undoRedo: boolean) {
    if (changes) {
      // 在格首输入时起点不动（assoc -1）
      this.start = changes.mapPos(this.start, -1);
      this.tableFrom = changes.mapPos(this.tableFrom, -1);
    }
    if (!changes && this.host.isConnected) return;
    const loc = this.locate();
    if (!loc) {
      // 表格或这个单元格已经不在了（撤销删掉了行、表格滚出视口被回收、切到源码模式）
      this.close(undoRedo ? Leave.KeepSelection : Leave.Stay);
      return;
    }
    this.tableFrom = loc.tableFrom;
    this.tableTo = loc.tableTo;
    if (loc.host !== this.host) this.mount(loc.host, true);
    const sel = this.main.state.selection.main;
    if (undoRedo && (sel.from < loc.from || sel.to > loc.to)) {
      // 撤销的是格外的改动：跟着选区回到主视图
      this.close(Leave.KeepSelection);
      return;
    }
    if (loc.src === this.text) {
      this.base = loc.src;
      this.start = loc.from;
      if (undoRedo) this.select(sel.anchor - loc.from, sel.head - loc.from);
      return;
    }
    this.reload(loc, undoRedo);
  }


  /** 按表格模型重新载入该格源码（外部变化） */
  private reload(loc: CellLocation, followSelection: boolean) {
    const text = this.text;
    this.base = loc.src;
    this.start = loc.from;
    this.dirty = false;
    this.before = null;
    const sel = this.main.state.selection.main;
    const inside = sel.from >= loc.from && sel.to <= loc.to;
    const change = minimalChange(text, loc.src, 0);
    this.nested.dispatch({
      changes: change ?? undefined,
      selection: followSelection && inside ? EditorSelection.single(sel.anchor - loc.from, sel.head - loc.from) : undefined,
      annotations: fromMain.of(true),
    });
  }

  private checkBlur() {
    if (this.closed || this.nested.hasFocus) return;
    // 切到别的窗口时保持会话：回来时焦点仍在单元格里
    if (!this.main.dom.ownerDocument.hasFocus()) return;
    this.close(Leave.Stay);
  }

  /** 宿主单元格所在的表格 DOM 被销毁：本次更新里没被重新挂上时再找一次，找不到就结束 */
  detached() {
    queueMicrotask(() => {
      if (!this.closed && !this.host.isConnected) this.onMainUpdate(null, false);
    });
  }

  /**
   * 结束会话：提交未提交的改动，嵌套视图换回静态渲染，按 leave 处理主视图的焦点与选区。
   * handoff 为真时焦点马上交给下一个单元格（openCell），不在中间把焦点还给主视图。
   */
  close(leave: Leave, handoff = false) {
    if (this.closed) return;
    this.commit(true);
    this.closed = true;
    live.delete(this);
    if (sessions.get(this.main) === this) sessions.delete(this.main);
    const host = this.host;
    const hadFocus = this.nested.hasFocus;
    this.nested.destroy();
    host.classList.remove(ACTIVE_CELL_CLASS);
    if (host.isConnected) {
      const wrap = host.closest(`.${TABLE_WRAP_CLASS}`) as TableDom | null;
      const loc = wrap && locateCell(this.main, wrap, this.row, this.col);
      renderCell(host, loc ? loc.src : this.base, this.main);
    }
    const main = this.main;
    // 主视图已销毁（换文档、测试结束）时不再派发
    if (!main.dom.isConnected) return;
    const doc = main.state.doc;
    let anchor: number | null = null;
    if (leave === Leave.Before) anchor = this.tableFrom > 0 ? this.tableFrom - 1 : this.tableFrom;
    else if (leave === Leave.After) anchor = this.tableTo < doc.length ? this.tableTo + 1 : this.tableTo;
    else if (leave === Leave.TableEnd) anchor = Math.min(this.tableTo, doc.length);
    const focus = leave !== Leave.Stay;
    if (focus) main.focus();
    main.dispatch({
      effects: setActiveTable.of(null),
      selection: anchor === null ? undefined : { anchor },
      scrollIntoView: anchor !== null,
      annotations: cellCommit.of(true),
    });
    // 焦点原在格内、会话因表格消失而结束（切源码模式等）：焦点不能丢在 body 上
    if (!focus && !handoff && hadFocus) main.focus();
  }

  /** 按行优先顺序移到相邻单元格（Tab / Shift+Tab、左右方向键出格） */
  moveBy(delta: number, anchor: number): boolean {
    const loc = this.locate();
    const t = loc?.wrap.tdTable;
    if (!loc || !t) return false;
    const cols = t.model.cols, rows = tableRows(t.model).length;
    const i = this.row * cols + this.col + delta;
    if (i < 0 || i >= rows * cols) return false;
    openCell(this.main, loc.wrap, Math.floor(i / cols), i % cols, anchor);
    return true;
  }

  /** 移到同一列的上一行或下一行 */
  moveRow(delta: number, anchor: number): boolean {
    const loc = this.locate();
    const t = loc?.wrap.tdTable;
    if (!loc || !t) return false;
    const r = this.row + delta;
    if (r < 0 || r >= tableRows(t.model).length) return false;
    openCell(this.main, loc.wrap, r, this.col, anchor);
    return true;
  }
}

function shiftSelection(sel: EditorSelection, by: number): EditorSelection {
  return EditorSelection.create(sel.ranges.map(r => EditorSelection.range(r.anchor + by, r.head + by)), sel.mainIndex);
}

/**
 * 打开 (row, col) 单元格的嵌套视图并聚焦，光标放在 anchor..head（超出格长的按格末尾算，Infinity 即格末）。
 * 已有会话时先结束它；同一个单元格只移动光标。
 */
export function openCell(main: EditorView, wrap: TableDom, row: number, col: number, anchor: number, head = anchor): CellSession | null {
  const prev = sessions.get(main);
  if (prev && !prev.closed) {
    if (prev.host === cellElement(wrap, row, col)) {
      prev.select(anchor, head);
      prev.nested.focus();
      return prev;
    }
    const sameTable = prev.host.closest(`.${TABLE_WRAP_CLASS}`) === wrap;
    prev.close(Leave.Stay, true);
    // 前一个会话的最后一次提交可能让这张表重建（补齐了缺的单元格）：它的宿主已被挪进新表格
    if (sameTable && !wrap.isConnected) wrap = (prev.host.closest(`.${TABLE_WRAP_CLASS}`) as TableDom | null) ?? wrap;
  }
  const loc = locateCell(main, wrap, row, col);
  if (!loc) return null;
  const len = loc.src.length;
  const a = Math.max(0, Math.min(anchor, len)), h = Math.max(0, Math.min(head, len));
  const s = new CellSession(main, loc, row, col, a, h);
  sessions.set(main, s);
  main.dispatch({
    selection: loc.exists ? EditorSelection.single(loc.from + a, loc.from + h) : EditorSelection.cursor(loc.from),
    effects: setActiveTable.of({ from: loc.tableFrom, to: loc.tableTo }),
    annotations: cellCommit.of(true),
  });
  s.nested.focus();
  return s;
}

/** 表格 widget 的 DOM 被销毁（TableWidget.destroy）：其中的会话等本次更新结束后核对 */
export function tableDomDestroyed(dom: HTMLElement) {
  for (const s of live) if (dom.contains(s.host)) s.detached();
}

// ── 嵌套视图的配置 ───────────────────────────────────────────────────

/** 单元格源码里不能出现未转义的竖线与换行：键入、粘贴、拖放的文字就地修正（组字除外，提交时再修） */
const cellInputFilter = EditorState.transactionFilter.of(tr => {
  if (!tr.docChanged || tr.annotation(fromMain) || tr.isUserEvent('input.type.compose')) return tr;
  const fixes = cellSourceFixes(tr.newDoc.toString());
  return fixes.length ? [tr, { changes: fixes, sequential: true }] : tr;
});

function atVisualEdge(view: EditorView, forward: boolean): boolean {
  const sel = view.state.selection.main;
  const next = view.moveVertically(sel, forward);
  if (next.head === sel.head) return true;
  const a = view.coordsAtPos(sel.head), b = view.coordsAtPos(next.head);
  // 没有布局（测试环境）时按单行处理
  return !a || !b || Math.abs(a.top - b.top) < 1;
}

function cellKeymap(s: CellSession): KeyBinding[] {
  const edge = (f: (view: EditorView, head: number, len: number) => boolean) => (view: EditorView) => {
    const sel = view.state.selection.main;
    return sel.empty && f(view, sel.head, view.state.doc.length);
  };
  const leave = (to: Leave) => { s.close(to); return true; };
  return [
    // Tab 跳格：光标放在格末（C1 的行为）；首尾格再按不出表格
    { key: 'Tab', run: () => { s.moveBy(1, Infinity); return true; }, shift: () => { s.moveBy(-1, Infinity); return true; } },
    { key: 'Escape', run: () => leave(Leave.TableEnd) },
    // 单元格里不能换行
    { key: 'Enter', run: () => true, shift: () => true },
    { key: 'Mod-Enter', run: () => true },
    // 撤销、重做只有主视图一份历史
    { key: 'Mod-z', run: () => { s.commit(true); undo(s.main); return true; }, preventDefault: true },
    { key: 'Mod-y', run: () => { s.commit(true); redo(s.main); return true; }, preventDefault: true },
    { key: 'Mod-Shift-z', run: () => { s.commit(true); redo(s.main); return true; }, preventDefault: true },
    // 方向键在格边上时出格：左右按行优先顺序到相邻格，出了首尾格离开表格；上下到同列的上下行，出了首尾行离开表格
    { key: 'ArrowLeft', run: edge((_v, head) => head === 0 && (s.moveBy(-1, Infinity) || leave(Leave.Before))) },
    { key: 'ArrowRight', run: edge((_v, head, len) => head === len && (s.moveBy(1, 0) || leave(Leave.After))) },
    { key: 'ArrowUp', run: edge((v, head) => atVisualEdge(v, false) && (s.moveRow(-1, head) || leave(Leave.Before))) },
    { key: 'ArrowDown', run: edge((v, head) => atVisualEdge(v, true) && (s.moveRow(1, head) || leave(Leave.After))) },
  ];
}

function nestedExtensions(s: CellSession): Extension[] {
  const main = s.main;
  const open = main.state.facet(openLinkHandler);
  return [
    cellLanguage(),
    tableCellReveal.of(true),
    revealState,
    inlineRevealExtension,
    EditorView.lineWrapping,
    EditorView.editorAttributes.of({ class: 'cm-td-cell-editor' }),
    // 脚注上标的编号、图片的相对路径、Ctrl+单击打开链接都跟主文档走
    footnoteNumberSource.of(() => main.state.facet(footnoteNumberSource)?.(main.state) ?? new Map()),
    open ? openLinkHandler.of(open) : [],
    documentLocation.of(main.state.facet(documentLocation)),
    EditorView.contentAttributes.of({
      spellcheck: main.contentDOM.getAttribute('spellcheck') ?? 'false',
      autocorrect: 'off',
      autocapitalize: 'off',
    }),
    cellInputFilter,
    Prec.highest(keymap.of(cellKeymap(s))),
    keymap.of(defaultKeymap),
  ];
}

// ── 主视图一侧 ───────────────────────────────────────────────────────

/** 主视图的扩展：会话的范围字段、丢掉嵌套 DOM 选区引起的主视图选区事务、每次更新后核对会话 */
export const tableCellEditing: Extension = [
  activeTableField,
  dropDomSelection,
  EditorView.updateListener.of(u => {
    const s = sessions.get(u.view);
    if (!s || s.closed) return;
    s.onMainUpdate(u.docChanged ? u.changes : null, u.transactions.some(tr => tr.isUserEvent('undo') || tr.isUserEvent('redo')));
  }),
];
