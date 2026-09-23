# Typedown 文档

| 文档 | 内容 |
|---|---|
| [architecture.md](architecture.md) | 当前架构：工程分层、进程与 DI 作用域、编辑器宿主、桥接协议与消息清单，以及快捷键、右键菜单、宿主浮层、滚动条、撤销的实现路径 |
| [editor-protocol.md](editor-protocol.md) | 新引擎页面与宿主之间的桥接协议 v1：信封与 JSON 约定、启动握手、带版本号的正文增量同步、消息清单、频率规则、浮层坐标、错误与契约测试 |
| [build.md](build.md) | 构建、运行、测试、打包签名与已知问题 |

文档只描述代码的当前状态，代码变了就改对应段落，不保留历史。`docs/artifacts/` 已被 gitignore，只放本地分析产物，不入库。
