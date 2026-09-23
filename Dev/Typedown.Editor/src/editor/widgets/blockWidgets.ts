import { EditorSelection } from '@codemirror/state';
import { EditorView, WidgetType } from '@codemirror/view';
import { renderBlockMathSync, loadKatex, escapeHtml } from '../../renderers/katex';
import { mermaidCached, renderMermaid } from '../../renderers/mermaid';
import { isInvisibleHtml, loadPurify, sanitizeHtmlSync } from '../../renderers/html';
import { UiText, uiText } from '../../shared/strings';
import { documentLocation } from '../state/documentLocation';
import { firstReference } from '../state/footnotes';
import { outlineField } from '../state/outline';
import { cachedHeight, heightKey, measureInto } from './heightCache';

export type DiagramKind = 'math' | 'mermaid';

/**
 * 单击渲染态的块：把光标放进源码，块组件 StateField 随之切到编辑态。
 * 带围栏的块（公式、mermaid）放到第一行内容行的行首（围栏行在编辑态是收起的），其余放到块首。
 */
function enterSource(view: EditorView, dom: HTMLElement, skipFence: boolean) {
  const pos = view.posAtDOM(dom);
  const doc = view.state.doc;
  const line = doc.lineAt(Math.min(pos, doc.length));
  const target = skipFence && line.number < doc.lines ? doc.line(line.number + 1).from : line.from;
  view.dispatch({ selection: { anchor: target }, userEvent: 'select.pointer', scrollIntoView: false });
  view.focus();
}

/** 渲染态整块替换的 widget 共用：左键按下进入源码（链接、按钮等交互元素照常） */
function clickToEdit(view: EditorView, dom: HTMLElement, skipFence: boolean, target: (e: MouseEvent) => boolean = () => true) {
  dom.addEventListener('mousedown', e => {
    if (e.button !== 0 || !target(e)) return;
    e.preventDefault();
    enterSource(view, dom, skipFence);
  });
}

interface DiagramDom extends HTMLElement { tdSrc?: string; tdTimer?: ReturnType<typeof setTimeout> }

/** mermaid 在编辑时的预览节流（渲染一次几十到几百毫秒，每键都渲染会卡） */
const MERMAID_PREVIEW_DELAY = 400;

/**
 * 公式块与 mermaid 的渲染物。两态模型（wysiwyg-engine-survey.md 第 1 节）：
 * - 渲染态（preview = false）：整块源码被这个 widget 替换，单击进入编辑态；
 * - 编辑态（preview = true）：源码行照常显示，这个 widget 挂在块后作为浮动预览（Muya 的 ag-container-preview：
 *   绝对定位浮在下文之上，不占文档高度，进出编辑态时下文不跳）。输入时 updateDOM 原地刷新：公式同步重渲染，
 *   mermaid 节流 400 ms，期间保留上一张图。
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
    if (this.preview) return 0;
    const h = cachedHeight(this.key);
    if (h !== undefined) return h;
    return this.kind === 'mermaid' ? 240 : 24 + 22 * Math.max(1, this.src.split('\n').length);
  }

  private render(view: EditorView, wrap: DiagramDom, body: HTMLElement) {
    wrap.tdSrc = this.src;
    const done = () => {
      if (this.preview) return;
      wrap.style.minHeight = '';
      measureInto(this.key, wrap);
      view.requestMeasure();
    };
    if (this.kind === 'math') {
      const text = { empty: uiText(UiText.EmptyMath), invalid: uiText(UiText.InvalidMath) };
      if (renderBlockMathSync(body, this.src, text)) { if (!this.preview) measureInto(this.key, wrap); }
      else {
        if (!body.childNodes.length) body.textContent = '…';
        loadKatex().then(() => { if (wrap.tdSrc === this.src) { renderBlockMathSync(body, this.src, text); done(); } }, () => undefined);
      }
      return;
    }
    if (!this.src.trim()) {
      body.innerHTML = `<div class="cm-td-render-empty">${escapeHtml(uiText(UiText.EmptyMermaid))}</div>`;
      return;
    }
    const hit = mermaidCached(this.src);
    if (hit !== undefined) { body.innerHTML = hit; if (!this.preview) measureInto(this.key, wrap); return; }
    if (!body.childNodes.length) body.textContent = '…';
    const target = document.createElement('div');
    renderMermaid(target, this.src).then(() => {
      if (wrap.tdSrc !== this.src) return; // 期间源码又变了
      body.replaceChildren(...Array.from(target.childNodes));
      done();
    }, () => undefined);
  }

  toDOM(view: EditorView) {
    const wrap = document.createElement('div') as DiagramDom;
    wrap.className = `cm-td-block cm-td-${this.kind}${this.preview ? ' cm-td-preview' : ''}`;
    wrap.contentEditable = 'false';
    const body = document.createElement('div');
    body.className = 'cm-td-block-body';
    if (this.preview) {
      // 预览浮层：外层 0 高，浮层绝对定位
      const float = document.createElement('div');
      float.className = 'cm-td-preview-float';
      float.appendChild(body);
      wrap.appendChild(float);
    } else {
      wrap.appendChild(body);
      const known = cachedHeight(this.key);
      if (known !== undefined) wrap.style.minHeight = known + 'px';
      clickToEdit(view, wrap, true);
    }
    this.render(view, wrap, body);
    return wrap;
  }

  updateDOM(dom: HTMLElement, view: EditorView): boolean {
    if (!this.preview || !dom.classList.contains('cm-td-preview') || !dom.classList.contains(`cm-td-${this.kind}`)) return false;
    const wrap = dom as DiagramDom;
    const body = wrap.querySelector<HTMLElement>('.cm-td-block-body');
    if (!body) return false;
    wrap.tdSrc = this.src;
    if (wrap.tdTimer !== undefined) clearTimeout(wrap.tdTimer);
    if (this.kind === 'math' || mermaidCached(this.src) !== undefined) this.render(view, wrap, body);
    else wrap.tdTimer = setTimeout(() => { wrap.tdTimer = undefined; if (wrap.tdSrc === this.src) this.render(view, wrap, body); }, MERMAID_PREVIEW_DELAY);
    return true;
  }

  ignoreEvent(e: Event) {
    return !this.preview && e.type === 'mousedown';
  }
}

/** 围栏的角色：收起的开围栏行（块首的内边距条）、收起的闭围栏行（块尾的内边距条） */
export enum FenceRole { Head, Foot }

