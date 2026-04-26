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

## Debug / Debug_Local 加载模式

`MarkdownEditor.LoadStaticResources()` 使用编译符号 `DEBUG` 决定加载方式：

- `#if DEBUG`: 导航到 `http://localhost:3000`
- `#else`: 导航到 `Resources/Statics/index.html`

当前 `Dev/Typedown/Typedown.csproj` 只声明了配置 `Debug;Release;Debug_Local`，未为 `Debug_Local` 额外定义 `DEBUG`。因此 `Debug_Local` 默认走 `Resources/Statics`，除非外部构建参数显式注入 `DEBUG`。
