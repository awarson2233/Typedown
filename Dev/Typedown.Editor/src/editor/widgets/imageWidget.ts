import { syntaxTree } from '@codemirror/language';
import type { EditorState } from '@codemirror/state';
import { WidgetType, type EditorView } from '@codemirror/view';
import { UiText, uiText } from '../../shared/strings';
import { documentLocation } from '../state/documentLocation';
import { ImageSourceKind, resolveImageSource } from '../../renderers/imageUrl';
import { cachedHeight, heightKey, rememberHeight } from './heightCache';

/**
 * 图片 widget（行内）。与 W1 的分工：何时显示它、光标进入时露出 `![alt](src)` 源码且图片保留，都是行内显形层
 * （decorations/inlineSpecs.ts 的 Image 分支）决定的；这里只负责图片本身：
 * - 地址解析：源码 → 可加载的地址（renderers/imageUrl.ts；本地文件经宿主的 https://typedown.image/ 加载）；
 * - 状态：加载中（Muya 的 400×250 转圈占位）、失败、空（源码为空）三种占位，成功显示图片；
 * - 高度缓存：按解析后的地址记住上次渲染的高度，widget 重建（显形切换、滚回视口）时先用缓存高度占位，避免跳动；
 *   同一地址加载过就直接显示，不再闪加载占位。
 * - 单击：把光标放到 alt 文字末尾（`]` 之前），显形由 W1 的规则随光标自动触发。
 * 构造参数用 ImageSpec（源码原文，不做解析），W1 在 inlineWidgets 的工厂里 `new ImageWidget({ src, alt, title })`。
 */
export interface ImageSpec {
  /** 链接目标的源码原文（可带尖括号、反斜杠转义、百分号转义） */
  readonly src: string;
  /** 方括号里的替代文字（源码原文） */
  readonly alt: string;
  /** 标题（去掉引号后的文字）；没有时为 null */
  readonly title: string | null;
}

enum LoadState { Loading, Loaded, Failed }

/** 地址 → 加载结果；成功过的地址再次出现时直接显示 */
const loadStates = new Map<string, LoadState>();

const ICON_IMAGE = '', ICON_FAIL = '';

/** alt 里的 markdown 标记去掉后给 <img alt>（与 Muya 的处理相同） */
const plainAlt = (alt: string) => alt.replace(/[`*{}[\]()#+\-.!_>~:|<>$]/g, '');

export class ImageWidget extends WidgetType {
  constructor(readonly spec: ImageSpec) { super(); }

  eq(o: ImageWidget) {
    return o.spec.src === this.spec.src && o.spec.alt === this.spec.alt && o.spec.title === this.spec.title;
  }

  private placeholder(wrap: HTMLElement, cls: 'cm-td-image-fail' | 'cm-td-image-empty', icon: string, text: string) {
    wrap.className = `cm-td-image ${cls}`;
    const i = document.createElement('span');
    i.className = 'cm-td-image-icon';
    i.textContent = icon;
    const t = document.createElement('span');
    t.className = 'cm-td-image-text';
    t.textContent = text;
    wrap.replaceChildren(i, t);
  }

  toDOM(view: EditorView) {
    const wrap = document.createElement('span');
    wrap.contentEditable = 'false';
    wrap.addEventListener('mousedown', e => {
      if (e.button !== 0) return;
      e.preventDefault();
      const at = imageAltEnd(view.state, view.posAtDOM(wrap));
      if (at === null) return;
      view.dispatch({ selection: { anchor: at }, userEvent: 'select.pointer' });
      view.focus();
    });
    const resolved = resolveImageSource(this.spec.src, view.state.facet(documentLocation).basePath);
    if (resolved.kind === ImageSourceKind.Empty) {
      this.placeholder(wrap, 'cm-td-image-empty', ICON_IMAGE, uiText(UiText.ImageEmpty));
      return wrap;
    }
    if (resolved.kind === ImageSourceKind.Unsupported) {
      this.placeholder(wrap, 'cm-td-image-fail', ICON_FAIL, `${uiText(UiText.ImageLoadFailed)}：${this.spec.src.trim()}`);
      return wrap;
    }
    const url = resolved.url;
    const key = heightKey('image', url);
    const img = document.createElement('img');
    img.alt = plainAlt(this.spec.alt);
    if (this.spec.title) img.title = this.spec.title;
    img.draggable = false;
    const state = loadStates.get(url);
    if (state === LoadState.Failed) {
      this.placeholder(wrap, 'cm-td-image-fail', ICON_FAIL, `${uiText(UiText.ImageLoadFailed)}：${this.spec.src.trim()}`);
      return wrap;
    }
    wrap.className = `cm-td-image ${state === LoadState.Loaded ? 'cm-td-image-success' : 'cm-td-image-loading'}`;
    const known = cachedHeight(key);
    if (known !== undefined) wrap.style.minHeight = known + 'px';
    const settle = () => {
      wrap.style.minHeight = '';
      requestAnimationFrame(() => { if (wrap.isConnected) rememberHeight(key, wrap.getBoundingClientRect().height); });
      view.requestMeasure();
    };
    img.addEventListener('load', () => {
      loadStates.set(url, LoadState.Loaded);
      wrap.className = 'cm-td-image cm-td-image-success';
      settle();
    });
    img.addEventListener('error', () => {
      loadStates.set(url, LoadState.Failed);
      this.placeholder(wrap, 'cm-td-image-fail', ICON_FAIL, `${uiText(UiText.ImageLoadFailed)}：${this.spec.src.trim()}`);
      settle();
    });
    img.src = url;
    wrap.appendChild(img);
    return wrap;
  }

  /** 鼠标按下由 widget 自己处理（放光标），其余事件交给编辑器 */
  ignoreEvent(e: Event) { return e.type === 'mousedown'; }
}

/**
 * widget 所在图片的 alt 末尾（`]` 的位置）。widget 或者替换整段 `![alt](src)`（posAtDOM 得到起点），或者在显形时挂在它末尾（得到终点），
 * 所以找起点或终点等于 at 的 Image 节点，取它的第二个 LinkMark。找不到时为 null。
 */
export function imageAltEnd(state: EditorState, at: number): number | null {
  let found: number | null = null;
  syntaxTree(state).iterate({
    from: at, to: at,
    enter: n => {
      if (found !== null) return false;
      if (n.name !== 'Image' || (n.from !== at && n.to !== at)) return;
      const marks = n.node.getChildren('LinkMark');
      if (marks.length >= 2 && state.sliceDoc(marks[1].from, marks[1].to) === ']') found = marks[1].from;
      return false;
    },
  });
  return found;
}

/** 重新加载某个地址时（例如宿主通知图片文件变了）清掉记下的结果；测试也用它复位 */
export function forgetImageStates() {
  loadStates.clear();
}
