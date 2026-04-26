# WinUI3 迁移前架构解耦计划

> **给后续 agent/开发者：** 执行本计划时应使用 `superpowers:subagent-driven-development` 或 `superpowers:executing-plans`，按任务逐项推进并在每个阶段后验证。

**目标：** 在不破坏当前 .NET 9 可运行基线的前提下，为后续 WinUI3 迁移切出必要的架构边界。

**架构原则：** 当前先保留 WinUI 2 / UWP XAML 家族的运行壳层和 `Typedown.XamlUI` 宿主能力，只抽出已经阻塞迁移的平台边界。第一阶段目标不是把 `Typedown.Core` 变成纯跨平台库，而是让未来 WinUI3 壳层能复用设置、文档流程、编辑器桥接和部分服务逻辑。

**技术栈现状：** .NET 9、`Typedown.XamlUI` 自定义 Windows XAML 宿主、WebView2、React 编辑器、EF Core SQLite、MSBuild / Visual Studio solution 配置。

---

## 当前阶段状态

- Phase 0：已完成。构建/运行基线、仓库检查脚本和 `Debug_Local|x64` 验证入口已建立。
- Phase 1：已完成。XamlUI 依赖已文档化并纳入主仓库 `Dev\Typedown.XamlUI`，主项目和 Core 项目引用仓库内 legacy host。
- Phase 2：已完成。AppData、settings、database、backup 路径已通过 `IAppDataPathProvider` 进入迁移期兼容边界。
- Phase 3：已完成。Dialog/file picker 已通过 Core 接口与 shell 实现隔离，close-only dialog 默认按钮语义已保留。
- Phase 4：已完成。UI dispatcher 与 window context 已通过 Core 接口抽出，当前 shell 通过 `UiDispatcher` / `WindowContext` 适配。
- Phase 5：已完成。编辑器 bridge 协议已文档化并从 `MarkdownEditor` 中收束到独立 bridge。
- Phase 6：已完成（当前 `work/phase6-app-activation` 实现，待主工作区 review/merge）。单实例与激活逻辑已通过 `IAppActivationService` 收束到 shell service。
- Phase 7：已完成。构建矩阵和 ARM64 风险已记录，不做 ARM64 适配。
- Phase 8：已完成。`Typedown.UI` / `Typedown.WinUI` / legacy XAML host 的模块边界已固化，不移动旧 XAML 控件。
- Phase 9：待执行。最小 WinUI3 shell spike，必须在 Phase 3/4/6 稳定后开始，并以 Phase 8 的模块边界为准。

Phase 3、Phase 4、Phase 6 必须串行推进。它们都会触碰 `Dev\Typedown\Injection.cs`，并且 Phase 6 会依赖前面已经稳定的 shell service 注册边界；不得并行创建实现分支。

## 当前基线

- 主仓库：`D:\source\repos\Typedown`
- 主分支：`winui3-migration`
- 主 fork remote：`awarson2233 https://github.com/awarson2233/Typedown`
- XamlUI legacy host：`D:\source\repos\Typedown\Dev\Typedown.XamlUI`
- XamlUI 来源：`D:\source\repos\Typedown.XamlUI` 的 `work/vs-debug-build-fixes` 状态
- 稳定本地运行模式：`Debug_Local|x64`，加载 `Dev\Typedown\Resources\Statics\index.html`
- 前端调试模式：`Debug|x64`，加载 `http://localhost:3000`，需要先运行 `yarn start`

已知基线命令：

```powershell
cd D:\source\repos\Typedown\Dev\Typedown.Editor
yarn
yarn build

$msbuild = & "${env:ProgramFiles(x86)}\Microsoft Visual Studio\Installer\vswhere.exe" -latest -requires Microsoft.Component.MSBuild -find MSBuild\Current\Bin\MSBuild.exe
& $msbuild D:\source\repos\Typedown\Dev\Typedown\Typedown.csproj /restore /t:Build /p:Configuration=Debug_Local /p:Platform=x64 /p:UseSharedCompilation=false /nologo /m:1 /nodeReuse:false /v:minimal
D:\source\repos\Typedown\Dev\Typedown\bin\x64\Debug_Local\net9.0-windows10.0.26100.0\win-x64\Typedown.exe
```

