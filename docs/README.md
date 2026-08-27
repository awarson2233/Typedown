# Typedown 文档中心与权威目录导航 (Documentation Index & Governance Map)

本文档定义 `docs/` 目录下的文档结构与生效权威规范。所有历史废弃文档已彻底清理删除，当前文档树保持纯净。

---

## 唯一生效权威文档 (Active & Authoritative)

在当前 **100% C# / WinUI 3 全面原生化迁移（Native Migration）** 路线下，以下文档为**唯一生效的权威规范**：

| 文档路径 | 权威级别 | 核心职责与涵盖内容 |
| :--- | :---: | :--- |
| [`docs/native-migration-target-architecture.md`](file:///d:/source/repos/Typedown/docs/native-migration-target-architecture.md) | **SSOT 目标架构** | 全面原生化总纲、分层架构、三轨并行实施路线（Track A/B/C）、模块与命名空间规范、文件所有权注册表、双 `EditorSurface` 与 Feature Toggle 切流机制、关键技术降级方案。 |
| [`docs/artifacts/native-migration-multiagent-execution-scheme.md`](file:///d:/source/repos/Typedown/docs/artifacts/native-migration-multiagent-execution-scheme.md) | **执行与协同规范** | 多 Agent 协同执行方案、W0–W5 波次推进依赖拓扑、Worktree 强隔离与 Review Gate、ARM64 MSBuild 全局构建锁治理。 |
| [`docs/artifacts/traces/*`](file:///d:/source/repos/Typedown/docs/artifacts/traces) | **基准测试数据** | WebView2 / Muya 历史性能 Trace 与原生迁移比对基准数据（冷热启动、滚动、内存基线）。 |

---

## 目录结构 (Directory Structure)

```text
docs/
├── README.md                                          # 文档中心索引与目录总览
├── native-migration-target-architecture.md            # 全面原生化目标架构与规范 (SSOT)
└── artifacts/
    ├── native-migration-multiagent-execution-scheme.md # 多 Agent 协同执行与波次编排方案
    └── traces/                                        # 性能测试追踪与基准数据集 (.json.gz)
```

---

## Agent 作业守则 (Agent Working Rules)

1. **查阅文档优先级**：
   - 任何涉及架构决策、模块职责、文件归属的问题，**只查阅 [`docs/native-migration-target-architecture.md`](file:///d:/source/repos/Typedown/docs/native-migration-target-architecture.md)**。
   - 任何涉及多 Agent 协同、Worktree 路径、波次依赖与构建锁的问题，**只查阅 [`docs/artifacts/native-migration-multiagent-execution-scheme.md`](file:///d:/source/repos/Typedown/docs/artifacts/native-migration-multiagent-execution-scheme.md)**。
2. **严禁引入废弃方案**：严禁在后续波次（W1–W5）中引入任何依赖 WebView2/Muya 跨进程 JSON 协议的新逻辑。
