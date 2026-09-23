import { StateEffect, StateField, type Transaction } from '@codemirror/state';
import { EditorView, ViewPlugin } from '@codemirror/view';

/**
 * 显形的两个「暂停」条件，行内显形 ViewPlugin 与块组件 StateField 共用：
 * - 鼠标冻结：按下到松开后约 100 ms 内不随选区重算显形，否则显形引起的横向位移会把单击变成拖选
 *   （atomic-editor inline-preview.ts 的做法）。
 * - 组字：input.type.compose 事务期间只映射装饰（SilverBullet util.ts 的做法），组字结束后补一次重算。
 */

export const setRevealFrozen = StateEffect.define<boolean>();
/** 要求显形层按当前选区重算一次（解冻、组字结束时发出） */
export const refreshReveal = StateEffect.define<null>();

export const revealFrozen = StateField.define<boolean>({
  create: () => false,
  update(value, tr) {
    for (const e of tr.effects) if (e.is(setRevealFrozen)) value = e.value;
    return value;
  },
});

export const isComposeTransaction = (tr: Transaction) => tr.isUserEvent('input.type.compose');
export const hasRefresh = (tr: Transaction) => tr.effects.some(e => e.is(refreshReveal) || (e.is(setRevealFrozen) && !e.value));

/** 冻结计时：与 DOM 解耦，便于单测。 */
export class FreezeTimer {
  private timer: unknown = null;
  frozen = false;
  constructor(
    private readonly onChange: (frozen: boolean) => void,
    private readonly delay = 100,
    private readonly schedule: (f: () => void, ms: number) => unknown = (f, ms) => setTimeout(f, ms),
    private readonly cancel: (t: unknown) => void = t => clearTimeout(t as ReturnType<typeof setTimeout>),
  ) {}
  down() {
    if (this.timer !== null) { this.cancel(this.timer); this.timer = null; }
    if (!this.frozen) { this.frozen = true; this.onChange(true); }
  }
  up() {
    if (!this.frozen) return;
    if (this.timer !== null) this.cancel(this.timer);
    this.timer = this.schedule(() => {
      this.timer = null;
      this.frozen = false;
      this.onChange(false);
    }, this.delay);
  }
}

const mouseFreeze = ViewPlugin.fromClass(class {
  timer: FreezeTimer;
  onUp = () => this.timer.up();
  constructor(readonly view: EditorView) {
    this.timer = new FreezeTimer(frozen => view.dispatch({ effects: setRevealFrozen.of(frozen) }));
    view.dom.ownerDocument.addEventListener('mouseup', this.onUp, true);
  }
  destroy() { this.view.dom.ownerDocument.removeEventListener('mouseup', this.onUp, true); }
}, {
  eventObservers: {
    mousedown(e) { if (e.button === 0) this.timer.down(); },
    compositionend() {
      const view = this.view;
      // 组字结束后的最后一次文本事务可能仍带 compose 标记；稍后在非组字状态下补一次重算
      setTimeout(() => { if (!view.composing) view.dispatch({ effects: refreshReveal.of(null) }); }, 20);
    },
  },
});

export const revealState = [revealFrozen, mouseFreeze];