## 核心判断

当前项目不是干净的分层架构。`Typedown.Core` 同时包含 XAML 控件、ViewModel、对话框、文件选择器、dispatcher、带 Windows UI 类型的持久化模型和服务逻辑。迁移前如果先尝试把它整体改成纯 Core，会把风险放大。

当前已选择“`UI` 与 `WinUI` 分离”的迁移方案。这个方案的含义是：

- 不把当前 `Typedown.XamlUI` 直接重命名为 `Typedown.UI`。
- 当前 `Typedown.XamlUI` 定位为 legacy XAML host / 旧 XAML 宿主层，只服务现有可运行基线。
- 长期新增 `Typedown.UI`，承载 WinUI3 迁移后的页面、控件、资源、UI ViewModel 和应用级 UI 编排。
- 长期新增或保留 `Typedown.WinUI`，承载 `App`、`Window`、WebView2 宿主、平台服务实现、DI 注册、文件 picker/dialog、dispatcher/window context、单实例激活等 Windows 壳层能力。
- WinUI3 迁移完成并达到功能等价后，legacy XAML host 应逐步删除，而不是成为新架构核心模块。

当前启动链：

```text
Typedown.Program.Main()
  -> App.Launch()
  -> App.LaunchNewApplication()
  -> XamlApplication.Run()
  -> App.OnLaunched()
  -> new MainWindow()
  -> MainWindow.Content = RootControl
  -> RootControl.Frame.Navigate(MainPage)
```

高风险耦合点：

- `Typedown.XamlUI`：被 `App`、`MainWindow`、`MarkdownEditor`、`WebViewController`、`WindowService` 使用。
- `Windows.UI.Xaml`：广泛存在于 `Dev\Typedown.Core` 的控件、页面、converter、ViewModel 和模型中。
- WebView2 composition 宿主：`Dev\Typedown\Utilities\WebViewController.cs` 同时负责输入转发、DPI、Win32 cursor/window style 和 composition controller 创建。
- 编辑器协议：前端使用 `window.chrome.webview`，C# 侧通过 `MarkdownEditor -> Transport -> RemoteInvoke/EventCenter` 转发。
- AppData 路径：`Config.GetLocalFolderPath()` 当前依赖 `ApplicationData.Current.LocalFolder`，失败后回退到 Documents。
- 数据库：运行时是 EF Core 9，但迁移快照和兼容逻辑仍带 EF Core 3.1 历史。
- 构建：主 app 和 Core 都直接引用仓库内 `Dev\Typedown.XamlUI` legacy host。

## 明确非目标

- 不回到 `uwp/main`。
- 不推送到原作者 remote。
- 不从全局 `Windows.UI.Xaml` 到 `Microsoft.UI.Xaml` 替换开始。
- 不在本阶段升级 React、CRA、TypeScript 或主要前端依赖。
- 不把 Linux/macOS 跨平台作为近期目标。
- 不把 PDF 移除/重写和壳层边界抽取混在同一阶段。
- 不尝试一次性把 `Typedown.Core` 改成纯 Core。
- 不把现有 `ARM64` solution 配置当成已验证 ARM64 支持。
- 不在切换到 WinUI3 shell 前适配 ARM64；当前 XamlUI/UWP host 架构不适合作为 ARM64 适配基础。

## 目标边界

迁移前最小有效边界如下：

```text
Typedown shell
  负责 XamlApplication/XamlWindow、HWND、XamlRoot、dispatcher、
  picker/dialog 实现、WebView2 composition host、单实例 IPC、
  以及所有 XamlUI 专属行为。

Typedown.Core
  负责文档流程、设置模型、编辑器命令、bridge 协议、
  数据库访问、导出配置，以及平台中立服务契约。

Typedown.Editor
  保持冻结的 React 输出，只通过已文档化 native bridge 协议通信。
```

这个边界不是完整跨平台边界。它的目标是让未来 WinUI3 shell 可以替换当前 `Typedown.XamlUI` shell，同时尽量保留现有业务行为。