/** 用收起围栏的块 */
export type FenceKind = 'code' | 'mermaid' | 'math' | 'frontmatter';

/**
 * 收起的围栏行（`$$`、`---`、闭合的 ``` 等）：整行被这个块级 widget 替换，只剩框的上下内边距。
 * 光标不会停在被替换的行里（blocks/fenceGuards.ts 的选区规范化与按键保护）。
 * active 只切换样式（Muya 在编辑中的块上下显示 `$$` / `---` 的伪元素），updateDOM 原地改类名。
 */
export class FenceWidget extends WidgetType {
  constructor(readonly role: FenceRole, readonly kind: FenceKind, readonly active: boolean) { super(); }
  eq(o: FenceWidget) { return o.role === this.role && o.kind === this.kind && o.active === this.active; }
  get estimatedHeight() { return this.kind === 'math' || this.kind === 'frontmatter' ? 8 : 14; }
  private cls() {
    return `cm-td-fence cm-td-fence-${this.role === FenceRole.Head ? 'head' : 'foot'} cm-td-fence-${this.kind}${this.active ? ' cm-td-fence-active' : ''}`;
  }
  toDOM() {
    const el = document.createElement('div');
    el.className = this.cls();
    el.setAttribute('aria-hidden', 'true');
    return el;
  }
  updateDOM(dom: HTMLElement) {
    dom.className = this.cls();
    return true;
  }
  ignoreEvent() { return false; }
}

/** 代码块首行里被隐藏的 ``` 与缩进（行内替换，不渲染任何东西；光标规范化据此识别） */
export class FencePrefixWidget extends WidgetType {
  eq() { return true; }
  toDOM() {
    const s = document.createElement('span');
    s.className = 'cm-td-fence-prefix';
    return s;
  }
  ignoreEvent() { return false; }
}

/** 代码块没写语言、光标在块内时，首行显示的「输入语言」占位 */
export class LanguagePlaceholderWidget extends WidgetType {
  eq() { return true; }
  toDOM() {
    const s = document.createElement('span');
    s.className = 'cm-td-code-lang-placeholder';
    s.textContent = uiText(UiText.LanguagePlaceholder);
    return s;
  }
  ignoreEvent() { return false; }
}

/**
 * HTML 块渲染态：DOMPurify 消毒后的结果（renderers/html.ts）。只含不可见内容的块不会用到它（始终是源码）。
 * 单击非交互处进入源码；预览里的链接不导航（宿主也会拦下页面外导航）。
 */
export class HtmlWidget extends WidgetType {
  readonly key: string;
  constructor(readonly src: string) {
    super();
    this.key = heightKey('html', src);
  }
  eq(o: HtmlWidget) { return o.src === this.src; }
  get estimatedHeight() { return cachedHeight(this.key) ?? 26 * Math.max(1, this.src.split('\n').length); }
  toDOM(view: EditorView) {
    const wrap = document.createElement('div');
    wrap.className = 'cm-td-block cm-td-html-preview';
    wrap.contentEditable = 'false';
    const known = cachedHeight(this.key);
    if (known !== undefined) wrap.style.minHeight = known + 'px';
    const base = view.state.facet(documentLocation).basePath;
    const fill = (html: string) => {
      const t = html.trim();
      if (!t || /^<([a-z][a-z\d]*)[^>]*?>(\s*)<\/\1>$/i.test(t)) {
        wrap.innerHTML = `<div class="cm-td-render-empty">${escapeHtml(uiText(UiText.EmptyHtml))}</div>`;
      } else wrap.innerHTML = html;
      wrap.style.minHeight = '';
      measureInto(this.key, wrap);
    };
    const now = sanitizeHtmlSync(this.src, base);
    if (now !== null) fill(now);
    else loadPurify().then(() => { fill(sanitizeHtmlSync(this.src, base) ?? ''); view.requestMeasure(); }, () => undefined);
    wrap.addEventListener('click', e => { if ((e.target as HTMLElement).closest('a')) e.preventDefault(); });
    clickToEdit(view, wrap, false, e => !(e.target as HTMLElement).closest('a, input, button, select, summary, details, video, audio'));
    return wrap;
  }
  ignoreEvent(e: Event) { return e.type === 'mousedown' || e.type === 'click'; }
}

