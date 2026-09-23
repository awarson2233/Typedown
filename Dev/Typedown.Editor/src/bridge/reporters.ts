import type { EditorState } from '@codemirror/state';
import type { EditorView, ViewUpdate } from '@codemirror/view';
import { redoDepth, undoDepth } from '@codemirror/commands';
import { syntaxTree } from '@codemirror/language';
import { headingIds, headingIndexAt, headingPlainText, outlineField, type Outline } from '../editor/state/outline';
import { blockContextAt, imageAt, inlineMarksAt } from '../editor/state/selectionInfo';
import { countStats, type Stats } from '../editor/state/stats';
import { Slot, type FrameQueue } from './frame';
import type { EventMap, EventType, OutlineItem, SelectionChangedPayload } from './protocol';

/**
 * 由编辑器状态派生、按帧合并上报的事件（docs/editor-protocol.md 第 6 节）：
 * history.changed（只在翻转时发）、selection.changed 与 selection.marks（与上次相同不发）、
 * outline.changed（标题层级、文本、顺序或当前标题变化时发）、stats.changed（空闲 300 ms 去抖，值不变不发）。
 * 计算都放在帧回调里做，事务里只登记脏标记，按键路径上不多花时间。
 */

/** selection.changed 的 text 只给单行、不超过这么多 UTF-16 码元的选区 */
export const SELECTION_TEXT_LIMIT = 200;

export interface ReporterOptions {
  emit: <T extends EventType>(t: T, p: EventMap[T]) => void;
  frames: FrameQueue;
  view: () => EditorView;
  /** 源码模式下 selection.changed 的 rich 为 null */
  sourceMode: () => boolean;
  /** 默认 300 ms */
  statsDelay?: number;
}

export class StateReporters {
  private history = { canUndo: false, canRedo: false };
  private selectionKey = '';
  private marksKey = '';
  /** doc.load 换了状态：大纲的 revision 从头计数，用代数区分 */
  private generation = 0;
  private outlineSeen = { generation: -1, revision: -1, current: -2 };
  private outlineItems: OutlineItem[] = [];
  private stats: Stats | null = null;
  private statsTimer: ReturnType<typeof setTimeout> | undefined;

  constructor(private readonly opts: ReporterOptions) {}

  /** 挂在 EditorView.updateListener 上 */
  onUpdate(u: ViewUpdate) {
    const f = this.opts.frames;
    if (u.transactions.length) f.schedule(Slot.History, this.historyTask);
    const reconfigured = u.transactions.some(tr => tr.reconfigured);
    // 语法树推进后，光标处的块与标记可能才第一次算得出来
    if (u.selectionSet || u.docChanged || reconfigured || syntaxTree(u.state) !== syntaxTree(u.startState)) {
      f.schedule(Slot.Selection, this.selectionTask);
      f.schedule(Slot.Marks, this.marksTask);
    }
    if (u.docChanged || u.selectionSet) f.schedule(Slot.Other, this.outlineTask);
    if (u.docChanged) this.scheduleStats();
  }

  /** doc.load 之后（setState 不触发 updateListener）：全部重新判定一次 */
  reset() {
    this.generation++;
    const f = this.opts.frames;
    f.schedule(Slot.History, this.historyTask);
    f.schedule(Slot.Selection, this.selectionTask);
    f.schedule(Slot.Marks, this.marksTask);
    f.schedule(Slot.Other, this.outlineTask);
    this.scheduleStats();
  }

  dispose() { clearTimeout(this.statsTimer); }

  private state(): EditorState { return this.opts.view().state; }

  private readonly historyTask = () => {
    const s = this.state();
    const canUndo = undoDepth(s) > 0, canRedo = redoDepth(s) > 0;
    if (canUndo === this.history.canUndo && canRedo === this.history.canRedo) return;
    this.history = { canUndo, canRedo };
    this.opts.emit('history.changed', { canUndo, canRedo });
  };

  private readonly selectionTask = () => {
    const p = selectionPayload(this.state(), this.opts.sourceMode());
    const key = JSON.stringify(p);
    if (key === this.selectionKey) return;
    this.selectionKey = key;
    this.opts.emit('selection.changed', p);
  };

  private readonly marksTask = () => {
    const s = this.state(), r = s.selection.main;
    const marks = inlineMarksAt(s, r.from, r.to);
    const key = marks.join(',');
    if (key === this.marksKey) return;
    this.marksKey = key;
    this.opts.emit('selection.marks', { marks });
  };

  private readonly outlineTask = () => {
    const s = this.state();
    const outline = s.field(outlineField, false);
    if (!outline) return;
    const seen = this.outlineSeen;
    const itemsChanged = seen.generation !== this.generation || seen.revision !== outline.revision;
    const current = headingIndexAt(outline, s.selection.main.head);
    if (!itemsChanged && current === seen.current) return;
    if (itemsChanged) this.outlineItems = outlineItems(outline);
    this.outlineSeen = { generation: this.generation, revision: outline.revision, current };
    this.opts.emit('outline.changed', { items: this.outlineItems, current: current >= 0 ? this.outlineItems[current] : null });
  };

  private scheduleStats() {
    clearTimeout(this.statsTimer);
    this.statsTimer = setTimeout(() => this.opts.frames.schedule(Slot.Other, this.statsTask), this.opts.statsDelay ?? 300);
  }

  private readonly statsTask = () => {
    const st = countStats(this.state().doc);
    if (this.stats && st.characters === this.stats.characters && st.words === this.stats.words) return;
    this.stats = st;
    this.opts.emit('stats.changed', st);
  };
}

export function selectionPayload(s: EditorState, sourceMode: boolean): SelectionChangedPayload {
  const r = s.selection.main;
  let text = '';
  if (!r.empty && r.to - r.from <= SELECTION_TEXT_LIMIT) {
    const t = s.sliceDoc(r.from, r.to);
    if (!t.includes('\n')) text = t;
  }
  return {
    hasText: !r.empty,
    text,
    rich: sourceMode ? null : { block: blockContextAt(s, r.from, r.to), selectedImage: imageAt(s, r.from, r.to) },
    anchor: r.anchor,
    head: r.head,
  };
}

export function outlineItems(outline: Outline): OutlineItem[] {
  const ids = headingIds(outline.headings);
  return outline.headings.map((h, i) => ({ id: ids[i], level: h.level, text: headingPlainText(h.text) }));
}