WinUI3 迁移后的目标边界如下：

```text
Typedown.WinUI
  负责 WinUI3 App/Window、WebView2 初始化、平台服务实现、DI 组合根、
  单实例激活、文件/对话框、dispatcher/window context 和 Windows 打包。

Typedown.UI
  负责应用页面、控件、资源字典、UI ViewModel、编辑器视图编排，
  只依赖 Core 契约和 WinUI 可用的 UI 技术，不承载进程启动或平台注册。

Typedown.Core
  负责文档流程、设置/数据库、编辑器 bridge 协议、导出配置和平台中立契约。

Typedown.Editor
  继续提供 React 编辑器 bundle；迁移期不升级 CRA/React/TypeScript。

Typedown.LegacyXamlHost 或 Typedown.XamlHost
  仅表示当前 Typedown.XamlUI 的临时归档/集成名称。
  它不应被命名为 Typedown.UI，也不应被新 WinUI3 代码依赖。
```

关键依赖方向：

```text
Typedown.WinUI -> Typedown.UI -> Typedown.Core
Typedown.WinUI -> Typedown.Core
Typedown.WinUI -> Typedown.Editor static bundle
Typedown.LegacyXamlHost -> 仅当前旧启动链使用
```

禁止方向：

- `Typedown.Core` 不引用 `Typedown.WinUI`。
- `Typedown.Core` 不新增 `Windows.UI.Xaml` 或 `Microsoft.UI.Xaml` 依赖。
- `Typedown.UI` 不负责进程启动、MSIX 打包、单实例 IPC 或平台服务注册。
- `Typedown.WinUI` 不直接沉淀页面业务逻辑；页面级逻辑应落到 `Typedown.UI`。

---

## Phase 0：冻结并文档化基线

**目标：** 在重构前把当前构建、运行、仓库依赖和验证方式固定下来。

**文件：**

- 新增：`docs/build-baseline.md`
- 新增：`scripts/verify-baseline.ps1`
- 新增：`scripts/verify-repos.ps1`

步骤：

- [ ] 记录主仓库和仓库内 XamlUI legacy host 的当前来源。
- [ ] 记录 `Debug`、`Debug_Local`、`Release` 的编辑器加载行为。
- [ ] 记录 `Debug|x64` 需要 `Dev\Typedown.Editor\yarn start`。
- [ ] 记录 `Debug_Local|x64` 是无需前端 dev server 的稳定基线。
- [ ] 添加仓库验证脚本，检查 `Typedown` 和 `Typedown.XamlUI` 都在 `winui3-migration`，且默认要求工作区干净。
- [ ] 添加 `Debug_Local|x64` 基线构建脚本。
- [ ] 运行脚本，并把验证方式写入文档。

验证：

```powershell
git -C D:\source\repos\Typedown status --short --branch
Test-Path D:\source\repos\Typedown\Dev\Typedown.XamlUI\Typedown.XamlUI.csproj
.\scripts\verify-baseline.ps1
```

建议提交：

```powershell
git add docs\build-baseline.md docs\winui3-migration-decoupling-plan.md scripts\verify-baseline.ps1 scripts\verify-repos.ps1
git commit -m "docs: freeze winui3 migration baseline"
```

## Phase 1：治理 XamlUI 依赖（已完成）

**目标：** 不再让 XamlUI legacy host 依赖成为隐式知识。

**文件：**

- 修改：`docs/build-baseline.md`
- 修改：`Dev\Typedown\Typedown.csproj`
- 修改：`Dev\Typedown.Core\Typedown.Core.csproj`
- 可选新增：`docs/xamlui-dependency.md`

步骤：

- [x] 记录为什么当前仍需要 `Typedown.XamlUI`，以及预期分支/commit。
- [x] 记录 `Typedown.csproj` 导入的 XamlUI props/targets 和复制的运行时文件。
- [x] 如果 `Dev\Typedown.XamlUI\Typedown.XamlUI.csproj` 缺失，在构建时给出明确错误。
- [x] 在 `Typedown.Core.csproj` 中也加入同样的缺失依赖提示。
- [x] 暂时保持 ProjectReference，不急于 submodule/subtree，先让基线脚本可重复验证。

