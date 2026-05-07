# Typedown WinUI3 架构审查记录

更新日期：`2026-05-07`

本文记录当前 `D:\source\repos\Typedown` 工作区的实际架构状态、主要问题和后续治理优先级。结论基于当前代码、项目引用、架构测试、现有文档，以及针对 `Core`、`Presentation`、`WinUI app`、总体边界的并行子代理审查。

## 一、当前有效架构

当前仓库的有效架构不是旧文档里那套仍在描述 `Typedown.UI` 的目标态，而是下面这个迁移中的双宿主结构：

```text
Typedown.Editor
  React/Web 前端编辑器产物，最终由 WebView2 承载

Typedown.Core
  平台中立程度有限的核心层
  持有模型、EF 持久化、editor bridge 基础服务、运行时模型、部分系统能力契约

Typedown.Presentation
  shell-agnostic MVVM / 应用编排层
  持有 AppViewModel、各子 ViewModel、UI 端口、部分设置/上传/编辑器编排逻辑

Typedown.WinUI
  WinUI 3 app head / XAML shell / WebView2 host / 平台服务实现 / 打包入口

Typedown
  legacy app head
  仍依赖 Typedown.XamlUI 承载旧 XAML host

Typedown.XamlUI
  legacy XAML runtime glue / host
```

当前代码中的主依赖方向基本成立：

```text
Typedown.WinUI -> Typedown.Presentation -> Typedown.Core
Typedown.WinUI -> Typedown.Core
Typedown -> Typedown.Presentation -> Typedown.Core
Typedown -> Typedown.XamlUI
```

这说明“分层方向”没有走偏，但很多边界仍处于迁移期妥协状态。

## 二、核心结论

### 2.1 方向合理，但治理已经掉相位

当前最严重的问题不是项目引用方向错误，而是：

- 架构测试已经不是可信 guardrail
- 文档大量停留在旧阶段命名和旧边界
- 生命周期和依赖图被 `IServiceProvider` 与根 provider 解析方式隐掉
- editor bridge、资源 owner、packaged path 等关键边界还没有收敛成稳定规则

换句话说：仓库现在是“能 build、能继续迁移”，但“不能依赖现有文档与测试来约束迁移质量”。

### 2.2 当前最危险的结构性问题

#### A. 架构 guardrail 失效

`Tests\Typedown.ArchitectureTests` 当前包含过期断言，已经不能稳定反映当前架构真相，例如：

- 仍断言 `Typedown.Core` / `Typedown.Presentation` 为 `net9.0`
- 仍断言旧菜单/旧 certificate 资产形状
- 多个测试把历史阶段事实当成当前事实

结果是：

- 当前 `ArchitectureTests` 处于红状态时，后续真正的边界退化也可能被掩盖
- “build 通过但架构已坏”与“测试本来就坏”无法区分

这不是文档问题，而是治理问题。

#### B. 文档真相源失效

当前 `docs/` 内存在多套不同相位的架构叙述：

- 一部分文档还描述 `Typedown.UI`
- 一部分文档还描述 `Core -> XamlUI`
- 一部分文档已经改用 `Typedown.Presentation`
- 一部分计划文档已经假设 `Typedown.UI` / `Core.Legacy` 退场

当前代码已经说明：

- `Dev\Typedown.UI` 不存在
- `Dev\Typedown.Core.Legacy` 不存在
- `Dev\Typedown.Core` 无项目引用
- `Dev\Typedown.Presentation` 已成为当前应用编排层

因此，如果继续按旧文档推进，只会把实现导向错误边界。

#### C. 生命周期模型不成立

`Typedown.Presentation` 和 `Typedown.Core` 把不少服务注册为 `Scoped`，但 `Typedown.WinUI` 组合根当前只构造一个根 provider，并直接从根 provider 解析 `AppViewModel`。

这会导致：

- `Scoped` 实际退化为 app-root singleton
- `AppViewModel` 及其下游可释放对象不再有明确 scope 生命周期
- `RemoteInvoke` / `EventCenter` / ViewModel 订阅释放语义失真

这已经不是代码风格问题，而是运行时结构问题。

#### D. 依赖图被 service locator 隐掉

