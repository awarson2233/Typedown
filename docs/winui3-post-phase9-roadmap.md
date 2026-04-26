# WinUI3 Phase 9 后续路线

本文档重新设计 Phase 9 之后的迁移路线。当前决策仍然是 `Typedown.UI` 与 `Typedown.WinUI` 分离，`Dev\Typedown.XamlUI` 只作为 legacy XAML host 保留到 WinUI3 功能等价后删除。

当前状态：Phase 9、Phase 10 和 Phase 11 已完成到 WinUI3 smoke/editor-host 基线。`Dev\Typedown.WinUI` 已成为 WinUI3 shell spike、平台服务、WebView2 editor host 和 Package/Unpackaged 双入口基线；日常启动入口为 `Debug_Local|x64 + Typedown.WinUI (Unpackaged)`，Package/MSIX 入口保留为 `Debug|x64 + Typedown.WinUI (Package)` 的部署验证路径。

## 总原则

- Phase 9 之后不再继续扩大 legacy `Typedown.XamlUI` 能力，只修阻塞性基线问题。
- 不把 `Dev\Typedown.XamlUI` 重命名为 `Typedown.UI`，也不把旧 host/run loop/HWND 代码迁入 `Typedown.UI`。
- `Typedown.WinUI` 先承担 WinUI3 shell、平台服务、WebView2 host 和打包入口。
- `Typedown.UI` 后建立页面、控件、资源、UI ViewModel 和 UI 编排边界。
- `Typedown.Core` 继续向平台中立契约收敛，不新增 `Windows.UI.Xaml` 或 `Microsoft.UI.Xaml` 依赖。
- ARM64 适配排在 WinUI3 shell 切换完成之后，不在 legacy XamlUI 架构上适配 ARM64。
- React/CRA/TypeScript 维持冻结，只验证同一份 editor bundle 能被新 shell 加载。

## 阶段总览

```text
Phase 9   最小 WinUI3 shell spike（已完成）
Phase 10  WinUI3 平台服务闭环与启动基线（已完成）
Phase 11  WebView2 editor host smoke 等价（已完成，真实本地文档 load/save 边界已完成）
Phase 12  Typedown.UI 项目骨架与 UI 注册边界（已完成）
Phase 13  低风险 UI 资源/页面/控件迁移（第一批已完成）
Phase 14  Debug_Local 主启动路径切换到 WinUI3
Phase 15  legacy XamlUI 退场与构建清理
Phase 16  ARM64 与打包验证
```

## Phase 9：最小 WinUI3 Shell Spike

**目标：** 证明 WinUI3 shell 可以启动、创建窗口、接入现有 DI/Core 契约，并加载最小 UI 容器。该阶段只做 spike，不追求功能等价。

**状态：已完成。** 当前最小 WinUI3 shell 位于 `Dev\Typedown.WinUI`，它不依赖 legacy `Dev\Typedown.XamlUI`。

**主要任务：**

- 新建 `Dev\Typedown.WinUI` 项目或等价 WinUI3 app 工程。
- 创建最小 `App`、`MainWindow`、composition root。
- 复用当前 `Typedown.Core` 的服务注册，避免复制业务逻辑。
- 接入已抽出的 `IAppDataPathProvider`、`IDialogService`、`IFilePickerService`、`IUiDispatcher`、`IWindowContext`、`IAppActivationService` 的 WinUI3 实现桩。
- 显示一个最小页面或容器，列出 WinUI3 侧尚未实现的能力。

**验收：**

- `Typedown.WinUI` 能在 x64 Debug 启动并显示窗口。
- 构建不依赖 `Dev\Typedown.XamlUI` 的 props/targets/runtime 文件。
- 新 shell 不要求 ARM64 通过。
- 生成 Phase 10 的缺口清单。

**执行方式：** 单独 worktree + 单个代码修改 agent。可以并行派 readonly agent 扫描 WinUI3 模板、服务接口和 legacy host 能力，但实际改代码必须集中到一个集成分支。

## Phase 10：WinUI3 平台服务闭环与启动基线

**目标：** 让 WinUI3 shell 替代 legacy shell 的平台服务入口，而不是只显示空窗口。

**状态：已完成。** 当前已完成 contracts 拆分、WinUI 平台服务桩、solution 配置收敛、Package/Unpackaged 启动基线。

**主要任务：**

- 实现 WinUI3 版 dispatcher/window context/window service。
- 实现 WinUI3 版 dialog 与 file picker。
- 实现 WinUI3 版 app activation/single instance。
- 建立 shell service 注册扩展，例如 `AddWinUIShellServices()`。
- 记录和 legacy 行为不一致的点，先不做美化或大规模 UI 迁移。
- `Debug_Local|x64 + Unpackaged` 必须保持无证书、无 MSIX 部署、无 WinAppSDK DeploymentManager 自动初始化。
- `Debug|x64 + Package` 保留为 signed MSIX 验证路径。