验证：

```powershell
.\scripts\verify-repos.ps1
.\scripts\verify-baseline.ps1
```

建议提交：

```powershell
git add docs Dev\Typedown\Typedown.csproj Dev\Typedown.Core\Typedown.Core.csproj
git commit -m "build: document xamlui dependency boundary"
```

## Phase 2：抽出 AppData 与设置路径边界（已完成）

**目标：** 让数据库、设置、备份路径不再直接依赖 UWP storage API。

**文件：**

- 新增：`Dev\Typedown.Core\Interfaces\IAppDataPathProvider.cs`
- 新增：`Dev\Typedown\Services\AppDataPathProvider.cs`
- 修改：`Dev\Typedown.Core\Config.cs`
- 修改：`Dev\Typedown.Core\Services\AppDbContext.cs`
- 修改：`Dev\Typedown\Injection.cs`
- 新增或修改：`Tests\Typedown.Test`

步骤：

- [x] 新增 `IAppDataPathProvider`，提供本地 app 文件夹、设置文件路径、数据库文件路径和备份路径。
- [x] 在 `Typedown.Services.AppDataPathProvider` 中实现当前 Windows/UWP 兼容行为。
- [x] 根据状态需求注册为 scoped 或 singleton；如果只是解析稳定路径，优先 singleton。
- [x] 让 `Config.GetLocalFolderPath()` 通过 provider 获取路径，或加入带移除说明的临时静态桥接。
- [x] 更新 `AppDbContext`，让数据库路径来自 provider，同时保持当前 `Storage.db` 位置不变。
- [x] 为 fake provider 增加路径相关测试。

验证：

```powershell
dotnet test Tests\Typedown.Test\Typedown.Test.csproj --configuration Debug --no-restore
.\scripts\verify-baseline.ps1
```

## Phase 3：抽出 Dialog 与 Picker 服务

**目标：** 从应当跨壳层复用的 ViewModel/服务中移除直接 picker/dialog 构造。

**文件：**

- 新增：`Dev\Typedown.Core\Interfaces\IDialogService.cs`
- 新增：`Dev\Typedown.Core\Interfaces\IFilePickerService.cs`
- 新增：`Dev\Typedown\Services\DialogService.cs`
- 新增：`Dev\Typedown\Services\FilePickerService.cs`
- 修改：`Dev\Typedown.Core\ViewModels\FileViewModel.cs`
- 修改：`Dev\Typedown.Core\Services\ImageAction.cs`
- 修改：直接创建 `FileOpenPicker`、`FileSavePicker`、`FolderPicker`、`AppContentDialog` 的相关控件或服务
- 修改：`Dev\Typedown\Injection.cs`

步骤：

- [x] 定义不暴露 `ContentDialogResult` 的 dialog result DTO。
- [x] 定义不暴露 `FileOpenPicker`、`FileSavePicker` 或 owner-window API 的 picker request DTO。
- [x] 在 shell 服务中实现当前 UWP picker/dialog 行为。
- [x] 用 `IFilePickerService` 替换 `FileViewModel.SaveAs()`、`FileViewModel.Export()`、`FileViewModel.Import()` 中的 picker 构造。
- [x] 用 `IDialogService` 替换 `FileViewModel` 中的恢复、保存、导入、导出错误对话框。
- [x] 用 `IDialogService` 替换 `ImageAction` 中直接调用 `AppContentDialog` 的逻辑。
- [x] 除非阻塞 ViewModel/服务抽取，否则暂不迁移 XAML 控件。

验证：

```powershell
dotnet test Tests\Typedown.Test\Typedown.Test.csproj --configuration Debug --no-restore
.\scripts\verify-baseline.ps1
```

手工 smoke：

```text
打开 markdown 文件
另存 markdown 文件
导入 HTML
导出 HTML 或图片
触发未保存关闭确认对话框
```

## Phase 4：抽出 UI Dispatcher 与 Window Context

