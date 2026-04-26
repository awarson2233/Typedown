# WinUI3 目标架构边界

本文档固化 Phase 8 的架构决定：`Typedown.UI`、`Typedown.WinUI` 和 `Dev\Typedown.XamlUI` 必须保持分离。当前阶段只定义边界和迁移顺序，不移动旧 XAML 控件，不创建 WinUI3 shell，不做 ARM64 适配。

## 目标模块

```text
Typedown.WinUI
  Windows App SDK / WinUI3 shell。
  负责 App、Window、DI composition root、平台服务实现、WebView2 宿主、
  单实例激活、文件 picker、dialog、dispatcher/window context、打包入口。

Typedown.UI
  应用 UI 层。
  负责页面、控件、资源、UI ViewModel、UI 编排和编辑器视图组合。
  它可以依赖 Typedown.Core 的契约和模型，但不负责进程启动、平台服务或打包。

Typedown.Core
  平台中立核心层。
  负责模型、设置/数据库、文档流程、编辑器 bridge 协议、导出配置、
  服务契约和可测试的非 UI 逻辑。

Dev\Typedown.XamlUI
  当前 legacy XAML host。
  只用于维持现有 WinUI 2 / UWP XAML 家族的可运行基线。
  它不是 Typedown.UI，WinUI3 功能等价后应删除。
```

依赖方向：

```text
Typedown.WinUI -> Typedown.UI -> Typedown.Core
Typedown.WinUI -> Typedown.Core
Typedown.WinUI -> Typedown.Editor static bundle
Dev\Typedown.XamlUI -> 仅当前旧启动链使用
```

禁止方向：

- `Typedown.Core` 不引用 `Typedown.UI`、`Typedown.WinUI` 或 `Dev\Typedown.XamlUI`。
- `Typedown.UI` 不引用 `Typedown.WinUI`。
- `Typedown.UI` 不接收 `Dev\Typedown.XamlUI` 的宿主/run loop/HWND 代码。
- `Typedown.WinUI` 不沉淀页面业务逻辑；页面级逻辑应落在 `Typedown.UI`。

## 当前归属清单

### 未来 Typedown.UI 候选

当前这些内容主要位于 `Dev\Typedown.Core`，但长期应迁入 `Typedown.UI`：

- `Controls\**`：`RootControl`、caption、editor controls、float controls、dialog controls、side pane、settings controls、common XAML controls。
- `Pages\**`：`MainPage`、`SettingsPage`、setting pages、page route。
- `Resources\Styles\*.xaml`、`Images\logo.ico`、大部分 `Resources\Strings\**`：UI 资源、设置页文案、dialog/common 文案。
- `Converters\**`：依赖 XAML binding、`Visibility`、`GridLength`、`ElementTheme`。
- `ViewModels\AppViewModel.cs`、`UIViewModel.cs`：持有 `Frame`、`XamlRoot`、theme、`Application.Current`。
- `ViewModels\FileViewModel.cs`、`SettingsViewModel.cs`、`ParagraphViewModel.cs`、`EditorViewModel.cs`：当前先视为 UI 编排层；后续只把纯规则下沉回 Core。
- UI utilities：`ControlExtensions.cs`、`ComboBoxPatch.cs`、`CoreDispatcherExtensions.cs`、`FilePickersExtensions.cs`、`InjectionExtensions.cs`、`ReactiveExtensions.cs`。

### 未来 Typedown.WinUI 候选

当前这些内容主要位于 `Dev\Typedown`，长期应收敛为 `Typedown.WinUI`：

- `Program.cs`、`App.cs`、`Windows\MainWindow.cs`、`Windows\WebViewInstallWindow.cs`：启动、窗口和 shell lifecycle。
- `Injection.cs`：DI composition root。后续应拆成 Core/UI 注册扩展 + WinUI adapter 注册。
- `Controls\MarkdownEditor.cs`、`Utilities\WebViewController.cs`：WebView2 host、composition controller、input forwarding、theme sync 和资源加载。
- `Services\FilePickerService.cs`、`Services\DialogService.cs`：文件 picker/dialog 平台实现。
- `Services\UiDispatcher.cs`、`Services\WindowContext.cs`、`Services\WindowService.cs`：dispatcher、window context、window adapter。
- `Services\AppActivationService.cs`：mutex、named pipe、二次启动转发、前置已有窗口。
- `Tools\Typedown.Package\**`：MSIX/Desktop Bridge 打包和 file association。

这些实现可以依赖 `Typedown.Core` 的接口，但接口本身不应暴露具体 WinUI shell 类型。

### 应留在 Typedown.Core

长期应保留或收敛在 `Typedown.Core` 的内容：

