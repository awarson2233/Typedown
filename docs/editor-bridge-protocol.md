# Editor Bridge Protocol

本文档定义 `Dev/Typedown.Editor/src/services/transport.ts` 与 Host (`Typedown`) 之间的稳定消息协议。

## Direction

- Editor -> Host: `window.chrome.webview.postMessage(JSON.stringify(payload))`
- Host -> Editor: `CoreWebView2.PostWebMessageAsString(JSON.stringify({ name, args }))`

## Editor -> Host Payload

### `invoke`

用于调用 Host 注册的 `RemoteInvoke` 方法。

```json
{
  "type": "invoke",
  "id": "invoke_12",
  "name": "GetCurrentTheme",
  "args": {}
}
```

### `message`

用于普通事件上报。

```json
{
  "type": "message",
  "name": "MarkdownChanged",
  "args": { "text": "..." }
}
```

### `diffmsg`

用于高频字符串 JSON 事件增量发送。

```json
{
  "type": "diffmsg",
  "diff": true,
  "name": "SelectionChanged",
  "args": "\"partial-json-fragment\"",
  "start": 5,
  "end": 18
}
```

约定：

- `diff: false` 时，`args` 是完整 JSON 字符串。
- `diff: true` 时，Host 使用 `start/end` 将 `args` 拼接到上一次缓存字符串，再 `JToken.Parse` 为对象后分发事件。

## Host -> Editor Payload

统一形状：

```json
{
  "name": "ThemeChanged",
  "args": { "theme": "Dark" }
}
```

## `invoke` Response

Host 以 `name = invoke request.id` 回发结果。

成功：

```json
{
  "name": "invoke_12",
  "args": { "code": 0, "data": {} }
}
```

失败：

```json
{
  "name": "invoke_12",
  "args": { "code": 1, "msg": "function [X] does not exist" }
}
```

## WinUI Phase 11 Contract-Backed Session Boundary

Phase 11 不再让 `WinUIEditorBridgeAdapter` 自己维护 smoke markdown/basePath/CurrentMarkdownLength。当前边界拆分为：

- `Dev\Typedown.Core.Contracts\Editor\IEditorDocumentSession`
  - 提供 `EditorDocumentState` 与 `EditorSettingsSnapshot`
  - 处理 editor event：`FileLoaded`、`MarkdownChange`、`CursorChange`、`StateChange`
  - 处理 smoke-safe remote invoke
- `Dev\Typedown.Core.Contracts\Editor\EditorDocumentState`
  - 当前最小状态：`Text`、`FilePath`、`BasePath`、`FileHash`、`CurrentHash`、`IsLoaded`、`IsSaved`、`LastEventName`
- `Dev\Typedown.Core.Contracts\Editor\EditorSettingsPayload`
  - 提供前端 `GetSettings` 需要的 JSON shape，保持 platform-neutral，不依赖 WinUI/WebView2/XAML/Newtonsoft/`Typedown.Core`
- `Dev\Typedown.Core.Contracts\Editor\IEditorHostSink` + `EditorHostMessage`
  - 让 host 用 contract DTO 发送 `LoadFile`；当前 WinUI 实现为 `WinUIEditorHostSink`

当前 WinUI 本地实现是 `Dev\Typedown.WinUI\Controls\WinUIEditorDocumentSession.cs`。它仍是 Phase 11 的 fake/local session，不接 `Typedown.Core`，但已经把文档状态和 settings payload 收敛到稳定 contract 上，后续真实 legacy/Core 文档服务可以在不改 editor bridge 协议的前提下替换 session 实现。

## WinUI Phase 11 Smoke Remote Surface

`WinUIEditorDocumentSession` 当前为 WinUI3 smoke host 提供完整 remote surface 的本地兜底响应，不依赖 `Typedown.Core` 或 legacy `Typedown.XamlUI`：

- `GetCurrentTheme`
- `ContentLoaded`
- `ExportCallback`
- `PrintHTML`
- `ResizeTable`
- `LoadImage`
- `GetSettings`
- `SetClipboard`
- `GetStringResources`
- `OpenNewWindow`
- `UnhandledException`

约定：

- 所有上述 `invoke` 都必须返回标准 envelope `{ name: id, args: { code, data|msg } }`，不能因为未实现而 reject promise。
- `GetSettings` 直接返回 session 的 `EditorSettingsPayload`，不再由 adapter 硬编码 smoke payload。
- `ResizeTable` 回显表格尺寸，兼容 `row/column` 与 `rows/columns`。
- `LoadImage` 至少回传 `{ url }`。
- `ExportCallback`、`PrintHTML`、`SetClipboard`、`OpenNewWindow`、`UnhandledException` 当前采用 smoke-safe stub；它们只记录状态并返回 `true` 或 `null`。
- `FileLoaded` 会把 session 状态重置到已加载/已保存基线，并刷新 `FileHash`/`CurrentHash`。
- `MarkdownChange` 会更新 `Text`/`CurrentHash`，并以 `CurrentHash == FileHash` 推导 `IsSaved`。

## WinUI Host Readiness Handshake

`Dev\Typedown.WinUI\Controls\WinUIEditorHost.cs` 不再依赖固定延迟发送 `LoadFile`。当前时序为：

1. WebView2 导航完成后，Host 发送 `WinUIHostReady`。
2. Editor 主动发起 `ContentLoaded` invoke。
3. Host 在收到 `ContentLoaded` 后，通过 `EditorHostMessage("LoadFile", ...)` 发送 session 当前 state 中的 `text/basePath`。

这样可以避免 bundle 尚未完成初始化时的竞态，也避免 `Unloaded` 之后残留 delayed send。

## Debug / Debug_Local 加载模式

`MarkdownEditor.LoadStaticResources()` 使用编译符号 `DEBUG` 决定加载方式：

- `#if DEBUG`: 导航到 `http://localhost:3000`
- `#else`: 导航到 `Resources/Statics/index.html`

当前 `Dev/Typedown/Typedown.csproj` 只声明了配置 `Debug;Release;Debug_Local`，未为 `Debug_Local` 额外定义 `DEBUG`。因此 `Debug_Local` 默认走 `Resources/Statics`，除非外部构建参数显式注入 `DEBUG`。