**验收：**

- WinUI3 shell 内可触发最小 picker/dialog smoke。
- 二次启动、窗口前置、退出清理有可验证路径。
- `Typedown.Core` 不新增 UI framework 依赖。

**执行方式：** 串行。该阶段会触碰 DI、窗口上下文和激活服务，和 Phase 9 的输出强耦合，不适合多代码 agent 同时改。

## Phase 11：WebView2 Editor Host 等价

**目标：** 在 WinUI3 shell 中重建 editor host，验证同一份 React bundle 和 native bridge 协议可复用。

**当前入口：** `Debug_Local|x64 + Typedown.WinUI (Unpackaged)` 是日常实现和验证入口；`Debug|x64 + Typedown.WinUI (Package)` 是部署验证入口。

**状态：已完成到 Phase 11 验收边界。** 已建立“可打开并可编辑 smoke”的 WinUI3 WebView2 editor host，并完成 contract-backed editor document session 边界。`Dev\Typedown.WinUI\Controls\WinUIEditorHost.cs` 通过本地 `WinUIEditorBridgeAdapter` 解析 `invoke` / `message` / `diffmsg`；文档状态、真实本地 markdown 文件 `LoadFile/Save/SaveAs(save copy)`、`GetSettings` payload、以及 `LoadFile/Search/Replace/SearchOpenChange/ThemeChanged/SettingsChanged/Export` 的 host command factory 已收敛到 `Dev\Typedown.Core.Contracts\Editor\*` + `WinUIEditorDocumentSession`。当前 WinUI host 还提供了本地文件运行路径：可在初始化前设置文件路径，或通过 host 方法触发 `LoadFile/Save/SaveAs`，并在 ready handshake 之后把 session 当前 state 发送给 WebView。当前实现仍只接本地文件系统和 smoke-safe stub，不引用 `Dev\Typedown.Core` 或 legacy `Dev\Typedown.XamlUI`。

**收尾结论：** Phase 11 不再继续扩大功能范围。真实 Core 文档服务、导出/打印、图片选择、浮层 UI、查找替换 UI 和完整 editor command parity 全部转入 Phase 13/14 前的后续功能迁移，不作为 Phase 12 的前置阻塞。

**主要任务：**

- [x] 新建 WinUI3 版最小 editor host 迁移点。
- [x] 加载 `Dev\Typedown\Resources\Statics\index.html` 或输出目录中的等价 static bundle。
- [x] 验证 `window.chrome.webview` 的 JS -> C# 原始消息接收路径。
- [x] 验证 C# -> JS 的 host message smoke 路径。
- [x] 建立本地 bridge/command adapter，支持前端最小 invoke 与编辑状态事件。
- [x] 让 WinUI smoke host 能打开并编辑一份本地 smoke markdown。
- [x] 修复 `Debug|x64 + Package` 的 editor static bundle 打包规则，确保 `Resources\Statics\index.html` 进入 MSIX payload。
- [ ] 后续迁移：将 `IEditorDocumentSession` 从当前本地文件系统实现替换为真实 legacy/Core 文档服务接入。
- [ ] 后续迁移：迁移真实 `MarkdownEditor` 命令编排、真实导出/打印/图片选择、完整主题同步和完整编辑状态事件。
- 后续迁移：对齐主题、DPI、输入转发、焦点和窗口句柄需求。
- 保持 bridge 协议语义不变；如果需要新增 WinUI 侧 adapter，先落在 `Typedown.WinUI`，不要提前批量移动 legacy XAML 页面/控件。

**验收：**

- WinUI3 shell 可以显示 editor，并能打开一份本地 smoke markdown 进入可编辑状态。
- 至少完成一次 editor bundle 初始化、JS/C# 双向消息 smoke，以及最小 `invoke`/`diffmsg` 适配。
- `Debug_Local|x64` 输出和 `Debug|x64` Package 输出都包含 `Resources\Statics\index.html`。
- Package 注册损坏时，使用当前 `Debug` 输出目录下的 `AppxManifest.xml` 重新注册；不把注册状态损坏误判为 WebView/editor 协议失败。
- 真实 Core 文档服务接入、导出/打印、浮层 UI、查找替换 UI 和完整 editor command parity 可拆到 Phase 11 后续子任务，不与 `Typedown.UI` 控件搬迁混做。
- 不升级前端依赖，不改变 bridge 协议语义。