**目标：** 让 ViewModel 不再直接依赖 `CoreDispatcher`、`XamlRoot` 和原始 HWND。

**文件：**

- 新增：`Dev\Typedown.Core\Interfaces\IUiDispatcher.cs`
- 新增：`Dev\Typedown.Core\Interfaces\IWindowContext.cs`
- 新增：`Dev\Typedown\Services\UiDispatcher.cs`
- 新增：`Dev\Typedown\Services\WindowContext.cs`
- 修改：`Dev\Typedown.Core\ViewModels\UIViewModel.cs`
- 修改：`Dev\Typedown.Core\ViewModels\AppViewModel.cs`
- 修改：`Dev\Typedown\Windows\MainWindow.cs`
- 修改：`Dev\Typedown\Services\WindowService.cs`
- 修改：`Dev\Typedown\Injection.cs`

步骤：

- [x] 增加 dispatcher 抽象，提供 `RunAsync` 和 `RunIdleAsync`。
- [x] 用当前 `CoreDispatcher` 实现该抽象。
- [x] 增加 window context 抽象，覆盖活动窗口 handle、XAML root、标题、激活状态和关闭请求。
- [x] 在 `MainWindow` 创建和加载时初始化 window context。
- [x] 替换 ViewModel 中直接使用 `CoreApplication.GetCurrentView().CoreWindow.Dispatcher` 的位置。
- [x] 减少新增 `AppViewModel.MainWindow` 和 `AppViewModel.XamlRoot` 使用；本阶段保留兼容 shim。

验证：

```powershell
.\scripts\verify-baseline.ps1
```

手工 smoke：

```text
启动 app
移动/调整窗口大小
切换主题
打开第二个文件/窗口
未保存关闭
```

## Phase 5：稳定编辑器 Bridge 协议（已完成）

**目标：** 让 WebView 编辑器协议能被未来 WinUI3 shell 复用。

**文件：**

- 新增：`docs/editor-bridge-protocol.md`
- 新增：`Dev\Typedown.Core\Interfaces\IEditorBridge.cs`
- 新增：`Dev\Typedown.Core\Services\EditorBridge.cs`
- 修改：`Dev\Typedown.Core\Services\Transport.cs`
- 必要时修改：`Dev\Typedown.Core\Services\RemoteInvoke.cs`
- 修改：`Dev\Typedown\Controls\MarkdownEditor.cs`
- 可选修改：`Dev\Typedown.Editor\src\services\transport.ts`

步骤：

- [x] 文档化消息形状：`invoke`、`message`、`diffmsg`、host response `{ name, args }`、invoke response `{ code, data }`、error response `{ code, msg }`。
- [x] 增加 `IEditorBridge`，表示 host 到 editor 发送、editor 到 host 接收，不暴露 `CoreWebView2`。
- [x] 将 JSON 协议解析和分发从 `MarkdownEditor` 移到 bridge 服务。
- [x] 保持 `Transport` 和 `RemoteInvoke` 行为兼容。
- [x] 除非只是加无协议变化的薄 `NativeBridge`，否则不改 `window.chrome.webview` 前端行为。
- [x] 保持 `Resources\Statics` 输出形态不变。

验证：

```powershell
cd D:\source\repos\Typedown\Dev\Typedown.Editor
yarn build
cd D:\source\repos\Typedown
.\scripts\verify-baseline.ps1
```

手工 smoke：

```text
编辑器输入
打开 markdown 文件
保存 markdown 文件
复制/粘贴文本和图片
触发导出流程
确认编辑器主题更新
```

## Phase 6：抽出单实例与激活边界

**目标：** 将 mutex、pipe、激活转发移入 shell-level 服务，方便 WinUI3 替换。

**文件：**

- 新增：`Dev\Typedown.Core\Interfaces\IAppActivationService.cs`
- 新增：`Dev\Typedown\Services\AppActivationService.cs`
- 修改：`Dev\Typedown\App.cs`
- 必要时修改：`Dev\Typedown\Utilities\Common.cs`
- 修改：`Dev\Typedown\Injection.cs`

步骤：

