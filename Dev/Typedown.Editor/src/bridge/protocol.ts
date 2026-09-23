/**
 * 编辑器桥接协议 v1 的页面侧类型（docs/editor-protocol.md，集成分支 35c80482）。
 * 与 Core 的 EditorWireTypes 是同一张表：每个类型名一个载荷 interface，按 `t` 组成可辨识联合。
 * 字段名、枚举字符串与协议文档第 5 节逐字一致；KeyboardKey / KeyboardModifiers 是 Win32 整数。
 * 可空字段写作 `x?: T | null`：宿主写出时省略值为 null 的字段，页面读时缺失与 null 同义。
 * 偏移一律是 UTF-16 码元，基于只含 `\n` 换行的正文。
 */

export const PROTOCOL_VERSION = 1;

// ── 信封（第 2、9 节） ──────────────────────────────────────────────

/** 命令与事件即发即走，请求与应答以 id 配对（两个方向各自从 1 计数） */
export interface Command<T extends string, P> { k: 'cmd'; t: T; p: P }
export interface Event<T extends string, P> { k: 'evt'; t: T; p: P }
export interface Request<T extends string, P> { k: 'req'; id: number; t: T; p: P }
export type Response<P> =
  | { k: 'res'; id: number; ok: true; p: P }
  | { k: 'res'; id: number; ok: false; err: WireErrorBody };

export interface WireErrorBody { code: WireError; message?: string }

/** 无字段的载荷 */
export type Empty = Record<string, never>;

// ── 词汇（EditorVocabulary.cs，camelCase） ─────────────────────────

/**
 * 字符串枚举的全部线上值，按 C# 声明顺序；与 Tests/Protocol/enums.json 逐项相同（protocol.test.ts 核对）。
 * 下面的联合类型都由它推出，运行期需要取值顺序的地方（BlockContext.kinds 排序等）也读它。
 */
export const ENUMS = {
  InlineMark: ['strong', 'emphasis', 'underline', 'strikethrough', 'highlight', 'inlineCode', 'inlineMath', 'link', 'image'],
  BlockKind: [
    'paragraph', 'heading1', 'heading2', 'heading3', 'heading4', 'heading5', 'heading6', 'codeBlock', 'mathBlock', 'htmlBlock', 'quote',
    'orderedList', 'bulletList', 'taskList', 'table', 'footnote', 'horizontalRule', 'frontMatter', 'vegaLiteChart', 'flowChart',
    'sequenceDiagram', 'plantUmlDiagram', 'mermaidDiagram',
  ],
  ParagraphPosition: ['before', 'after'],
  CopyFormat: ['rich', 'plainText', 'markdown', 'html'],
  PasteFormat: ['rich', 'plainText'],
  TableAxis: ['row', 'column'],
  TableEdit: ['insertRowAbove', 'insertRowBelow', 'removeRow', 'insertColumnLeft', 'insertColumnRight', 'removeColumn'],
  ImageTarget: ['edited', 'selected'],
  ImageToolbarAction: ['edit', 'inline', 'alignLeft', 'alignCenter', 'alignRight', 'delete'],
  SearchDirection: ['next', 'previous'],
  ExportPurpose: ['export', 'print'],
  TooltipKind: ['copyContent', 'ctrlClickToOpenLink', 'resizeTable', 'alignLeft', 'alignCenter', 'alignRight', 'deleteTable'],
  ImageSourceKind: ['filePath', 'dataUrl', 'webUrl'],
  EditorWireError: ['unknownType', 'invalidPayload', 'notReady', 'canceled', 'failed'],
} as const;
export type EnumName = keyof typeof ENUMS;

export type InlineMark = typeof ENUMS.InlineMark[number];
export type BlockKind = typeof ENUMS.BlockKind[number];
export type ParagraphPosition = typeof ENUMS.ParagraphPosition[number];
export type CopyFormat = typeof ENUMS.CopyFormat[number];
export type PasteFormat = typeof ENUMS.PasteFormat[number];
export type TableAxis = typeof ENUMS.TableAxis[number];
export type TableEdit = typeof ENUMS.TableEdit[number];
export type ImageTarget = typeof ENUMS.ImageTarget[number];
export type ImageToolbarAction = typeof ENUMS.ImageToolbarAction[number];
export type SearchDirection = typeof ENUMS.SearchDirection[number];
export type ExportPurpose = typeof ENUMS.ExportPurpose[number];
export type TooltipKind = typeof ENUMS.TooltipKind[number];
export type ImageSourceKind = typeof ENUMS.ImageSourceKind[number];
export type WireError = typeof ENUMS.EditorWireError[number];
export const WIRE_ERRORS: readonly WireError[] = ENUMS.EditorWireError;