**执行方式：** 串行集成，允许并行 readonly agent 分别检查 legacy `MarkdownEditor`、`WebViewController`、`Transport` 协议，但代码修改应由一个 agent 完成。

## Phase 12：Typedown.UI 项目骨架与 UI 注册边界

**目标：** 建立真正的 `Typedown.UI`，但只先承接 UI 层注册和最小页面，不做批量控件搬迁。

**状态：已完成。** Phase 12 的目的不是迁移 legacy `Dev\Typedown.XamlUI`，也不是把 `Typedown.WinUI` 与 `Typedown.UI` 合并；它已建立 UI 层边界，让后续 Phase 13 能按目录搬迁页面/控件/资源。

**详细计划：** 见 [phase12-mvvm-ui-plan.md](./phase12-mvvm-ui-plan.md)。Phase 12 引入 MVVM 骨架，但不改变当前页面布局。`Typedown.UI` 先承接 `MainPageViewModel`、MVVM 基础类型和 `AddTypedownUI` 注册入口；`Typedown.WinUI` 仍负责 Window、Frame、平台服务、WebView2 host 和 Package/Unpackaged 启动。

**主要任务：**

- [x] 新建 `Dev\Typedown.UI` 项目，目标框架与当前 WinUI shell 保持一致。
- [x] 定义 UI 层服务注册扩展 `AddTypedownUI()`，但不在 UI 层注册 WinUI platform services。
- [x] 放入最小 MVVM 骨架，先承接页面级组合，不搬迁 legacy host/run loop/HWND 代码。
- [x] 将 `MainPageViewModel` 和页面级状态注册从 WinUI shell 中拆出到 UI 层。
- [x] 明确 `Typedown.UI -> Typedown.Core.Contracts`，禁止 `Typedown.UI -> Typedown.WinUI`。
- [x] 更新 architecture tests，锁定 `Typedown.WinUI -> Typedown.UI`、`Typedown.UI` 不反向引用 WinUI、`Typedown.Core.Contracts` 不引用 UI framework。

**验收：**

- `Typedown.WinUI -> Typedown.UI -> Typedown.Core.Contracts` 依赖方向成立；如确需 `Typedown.UI -> Typedown.Core`，必须记录具体原因。
- legacy 启动路径仍可保留到切换完成。
- UI 项目不包含 XamlUI host/run loop/HWND 代码。
- `Debug_Local|x64 + Typedown.WinUI (Unpackaged)` 仍可构建并加载 editor bundle。
- `Debug|x64 + Typedown.WinUI (Package)` 仍可构建，Package payload 仍包含 editor static bundle。

**执行方式：** 可以多 agent 拆分，但必须只有一个集成任务修改 `.sln` / `.csproj`。推荐拆分如下：

- Agent A：项目骨架与 solution 集成，负责 `Dev\Typedown.UI`、`Typedown.sln`、项目引用。
- Agent B：UI 注册边界，负责 `AddTypedownUI()`、最小 ViewModel/page 组合点，不修改 solution。
- Agent C：架构测试与文档，负责依赖方向测试、Phase 12 文档和验证命令。

合并顺序必须串行：先 Agent A，再 Agent B，最后 Agent C。每步合并后运行 architecture tests，最后运行 WinUI Debug/Debug_Local 构建。

## Phase 13：低风险 UI 资源/页面/控件迁移

**目标：** 按风险从低到高把 UI 层内容迁入 `Typedown.UI`，不要一次性搬空 `Typedown.Core`。

**详细计划：** 见 [phase13-ui-migration-plan.md](./phase13-ui-migration-plan.md)。Phase 13 先做清点和分批迁移，不移动 WinUI shell、WebView2 host、平台服务、打包入口，也不删除 legacy `Typedown.XamlUI`。

**第一批范围：** 只迁移 `Typedown.WinUI` smoke 页面当前暴露的纯展示状态和静态说明文本。候选清单已记录在 [phase13-ui-migration-plan.md](./phase13-ui-migration-plan.md) 的 `Phase 13 Inventory`。`WinUIEditorHost`、WinUI platform services、`Package.appxmanifest`、`launchSettings.json`、legacy `Typedown.XamlUI` 和 legacy `MarkdownEditor` 均延后，不进入第一批迁移。

**第一批状态：已完成。** 已完成到 `Typedown.UI.Resources.MainPageTextResources`。当前只移动 smoke 页面静态文本和默认状态提示；`MainPage.xaml` 布局、`WinUIEditorHost`、platform services、Package/Unpackaged 启动配置均保持在 `Typedown.WinUI`。

**推荐顺序：**

