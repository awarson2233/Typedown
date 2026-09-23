/**
 * 页面 ↔ 宿主协议（webview-host-architecture.md 第 6.3 节）。C1 只定下正文同步三条消息的形状，
 * 等宿主侧 IEditorSession 完成后在 C1 后半段接通；C# 侧 DTO 与这里逐项对应。
 */

/** 宿主 → 页面：整篇载入。text 只含 `\n` 换行，编码、BOM、原换行符由宿主记住并在保存时还原。 */
export interface DocLoad { type: 'doc.load'; version: number; text: string }

/** 页面 → 宿主：按动画帧合并的增量，载荷与改动大小成正比。changes 是 CM6 ChangeSet 的 JSON 形式。 */
export interface DocChanged { type: 'doc.changed'; baseVersion: number; version: number; changes: unknown[] }

/** 宿主 → 页面（请求）：保存前冲刷未发出的增量；应答带当前版本号。 */
export interface DocFlush { type: 'doc.flush'; requestId: number }

export type HostToPage = DocLoad | DocFlush;
export type PageToHost = DocChanged;
