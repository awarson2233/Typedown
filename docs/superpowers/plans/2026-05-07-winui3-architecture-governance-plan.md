# Typedown WinUI3 Architecture Governance Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 让 Typedown 当前 `Core / Presentation / WinUI / legacy shell` 迁移态重新获得可信的架构治理能力，优先修复 guardrail、生命周期模型和关键边界 owner。

**Architecture:** 本计划不先做“大重构”，而是先把当前真实架构重新写实到 docs/tests，再收紧最危险的运行时结构问题。整体执行采用“串行定真相 + 并行修独立域 + 串行集成验证”的策略，避免多个 worker 同时争抢 `Presentation` 与 `WinUI` 的共享边界文件。

**Tech Stack:** .NET 10, WinUI 3, Microsoft.Extensions.DependencyInjection, MSTest architecture tests, WebView2 host, git worktrees, subagent-driven-development.

---

## 0. 执行方式

- 当前计划默认按你指定的两套流程执行：
  - `superpowers:dispatching-parallel-agents`
  - `superpowers:subagent-driven-development`
- 但不是所有任务都并行。
- 原则：
  - 先用串行任务确定“当前真相”
  - 再把互不共享写集的任务并行出去
  - 最后再串行做集成与回归

## 1. 当前问题分组

本计划只处理四类高优先级问题：

1. 架构 tests / docs 已掉相位
2. 生命周期模型不成立
3. `Presentation` / `WinUI` 依赖图与订阅释放不透明
4. editor bridge / 资源 / packaged 路径 owner 未定型

不在本计划内的内容：

- 全量平台中立化
- 把全部 Win32 / XAML 形状从 `Core` / `Presentation` 中拔掉
- legacy shell 彻底删除
- editor bridge 全量协议重构

## 2. 推荐 worktree / subagent 拆法

### 2.1 串行 Phase A：先定真相

这阶段不要并行。

原因：

- `docs/`
- `Tests/Typedown.ArchitectureTests`
- `Dev/Typedown.Core/*.csproj`
- `Dev/Typedown.Presentation/*.csproj`
- `Dev/Typedown.WinUI/*.csproj`

这些文件会互相定义“当前架构真相”，并行改很容易互相覆盖。

### 2.2 并行 Phase B：三个独立问题域

在 Phase A 合入后，再并行发三个 implementer worker：

- Worker A：生命周期与 scope/disposal
- Worker B：依赖图透明化与订阅归属
- Worker C：owner 治理（editor bridge / resources / packaged path）

要求三个 worker 的写集尽量解耦，避免同时改同一个核心文件。

### 2.3 串行 Phase C：集成与收口

并行任务合入后，由主控统一做：

- 冲突化解
- 最终文档同步
- 全量验证

## 3. Phase A：恢复真相源

### Task 1: 修正文档中的当前有效架构叙述

**Files:**
- Modify: `docs/xamlui-dependency.md`
- Modify: `docs/winui3-target-architecture.md`
- Modify: `docs/winui3-post-phase9-roadmap.md`
- Modify: `docs/build-matrix.md`
- Modify: `docs/build-baseline.md`
- Reference: `docs/winui3-architecture-review-2026-05-07.md`

**目标：**

- 明确当前代码中的有效架构已经是 `Typedown.Presentation`，不是 `Typedown.UI`
- 删除或降级那些已经与当前代码矛盾的说明
- 明确哪些内容是“历史计划痕迹”，哪些是“当前事实”

**完成标准：**

- 文档中不再把 `Dev/Typedown.UI` 写成当前存在的模块
- 文档中不再把 `Dev/Typedown.Core` 写成仍直接引用 `Typedown.XamlUI`
- 文档中明确 dual-shell 当前态

**验证：**

Run:

```powershell
rg -n "Typedown.UI|Typedown.Core.Legacy|Core.Contracts|Typedown.XamlUI.csproj" .\docs
```

Expected:

- 只保留“历史/迁移说明”语境中的必要提及
- 不再把这些内容写成当前实现事实

### Task 2: 修正 ArchitectureTests 到当前代码真相

**Files:**
- Modify: `Tests/Typedown.ArchitectureTests/Phase10CoreContractsBoundaryTests.cs`
- Modify: `Tests/Typedown.ArchitectureTests/Phase14UiVisualBoundaryTests.cs`
- Modify: `Tests/Typedown.ArchitectureTests/Phase15PresentationBoundaryTests.cs`
- Modify: other failing files only if directly required by the current failure set

**目标：**