/** Win32 虚拟键码（Typedown.Core.Models.KeyboardKey），页面按 KeyboardEvent.keyCode 比较 */
export type KeyboardKey = number;
/** 标志位（Typedown.Core.Models.KeyboardModifiers） */
export type KeyboardModifiers = number;
export const KeyboardModifier = { None: 0, Control: 1, Menu: 2, Shift: 4, Windows: 8 } as const;

/** CSS px，相对 WebView 视口左上角（第 7 节） */
export interface EditorRect { x: number; y: number; width: number; height: number }
export interface ImageInfo { src: string; alt: string; title: string }
export interface SearchOptions { caseSensitive: boolean; wholeWord: boolean; regex: boolean }
export interface KeyChord { key: KeyboardKey; modifiers: KeyboardModifiers }
export interface TableSize { rows: number; columns: number }
export interface EditorColor { r: number; g: number; b: number; a: number }
export interface EditorTheme { isDark: boolean; accent: EditorColor; background: EditorColor }
export interface ClipboardContent { plainText?: string | null; html?: string | null }
/** HeadingId 在线上展开成字符串，页面只当不透明字符串 */
export type HeadingId = string;
export interface OutlineItem { id: HeadingId; level: number; text: string }
export interface BlockContext { kinds: BlockKind[]; multipleBlocks: boolean; codeLike: boolean; codeLine: boolean; blockCommandsDisabled: boolean }
export interface RichSelection { block: BlockContext; selectedImage?: ImageInfo | null }
export interface ExportHtmlOptions { extraHead?: string | null; extraBody?: string | null; header?: string | null; footer?: string | null }
export interface ImageSource { kind: ImageSourceKind; value: string }

/** 全部字段可空：初始态给全量，view.settings 只给变化的字段（null 与缺失同义） */
export interface EditorSettings {
  sourceCode?: boolean | null;
  typewriter?: boolean | null;
  focusMode?: boolean | null;
  searchIsCaseSensitive?: boolean | null;
  searchIsRegexp?: boolean | null;
  searchIsWholeWord?: boolean | null;
  fontSize?: number | null;
  lineHeight?: number | null;
  autoPairBracket?: boolean | null;
  autoPairQuote?: boolean | null;
  trimUnnecessaryCodeBlockEmptyLines?: boolean | null;
  preferLooseListItem?: boolean | null;
  autoPairMarkdownSyntax?: boolean | null;
  editorAreaWidth?: string | null;
  tabSize?: number | null;
  spellcheckEnabled?: boolean | null;
}

/**
 * 初始态（第 3 节）：宿主导航前以 `window.__typedownInit = {...}` 注入，页面启动时同步读取。
 * keymap 是和弦数组（与 view.keymap 的 chords 相同）；页面同时容忍 `{chords}` 形态。
 */
export interface EditorInitState {
  protocol: number;
  settings: EditorSettings;
  theme: EditorTheme;
  keymap: KeyChord[];
  locale: string;
}

// ── 命令载荷（宿主→页面 cmd） ────────────────────────────────────

export interface DocLoadPayload {
  /** 宿主分配：宿主此前见过的最大版本号加 2^20，在途的旧增量追不上它 */
  version: number;
  text: string;
  basePath: string;
  selection?: { anchor: number; head: number } | null;
  scrollTop?: number | null;
}
export interface DocImportHtmlPayload { html: string }
export interface ClipboardCopyPayload { format: CopyFormat }
export interface ClipboardPastePayload { format: PasteFormat; text: string; html: string }
export interface FormatTogglePayload { mark: InlineMark }
export interface BlockSetKindPayload { kind: BlockKind }
export interface BlockInsertPayload { position: ParagraphPosition }
export interface TableInsertPayload { rows: number; columns: number }
export interface TableEditPayload { edit: TableEdit }
export interface ImageInsertPayload { src: string; alt?: string | null; title?: string | null }
export interface ImageReplacePayload { target: ImageTarget; src: string; alt?: string | null; title?: string | null }
export interface ImageToolbarActionPayload { action: ImageToolbarAction }
export interface ImageZoomPayload { percent: number }
export interface SearchSetPayload { query?: string | null; options: SearchOptions }
export interface SearchStepPayload { direction: SearchDirection }
export interface SearchReplacePayload { query?: string | null; replacement: string; all: boolean; options: SearchOptions }
export interface OutlineRevealPayload { id: HeadingId }
export interface ViewSettingsPayload { changes: EditorSettings }
export interface ViewThemePayload { theme: EditorTheme }
export interface ViewKeymapPayload { chords: KeyChord[] }
export interface ViewScrollToPayload { x: number; y: number }