- [x] 定义激活操作：首次启动、转发到已有实例、接收命令行打开请求。
- [x] 将 `Mutex`、`NamedPipeServerStream`、`NamedPipeClientStream` 从 `App.cs` 移入 `AppActivationService`。
- [x] 让 `App.cs` 只负责 shell 启动、WebView2 前置检查、窗口创建和 dispatcher 传递。
- [x] 保留转发打开文件后的 `SetForegroundWindow` 行为。

验证：

```powershell
.\scripts\verify-baseline.ps1
```

手工 smoke：

```text
启动一次 app
再次用 markdown 文件路径启动 app
确认已有实例打开/激活该文件
确认首个实例保持响应
```

## Phase 7：构建矩阵记录，不做 ARM64 适配（已完成）

**目标：** 记录当前构建矩阵和 ARM64 配置风险，但不在当前 XamlUI/UWP host 架构上适配 ARM64。ARM64 适配必须推迟到 WinUI3 shell 切换后。

**文件：**

- 新增：`docs/build-matrix.md`
- 可选新增：`scripts/inspect-build-matrix.ps1`

步骤：

- [ ] 记录当前 mismatch：部分 `ARM64` solution 配置仍将主 app 映射到 `x64`；`Debug_Local|ARM64` 才是真实 app-level ARM64 路径。
- [ ] 记录当前 packaging 的 `AppxBundlePlatforms` 与 ARM64 风险。
- [ ] 不新增 ARM64 构建脚本。
- [ ] 不运行 ARM64 构建作为当前阶段验证。
- [ ] 在 WinUI3 shell 切换后，重新制定 ARM64 适配计划和验证脚本。

完成记录：

- [x] 新增 `docs/build-matrix.md`，记录 solution、app/core/package 配置和 ARM64 风险。
- [x] 新增 `scripts/inspect-build-matrix.ps1`，提供只读配置检查。
- [x] 明确 `Debug|ARM64` / `Release|ARM64` 中多个项目仍映射到 `x64`。
- [x] 明确 `AppxBundlePlatforms` 当前只覆盖 `x64` / `x86`。
- [x] 明确 ARM64 适配推迟到 WinUI3 shell 切换后。

验证：

```powershell
.\scripts\verify-baseline.ps1
```

## Phase 8：固化 UI / WinUI / Legacy Host 模块边界（已完成）

**目标：** 在创建 WinUI3 shell 前，先把模块命名、依赖方向和迁移任务写死，避免把旧 `Typedown.XamlUI` 错命名为长期 `UI` 模块。

**文件：**

- 修改：`docs/winui3-migration-decoupling-plan.md`
- 修改：`docs/subagent-worktree-execution-plan.md`
- 修改：`docs/xamlui-dependency.md`
- 新增：`docs/winui3-target-architecture.md`

步骤：

- [x] 记录 `Typedown.XamlUI` 的当前职责：旧 XAML app/window/run loop、WinRT/XAML 运行时 glue、运行时资源/pri/dll 复制。
- [x] 记录 `Typedown.UI` 的长期职责：页面、控件、资源、UI ViewModel、编辑器视图编排。
- [x] 记录 `Typedown.WinUI` 的长期职责：App/Window、平台服务实现、DI composition root、WebView2 host、打包入口。
- [x] 明确 `Typedown.XamlUI` 不改名为 `Typedown.UI`；如需纳入主仓库，只能命名为 legacy host 语义。
- [x] 明确迁移前不把旧 UWP XAML 控件批量移动到 `Typedown.UI`，避免把 `Windows.UI.Xaml` 污染到目标模块。
- [x] 为后续 worktree 创建 `work/phase8-ui-winui-boundary`，只允许文档和最小 solution/project 边界验证，不做控件迁移。

完成记录：

- [x] 新增 `docs/winui3-target-architecture.md`。
- [x] 并行只读扫描 `Dev\Typedown.Core`、`Dev\Typedown`、`Dev\Typedown.XamlUI`。
- [x] 固化 `Typedown.UI`、`Typedown.WinUI`、`Typedown.Core`、legacy host 的职责和依赖方向。
- [x] 记录 Phase 9 前禁止事项。
- [x] 记录 Phase 9 最小 WinUI3 shell spike 验证目标。

