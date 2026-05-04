# Typedown.WinUI 功能接入缺口清单

本文记录 `Dev\Typedown.WinUI` 相较 legacy `Dev\Typedown` 尚未接入或仅部分接入的功能，用于 WinUI3 迁移后续跟踪。

## 当前结论

`Typedown.WinUI` 当前已具备 WinUI3 主壳、WebView2 editor host、部分平台服务、部分设置页和部分编辑器命令桥接，但仍不是 legacy `Typedown` 的完整功能等价版本。

## 尚未完整接入的功能

### 1. 设置页路由与导航不完整

Legacy `Typedown.Pages.Route.GetSettingsPageType` 已接入：

- `About`
- `Editor`
- `ExportConfig`
- `Export`
- `General`
- `Image`
- `ImageUpload`
- `Shortcut`
- `UploadConfig`
- `View`

WinUI `Typedown.WinUI.Pages.Route.GetSettingsPageType` 当前只接入：

- `About`
- `Editor`
- `Export`
- `General`
- `Image`
- `View`

`Dev\Typedown.WinUI\Pages\SettingPages` 中已经存在 `ExportConfigPage`、`ImageUploadPage`、`ShortcutPage`、`UploadConfigPage`，但尚未挂到 `Route` 和 `SettingsPage` 的 `NavigationView`。

### 2. 文件菜单动态项不完整

Legacy `FileItem` 会动态填充：

- 最近打开文件列表，来自 `AccessHistory.FileRecentlyOpened`
- 导出配置列表，来自 `IFileExport.ExportConfigs`

WinUI `FileItem` 当前仍主要保留占位：

- `NoRecentFilesItem`
- `NoExportConfigItem`
- `PDF` / `HTML` / `TEXT` 被显式禁用
- `NewWindow` 被显式禁用

### 3. 导出与打印管线未接入

Legacy `FileExport.Print` 使用：

- `IFileConverter.HtmlToPdf`
- `PrintHelper.PrintPDF`

WinUI `WinUIFileExport.Print` 当前直接抛出 `NotSupportedException`，说明 WebView-backed HTML-to-PDF / Print 管线尚未接入 WinUI app head。

### 4. 左侧栏页面未接入

Legacy 左侧栏包含：

- `FolderPage`
- `TocPage`
- `SidePaneControls.Pages.Route`

WinUI 当前没有对应的 `Controls\SidePaneControls\Pages` 页面实现，且 `LeftPane.OnSelectionChanged` 为空。因此工作目录树、目录大纲和侧栏页面切换仍未完成。

### 5. 文件操作仍有缺口

Legacy `FileOperation` 支持：

- 删除到回收站
- 复制
- 剪切
- 粘贴
- 移动
- 重命名
- 文件名合法性检查

WinUI `WinUIFileOperation` 当前仍未实现：

- `Delete`
- `PasteFromClipboard`

复制、移动、重命名、复制/剪切到剪贴板已部分接入。

### 6. 编辑器容器交互未完整接入

Legacy `EditorContainer` 支持：

- 拖入 Markdown 文件打开
- 拖入图片插入
- `Ctrl + MouseWheel` 调整字体大小
- 滚动条同步到编辑器
- 响应编辑器 `OnScroll` 事件
- 触摸/鼠标滚动条模式切换

WinUI `EditorContainer` 当前这些入口仍为空或未完整实现：

- `OnDragEnter`
- `OnDrop`
- `OnScroll`

### 7. 图片右键菜单功能未接入

Legacy `ContextMenuItems.ImageItem` 支持：

- 打开图片所在位置
- 复制图片到
- 移动图片到
- 上传图片
- 跳转图片上传设置
- 保存图片
- 删除图片文件

WinUI `ContextMenuItemStubs.ImageItem` 中对应事件当前为空方法，因此图片上下文菜单仍是占位状态。

### 8. 浮动控件存在简化迁移

Legacy shell 注册了：

- `FrontMenu`
- `TableTools`
- `ImageSelector`
- `ImageToolbar`
- `ToolTip`

WinUI `WinUIFloatViewService` 已用 `MenuFlyout` 等方式实现部分能力，但 `FrontMenu`、`TableTools` 未按 legacy XAML 控件完整迁移，目前属于简化实现。

### 9. 快捷键设置与菜单快捷键注册未完整接入

Legacy 菜单项通过：

- `RegisterWindowShortcut`
- `RegisterEditorShortcut`

接入设置中的快捷键，并提供 `ShortcutPage`。

WinUI 菜单当前主要绑定命令，没有完整对齐 legacy 菜单快捷键注册链路。`ShortcutPage` 文件存在，但尚未进入设置导航。

### 10. 上传配置与图片上传配置未完整接入

Legacy 包含：

- `UploadConfigPage`
- `ImageUploadPage`
- Git / OSS / FTP / SCP / PowerShell 配置项
- 图片右键菜单中的上传配置列表

WinUI 相关页面和配置控件文件已存在，但未挂到设置导航；图片右键上传菜单也未接入已启用的上传配置列表。

## 已部分接入的能力

- WinUI3 `App` / `Window` / Mica / 标题栏。
- `RootControl` / `MainPage` 主壳。
- WebView2 `WinUIEditorHost`。
- 本地文件打开、保存、另存为的 editor host 持久化入口。
- 编辑器 bridge 对部分事件的处理：
  - `FileLoaded`
  - `MarkdownChange`
  - `CursorChange`
  - `StateChange`
- 查找替换浮窗已有 WinUI 实现。
- 格式、段落、视图菜单的大部分命令绑定。
- 表格插入对话框已有简化 WinUI 实现。
- 设置页中 `General`、`View`、`Editor`、`Image`、`Export`、`About` 已挂载。

## 建议优先级

1. 挂载已有设置页：`ExportConfigPage`、`ImageUploadPage`、`ShortcutPage`、`UploadConfigPage`。
2. 补齐 `FileItem` 的最近文件和导出配置动态菜单。
3. 接入 `WinUIFileExport.Print` 与导出管线。
4. 迁移左侧栏 `FolderPage` / `TocPage`。
5. 补齐 `EditorContainer` 的拖放、滚动和字体缩放。
6. 补齐图片右键菜单的文件操作和上传操作。