/** 命令类型名 → 载荷 */
export interface CommandMap {
  'doc.load': DocLoadPayload;
  'doc.importHtml': DocImportHtmlPayload;
  'history.undo': Empty;
  'history.redo': Empty;
  'history.clear': Empty;
  'selection.selectAll': Empty;
  'selection.delete': Empty;
  'clipboard.copy': ClipboardCopyPayload;
  'clipboard.cut': Empty;
  'clipboard.paste': ClipboardPastePayload;
  'format.toggle': FormatTogglePayload;
  'format.clear': Empty;
  'block.setKind': BlockSetKindPayload;
  'block.promote': Empty;
  'block.demote': Empty;
  'block.insert': BlockInsertPayload;
  'block.delete': Empty;
  'block.duplicate': Empty;
  'block.menuClosed': Empty;
  'table.insert': TableInsertPayload;
  'table.edit': TableEditPayload;
  'image.insert': ImageInsertPayload;
  'image.replace': ImageReplacePayload;
  'image.toolbarAction': ImageToolbarActionPayload;
  'image.zoom': ImageZoomPayload;
  'search.set': SearchSetPayload;
  'search.step': SearchStepPayload;
  'search.replace': SearchReplacePayload;
  'search.end': Empty;
  'outline.reveal': OutlineRevealPayload;
  'view.settings': ViewSettingsPayload;
  'view.theme': ViewThemePayload;
  'view.keymap': ViewKeymapPayload;
  'view.scrollTo': ViewScrollToPayload;
  'view.refreshViewport': Empty;
}

// ── 事件载荷（页面→宿主 evt） ────────────────────────────────────

export interface LifecycleReadyPayload { protocol: number; engine: string }
export interface LifecycleFaultPayload { message: string; stack?: string | null; fatal: boolean }
/** 正文里的一处替换：from / to 相对 baseVersion 的正文 */
export interface WireChange { from: number; to: number; insert: string }
export interface DocChangedPayload {
  baseVersion: number;
  /** = baseVersion + 1 */
  version: number;
  /** 按 from 升序、互不重叠 */
  changes: WireChange[];
}
export interface DocRenderedPayload { version: number }
export interface HistoryChangedPayload { canUndo: boolean; canRedo: boolean }
export interface SelectionChangedPayload {
  hasText: boolean;
  /** 单行且不超过 200 个码元时是选中原文，否则空串 */
  text: string;
  /** null 表示源码模式 */
  rich?: RichSelection | null;
  anchor: number;
  head: number;
}
export interface SelectionMarksPayload { marks: InlineMark[] }
export interface OutlineChangedPayload { items: OutlineItem[]; current?: OutlineItem | null }
export interface StatsChangedPayload { characters: number; words: number }
export interface SearchResultPayload { count: number; current: number }
export interface ViewViewportPayload { viewportWidth: number; viewportHeight: number; maximumX: number; maximumY: number; scrollX: number; scrollY: number }
export interface ViewShortcutPayload { key: KeyboardKey; modifiers: KeyboardModifiers }
export interface ViewOpenLinkPayload { uri: string }
export interface FloatAnchorPayload { anchor?: EditorRect | null }
export interface FloatImageEditorPayload { anchor?: EditorRect | null; image: ImageInfo }
export interface FloatTableToolsPayload { anchor?: EditorRect | null; axis: TableAxis }
export interface FloatTooltipPayload { kind: TooltipKind; anchor?: EditorRect | null }

/** 事件类型名 → 载荷 */
export interface EventMap {
  'lifecycle.ready': LifecycleReadyPayload;
  'lifecycle.fault': LifecycleFaultPayload;
  'doc.changed': DocChangedPayload;
  'doc.rendered': DocRenderedPayload;
  'history.changed': HistoryChangedPayload;
  'selection.changed': SelectionChangedPayload;
  'selection.marks': SelectionMarksPayload;
  'outline.changed': OutlineChangedPayload;
  'stats.changed': StatsChangedPayload;
  'search.result': SearchResultPayload;
  'view.viewport': ViewViewportPayload;
  'view.shortcut': ViewShortcutPayload;
  'view.openLink': ViewOpenLinkPayload;
  'float.blockMenu': FloatAnchorPayload;
  'float.formatPicker': FloatAnchorPayload;
  'float.imageToolbar': FloatAnchorPayload;
  'float.imageEditor': FloatImageEditorPayload;
  'float.tableTools': FloatTableToolsPayload;
  'float.tooltip': FloatTooltipPayload;
  'float.tooltipDismissed': Empty;
}

// ── 请求（第 5 节「请求」） ────────────────────────────────────────

export interface DocFlushResult { version: number }
export interface DocGetTextResult { version: number; text: string }
export interface ExportRenderHtmlPayload { purpose: ExportPurpose; title: string; basePath?: string | null; options?: ExportHtmlOptions | null }
export interface ExportRenderHtmlResult { html: string }
export interface SelectionContextAtPayload { x: number; y: number }
export type ClipboardWriteResult = Empty;
export interface ImageResolvePayload { source: ImageSource }
export interface ImageResolveResult { src: string }

