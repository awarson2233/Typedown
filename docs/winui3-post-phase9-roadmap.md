# WinUI3 Phase 9 后续路线

本文档重新设计 Phase 9 之后的迁移路线。当前决策仍然是 `Typedown.UI` 与 `Typedown.WinUI` 分离，`Dev\Typedown.XamlUI` 只作为 legacy XAML host 保留到 WinUI3 功能等价后删除。

当前状态：Phase 9 和 Phase 10 已完成。`Dev\Typedown.WinUI` 已成为 WinUI3 shell spike 和平台服务基线；日常启动入口为 `Debug_Local|x64 + Typedown.WinUI (Unpackaged)`，Package/MSIX 入口保留为 `Debug|x64 + Typedown.WinUI (Package)` 的部署验证路径。

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
Phase 11  WebView2 editor host 等价（下一步）
Phase 12  Typedown.UI 项目骨架与 UI 注册边界
Phase 13  低风险 UI 资源/页面/控件迁移
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

**当前入口：** 从 `Debug_Local|x64 + Typedown.WinUI (Unpackaged)` 开始实现和验证。不要把 Package 证书问题作为 Phase 11 的阻塞项。

**主要任务：**

- 新建或迁移 WinUI3 版 `MarkdownEditor` / `WebViewController`。
- 加载 `Dev\Typedown\Resources\Statics\index.html` 或等价 static bundle。
- 验证 `window.chrome.webview` 的 JS -> C# 调用。
- 验证 C# -> JS 的 editor command/message。
- 对齐主题、DPI、输入转发、焦点和窗口句柄需求。
- 保持 bridge 协议语义不变；如果需要新增 WinUI 侧 adapter，先落在 `Typedown.WinUI`，不要提前批量移动 legacy XAML 页面/控件。

**验收：**

- WinUI3 shell 可以显示 editor。
- 至少完成一次打开文档、编辑器初始化、JS/C# 双向消息 smoke。
- 不升级前端依赖，不改变 bridge 协议语义。

**执行方式：** 串行集成，允许并行 readonly agent 分别检查 legacy `MarkdownEditor`、`WebViewController`、`Transport` 协议，但代码修改应由一个 agent 完成。

## Phase 12：Typedown.UI 项目骨架与 UI 注册边界

**目标：** 建立真正的 `Typedown.UI`，但只先承接 UI 层注册和最小页面，不做批量控件搬迁。

**主要任务：**

- 新建 `Dev\Typedown.UI` 项目。
- 定义 UI 层服务注册扩展，例如 `AddTypedownUI()`。
- 放入最小 page/control/resource 骨架。
- 将页面级 ViewModel 注册从 WinUI shell 中拆出到 UI 层。
- 明确 `Typedown.UI -> Typedown.Core`，禁止 `Typedown.UI -> Typedown.WinUI`。

**验收：**

- `Typedown.WinUI -> Typedown.UI -> Typedown.Core` 依赖方向成立。
- legacy 启动路径仍可保留到切换完成。
- UI 项目不包含 XamlUI host/run loop/HWND 代码。

**执行方式：** 可以多 agent 拆分为项目骨架、DI 注册、依赖验证三个任务，但需要一个集成 worktree 统一合并，避免 solution/project 文件冲突。

## Phase 13：低风险 UI 资源/页面/控件迁移

**目标：** 按风险从低到高把 UI 层内容迁入 `Typedown.UI`，不要一次性搬空 `Typedown.Core`。

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

下一步应启动 Phase 11：在现有 `Dev\Typedown.WinUI` 上接入 WebView2 editor host 和应用骨架。不要在 Phase 11 同时创建完整 `Typedown.UI` 或迁移控件；那会把“editor host 等价”和“UI 搬迁”两个风险叠在一起。
