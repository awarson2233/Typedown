/**
 * 编辑器桥接协议 v1 的页面侧类型（docs/editor-protocol.md，集成分支 b7e3ba8d）。
 * C1 只落正文同步：doc.load / doc.changed / doc.rendered / doc.flush / doc.getText，其余消息在 C5 补齐。
 * 偏移一律是 UTF-16 码元，基于只含 `\n` 换行的正文。
 */

export const PROTOCOL_VERSION = 1;

/** 信封：命令与事件即发即走，请求与应答以 id 配对（两个方向各自从 1 计数） */
export interface Command<T extends string, P> { k: 'cmd'; t: T; p: P }
export interface Event<T extends string, P> { k: 'evt'; t: T; p: P }
export interface Request<T extends string, P> { k: 'req'; id: number; t: T; p: P }
export type Response<P> =
  | { k: 'res'; id: number; ok: true; p: P }
  | { k: 'res'; id: number; ok: false; err: { code: WireError; message?: string } };

export type WireError = 'unknownType' | 'invalidPayload' | 'notReady' | 'canceled' | 'failed';

/** 正文里的一处替换：from / to 相对 baseVersion 的正文 */
export interface WireChange { from: number; to: number; insert: string }

export interface DocLoadPayload {
  /** 宿主分配，严格大于此前出现过的任何版本号 */
  version: number;
  text: string;
  basePath: string;
  selection?: { anchor: number; head: number };
  scrollTop?: number;
}
export interface DocChangedPayload {
  baseVersion: number;
  /** = baseVersion + 1 */
  version: number;
  /** 按 from 升序、互不重叠 */
  changes: WireChange[];
}
export interface DocRenderedPayload { version: number }
export interface DocFlushResult { version: number }
export interface DocGetTextResult { version: number; text: string }

export type DocLoad = Command<'doc.load', DocLoadPayload>;
export type DocChanged = Event<'doc.changed', DocChangedPayload>;
export type DocRendered = Event<'doc.rendered', DocRenderedPayload>;
export type DocFlush = Request<'doc.flush', Record<string, never>>;
export type DocGetText = Request<'doc.getText', Record<string, never>>;

/** 宿主 → 页面 */
export type HostToPage = DocLoad | DocFlush | DocGetText;
/** 页面 → 宿主 */
export type PageToHost = DocChanged | DocRendered | Response<DocFlushResult> | Response<DocGetTextResult>;
