# [SUPERSEDED] Editor Runtime Architecture & Bridge Protocol

> [!WARNING]
> **本文档已被废弃 (SUPERSEDED)**
> 依据 `docs/native-migration-target-architecture.md` 与多 Agent 架构裁决方案，Typedown 编辑器核心已正式确立**全面原生化架构路线**，全面移除 Chromium/WebView2 + React/Muya 跨进程体系。
> 
> **唯一生效的目标架构文档**：请参阅 [`docs/native-migration-target-architecture.md`](file:///D:/source/repos/Typedown/docs/native-migration-target-architecture.md)。

---

## 废弃背景与裁决说明

在早期架构演进中，本项目曾尝试通过 `WebView2 + React/Muya` 混合宿主方案构建编辑器表面。由于该方案存在严重的进程开销、冷启动延迟、复杂的 JSON-RPC 跨进程同步与双重状态源冲突（Single Source of Truth 割裂），经架构仲裁决定执行**100% C# / WinUI 3 全面原生化重构**。

### 决策反转与作废记录 (AD Reversals)

| 架构决策 (AD) | 原始决策内容 | 裁决状态 | 作废原因与替代方案 |
| :--- | :--- | :--- | :--- |
| **AD-1** | 双向 JSON-RPC 跨进程桥接协议 (`editor-bridge-protocol`) | **已作废 (REVERSED)** | 原生模式下所有编辑操作与状态流均在 C# 进程内同步/异步调用，彻底废弃序列化 JSON 桥接。 |
| **AD-2** | Muya JS 状态为 Single Source of Truth (SSOT) | **已作废 (REVERSED)** | 原生编辑内核以 C# 文档模型 (`Typedown.Core.Markdown`) 和 AST 为唯一样本源。 |
| **AD-4** | 基于 React / HTML DOM 的编辑视图渲染管线 | **已作废 (REVERSED)** | 废弃 Web DOM 树，改由 WinUI 3 XAML RichTextBlock / Canvas / Custom TextLayout 原生控件渲染。 |
| **AD-9** | 基于 Web Selection API 的光标与选区跨进程同步 | **已作废 (REVERSED)** | 由原生 `CoreTextEditContext` / TextPattern 和 C# `SelectionState` 直接驱动。 |

### 生效并继承的决策 (Surviving Decision)

- **AD-8 (EditorSurface 抽象接缝)**：
  - **保留并继承**。将编辑器表面抽象为接口 `IEditorSurface`。
  - 在原生化迁移过渡期（Track A / Track B / Track C），通过 `IEditorSurface` 接口契约实现 `WebView2MuyaEditorSurface`（存量过渡）与 `NativeEditorSurface`（原生新引擎）双表面并存与平滑 Feature Toggle 切流。
  - 当原生编辑引擎成熟并完成全量割接后，`WebView2MuyaEditorSurface` 与 `Dev/Typedown.Editor` 将被彻底移除。

---

## 历史归档索引

- 历史 Bridge 协议定义（归档备查）：[`docs/editor-bridge-protocol.md`](file:///D:/source/repos/Typedown/docs/editor-bridge-protocol.md)
- 历史 WinUI 3 迁移解耦计划（归档备查）：[`docs/winui3-migration-decoupling-plan.md`](file:///D:/source/repos/Typedown/docs/winui3-migration-decoupling-plan.md)
- **当前权威架构规范**：[`docs/native-migration-target-architecture.md`](file:///D:/source/repos/Typedown/docs/native-migration-target-architecture.md)
