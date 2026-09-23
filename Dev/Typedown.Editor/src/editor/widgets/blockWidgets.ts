import { WidgetType, type EditorView } from '@codemirror/view';
import { renderMath, renderMathSync } from '../../renderers/katex';
import { mermaidCached, renderMermaid } from '../../renderers/mermaid';
import { cachedHeight, heightKey, measureInto } from './heightCache';

export type DiagramKind = 'math' | 'mermaid';

/**
 * 公式块与 mermaid 的渲染物。两态模型（wysiwyg-engine-survey.md 第 1 节）：
 * - 渲染态（preview = false）：整块源码被这个 widget 替换，单击进入编辑态；
 * - 编辑态（preview = true）：源码行照常显示，这个 widget 作为块后预览。
 * 渲染在 toDOM 里发生，而 CM6 只为视口内的 widget 调 toDOM，所以图表与公式天然是「进入视口才渲染」。
 */
export class DiagramWidget extends WidgetType {
  readonly key: string;
  constructor(readonly kind: DiagramKind, readonly src: string, readonly preview: boolean) {
    super();
    this.key = heightKey(kind, src);
  }

  eq(o: DiagramWidget) {
    return o.kind === this.kind && o.src === this.src && o.preview === this.preview;
  }

  get estimatedHeight() {
    const h = cachedHeight(this.key);
    if (h !== undefined) return h;
    return this.kind === 'mermaid' ? 240 : 24 + 22 * Math.max(1, this.src.split('\n').length);
  }

  toDOM(view: EditorView) {
    const wrap = document.createElement('div');
    wrap.className = `cm-td-block cm-td-${this.kind}${this.preview ? ' cm-td-preview' : ''}`;
    const known = cachedHeight(this.key);
    if (known !== undefined) wrap.style.minHeight = known + 'px';
    const body = document.createElement('div');
    body.className = 'cm-td-block-body';
    wrap.appendChild(body);
    const done = () => {
      wrap.style.minHeight = '';
      measureInto(this.key, wrap);
      view.requestMeasure();
    };
    if (this.kind === 'math') {
      if (renderMathSync(body, this.src, true)) measureInto(this.key, wrap);
      else { body.textContent = '…'; renderMath(body, this.src, true).then(done, () => undefined); }
    } else {
      const hit = mermaidCached(this.src);
      if (hit !== undefined) { body.innerHTML = hit; measureInto(this.key, wrap); }
      else { body.textContent = '…'; renderMermaid(body, this.src).then(done, () => undefined); }
    }
    if (!this.preview) {
      // 渲染态：单击把光标放进源码首行，块组件 StateField 随之切换到编辑态
      wrap.addEventListener('mousedown', e => {
        if (e.button !== 0) return;
        e.preventDefault();
        const pos = view.posAtDOM(wrap);
        view.dispatch({ selection: { anchor: pos }, userEvent: 'select.pointer' });
        view.focus();
      });
    }
    return wrap;
  }

  ignoreEvent(e: Event) {
    return !this.preview && e.type === 'mousedown';
  }
}