验证：

```powershell
git diff -- docs\winui3-migration-decoupling-plan.md docs\subagent-worktree-execution-plan.md docs\xamlui-dependency.md
.\scripts\verify-baseline.ps1
```

手工 review：

```text
确认所有新文档都使用同一套命名：
Typedown.UI = 长期 UI 层
Typedown.WinUI = WinUI3 shell
Typedown.XamlUI / LegacyXamlHost = 当前旧宿主
```

## Phase 9：创建 WinUI3 Shell Spike

**目标：** 在迁移全部 XAML 控件前，证明前面抽出的边界确实可复用。

**文件：**

- 在 Phase 0-8 稳定后再新增 WinUI3 shell 项目，并以 `docs/winui3-target-architecture.md` 为边界约束。
- 尽量只通过已抽出的接口引用 `Typedown.Core`。
- 复用 `Resources\Statics` 和编辑器 bridge 协议。

步骤：

- [ ] 创建最小 WinUI3 app 项目，能启动、创建窗口并注册 shell 服务。
- [ ] 在 WinUI3 WebView2 中加载同一份静态编辑器 bundle。
- [ ] 实现最小 WinUI3 版本 `IEditorBridge`、`IUiDispatcher`、`IWindowContext`、`IDialogService`、`IFilePickerService`。
- [ ] 在 shell/editor proof 成功前，暂不迁移 `RootControl`、`MainPage` 和详细 XAML resources。
- [ ] 对比当前 `Debug_Local|x64` 基线，列出功能缺口。

验证：

```text
WinUI3 shell 能启动
编辑器静态 bundle 能加载
host-to-editor message 能工作
editor-to-host invoke 能工作
open/save picker 能工作
settings/db path 指向预期迁移位置
```

---

## 可并行工作流

Phase 0 提交后，可以按以下工作流分配 subagent 或 worktree。当前 Phase 1、Phase 2、Phase 3、Phase 4、Phase 5 已完成，后续应从 Phase 6/7/8 开始推进：

- 构建/仓库治理：Phase 0、Phase 1、Phase 7。
- 平台服务：Phase 2、Phase 3、Phase 4 已完成；Phase 6 必须在 Phase 4 合并验证后执行。
- 编辑器 bridge：Phase 5。
- 模块边界治理：Phase 8，只做命名、职责、依赖方向和迁移切分。
- WinUI3 spike：Phase 9，仅在平台服务、编辑器 bridge 和模块边界稳定后开始。

避免并行编辑同一文件：

- `Dev\Typedown\Injection.cs` 会被多数服务抽取阶段修改。
- `Dev\Typedown.Core\ViewModels\FileViewModel.cs` 同时涉及 picker/dialog 和 editor/export 流程。
- `Dev\Typedown\Controls\MarkdownEditor.cs` 同时涉及 editor bridge 和 WebView host。
- `Typedown.sln` 与 csproj 文件在构建治理阶段应由单一工作流负责。

## 推荐立即执行

下一步执行 Phase 9：创建最小 WinUI3 shell spike。Phase 9 必须遵守 `docs/winui3-target-architecture.md`，不得把 legacy host 内容迁入 `Typedown.UI`，不得做 ARM64 适配。

## 正式 WinUI3 迁移前的完成标准

- `Debug_Local|x64` 构建和启动方式已脚本化验证。
- XamlUI legacy host 已纳入主仓库并可检查。
- App data / database 路径已通过接口访问。
- 文档流程用到的 dialog 和 file picker 已通过接口访问。
- UI dispatcher 和 window context 已通过接口访问。
- 编辑器 bridge 协议已文档化，不再只由 `MarkdownEditor` 隐式承载。
- 单实例 IPC 已抽成可替换 shell service。
- 当前 ARM64 风险已文档化，正式 ARM64 适配计划推迟到 WinUI3 shell 切换后。
- React 编辑器保持功能不变，构建输出仍落在 `Dev\Typedown\Resources\Statics`。