/** 宿主→页面的请求：类型名 → [载荷, 应答]。结果可空的请求，成功应答可以是 `"p":null`；其余缺 p 按 {} 读 */
export interface HostRequestMap {
  'doc.flush': [Empty, DocFlushResult];
  'doc.getText': [Empty, DocGetTextResult];
  'export.renderHtml': [ExportRenderHtmlPayload, ExportRenderHtmlResult];
  /** 坐标不在正文上时应答 null */
  'selection.contextAt': [SelectionContextAtPayload, RichSelection | null];
}

/** 页面→宿主的请求：类型名 → [载荷, 应答] */
export interface PageRequestMap {
  /** 用户取消时应答 null */
  'table.pickSize': [Empty, TableSize | null];
  'clipboard.write': [ClipboardContent, ClipboardWriteResult];
  /** 放弃插入时应答 null */
  'image.resolve': [ImageResolvePayload, ImageResolveResult | null];
}

export type CommandType = keyof CommandMap;
export type EventType = keyof EventMap;
export type HostRequestType = keyof HostRequestMap;
export type PageRequestType = keyof PageRequestMap;

// ── 可辨识联合 ──────────────────────────────────────────────────────

export type HostCommand = { [T in CommandType]: Command<T, CommandMap[T]> }[CommandType];
export type PageEvent = { [T in EventType]: Event<T, EventMap[T]> }[EventType];
export type HostRequest = { [T in HostRequestType]: Request<T, HostRequestMap[T][0]> }[HostRequestType];
export type PageRequest = { [T in PageRequestType]: Request<T, PageRequestMap[T][0]> }[PageRequestType];
export type HostResponse = { [T in PageRequestType]: Response<PageRequestMap[T][1]> }[PageRequestType];
export type PageResponse = { [T in HostRequestType]: Response<HostRequestMap[T][1]> }[HostRequestType];

/** 宿主 → 页面 */
export type HostToPage = HostCommand | HostRequest | HostResponse;
/** 页面 → 宿主 */
export type PageToHost = PageEvent | PageRequest | PageResponse;

/** 各类型名的全集（运行期校验与契约测试用） */
export const COMMAND_TYPES = [
  'doc.load', 'doc.importHtml', 'history.undo', 'history.redo', 'history.clear', 'selection.selectAll', 'selection.delete',
  'clipboard.copy', 'clipboard.cut', 'clipboard.paste', 'format.toggle', 'format.clear', 'block.setKind', 'block.promote', 'block.demote',
  'block.insert', 'block.delete', 'block.duplicate', 'block.menuClosed', 'table.insert', 'table.edit', 'image.insert', 'image.replace',
  'image.toolbarAction', 'image.zoom', 'search.set', 'search.step', 'search.replace', 'search.end', 'outline.reveal', 'view.settings',
  'view.theme', 'view.keymap', 'view.scrollTo', 'view.refreshViewport',
] as const satisfies readonly CommandType[];
export const EVENT_TYPES = [
  'lifecycle.ready', 'lifecycle.fault', 'doc.changed', 'doc.rendered', 'history.changed', 'selection.changed', 'selection.marks',
  'outline.changed', 'stats.changed', 'search.result', 'view.viewport', 'view.shortcut', 'view.openLink', 'float.blockMenu',
  'float.formatPicker', 'float.imageToolbar', 'float.imageEditor', 'float.tableTools', 'float.tooltip', 'float.tooltipDismissed',
] as const satisfies readonly EventType[];
export const HOST_REQUEST_TYPES = ['doc.flush', 'doc.getText', 'export.renderHtml', 'selection.contextAt'] as const satisfies readonly HostRequestType[];
export const PAGE_REQUEST_TYPES = ['table.pickSize', 'clipboard.write', 'image.resolve'] as const satisfies readonly PageRequestType[];

// 编译期守住「全集数组 ≡ 映射表的键」：漏写或多写都会让下面某个类型变成 never
type Exact<A, B> = [A] extends [B] ? ([B] extends [A] ? true : never) : never;
export const _typeListsAreComplete: [
  Exact<typeof COMMAND_TYPES[number], CommandType>,
  Exact<typeof EVENT_TYPES[number], EventType>,
  Exact<typeof HOST_REQUEST_TYPES[number], HostRequestType>,
  Exact<typeof PAGE_REQUEST_TYPES[number], PageRequestType>,
] = [true, true, true, true];

// 旧名保留：docSync 与其单测使用
export type DocLoad = Command<'doc.load', DocLoadPayload>;
export type DocChanged = Event<'doc.changed', DocChangedPayload>;
export type DocRendered = Event<'doc.rendered', DocRenderedPayload>;
export type DocFlush = Request<'doc.flush', Empty>;
export type DocGetText = Request<'doc.getText', Empty>;