- 把过期断言更新到当前 repo 真实状态
- 不把“历史阶段事实”继续写成当前 guardrail
- 保持 tests 仍然在约束真正重要的边界

**必须保留的 guardrail：**

- `Core` 不引用 WinUI/XamlUI
- `Presentation` 不引用 WinUI/XamlUI
- `WinUI` 不反向依赖 XamlUI
- `Core -> Presentation -> WinUI` 的大方向不反转

**必须删除或重写的过期 guardrail：**

- `net9.0` 断言
- 缺失的 dev cert 文件断言（除非决定恢复 repo-reproducible packaged path）
- 已失效的旧 XAML/menu snippet 断言

**验证：**

Run:

```powershell
dotnet test .\Tests\Typedown.ArchitectureTests\Typedown.ArchitectureTests.csproj -c Debug -p:UseSharedCompilation=false /nodeReuse:false /v:minimal --no-restore
```

Expected:

- 从当前 `6` 个失败下降到 `0` 个失败

### Task 3: 明确 packaged/MSIX 路径支持级别

**Files:**
- Modify: `docs/build-baseline.md`
- Modify: `docs/build-matrix.md`
- Modify: `Tests/Typedown.ArchitectureTests/Phase10CoreContractsBoundaryTests.cs`
- Optional Modify: `scripts/install-winui-dev-certificate.ps1`
- Optional Modify: `Dev/Typedown.WinUI/Typedown.WinUI.csproj`

**目标：**

在“恢复 repo 可复现 packaged path”和“明确 packaged path 为本机手工路径”之间做一次明确选择，并把文档/tests 同步到同一结论。

**决策门槛：**

- 如果 repo 中不会提交 `.pfx/.cer`，tests 不能再把它当必备资产
- 如果 packaged path 仍是正式 baseline，就必须补足对应资产与脚本说明

**验证：**

Run:

```powershell
Test-Path .\Dev\Typedown.WinUI\Typedown.WinUI.DevTest.pfx
Test-Path .\Dev\Typedown.WinUI\Typedown.WinUI.DevTest.cer
```

Expected:

- 结论与 docs/tests 一致

## 4. Phase B：并行实施域

### Worker A: 生命周期与 scope/disposal

**Worktree:** `.worktrees/winui-arch-lifetime`

**Files:**
- Modify: `Dev/Typedown.WinUI/App.xaml.cs`
- Modify: `Dev/Typedown.Presentation/PresentationServiceCollectionExtensions.cs`
- Modify: `Dev/Typedown.Core/CoreServiceCollectionExtensions.cs`
- Modify: `Dev/Typedown.Presentation/ViewModels/AppViewModel.cs`
- Modify: relevant disposal-bearing ViewModels only if needed

**目标：**

- 决定当前 WinUI 主路径是：
  - 明确 app-root singleton graph
  - 或明确 scoped graph 并真正创建/释放 scope
- 让 DI lifetime 与实际运行时模型一致

**限制：**

- 不同时改 editor bridge owner
- 不在本任务里重写所有 ViewModel 构造函数

**验证：**

Run:

```powershell
dotnet test .\Tests\Typedown.ArchitectureTests\Typedown.ArchitectureTests.csproj -c Debug -p:UseSharedCompilation=false /nodeReuse:false /v:minimal --no-restore
dotnet build .\Dev\Typedown.WinUI\Typedown.WinUI.csproj -c Debug_Local -p:Platform=x64 -p:UseSharedCompilation=false /nodeReuse:false /v:minimal
```

### Worker B: Presentation 依赖图与订阅归属

**Worktree:** `.worktrees/winui-arch-presentation-deps`

**Files:**
- Modify: `Dev/Typedown.Presentation/ViewModels/EditorViewModel.cs`
- Modify: `Dev/Typedown.Presentation/ViewModels/FileViewModel.cs`
- Modify: `Dev/Typedown.Presentation/ViewModels/FloatViewModel.cs`
- Modify: `Dev/Typedown.Presentation/ViewModels/FormatViewModel.cs`
- Modify: `Dev/Typedown.Presentation/ViewModels/ParagraphViewModel.cs`
- Modify: `Tests/Typedown.ArchitectureTests/*` if new guardrail is added

**目标：**

- 把未纳入 `CompositeDisposable` 的订阅/handler 收回到可释放生命周期
- 抑制继续扩散的 `IServiceProvider` / service-location 模式
- 至少先把“订阅无主”修完

**限制：**

- 不在本任务里做大规模构造函数注入重写
- 不与 Worker A 同时改 `App.xaml.cs`

