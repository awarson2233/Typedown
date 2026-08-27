# Typedown 文档中心与权威目录导航 (Documentation Index & Governance Map)

本文档定义 `docs/` 目录下的文档分类、生效权威层级与历史归档映射。后续所有开发任务与 Agent 均必须以此为准，避免受历史废弃方案污染。

---

## 一、 唯一生效权威文档 (Active & Authoritative)

在当前 **100% C# / WinUI 3 全面原生化迁移（Native Migration）** 路线下，以下文档为**唯一生效的权威规范**：

| 文档路径 | 权威级别 | 核心职责与涵盖内容 |
| :--- | :---: | :--- |
| [`docs/native-migration-target-architecture.md`](file:///d:/source/repos/Typedown/docs/native-migration-target-architecture.md) | **SSOT 目标架构** | 全面原生化总纲、分层架构、三轨并行实施路线（Track A/B/C）、模块与命名空间规范、文件所有权注册表、双 `EditorSurface` 与 Feature Toggle 切流机制、关键技术降级方案。 |
| [`docs/artifacts/native-migration-multiagent-execution-scheme.md`](file:///d:/source/repos/Typedown/docs/artifacts/native-migration-multiagent-execution-scheme.md) | **执行与协同规范** | 多 Agent 协同执行方案、W0–W5 波次推进依赖拓扑、Worktree 强隔离与 Review Gate、ARM64 MSBuild 全局构建锁治理。 |
| [`docs/artifacts/traces/*`](file:///d:/source/repos/Typedown/docs/artifacts/traces) | **基准测试数据** | WebView2 / Muya 历史性能 Trace 与原生迁移比对基准数据（冷热启动、滚动、内存基线）。 |

---

## 二、 历史归档与废弃文档 (Superseded & Historical Records)

> [!WARNING]
> **以下文档均已标记为 `[SUPERSEDED]`（废弃状态）**。
> 这些文档记录了早期向 WinUI 3 迁移或维护 WebView2 + React/Muya 跨进程桥接的历史阶段，**不得作为新功能实现的依据**。仅保留用于架构测试断言兼容与历史上下文追溯。

### 1. 编辑器与桥接协议历史文档 (Editor & Bridge Protocol Legacy)
* [`docs/editor-runtime-architecture.md`](file:///d:/source/repos/Typedown/docs/editor-runtime-architecture.md)：*已废弃*。原 Muya 跨进程维护方案（除 **AD-8 EditorSurface 接缝**保留继承外，AD-1/2/4/9 均已反转作废）。
* [`docs/editor-bridge-protocol.md`](file:///d:/source/repos/Typedown/docs/editor-bridge-protocol.md)：*已废弃*。原 WebView2 与 React 前端之间的 JSON 消息通信协议。

### 2. WinUI 3 早期阶段审计与演进文档 (WinUI 3 Early Phase Audits)
* [`docs/winui3-target-architecture.md`](file:///d:/source/repos/Typedown/docs/winui3-target-architecture.md)：*已废弃*。2026 年 5 月阶段性模块划分方案，已被原生化目标架构取代。
* [`docs/winui3-post-phase9-roadmap.md`](file:///d:/source/repos/Typedown/docs/winui3-post-phase9-roadmap.md)：*已废弃*。历史 Post-Phase 9 路线图。
* [`docs/winui3-architecture-review-2026-05-07.md`](file:///d:/source/repos/Typedown/docs/winui3-architecture-review-2026-05-07.md)：*已废弃*。历史架构审查记录。
* [`docs/winui3-feature-gap-audit.md`](file:///d:/source/repos/Typedown/docs/winui3-feature-gap-audit.md)：*已废弃*。历史功能差异审计。
* [`docs/winui3-main-editor-unwired-audit.md`](file:///d:/source/repos/Typedown/docs/winui3-main-editor-unwired-audit.md)：*已废弃*。主编辑器未连线状态审计。
* [`docs/winui3-migration-decoupling-plan.md`](file:///d:/source/repos/Typedown/docs/winui3-migration-decoupling-plan.md)：*已废弃*。历史解耦方案。
* [`docs/winui3-settings-unwired-audit.md`](file:///d:/source/repos/Typedown/docs/winui3-settings-unwired-audit.md)：*已废弃*。设置界面未连线状态审计。

### 3. 构建与早期多 Agent 规划 (Build & Multiagent Legacy)
* [`docs/build-baseline.md`](file:///d:/source/repos/Typedown/docs/build-baseline.md)：*已废弃*。早期构建基线治理文档。
* [`docs/build-matrix.md`](file:///d:/source/repos/Typedown/docs/build-matrix.md)：*已废弃*。早期构建矩阵记录。
* [`docs/subagent-worktree-execution-plan.md`](file:///d:/source/repos/Typedown/docs/subagent-worktree-execution-plan.md)：*已废弃*。早期子 Agent 协同方案，已被原生化执行方案取代。

### 4. Superpowers 历史规划与设计规范 (Superpowers Archive)
* `docs/superpowers/plans/*` (9 个历史任务规划)：*已废弃*。记录 2026 年 4-5 月的局部重构规划。
* `docs/superpowers/specs/*`：*已废弃*。历史特定功能设计规范。

---

## 三、 Agent 作业守则 (Agent Working Rules)

1. **查阅文档优先级**：
   - 任何涉及架构决策、模块职责、文件归属的问题，**只查阅 [`docs/native-migration-target-architecture.md`](file:///d:/source/repos/Typedown/docs/native-migration-target-architecture.md)**。
   - 任何涉及多 Agent 协同、Worktree 路径、波次依赖与构建锁的问题，**只查阅 [`docs/artifacts/native-migration-multiagent-execution-scheme.md`](file:///d:/source/repos/Typedown/docs/artifacts/native-migration-multiagent-execution-scheme.md)**。
2. **严禁引用废弃方案**：严禁在后续波次（W1–W5）中引入任何依赖 WebView2/Muya 跨进程 JSON 协议的新逻辑。