export { isInvisibleHtml };

export interface TocEntry { readonly level: number; readonly text: string }

const sameEntries = (a: readonly TocEntry[], b: readonly TocEntry[]) =>
  a === b || (a.length === b.length && a.every((e, i) => e.level === b[i].level && e.text === b[i].text));

/**
 * `[TOC]` 渲染态：按大纲（editor/state/outline.ts 的行首扫描，不依赖语法树）列出标题。
 * 单击条目跳到对应标题（按条目序号取当前大纲里的位置），单击其余地方进入源码。
 */
export class TocWidget extends WidgetType {
  constructor(readonly entries: readonly TocEntry[]) { super(); }
  eq(o: TocWidget) { return sameEntries(o.entries, this.entries); }
  get estimatedHeight() { return 16 + 26 * Math.max(1, this.entries.length); }
  toDOM(view: EditorView) {
    const wrap = document.createElement('div');
    wrap.className = 'cm-td-block cm-td-toc';
    wrap.contentEditable = 'false';
    if (!this.entries.length) {
      const empty = document.createElement('div');
      empty.className = 'cm-td-toc-empty';
      empty.textContent = uiText(UiText.TocEmpty);
      wrap.appendChild(empty);
    } else {
      const top = Math.min(...this.entries.map(e => e.level));
      const ul = document.createElement('ul');
      this.entries.forEach((e, i) => {
        const li = document.createElement('li');
        li.className = `cm-td-toc-item cm-td-toc-h${e.level}`;
        li.style.setProperty('--td-toc-indent', String(e.level - top));
        const a = document.createElement('a');
        a.className = 'cm-td-toc-link';
        a.dataset.index = String(i);
        a.textContent = e.text;
        li.appendChild(a);
        ul.appendChild(li);
      });
      wrap.appendChild(ul);
    }
    wrap.addEventListener('mousedown', e => {
      if (e.button !== 0) return;
      e.preventDefault();
      const link = (e.target as HTMLElement).closest<HTMLElement>('.cm-td-toc-link');
      if (!link) { enterSource(view, wrap, false); return; }
      const heading = view.state.field(outlineField, false)?.headings[Number(link.dataset.index)];
      if (!heading) return;
      view.dispatch({
        selection: EditorSelection.cursor(view.state.doc.lineAt(heading.from).to),
        effects: EditorView.scrollIntoView(heading.from, { y: 'start', yMargin: 20 }),
        userEvent: 'select',
      });
      view.focus();
    });
    return wrap;
  }
  ignoreEvent(e: Event) { return e.type === 'mousedown'; }
}

/**
 * 脚注定义区左上角的编号标签（Muya 的 .ag-footnote-input 位置与字体），替换掉源码里的 `[^label]: `。
 * 编号与行内引用的上标一致（editor/state/footnotes.ts）；光标进入 `[^label]:` 时显形为源码。
 */
export class FootnoteLabelWidget extends WidgetType {
  constructor(readonly label: string, readonly number: number) { super(); }
  eq(o: FootnoteLabelWidget) { return o.label === this.label && o.number === this.number; }
  toDOM() {
    const s = document.createElement('span');
    s.className = 'cm-td-footnote-label';
    s.textContent = this.number > 0 ? String(this.number) : this.label;
    s.title = `[^${this.label}]`;
    return s;
  }
  ignoreEvent() { return false; }
}

/** 脚注定义区右下角的「↩」：回到第一处引用 */
export class FootnoteBackWidget extends WidgetType {
  constructor(readonly label: string) { super(); }
  eq(o: FootnoteBackWidget) { return o.label === this.label; }
  toDOM(view: EditorView) {
    const s = document.createElement('span');
    s.className = 'cm-td-footnote-back';
    s.textContent = '↩';
    s.title = uiText(UiText.FootnoteBack);
    s.addEventListener('mousedown', e => {
      if (e.button !== 0) return;
      e.preventDefault();
      const pos = firstReference(view.state, this.label);
      if (pos < 0) return;
      // 引用处的 label 可能与定义的大小写不同，按引用自身的 `]` 定位到它之后
      const close = view.state.doc.sliceString(pos, Math.min(view.state.doc.length, pos + 1000)).indexOf(']');
      view.dispatch({ selection: { anchor: close < 0 ? pos : pos + close + 1 }, effects: EditorView.scrollIntoView(pos, { y: 'center' }), userEvent: 'select' });
      view.focus();
    });
    return s;
  }
  ignoreEvent(e: Event) { return e.type === 'mousedown'; }
}
