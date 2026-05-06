# Typedown.WinUI 功能接入清单

更新日期：`2026-05-06`

本文记录 `Dev\Typedown.WinUI` 当前相较 legacy `Dev\Typedown` 的功能接入现状，重点区分三类状态：

- `[x]` 已确认接通
- `[ ]` 已发现缺口，尚未补齐
- `[-]` 有入口但不是主导航入口，属于信息补充，不单独视为缺口

## 一、菜单与编辑器模式

- `[ ]` 查看菜单中的 `源代码模式`
  - WinUI 菜单点击会修改 `SettingsViewModel.SourceCode`
  - 但 editor 前端消费的是 `options.sourceCode`
  - 当前 `GetSettings()` 初始下发与 `SettingsChanged` 增量下发走到 WinUI bridge 后，发送出去的字段名仍是 `SourceCode`
  - 结果是设置值会变，但前端编辑器不会按该值在 `Muya` 与 `CodeMirror` 之间切换

- `[ ]` 查看菜单中的 `专注模式`
  - WinUI 菜单点击会修改 `SettingsViewModel.FocusMode`
  - 但 editor 前端消费的是 `focusMode`
  - 当前 bridge 发送的是 `FocusMode`
  - 结果是设置值会变，但前端不会调用 `editor.setFocusMode(...)`

- `[ ]` 查看菜单中的 `打字机模式`
  - WinUI 菜单点击会修改 `SettingsViewModel.Typewriter`
  - 但 editor 前端消费的是 `typewriter`
  - 当前 bridge 发送的是 `Typewriter`
  - 结果是设置值会变，但前端不会切换打字机滚动/留白行为

- `[ ]` `新建窗口`
  - `OpenNewWindow` 在 WinUI editor host 侧仍未接入
  - 当前仍会走 `NotSupportedException`

## 二、设置页主导航与子页面入口

- `[x]` `Route.GetSettingsPageType(...)` 已接入以下页面：
  - `About`
  - `Editor`
  - `Export`
  - `ExportConfig`
  - `General`
  - `Image`
  - `ImageUpload`
  - `Shortcut`
  - `UploadConfig`
  - `View`

- `[-]` 以下页面已可通过页内导航进入，但没有挂到设置页左侧主菜单：
  - `ShortcutPage`
  - `ImageUploadPage`
  - `UploadConfigPage`
  - `ExportConfigPage`

- `[x]` `导出 -> PDF/HTML 配置子页面跳转`
  - `ExportPage` 列表点击已导航到 `Settings/ExportConfig?...`
  - `ExportConfigPage` 会按类型切换 `PDFConfig / HTMLConfig / ImageConfig`

## 三、外观 / 查看页设置接线

### 3.1 已确认可改值但 WinUI 壳层尚未消费

- `[ ]` `StatusBarOpen`
  - 设置值会变化
  - 但 `MainPage.xaml` 中状态栏仍固定 `x:Load="True"`，未按设置显示/隐藏

- `[ ]` `SidePaneOpen`
  - 菜单与设置页都能修改该值
  - 但 `MainContent` 当前没有像 legacy 那样订阅该设置并驱动侧栏展开/收起

- `[ ]` `AppTheme`
  - `Presentation` 侧会更新 `UIViewModel.ActualTheme`
  - 但 WinUI `App / Window / RootControl / MainPage` 未发现对应主题切换接线

- `[ ]` `UseMicaEffect`
  - WinUI 启动时直接启用 `MicaBackdrop`
  - 未按设置开关动态控制

- `[ ]` `Topmost`
  - 设置值存在
  - WinUI 当前未发现窗口层消费者

- `[ ]` `AppCompactMode`
  - legacy 会影响菜单栏/标题区布局
  - WinUI `MenuBar` 当前相关区域基本写死为常显，没有按设置切换

- `[ ]` `UseEditorMicaEffect`
  - 设置值存在
  - 当前未发现 WinUI 壳层或 editor 前端的实际消费者

- `[ ]` `AnimationEnable`
  - 目前只在部分浮层/编辑器容器动画中使用
  - 设置页导航、主壳转场、侧栏转场未像 legacy 那样系统接入

### 3.2 已确认接通的外观 / 查看页项

- `[x]` `SidePane` 菜单项本身可点击并改变设置值
- `[x]` `StatusBar` 菜单项本身可点击并改变设置值
- `[ ]` `源代码模式 / 专注模式 / 打字机模式` 目前仅能改设置值，尚未真正作用到 editor 前端

## 四、编辑器设置页

### 4.1 页面存在，但未真正作用到编辑器前端

