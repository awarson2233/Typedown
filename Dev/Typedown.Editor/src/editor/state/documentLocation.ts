import { Facet } from '@codemirror/state';

/**
 * 当前文档的位置信息（doc.load 的 basePath）。每次 doc.load 建新状态时随之配置，
 * 图片与 HTML 块里的相对路径据此解析成宿主的本地图片地址（renderers/imageUrl.ts）。
 */
export interface DocumentLocation {
  /** 文档所在目录的绝对路径（Windows 路径）；未保存的文档是设置里的默认图片目录；未知时为空串 */
  readonly basePath: string;
}

export const NO_LOCATION: DocumentLocation = { basePath: '' };

export const documentLocation = Facet.define<DocumentLocation, DocumentLocation>({
  combine: values => values[0] ?? NO_LOCATION,
});