- `Models\PersistentModels\**`、大多数 `Models\RuntimeModels\**`、`Enums\**`。
- `Services\AppDbContext.cs`、`Migrations\**`、`Services\AccessHistory.cs`、`Services\AutoBackup.cs`。
- `Services\Transport.cs`、`Services\RemoteInvoke.cs`、`Services\EditorBridge.cs`。
- `Utilities\ExportMarkdown.cs`、`FileTypeHelper.cs`、`CommandLine.cs`、纯算法/解析/格式处理工具。
- Core ports：`IEditorBridge`、`IFilePickerService`、`IDialogService`、`IFileExport`、`IFileOperation`、`IClipboard`、`IAppDataPathProvider`、`IPowerShellService`、`IAppActivationService`。

需要后续收敛的接口：

- `IMarkdownEditor` 当前暴露 `Windows.Foundation` 类型，应在 UI/WinUI 边界中重新设计。
- `IWindowService`、`IWindowPrivate`、`IKeyboardAccelerator` 当前仍泄漏 XAML/Windows 类型，应改契约或迁出 Core。
- `Config` 仍包含 `Windows.ApplicationModel` / `Windows.Storage` 依赖，Windows packaged/local folder 判断应下沉到 WinUI provider。

## Legacy XAML Host 边界

`Dev\Typedown.XamlUI` 是 legacy host，不是 `Typedown.UI`。它当前负责：

- `XamlApplication`：旧 UWP/WinUI 2 XAML app lifecycle、`WindowsXamlManager` 初始化、自定义 dispatcher/message loop。
- `Window` / `XamlWindow`：Win32 HWND 创建、`DesktopWindowXamlSource` 嵌入、DPI、大小/位置、关闭、owner/topmost、system menu。
- 无边框窗口行为：caption buttons、drag region、resize hit-test、DWM dark mode、Win10/Win11 frame 差异。
- XAML runtime glue：metadata provider、`XamlControlsResources`、`CommonResources`、theme propagation。
- workaround：ContentDialog、ComboBox、Popup/MenuFlyout 旧宿主修补。
- legacy build/runtime 制品：`buildTransitive`、`lib`、`ref`、`runtimes`、`tools`。

这些内容不得迁入 `Typedown.UI`：

- `XamlApplication`、`XamlWindow`、`Window`、`Dispatcher`。
- `DesktopWindowXamlSource`、`WindowsXamlManager`、`Windows.UI.Xaml.Hosting.*`。
- `Microsoft.UI.Xaml.dll/.pri/.winmd`、HostingContract winmd、`buildTransitive` targets/props。
- Win32 frame、DWM、caption hit-test、child drag window、message pump patch。
- `Typedown.XamlUI.Patchs` 里的旧 UWP XAML host workaround。

删除 legacy host 前，WinUI3 shell 必须替代这些能力：

- app lifecycle、single instance、activation、退出清理、dispatcher。
- window creation、show/hide、close/closing/closed、title、size/location、DPI、state、topmost、owner。
- HWND/XamlRoot/window handle 服务，尤其 WebView2 composition host 需要的窗口句柄。
- borderless window、caption buttons、drag regions、resize hit-test、system menu、dark mode。
- theme/resource bootstrapping、dialog/flyout theme 行为。
- 旧 XAML `props/targets`、PRI、manifest、winmd/pri/dll runtime pipeline。

## Phase 9 前禁止事项

- 不在 `Typedown.Core` 新增 `Windows.UI.Xaml` 或 `Microsoft.UI.Xaml` using。
- 不在 `Typedown.Core` 新增 `Page`、`UserControl`、`ResourceDictionary`、`DependencyProperty`、`DispatcherTimer`。
- 不让 Core 接口继续新增 `XamlRoot`、`UIElement`、`Rectangle`、`Point`、`Rect` 等 UI 类型。
- 不把 picker、dialog、window、theme、dispatcher 实现放回 Core。
- 不把 `Dev\Typedown.XamlUI` 重命名或提升为 `Typedown.UI`。
- 不升级 React、CRA、TypeScript 或改变 `Typedown.Editor` bundle 形态。
- 不做 ARM64 适配；ARM64 仍等 WinUI3 shell 后重新规划。

## Phase 9 最小 Spike 目标

Phase 9 只验证 WinUI3 shell 是否能复用前面抽出的边界，不迁移全部控件：

- 创建最小 `Typedown.WinUI` shell，能启动、创建窗口并注册 WinUI 平台服务。
- 显示现有 `RootControl` 或等价最小 UI 容器。
- 注册 `IMarkdownEditor` 的 WinUI/WebView2 host 实现。
- 加载同一份 `Resources\Statics` editor bundle。
- 验证一次 JS -> C# editor invoke 和一次 C# -> JS message。
- 验证 picker、dialog、dispatcher/window context、app activation 的最小 smoke。
- 对比 `Debug_Local|x64` 基线列出功能缺口。