**验证：**

Run:

```powershell
dotnet test .\Tests\Typedown.ArchitectureTests\Typedown.ArchitectureTests.csproj -c Debug -p:UseSharedCompilation=false /nodeReuse:false /v:minimal --no-restore
```

### Worker C: owner 治理（editor bridge / resources / packaged path）

**Worktree:** `.worktrees/winui-arch-owner-governance`

**Files:**
- Modify: `docs/editor-bridge-protocol.md`
- Modify: `docs/build-baseline.md`
- Modify: `Dev/Typedown.WinUI/Typedown.WinUI.csproj`
- Modify: `Dev/Typedown/Typedown.csproj`
- Modify: `Tests/Typedown.ArchitectureTests/Phase15PresentationBoundaryTests.cs`
- Modify: `Tests/Typedown.ArchitectureTests/Phase13LegacyTextResourceTests.cs`

**目标：**

- 明确 editor bridge contract 当前 owner
- 明确 editor static bundle 的最终 owner 与迁移中 owner
- 明确 WinUI/legacy 资源互相引用是临时状态还是保留策略

**限制：**

- 先做 owner 声明与 guardrail，不做 editor host 大重构
- 不和 Worker A/B 一起改 `AppViewModel.cs`

**验证：**

Run:

```powershell
rg -n "Resources\\Statics|Resources\\Strings|EditorHostContracts|Typedown.Editor" .\Dev .\docs .\Tests
dotnet test .\Tests\Typedown.ArchitectureTests\Typedown.ArchitectureTests.csproj -c Debug -p:UseSharedCompilation=false /nodeReuse:false /v:minimal --no-restore
```

## 5. Phase C：集成收口

### Task 4: 合并并行结果并清理交叉断言

**Files:**
- Modify: whichever files conflict across merged workers
- Modify: `docs/winui3-architecture-review-2026-05-07.md`

**目标：**

- 统一最终术语
- 消除 tests/docs 中重复或互相冲突的边界声明
- 把“迁移债”和“正式边界”明确区分

### Task 5: 最终验证

**Files:**
- No functional changes expected

**必须执行的验证：**

```powershell
git status --short
git diff --check
dotnet test .\Tests\Typedown.ArchitectureTests\Typedown.ArchitectureTests.csproj -c Debug -p:UseSharedCompilation=false /nodeReuse:false /v:minimal --no-restore
dotnet build .\Dev\Typedown.Core\Typedown.Core.csproj -c Debug -p:UseSharedCompilation=false /nodeReuse:false /v:minimal
dotnet build .\Dev\Typedown.Presentation\Typedown.Presentation.csproj -c Debug -p:UseSharedCompilation=false /nodeReuse:false /v:minimal
dotnet build .\Dev\Typedown.WinUI\Typedown.WinUI.csproj -c Debug_Local -p:Platform=x64 -p:UseSharedCompilation=false /nodeReuse:false /v:minimal
```

**Expected:**

- `ArchitectureTests` 绿
- `Core` / `Presentation` / `WinUI` 构建通过
- 没有 whitespace / patch formatting 问题

## 6. 推荐 dispatch 顺序

推荐实际执行顺序如下：

1. 先由主控串行完成 `Phase A`
2. 再并行派发 `Worker A/B/C`
3. 每个 worker 完成后都做：
   - spec compliance review
   - code quality review
4. 三个 worker 合流后，主控执行 `Phase C`

## 7. 不要这样执行

- 不要一开始就并行改 `ArchitectureTests` 和核心 `Presentation/WinUI` 边界文件
- 不要在 tests 还是红的时候继续做大重构
- 不要同时让多个 worker 修改 `App.xaml.cs`、`AppViewModel.cs`、`IWindowContext.cs`
- 不要把“历史文档中的 Typedown.UI”继续当当前工程事实

## 8. 完成定义

本计划完成的标志不是“代码更优雅”，而是：

- 当前有效架构有且仅有一套写实描述
- `ArchitectureTests` 重新成为可信 guardrail
- WinUI 运行时生命周期模型与 DI 注册语义一致
- editor/resource/packaged path 的 owner 至少被明确写清

## 9. 执行建议

按你当前要求，后续最合适的执行方式是：

- `Subagent-Driven (recommended)`
- 先串行完成 `Phase A`
- 再按 `dispatching-parallel-agents` 派发 `Phase B`
- 每个实现任务都走 `subagent-driven-development` 的 implementer/spec-review/quality-review 闭环