当前 `Presentation` 的主要 ViewModel 普遍只接收 `IServiceProvider`，然后在属性访问时解析：

- 子 ViewModel
- 端口接口
- 运行时服务

WinUI 控件侧又进一步通过 `AppViewModel.ServiceProvider` 做二次 service-location。

结果是：

- 构造函数不表达真实依赖
- 可空性和依赖闭包不可见
- 很难判断某个行为到底属于 VM、组合根、还是壳层
- 很难通过测试阻止“继续变坏”

#### E. Presentation / Core 仍有壳层概念泄漏

虽然 `Presentation` 当前不直接引用 WinUI/XamlUI 包，但端口形状仍暴露：

- `nint WindowHandle`
- `object ViewRoot`
- `GetXamlSourceHandle(...)`
- `TextDataFormat.Xaml`
- `PInvoke.WINDOWPLACEMENT`

`Core` 也仍公开大量 Win32 / Shell / DWM PInvoke。

这些在迁移期可以接受，但不能再被当作“已经平台中立”。

#### F. Editor bridge 还没有明确 owner

当前 editor 相关边界有两套现实：

- legacy app 侧仍保留 `MarkdownEditor` / `WebViewController`
- WinUI 侧已有 `WinUIEditorHost` / `WinUIEditorDocumentSession`

但同时：

- `EditorHostContracts` 仍在 `Typedown.WinUI`
- `WinUIEditorDocumentSession` 已直接接入 `RemoteInvoke` / `EventCenter` / `AppViewModel`
- 测试又把这些 WinUI host 文件当成“接近共享协议”的东西来检查

这说明“editor bridge contract 是壳层私有实现，还是共享边界”仍未定型。

#### G. 资源和静态产物 owner 交叉

当前仍存在：

- `Typedown.WinUI` 从 `Dev\Typedown\Resources\Statics` 拿 editor bundle
- legacy `Typedown` 反向嵌入 `Dev\Typedown.WinUI\Resources\Strings`

这在 cutover 前可以接受，但必须在计划中明确最终 owner，否则双宿主会持续交叉引用资源布局。

#### H. Packaged/MSIX 路径与仓库事实不一致

当前 WinUI 项目和架构测试仍把 packaged path 当成仓库可复现路径，但工作区中缺少对应 dev certificate 资产。

这意味着需要尽快选一条：

- 要么把 packaged 路径恢复成 repo-reproducible baseline
- 要么明确降级为本机手工验证路径，并同步修文档和测试

## 三、哪些部分是合理的

尽管问题很多，下面这些判断目前仍然成立：

- `Core -> Presentation -> WinUI` 的大方向是对的
- `Typedown.WinUI` 没有再反向依赖 `Typedown.XamlUI`
- `Typedown.Presentation` 已经承担当前 repo 中“原本想放到 Typedown.UI”的大部分 shell-agnostic MVVM 与编排职责
- `Typedown.WinUI` 当前拥有 XAML shell、平台服务、WebView2 host，本身是合理的
- legacy `Typedown` 继续作为对照壳存在，在迁移期是合理的，只是必须明确支持级别

## 四、建议的治理优先级

建议按下面顺序推进，而不是先做更大的抽象清理：

1. 先恢复架构 guardrail
2. 再明确生命周期模型
3. 再收缩 service locator 与订阅释放问题
4. 再决定 editor bridge owner 与资源 owner
5. 最后处理 packaged/MSIX 路径和更深层的边界净化

原因很直接：

- 如果 guardrail 还是红的，后续任何重构都无法证明没有把边界继续做坏
- 如果生命周期模型不明确，后面清理依赖注入只会反复返工

## 五、后续实施约束

后续实施建议遵守以下约束：

- 不在同一轮并行 worker 中同时修改 `App.xaml.cs`、`AppViewModel.cs`、`IWindowContext.cs` 这类高冲突边界文件
- 先修 tests/docs，再做结构性重构
- 每轮都要求：
  - focused architecture tests
  - `Typedown.WinUI` `Debug_Local|x64` build
  - `git diff --check`
- 所有并行实现必须在独立 worktree 中进行

## 六、对应实施计划

与本文对应的实施计划见：

- `docs/superpowers/plans/2026-05-07-winui3-architecture-governance-plan.md`
