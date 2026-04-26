# Phase 10b WinUI Platform Services

## 本阶段完成内容

Phase 10b 的目标不是迁移编辑器或接入真实 `Typedown.Core`，而是在 `Dev/Typedown.WinUI` 中补齐一组最小平台服务，让 WinUI3 shell 可以真正消费 `Dev/Typedown.Core.Contracts` 暴露的接口。

当前已完成：

- 新增 `WinUIAppDataPathProvider`，为 WinUI shell 提供本地 app data、settings、db、backup 路径。
- 新增 `WinUIWindowContext`，包装 `Microsoft.UI.Xaml.Window`，提供标题、激活状态、`ViewRoot`、`Activate`、`BringToFront`、`RequestClose`。
- 新增 `WinUIUiDispatcher`，基于 `DispatcherQueue` 提供 `RunAsync` / `RunIdleAsync` 及泛型返回值版本。
- 新增 `WinUIDialogService`，基于 `ContentDialog` 和 `XamlRoot` 的最小实现。
- 新增 `WinUIFilePickerService`，基于 WinUI3 picker 并通过窗口 HWND 完成初始化。
- 新增 `WinUIAppActivationService`，保留 contracts 事件面，提供最小可编译激活返回。
- 新增 `WinUIPlatformServices` 聚合类，并由 `App.OnLaunched` 负责创建和接线。
- 更新 `MainPage` 与 probe 文案，明确 Phase 10b 只验证平台服务，不宣称真实 UI / editor 已迁移。
- 更新架构测试，持续守住 `Typedown.WinUI -> Typedown.Core.Contracts` 的边界，不允许直接引用 legacy core 或 legacy XAML host。

## 仍然刻意不做的事

本阶段边界保持严格：

- 不引用 `Dev/Typedown.Core`。
- 不引用 `Dev/Typedown.XamlUI`。
- 不迁移旧页面控件。
- 不接入真实编辑器或 WebView2 host parity。
- 不做旧数据迁移。
- 不做 ARM64。

也就是说，Phase 10b 证明的是：

`Typedown.WinUI` 已经具备一套可编译、可接线、可被后续 phase 替换/增强的平台服务外壳。

它还没有证明：

真实产品行为已经从 legacy app 迁到 WinUI3。

## 实现位置

- `Dev/Typedown.WinUI/Services/WinUIAppDataPathProvider.cs`
- `Dev/Typedown.WinUI/Services/WinUIWindowContext.cs`
- `Dev/Typedown.WinUI/Services/WinUIUiDispatcher.cs`
- `Dev/Typedown.WinUI/Services/WinUIDialogService.cs`
- `Dev/Typedown.WinUI/Services/WinUIFilePickerService.cs`
- `Dev/Typedown.WinUI/Services/WinUIAppActivationService.cs`
- `Dev/Typedown.WinUI/Services/WinUIPlatformServices.cs`

接线入口：

- `Dev/Typedown.WinUI/App.xaml.cs`
- `Dev/Typedown.WinUI/Phase10ContractsProbe.cs`
- `Dev/Typedown.WinUI/Views/MainPage.xaml`
- `Dev/Typedown.WinUI/Views/MainPage.xaml.cs`

## 验证命令

```powershell
dotnet build .\Dev\Typedown.WinUI\Typedown.WinUI.csproj -c Debug -p:Platform=x64 /nologo /v:minimal /m:1 /nodeReuse:false
dotnet test .\Tests\Typedown.ArchitectureTests\Typedown.ArchitectureTests.csproj -c Debug /nologo /v:minimal
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\verify-baseline.ps1 -RepoRoot . -ExpectedMainBranch work/phase10b-winui-platform-services -AllowMainDirty -SkipEditorBuild
```

## 后续建议

### Phase 10c

- 把这批最小平台服务进一步抽成更稳定的 WinUI shell composition 根。
- 开始识别哪些 `Typedown.Core.Contracts` 接口需要补充更细的结果对象、错误语义和生命周期约束。
- 视需要增加针对 picker / dialog / activation 的更细粒度测试。

### Phase 11

- 接入真实 WebView2 editor host。
- 逐步把 legacy app 中与编辑器壳层直接相关的能力迁到 WinUI3 shell。
- 在不破坏 contracts 边界的前提下，规划真实 core 适配层，而不是直接把 `Typedown.Core` 拉进 WinUI 项目。
