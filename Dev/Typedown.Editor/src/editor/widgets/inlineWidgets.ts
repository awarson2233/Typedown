import { WidgetType, type EditorView } from '@codemirror/view';
import { loadKatex, renderMathSync } from '../../renderers/katex';
import type { InlineWidgetSpec } from '../decorations/inlineSpecs';

const BULLETS = ['•', '◦', '▪'];

export class BulletWidget extends WidgetType {
  constructor(readonly depth: number) { super(); }
  eq(o: BulletWidget) { return o.depth === this.depth; }
  toDOM() {
    const s = document.createElement('span');
    s.className = 'cm-td-bullet';
    s.textContent = BULLETS[(this.depth - 1) % BULLETS.length];
    return s;
  }
  ignoreEvent() { return false; }
}

export class OrderedWidget extends WidgetType {
  constructor(readonly text: string) { super(); }
  eq(o: OrderedWidget) { return o.text === this.text; }
  toDOM() {
    const s = document.createElement('span');
    s.className = 'cm-td-ordered';
    s.textContent = this.text;
    return s;
  }
  ignoreEvent() { return false; }
}

/** 任务框：点击只把 `[ ]` 与 `[x]` 中间那一个字符换掉（最小源码替换）。 */
export class TaskWidget extends WidgetType {
  constructor(readonly checked: boolean) { super(); }
  eq(o: TaskWidget) { return o.checked === this.checked; }
  toDOM(view: EditorView) {
    const box = document.createElement('input');
    box.type = 'checkbox';
    box.className = 'cm-td-task';
    box.checked = this.checked;
    box.addEventListener('mousedown', e => e.preventDefault());
    box.addEventListener('click', e => {
      e.preventDefault();
      const pos = view.posAtDOM(box);
      const text = view.state.sliceDoc(pos, pos + 12);
      const m = /\[( |x|X)\]/.exec(text);
      if (!m) return;
      const at = pos + m.index + 1;
      view.dispatch({ changes: { from: at, to: at + 1, insert: this.checked ? ' ' : 'x' }, userEvent: 'input.task' });
    });
    return box;
  }
  ignoreEvent(e: Event) { return e.type === 'mousedown' || e.type === 'click'; }
}

/** 行内公式渲染态；KaTeX 未加载时先以源码占位，加载后原地填充（行内元素，高度变化很小）。 */
export class InlineMathWidget extends WidgetType {
  constructor(readonly src: string) { super(); }
  eq(o: InlineMathWidget) { return o.src === this.src; }
  toDOM() {
    const s = document.createElement('span');
    s.className = 'cm-td-math-inline';
    if (!renderMathSync(s, this.src, false)) {
      s.textContent = this.src;
      s.classList.add('cm-td-pending');
      loadKatex().then(() => { s.classList.remove('cm-td-pending'); renderMathSync(s, this.src, false); }, () => undefined);
    }
    return s;
  }
  ignoreEvent() { return false; }
}

export function inlineWidget(spec: InlineWidgetSpec): WidgetType {
  switch (spec.type) {
    case 'bullet': return new BulletWidget(spec.depth);
    case 'ordered': return new OrderedWidget(spec.text);
    case 'task': return new TaskWidget(spec.checked);
    case 'inline-math': return new InlineMathWidget(spec.src);
  }
}
