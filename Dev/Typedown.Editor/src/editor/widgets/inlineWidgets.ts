import { WidgetType, type EditorView } from '@codemirror/view';
import { loadKatex, renderMathSync } from '../../renderers/katex';
import type { InlineWidgetSpec } from '../decorations/inlineSpecs';
import type { MarkerSpec } from '../decorations/lineStructure';
import { ImageWidget } from './imageWidget';

/** 与浏览器默认样式相同：第一层实心圆，第二层空心圆，更深为方块（旧编辑器用的就是 `<ul>` 的默认符号） */
const BULLET_STYLES = ['disc', 'circle', 'square'];

/**
 * 列表符号：宽度为 0 的行内块，里面放一个空的 `<ul>`/`<ol>` 列表项，让浏览器按 list-style-position: outside
 * 在内容起点左侧画出符号，字形、字号、位置与旧编辑器的真实列表完全相同。
 */
export class ListMarkerWidget extends WidgetType {
  constructor(readonly spec: Extract<MarkerSpec, { type: 'bullet' | 'ordered' }>) { super(); }
  eq(o: ListMarkerWidget) {
    const a = this.spec, b = o.spec;
    return a.type === b.type && a.shift === b.shift && (a.type === 'bullet' ? a.depth === (b as typeof a).depth : a.number === (b as typeof a).number);
  }
  toDOM() {
    const box = document.createElement('span');
    box.className = 'cm-td-marker';
    if (this.spec.shift) box.style.setProperty('--td-marker-shift', String(this.spec.shift));
    const list = document.createElement(this.spec.type === 'ordered' ? 'ol' : 'ul');
    list.className = 'cm-td-marker-list';
    if (this.spec.type === 'ordered') (list as HTMLOListElement).start = this.spec.number;
    else list.style.listStyleType = BULLET_STYLES[Math.min(this.spec.depth, BULLET_STYLES.length) - 1];
    list.appendChild(document.createElement('li'));
    box.appendChild(list);
    return box;
  }
  ignoreEvent() { return false; }
}

/** 任务框：点击只把 `[ ]` 与 `[x]` 中间那一个字符换掉（最小源码替换）。 */
export class TaskWidget extends WidgetType {
  constructor(readonly spec: Extract<MarkerSpec, { type: 'task' }>) { super(); }
  eq(o: TaskWidget) { return o.spec.checked === this.spec.checked && o.spec.offset === this.spec.offset && o.spec.shift === this.spec.shift; }
  toDOM(view: EditorView) {
    const box = document.createElement('span');
    box.className = 'cm-td-marker cm-td-marker-task';
    if (this.spec.shift) box.style.setProperty('--td-marker-shift', String(this.spec.shift));
    const input = document.createElement('input');
    input.type = 'checkbox';
    input.className = 'cm-td-task';
    input.checked = this.spec.checked;
    input.tabIndex = -1;
    input.addEventListener('mousedown', e => e.preventDefault());
    input.addEventListener('click', e => {
      e.preventDefault();
      toggleTask(view, view.posAtDOM(box) + this.spec.offset);
    });
    box.appendChild(input);
    return box;
  }
  ignoreEvent(e: Event) { return e.type === 'mousedown' || e.type === 'click'; }
}

/** 把 bracket（`[` 的位置）处的任务框翻转：`[ ]` → `[x]`，`[x]`/`[X]` → `[ ]`。位置不是任务框时不动。 */
export function toggleTask(view: EditorView, bracket: number): boolean {
  const text = view.state.sliceDoc(bracket, bracket + 3);
  const m = /^\[( |x|X)\]$/.exec(text);
  if (!m) return false;
  view.dispatch({ changes: { from: bracket + 1, to: bracket + 2, insert: m[1] === ' ' ? 'x' : ' ' }, userEvent: 'input.task' });
  return true;
}

/**
 * 行内公式。渲染态替换源码；preview 为显形态挂在源码前的浮出预览（旧编辑器 `.ag-math-render` 的弹出层）。
 * KaTeX 未加载时先以源码占位，加载后原地填充（行内元素，高度变化很小）。
 */
export class InlineMathWidget extends WidgetType {
  constructor(readonly src: string, readonly preview = false) { super(); }
  eq(o: InlineMathWidget) { return o.src === this.src && o.preview === this.preview; }
  toDOM() {
    const s = document.createElement('span');
    s.className = 'cm-td-math-inline';
    if (!renderMathSync(s, this.src, false)) {
      s.textContent = this.src;
      s.classList.add('cm-td-pending');
      loadKatex().then(() => { s.classList.remove('cm-td-pending'); renderMathSync(s, this.src, false); }, () => undefined);
    }
    if (!this.preview) return s;
    const anchor = document.createElement('span');
    anchor.className = 'cm-td-math-preview';
    anchor.setAttribute('aria-hidden', 'true');
    anchor.appendChild(s);
    return anchor;
  }
  ignoreEvent() { return false; }
}

export class EmojiWidget extends WidgetType {
  constructor(readonly char: string) { super(); }
  eq(o: EmojiWidget) { return o.char === this.char; }
  toDOM() {
    const s = document.createElement('span');
    s.className = 'cm-td-emoji';
    s.textContent = this.char;
    return s;
  }
  ignoreEvent() { return false; }
}

/** 脚注引用上标。悬停看脚注内容由宿主浮层负责，这里只渲染编号。 */
export class FootnoteRefWidget extends WidgetType {
  constructor(readonly label: string, readonly number: number | null) { super(); }
  eq(o: FootnoteRefWidget) { return o.label === this.label && o.number === this.number; }
  toDOM() {
    const s = document.createElement('sup');
    s.className = 'cm-td-footnote-ref';
    s.dataset.label = this.label;
    s.textContent = this.number !== null ? String(this.number) : this.label;
    return s;
  }
  ignoreEvent() { return false; }
}

/** 行内 `<br>` 的渲染态 */
export class HtmlBreakWidget extends WidgetType {
  eq() { return true; }
  toDOM() {
    const s = document.createElement('span');
    s.className = 'cm-td-html-br';
    s.appendChild(document.createElement('br'));
    return s;
  }
  ignoreEvent() { return false; }
}

/** preview：显形态插在源码旁的版本（目前只有行内公式区分两种形态） */
export function inlineWidget(spec: InlineWidgetSpec, preview = false): WidgetType {
  switch (spec.type) {
    case 'bullet': case 'ordered': return new ListMarkerWidget(spec);
    case 'task': return new TaskWidget(spec);
    case 'inline-math': return new InlineMathWidget(spec.src, preview);
    case 'image': return new ImageWidget({ src: spec.src, alt: spec.alt, title: spec.title });
    case 'emoji': return new EmojiWidget(spec.char);
    case 'footnote-ref': return new FootnoteRefWidget(spec.label, spec.number);
    case 'html-break': return new HtmlBreakWidget();
  }
}