- `[ ]` `FontSize`
- `[ ]` `LineHeight`
- `[ ]` `EditorAreaWidth`
- `[ ]` `AutoPairQuote`
- `[ ]` `AutoPairBracket`
- `[ ]` `AutoPairMarkdownSyntax`
  - 以上项目在 WinUI 页面里都有控件绑定，值也会写回 `SettingsViewModel`
  - 但 editor 前端读取的是 `fontSize / lineHeight / editorAreaWidth / autoPairQuote / autoPairBracket / autoPairMarkdownSyntax`
  - 当前 `EditorViewModel.GetSettings()` 初始返回与 `SettingsChanged` 增量通知发送的是 PascalCase 字段名
  - WinUI bridge 这里没有沿用 `Config.EditorJsonSerializerSettings` 的 camelCase 约定
  - 结果是“设置页看起来能改，存储层也在变，但 editor 前端没有成功接线”

- `[ ]` `TabSize`
  - 除了同样存在 PascalCase / camelCase 不匹配外
  - `SettingsViewModel.notifySet` 里当前也没有 `TabSize`
  - 即使修正命名问题，运行时增量通知路径仍然是不完整的

### 4.2 明确缺口

- `[ ]` `SpellcheckEnabled`
  - 当前 WinUI 页面中仍是注释残留，不是正式可用项
  - `SettingsViewModel.notifySet` 未包含该字段
  - `EditorViewModel.GetSettings()` 初始下发也未包含该字段
  - editor 前端本身支持 `spellcheckEnabled`，缺口主要在 WinUI / Presentation 接线

## 五、导出与打印

- `[x]` 导出配置列表动态加载
- `[x]` 导出配置详情页跳转
- `[x]` `PDFConfig / HTMLConfig / ImageConfig` 子配置页装配与导航存在

- `[ ]` 导出配置编辑未完整接通
  - `ExportConfigPage` 能打开对应子页面，但“已打开”不等于“已接通”
  - `PDFConfig.xaml.cs` 中这些用于 `x:Bind` 的属性：
    - `PageSizeComboxItems`
    - `PageSizeComboxSelectedItem`
    - `PageMarginComboxItems`
    - `PageMarginComboxSelectedItem`
  - 当前只是普通 C# 属性，在 `OnLoaded` 里赋值
  - WinUI 工程本身没有接入 Fody，因此不会像旧版 UWP 工程那样自动补属性变更通知
  - 同时这里也没有显式 `Bindings.Update()` 或手动触发 `PropertyChanged`
  - 结果是 PDF 配置页里的纸张尺寸 / 页边距预设这组 UI，当前不能算已经可靠接通
  - `HTMLConfig / ImageConfig` 的基础模型装载与卸载保存路径存在，但仍需要按实际交互逐项复核，当前不能再按“已全部接通”处理

- `[ ]` WinUI 打印体验仍与 legacy 不完全等价
  - 当前 `WinUIFileExport.Print(...)` 会先转 PDF，再走系统 Shell `print`
  - 与 legacy 的 `PrintHelper.PrintPDF` 管线不同
  - 是否视为必须补齐，取决于你是否要求完全保持 legacy 的打印交互

## 六、图片上传与相关设置

- `[x]` `ImagePage -> ImageUploadPage` 入口
- `[x]` `ImageUploadPage -> UploadConfigPage` 入口
- `[x]` 上传配置列表与详情页跳转
- `[x]` 图片设置页中的上传配置下拉项同步

## 七、当前建议优先级

1. `[ ]` 补齐 `View / 外观` 页的 WinUI 壳层设置消费
   - `StatusBarOpen`
   - `SidePaneOpen`
   - `AppTheme`
   - `UseMicaEffect`
   - `Topmost`
   - `AppCompactMode`
   - `UseEditorMicaEffect`
   - `AnimationEnable`

2. `[ ]` 先修正 editor bridge 的设置字段命名
   - `SourceCode -> sourceCode`
   - `FocusMode -> focusMode`
   - `Typewriter -> typewriter`
   - `FontSize -> fontSize`
   - `LineHeight -> lineHeight`
   - `EditorAreaWidth -> editorAreaWidth`
   - `AutoPairQuote -> autoPairQuote`
   - `AutoPairBracket -> autoPairBracket`
   - `AutoPairMarkdownSyntax -> autoPairMarkdownSyntax`
   - `TabSize -> tabSize`
   - 以及搜索相关字段

3. `[ ]` 补齐 `TabSize / SpellcheckEnabled`

4. `[ ]` 修正导出配置页中从旧版复制过来的绑定更新机制

5. `[ ]` 决定是否补齐 `OpenNewWindow`

6. `[ ]` 决定是否要把 WinUI 打印行为继续向 legacy 体验对齐
