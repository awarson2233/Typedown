import { WidgetType } from '@codemirror/view';

/**
 * 占位实现：图片 widget 归 W2（加载中、失败、空三种占位与本地路径解析都在 W2 的版本里）。
 * 这里只按约定的接口导出，让 W1 的图片显形规则能编译、能在 dev 页看到图；合并时直接取 W2 的文件。
 */
export interface ImageSpec {
  /** 源码原文，相对路径与本地路径由页面内部解析 */
  src: string;
  alt: string;
  title: string | null;
}

export class ImageWidget extends WidgetType {
  constructor(readonly spec: ImageSpec) { super(); }
  eq(o: ImageWidget) { return o.spec.src === this.spec.src && o.spec.alt === this.spec.alt && o.spec.title === this.spec.title; }
  toDOM() {
    const img = document.createElement('img');
    img.className = 'cm-td-image';
    img.src = this.spec.src;
    img.alt = this.spec.alt;
    if (this.spec.title) img.title = this.spec.title;
    return img;
  }
  ignoreEvent() { return false; }
}