1. 资源字典、字符串、图片、converter。
2. 纯显示控件和无平台服务依赖的 controls。
3. settings 页面和低风险页面。
4. root/navigation 结构。
5. 依赖 editor/window/dialog 的复杂页面。

**验收：**

- 每一批迁移后 WinUI3 shell 可构建。
- legacy `Debug_Local|x64` 在退场前仍可作为对照基线。
- `Typedown.Core` 中 UI framework 引用数量单调减少。

**执行方式：** 可使用多 worktree 多 agent，但每批必须按目录分片，且同一批不能同时修改同一个 `.csproj`、`.sln` 或共享资源字典。每批完成后回到集成分支验证。

## Phase 14：Debug_Local 主启动路径切换到 WinUI3

**目标：** 让 WinUI3 shell 成为主调试入口，legacy shell 退到对照/回滚路径。

**主要任务：**

- 调整 solution startup 与文档，使 `Typedown.WinUI` 成为默认 Debug/Debug_Local 入口。
- 建立 WinUI3 版 `verify-baseline` 或扩展现有脚本。
- 对齐当前核心用户路径：启动、打开文件、编辑、保存、设置、导出或已保留的导出入口。
- 对比 legacy shell，列出仍未迁移的非阻塞能力。

**验收：**

- VS 直接启动进入 WinUI3 shell。
- `Debug_Local|x64` 不需要前端 dev server 即可运行。
- 旧 shell 可以保留但不再作为主线入口。

**执行方式：** 串行。该阶段改变默认启动路径和验证脚本，不应并行修改。

## Phase 15：Legacy XamlUI 退场与构建清理

**目标：** 删除或归档 `Dev\Typedown.XamlUI` 依赖，清理 UWP/WinUI2 host 构建链。

**主要任务：**

- 移除主 app/Core 对 `Dev\Typedown.XamlUI` 的引用。
- 删除 XamlUI props/targets/runtime copy 逻辑。
- 清理 legacy PRI、winmd、dll、buildTransitive 相关路径。
- 删除或隔离旧 `Windows.UI.Xaml.Hosting` host 代码。
- 更新 docs，说明回滚点和最后一个 legacy host commit。

**验收：**

- 主线构建不再需要 `Typedown.XamlUI.pri`、legacy `Microsoft.UI.Xaml.dll` 或 HostingContract 制品。
- WinUI3 shell 仍可完成 Phase 14 的 smoke。
- 删除不影响 editor static bundle。

**执行方式：** 串行。该阶段是高风险删除，需要先有 Phase 14 的稳定替代入口。

## Phase 16：ARM64 与打包验证

**目标：** 在 WinUI3 shell 稳定后再开始 ARM64 和 MSIX/Windows App SDK 打包验证。

**主要任务：**

- 修正 solution/project 的 ARM64 配置映射。
- 验证 `win-arm64` restore/build。
- 验证 Windows App SDK、WebView2、SQLite、packaging 在 ARM64 的行为。
- 更新构建矩阵，把 ARM64 从“风险记录”升级为“验证目标”。

**验收：**

- x64 仍是稳定基线。
- ARM64 构建问题被记录为 WinUI3 shell 之后的新任务，不回到 legacy XamlUI 架构修。
- 打包路径有明确成功/失败日志。

**执行方式：** 可并行做配置扫描和依赖兼容性扫描，但实际 build/packaging 修复应串行。

## 串并行策略

- Phase 9、10、11、14、15 必须串行，因为它们共享 shell、DI、启动路径和 WebView host。
- Phase 12 可以有限并行，但必须避免多个 agent 同时修改 solution/project 文件。
- Phase 13 最适合多 agent 多 worktree，按目录批次拆分迁移。
- Phase 16 可先并行扫描，修复阶段串行。

## 推荐分支模型

```text
winui3-migration
  work/phase9-winui3-shell-spike
  work/phase10-winui3-platform-services
  work/phase11-winui3-editor-host
  work/phase12-typedown-ui-skeleton
  work/phase13-ui-migration-batch-N
  work/phase14-winui3-debug-local-cutover
  work/phase15-retire-legacy-xamlui
  work/phase16-arm64-packaging
```

每个阶段都应通过 PR 回到 `awarson2233/winui3-migration`，不推送到原作者 remote。

## 下一步建议

下一步应启动 Phase 12：创建 `Dev\Typedown.UI` 的最小项目骨架和 UI 注册边界。不要在 Phase 12 批量搬迁 legacy XAML 控件，也不要把 `Typedown.WinUI` 与 `Typedown.UI` 合并；Phase 12 只为 Phase 13 的低风险 UI 迁移建立可测试边界。
