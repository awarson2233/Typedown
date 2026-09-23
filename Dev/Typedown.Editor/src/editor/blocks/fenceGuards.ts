import { EditorSelection, EditorState, Prec, type Extension, type Text } from '@codemirror/state';
import { keymap, type Command } from '@codemirror/view';
import { completionStatus } from '@codemirror/autocomplete';
import { isComposeTransaction } from '../decorations/revealState';
import { blockField } from '../widgets/blockField';
import { FencePrefixWidget, FenceRole, FenceWidget } from '../widgets/blockWidgets';

/**
 * 收起的围栏行的光标与删除保护。块组件把围栏行整行替换（`$$`、`---`、闭合的 ```）或把 ``` 前缀替换掉（代码块首行只露语言名），
 * 这些位置在屏幕上看不见，光标停在那里时输入会写进围栏、Backspace / Delete 会把相邻行并进围栏，块就坏了。所以：
 * - 选区规范化：空光标落进隐藏范围时按移动方向挪出去（向后移入 → 范围之后，向前移入 → 范围之前；单击落在收起的围栏上 → 块内）；
 *   有选区（选择范围、全选）时不动，复制出来的仍是完整源码；
 * - 按键保护：Backspace / Delete 要删的字符在隐藏范围里、或是连接围栏行与相邻行的换行时不执行
 *   （闭围栏之后的行首按 Backspace 改为把光标移进块尾）；代码块语言行里按 Enter 跳到第一行代码。
 */

export enum GuardKind {
  /** 收起的开围栏行 */
  OpenLine,
  /** 收起的闭围栏行 */
  CloseLine,
  /** 代码块首行里隐藏的 ``` 前缀（语言名在它后面，可编辑） */
  Prefix,
}

export interface HiddenRange {
  readonly kind: GuardKind;
  /** 隐藏的字符范围 [from, to)；整行收起时 to 是行尾 */
  readonly from: number;
  readonly to: number;
}

/** pos 附近（含两端）的隐藏范围 */
export function hiddenRangesAround(state: EditorState, from: number, to = from): HiddenRange[] {
  const out: HiddenRange[] = [];
  const set = state.field(blockField, false)?.decos;
  if (!set) return out;
  set.between(Math.max(0, from - 1), Math.min(state.doc.length, to + 1), (f, t, d) => {
    const w = d.spec.widget;
    if (w instanceof FenceWidget && f < t) out.push({ kind: w.role === FenceRole.Head ? GuardKind.OpenLine : GuardKind.CloseLine, from: f, to: t });
    else if (w instanceof FencePrefixWidget) out.push({ kind: GuardKind.Prefix, from: f, to: t });
  });
  return out;
}

/**
 * 空光标不能停的位置：整行收起时是该行的 [from, to]（含行尾），``` 前缀是 [from, to)（to 是语言名起点，可停）。
 * 返回挪出后的位置；不在任何隐藏范围里时原样返回。
 */
export function normalizeCursor(doc: Text, ranges: readonly HiddenRange[], prev: number, head: number, pointer: boolean): number {
  for (const r of ranges) {
    const lastForbidden = r.kind === GuardKind.Prefix ? r.to - 1 : r.to;
    if (head < r.from || head > lastForbidden) continue;
    const after = lastForbidden + 1 <= doc.length ? lastForbidden + 1 : -1;
    const before = r.from > 0 ? r.from - 1 : -1;
    // 单击：开围栏 / 前缀进块内（向后），闭围栏回到块尾（向前）
    const forward = pointer ? r.kind !== GuardKind.CloseLine : head > prev;
    const target = forward ? (after >= 0 ? after : before) : (before >= 0 ? before : after);
    return target < 0 ? head : target;
  }
  return head;
}

const cursorFilter = EditorState.transactionFilter.of(tr => {
  if (!tr.selection || tr.docChanged || isComposeTransaction(tr)) return tr;
  const sel = tr.selection;
  if (!sel.ranges.some(r => r.empty)) return tr;
  const state = tr.state;
  const prev = tr.startState.selection;
  const pointer = tr.isUserEvent('select.pointer');
  let changed = false;
  const ranges = sel.ranges.map((r, i) => {
    if (!r.empty) return r;
    const head = normalizeCursor(state.doc, hiddenRangesAround(state, r.head), (prev.ranges[i] ?? prev.main).head, r.head, pointer);
    if (head === r.head) return r;
    changed = true;
    return EditorSelection.cursor(head, head < r.head ? 1 : -1);
  });
  if (!changed) return tr;
  return [tr, { selection: EditorSelection.create(ranges, sel.mainIndex), sequential: true }];
});

/** 删除 [at, at + 1) 这个字符会不会破坏围栏；是闭围栏之后的换行时给出应当移去的位置 */
export function protectedChar(state: EditorState, at: number): { blocked: boolean; moveTo: number } {
  const none = { blocked: false, moveTo: -1 };
  const doc = state.doc;
  if (at < 0 || at >= doc.length) return none;
  // 这个字符所在行与下一行上的隐藏范围（语言行的前缀可能离行尾的换行很远）
  for (const r of hiddenRangesAround(state, doc.lineAt(at).from, doc.lineAt(Math.min(at + 1, doc.length)).to)) {
    const line = doc.lineAt(r.from);
    // 隐藏的字符本身
    if (at >= r.from && at < r.to) return { blocked: true, moveTo: -1 };
    // 围栏行两端的换行：行首之前那个、行尾那个（前缀所在的语言行也一样）
    if (at === line.from - 1 || at === line.to) {
      const moveTo = r.kind === GuardKind.CloseLine && at === line.to ? line.from - 1 : -1;
      return { blocked: true, moveTo };
    }
  }
  return none;
}

const guardDelete = (backward: boolean): Command => view => {
  const { state } = view;
  const sel = state.selection.main;
  if (!sel.empty || state.selection.ranges.length > 1) return false;
  const at = backward ? sel.head - 1 : sel.head;
  const p = protectedChar(state, at);
  if (!p.blocked) return false;
  if (p.moveTo >= 0) view.dispatch({ selection: { anchor: p.moveTo }, scrollIntoView: true, userEvent: 'select' });
  return true;
};

/** 代码块语言行里按 Enter：不拆行，跳到第一行代码 */
const enterInLanguageLine: Command = view => {
  const { state } = view;
  const sel = state.selection.main;
  if (!sel.empty || completionStatus(state) === 'active') return false;
  const line = state.doc.lineAt(sel.head);
  const prefix = hiddenRangesAround(state, line.from).find(r => r.kind === GuardKind.Prefix && r.from === line.from);
  if (!prefix || line.number >= state.doc.lines) return false;
  view.dispatch({ selection: { anchor: state.doc.line(line.number + 1).from }, scrollIntoView: true, userEvent: 'select' });
  return true;
};

export const fenceGuards: Extension = [
  cursorFilter,
  Prec.highest(keymap.of([
    { key: 'Backspace', run: guardDelete(true) },
    { key: 'Delete', run: guardDelete(false) },
    { key: 'Mod-Backspace', run: guardDelete(true) },
    { key: 'Mod-Delete', run: guardDelete(false) },
    { key: 'Enter', run: enterInLanguageLine },
  ])),
];
