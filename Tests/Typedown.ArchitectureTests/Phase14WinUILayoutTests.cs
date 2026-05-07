using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Typedown.ArchitectureTests;

[TestClass]
public class Phase14WinUILayoutTests
{
    private static readonly string RepoRoot = FindRepoRoot();

    [TestMethod]
    public void MainPageXaml_UsesActiveEditorShellVisualStructure()
    {
        var winuiRoot = Path.Combine(RepoRoot, "Dev", "Typedown.WinUI");
        var mainPage = File.ReadAllText(Path.Combine(winuiRoot, "Views", "MainPage.xaml"));

        AssertContains(mainPage, "<controls:MenuBar");
        AssertContains(mainPage, "<controls:MainContent");
        AssertContains(mainPage, "x:Name=\"MainContent\"");
        AssertContains(mainPage, "Grid.Row=\"1\"");
        AssertContains(mainPage, "<controls:StatusBar");
        AssertContains(mainPage, "x:Class=\"Typedown.WinUI.Views.MainPage\"");
        AssertDoesNotContain(mainPage, "MigrationStatusPanel");
        AssertDoesNotContain(mainPage, "Phase 14 migration status");
    }

    [TestMethod]
    public void WinUILegacyCopiedSources_AreRemoved()
    {
        var winuiRoot = Path.Combine(RepoRoot, "Dev", "Typedown.WinUI");
        var copiedRoot = Path.Combine(winuiRoot, "LegacyCopied");
        var project = File.ReadAllText(Path.Combine(winuiRoot, "Typedown.WinUI.csproj"));

        Assert.IsFalse(Directory.Exists(copiedRoot), "LegacyCopied contains inactive migration copies and must stay out of the WinUI app.");
        AssertDoesNotContain(project, "LegacyCopied");
    }

    [TestMethod]
    public void WinUIStartup_UsesCopiedRootControlShell()
    {
        var winuiRoot = Path.Combine(RepoRoot, "Dev", "Typedown.WinUI");
        var app = File.ReadAllText(Path.Combine(winuiRoot, "App.xaml.cs"));
        var project = File.ReadAllText(Path.Combine(winuiRoot, "Typedown.WinUI.csproj"));
        var root = File.ReadAllText(Path.Combine(winuiRoot, "Controls", "RootControl.xaml"));
        var rootCode = File.ReadAllText(Path.Combine(winuiRoot, "Controls", "RootControl.xaml.cs"));
        var mainPage = File.ReadAllText(Path.Combine(winuiRoot, "Views", "MainPage.xaml"));

        AssertContains(app, "RootControl");
        AssertContains(app, "ConfigureNativeTitleBar(window)");
        AssertContains(app, "targetWindow.ExtendsContentIntoTitleBar = true");
        AssertContains(app, "window.SystemBackdrop = Config.IsMicaSupported && enable ? new MicaBackdrop() : null;");
        AssertContains(app, "targetWindow.AppWindow.TitleBar");
        AssertContains(app, "window.SetTitleBar(rootControl.TitleBarElement)");
        AssertContains(root, "x:Name=\"AppTitleBar\"");
        AssertContains(root, "x:Name=\"TitleDragRegion\"");
        AssertContains(root, "x:Name=\"BackButton\"");
        AssertContains(root, "Width=\"32\"");
        AssertContains(root, "Height=\"32\"");
        AssertContains(root, "<VisualStateGroup x:Name=\"NavigationState\">");
        AssertAppearsBefore(root, "<VisualStateManager.VisualStateGroups>", "<Grid x:Name=\"RootGrid\"");
        AssertContains(root, "Style=\"{ThemeResource TitleBarBackButtonStyle}\"");
        AssertContains(root, "Text=\"{u:LocaleString Key=AppName}\"");
        AssertContains(rootCode, "Frame.Navigate(typeof(Views.MainPage), MainPageNavigationParameter)");
        AssertContains(rootCode, "public UIElement TitleBarElement => TitleDragRegion");
        AssertContains(rootCode, "Frame.SourcePageType == typeof(Pages.SettingsPage)");
        AssertContains(rootCode, "BackButton.Visibility = isSettingsPage ? Visibility.Visible : Visibility.Collapsed;");
        AssertContains(rootCode, "TitlePanel.Margin = isSettingsPage ? new Thickness(0) : new Thickness(4, 0, 0, 0);");
        AssertContains(rootCode, "VisualStateManager.GoToState(this");
        AssertContains(mainPage, "<controls:MenuBar");
        AssertContains(root, "<Frame");
        AssertDoesNotContain(root, "<local:Caption");
        AssertDoesNotContain(project, "Controls\\RootControl.xaml");
    }

    [TestMethod]
    public void RestoredVisualShell_DoesNotOverlayEditorOrLeftPaneByDefault()
    {
        var winuiRoot = Path.Combine(RepoRoot, "Dev", "Typedown.WinUI");
        var app = File.ReadAllText(Path.Combine(winuiRoot, "App.xaml"));
        var leftPane = File.ReadAllText(Path.Combine(winuiRoot, "Controls", "SidePaneControls", "LeftPane.xaml"));
        var editorContainer = File.ReadAllText(Path.Combine(winuiRoot, "Controls", "EditorControls", "EditorContainer.xaml"));
        var mainContent = File.ReadAllText(Path.Combine(winuiRoot, "Controls", "EditorControls", "MainContent.xaml"));
        var editorHost = File.ReadAllText(Path.Combine(winuiRoot, "Controls", "EditorControls", "Hosting", "WinUIEditorHost.cs"));
        var project = File.ReadAllText(Path.Combine(winuiRoot, "Typedown.WinUI.csproj"));

        AssertContains(app, "x:Key=\"TypedownEditorBackgroundBrush\"");
        AssertContains(app, "#F9F9F9");
        AssertContains(app, "#282828");
        AssertContains(leftPane, "x:Load=\"{x:Bind IsSearchPaneOpen, Mode=OneWay}\"");
        AssertContains(leftPane, "Background=\"{ThemeResource TypedownEditorBackgroundBrush}\"");
        AssertContains(editorContainer, "x:Name=\"EditorInitErrorView\" x:Load=\"False\"");
        AssertContains(editorContainer, "x:Name=\"FindReplacePopup\"");
        AssertContains(editorContainer, "Background=\"{ThemeResource TypedownEditorBackgroundBrush}\"");
        AssertContains(mainContent, "<ColumnDefinition x:Name=\"LeftPaneColumn\" Width=\"300\"/>");
        AssertContains(mainContent, "Background=\"{ThemeResource TypedownEditorBackgroundBrush}\"");
        AssertContains(mainContent, "Value=\"{x:Bind local:MainContent.GetColumnWidthNegative(LeftPaneColumn.Width), Mode=OneWay}\"");
        AssertContains(mainContent, "x:Load=\"{x:Bind IsLeftPaneLoad, Mode=OneWay}\"");
        AssertContains(mainContent, "xmlns:toolkit=\"using:CommunityToolkit.WinUI.Controls\"");
        AssertContains(mainContent, "<toolkit:GridSplitter");
        AssertContains(mainContent, "ResizeDirection=\"Columns\"");
        AssertContains(editorHost, "Content = webView;");
        AssertContains(editorHost, "CreateCurrentThemePayload()");
        AssertContains(editorHost, "ActualTheme == ElementTheme.Dark");
        AssertContains(editorHost, "ApplyNativeEditorBackground(themePayload.Background)");
        AssertContains(editorHost, "AddScriptToExecuteOnDocumentCreatedAsync(BuildInitialEditorBackgroundScript(themePayload.Background))");
        AssertContains(editorHost, "document.documentElement.style.backgroundColor = color");
        AssertContains(editorHost, "themeProvider: CreateCurrentThemePayload");
        AssertContains(editorHost, "Opacity = 0");
        AssertContains(editorHost, "webView.Opacity = 1");
        AssertDoesNotContain(editorHost, "statusBorder");
        AssertDoesNotContain(editorHost, "messageBorder");
        AssertContains(project, "CommunityToolkit.WinUI.Controls.Sizers");
    }

    [TestMethod]
    public void StatusBarSidePaneButton_ControlsMainContentSidePane()
    {
        var winuiRoot = Path.Combine(RepoRoot, "Dev", "Typedown.WinUI");
        var statusBar = File.ReadAllText(Path.Combine(winuiRoot, "Controls", "EditorControls", "StatusBar.xaml"));
        var statusBarCode = File.ReadAllText(Path.Combine(winuiRoot, "Controls", "EditorControls", "StatusBar.xaml.cs"));
        var mainContentCode = File.ReadAllText(Path.Combine(winuiRoot, "Controls", "EditorControls", "MainContent.xaml.cs"));
        var mainPageCode = File.ReadAllText(Path.Combine(winuiRoot, "Views", "MainPage.xaml.cs"));

        AssertContains(statusBar, "x:Name=\"SidePaneToggleButton\"");
        AssertContains(statusBar, "IsChecked=\"{x:Bind IsSidePaneOpen, Mode=TwoWay}\"");
        AssertContains(statusBar, "Glyph=\"{x:Bind IsSidePaneOpen, Converter={StaticResource SidePaneIconConverter}, Mode=OneWay}\"");
        AssertContains(statusBarCode, "public static readonly DependencyProperty IsSidePaneOpenProperty");
        AssertContains(statusBarCode, "public event EventHandler<bool>? SidePaneOpenChanged");
        AssertContains(mainContentCode, "public void SetSidePaneOpen(bool isOpen)");
        AssertContains(mainPageCode, "StatusBar.SidePaneOpenChanged += OnStatusBarSidePaneOpenChanged");
        AssertContains(mainPageCode, "MainContent.SetSidePaneOpen(isOpen)");
    }

    [TestMethod]
    public void StatusBar_WiresWordCountToEditorRuntimeState()
    {
        var winuiRoot = Path.Combine(RepoRoot, "Dev", "Typedown.WinUI");
        var statusBar = File.ReadAllText(Path.Combine(winuiRoot, "Controls", "EditorControls", "StatusBar.xaml"));
        var statusBarCode = File.ReadAllText(Path.Combine(winuiRoot, "Controls", "EditorControls", "StatusBar.xaml.cs"));
        var settingsViewModel = File.ReadAllText(Path.Combine(RepoRoot, "Dev", "Typedown.Presentation", "ViewModels", "SettingsViewModel.cs"));
        var editorViewModel = File.ReadAllText(Path.Combine(RepoRoot, "Dev", "Typedown.Presentation", "ViewModels", "EditorViewModel.cs"));

        AssertContains(settingsViewModel, "public int WordCountMethod");
        AssertContains(editorViewModel, "ContentState = arg[\"state\"].ToObject<ContentState>();");
        AssertContains(statusBar, "SelectedIndex=\"{x:Bind Settings.WordCountMethod, Mode=TwoWay}\"");
        AssertContains(statusBar, "Text=\"{x:Bind Editor.ContentState.WordCount.Character, Mode=OneWay}\"");
        AssertContains(statusBar, "Text=\"{x:Bind Editor.ContentState.WordCount.Word, Mode=OneWay}\"");
        AssertContains(statusBar, "Text=\"{x:Bind CharacterUnit(Editor.ContentState.WordCount.Character), Mode=OneWay}\"");
        AssertContains(statusBar, "Text=\"{x:Bind WordUnit(Editor.ContentState.WordCount.Word), Mode=OneWay}\"");
        AssertContains(statusBarCode, "public SettingsViewModel? Settings => ViewModel?.SettingsViewModel;");
        AssertContains(statusBarCode, "public EditorViewModel? Editor => ViewModel?.EditorViewModel;");
        AssertContains(statusBarCode, "private string CharacterUnit(int number)");
        AssertContains(statusBarCode, "private string WordUnit(int number)");
    }

    [TestMethod]
    public void WinUIFileMenu_UsesDynamicRecentFilesAndExportConfigs()
    {
        var winuiRoot = Path.Combine(RepoRoot, "Dev", "Typedown.WinUI");
        var fileItemXaml = File.ReadAllText(Path.Combine(winuiRoot, "Controls", "EditorControls", "MenuBarItems", "FileItem.xaml"));
        var menuCode = File.ReadAllText(Path.Combine(winuiRoot, "Controls", "EditorControls", "MenuBarItems", "FileItem.xaml.cs"));

        AssertDoesNotContain(fileItemXaml, "Loaded=\"OnOpenRecentSubMenuLoaded\"");
        AssertDoesNotContain(fileItemXaml, "Loaded=\"OnExportSubMenuLoaded\"");
        AssertContains(fileItemXaml, "PointerEntered=\"OnOpenRecentSubMenuRequested\"");
        AssertContains(fileItemXaml, "PointerEntered=\"OnExportSubMenuRequested\"");
        AssertDoesNotContain(fileItemXaml, "CommandParameter=\"pdf\"");
        AssertDoesNotContain(fileItemXaml, "CommandParameter=\"html\"");
        AssertDoesNotContain(fileItemXaml, "CommandParameter=\"text\"");

        AssertDoesNotContain(menuCode, "UpdateOpenRecentItem();");
        AssertDoesNotContain(menuCode, "UpdateExportItem();");
        AssertContains(menuCode, "EnsureInitialized()");
        AssertContains(menuCode, "UpdateOpenRecentItemAsync");
        AssertContains(menuCode, "UpdateExportItemAsync");
        AssertContains(menuCode, "SetCommand(NewWindowItem, files?.NewWindowCommand);");
        AssertContains(menuCode, "FileRecentlyOpened");
        AssertContains(menuCode, "ExportConfigs");
        AssertContains(menuCode, "files.OpenFileCommand");
        AssertContains(menuCode, "files.ExportCommand");
        AssertContains(menuCode, "CommandParameter = file");
        AssertContains(menuCode, "CommandParameter = config");
        AssertDoesNotContain(menuCode, "NoRecentFilesItem.IsEnabled = false;");
        AssertDoesNotContain(menuCode, "NoExportConfigItem.IsEnabled = false;");
    }

    [TestMethod]
    public void WinUIStartup_DefersDatabaseWorkUntilEditorFileLoaded()
    {
        var winuiRoot = Path.Combine(RepoRoot, "Dev", "Typedown.WinUI");
        var presentationRoot = Path.Combine(RepoRoot, "Dev", "Typedown.Presentation");
        var coreRoot = Path.Combine(RepoRoot, "Dev", "Typedown.Core");

        var dispatcherCode = File.ReadAllText(Path.Combine(winuiRoot, "Services", "WinUIUiDispatcher.cs"));
        var fileViewModel = File.ReadAllText(Path.Combine(presentationRoot, "ViewModels", "FileViewModel.cs"));
        var settingsViewModel = File.ReadAllText(Path.Combine(presentationRoot, "ViewModels", "SettingsViewModel.cs"));
        var accessHistory = File.ReadAllText(Path.Combine(coreRoot, "Services", "AccessHistory.cs"));
        var fileExport = File.ReadAllText(Path.Combine(winuiRoot, "Services", "WinUIFileExport.cs"));

        AssertDoesNotContain(dispatcherCode, "if (dispatcherQueue.HasThreadAccess)");
        AssertContains(fileViewModel, "WaitForInitialEditorFileLoadedAsync");
        AssertContains(fileViewModel, "RunAfterInitialEditorFileLoadedAsync");
        AssertContains(fileViewModel, "SettingsViewModel.LastFilePath");
        AssertContains(fileViewModel, "SettingsViewModel.LastFolderPath");
        AssertContains(settingsViewModel, "public string LastFilePath");
        AssertContains(settingsViewModel, "public string LastFolderPath");
        AssertDoesNotContain(accessHistory, "_ = UpdateRecentlyOpened();");
        AssertContains(accessHistory, "initializationTask ??= UpdateRecentlyOpened();");
        AssertDoesNotContain(fileExport, "Initialize();");
        AssertContains(fileExport, "EnsureExportConfigsLoaded");
    }

    [TestMethod]
    public void AccessHistory_DoesNotBackfillDatabaseOnEveryMutation()
    {
        var accessHistory = File.ReadAllText(Path.Combine(RepoRoot, "Dev", "Typedown.Core", "Services", "AccessHistory.cs"));

        AssertDoesNotContain(accessHistory, "if (FileRecentlyOpened.Count < maxCount)");
        AssertDoesNotContain(accessHistory, "if (FolderRecentlyOpened.Count < maxCount)");
        AssertContains(accessHistory, "CollectionChangeAction.Refresh");
        AssertContains(accessHistory, "LoadFileRecentlyOpenedAsync");
        AssertContains(accessHistory, "LoadFolderRecentlyOpenedAsync");
        AssertContains(accessHistory, "AsNoTracking()");
    }

    [TestMethod]
    public void AppDbContext_SkipsRawSqliteMetadataProbeWithoutPackageIdentity()
    {
        var appDbContext = File.ReadAllText(Path.Combine(RepoRoot, "Dev", "Typedown.Core", "Services", "AppDbContext.cs"));

        AssertContains(appDbContext, "if (!CanProbeLegacySqliteMetadata())");
        AssertContains(appDbContext, "private static bool CanProbeLegacySqliteMetadata()");
        AssertContains(appDbContext, "GetCurrentPackageFullName");
        AssertContains(appDbContext, "return GetCurrentPackageFullName(ref length, IntPtr.Zero) == ErrorInsufficientBuffer;");
    }

    [TestMethod]
    public void WinUIStartup_PrewarmsSharedWebViewEnvironment()
    {
        var winuiRoot = Path.Combine(RepoRoot, "Dev", "Typedown.WinUI");
        var app = File.ReadAllText(Path.Combine(winuiRoot, "App.xaml.cs"));
        var platformServices = File.ReadAllText(Path.Combine(winuiRoot, "Services", "WinUIPlatformServices.cs"));

        AssertContains(platformServices, "WinUIWebViewEnvironmentService");
        AssertContains(platformServices, "WebViewEnvironmentService = new WinUIWebViewEnvironmentService");
        AssertContains(platformServices, "public WinUIWebViewEnvironmentService WebViewEnvironmentService { get; }");
        AssertContains(app, "platformServices.WebViewEnvironmentService.StartPrewarm();");
        AssertContains(app, ".AddSingleton(platformServices.WebViewEnvironmentService)");
    }

    [TestMethod]
    public void WinUIEditorHost_UsesSharedPrewarmedWebViewEnvironment()
    {
        var host = File.ReadAllText(Path.Combine(RepoRoot, "Dev", "Typedown.WinUI", "Controls", "EditorControls", "Hosting", "WinUIEditorHost.cs"));

        AssertContains(host, "WinUIWebViewEnvironmentService");
        AssertContains(host, "GetEnvironmentAsync()");
        AssertContains(host, "await webView.EnsureCoreWebView2Async(environment);");
    }

    [TestMethod]
    public void WinUIFloatViewService_AnchorsEditorRelativeFlyoutsToEditorContainer()
    {
        var winuiRoot = Path.Combine(RepoRoot, "Dev", "Typedown.WinUI");
        var editorContainer = File.ReadAllText(Path.Combine(winuiRoot, "Controls", "EditorControls", "EditorContainer.xaml"));
        var editorContainerCode = File.ReadAllText(Path.Combine(winuiRoot, "Controls", "EditorControls", "EditorContainer.xaml.cs"));
        var floatViewService = File.ReadAllText(Path.Combine(winuiRoot, "Services", "WinUIFloatViewService.cs"));

        AssertContains(editorContainer, "x:Name=\"FloatAnchorCanvas\"");
        AssertContains(editorContainer, "x:Name=\"FloatAnchorElement\"");
        AssertContains(editorContainerCode, "GetFloatAnchor(Rect rect)");
        AssertContains(editorContainerCode, "MoveFloatAnchor(");
        AssertContains(editorContainerCode, "Canvas.SetLeft(FloatAnchorElement");
        AssertContains(editorContainerCode, "Canvas.SetTop(FloatAnchorElement");
        AssertContains(floatViewService, "TryResolveEditorRectAnchor");
        AssertContains(floatViewService, "GetFloatAnchor(rect)");
        AssertContains(floatViewService, "ResolveEditorAnchor()");
        AssertContains(floatViewService, "MarkdownEditorPresenter");
        AssertContains(floatViewService, "flyout.ShowAt(rectAnchor");
        AssertDoesNotContain(floatViewService, "var anchor = ResolveAnchor();");
    }

    private static void AssertContains(string source, string snippet)
    {
        StringAssert.Contains(source, snippet);
    }

    private static void AssertDoesNotContain(string source, string snippet)
    {
        Assert.IsFalse(source.Contains(snippet, StringComparison.Ordinal), $"Did not expect to find snippet: {snippet}");
    }

    private static void AssertAppearsBefore(string source, string first, string second)
    {
        var firstIndex = source.IndexOf(first, StringComparison.Ordinal);
        var secondIndex = source.IndexOf(second, StringComparison.Ordinal);
        Assert.IsTrue(firstIndex >= 0, $"Did not find snippet: {first}");
        Assert.IsTrue(secondIndex >= 0, $"Did not find snippet: {second}");
        Assert.IsTrue(firstIndex < secondIndex, $"Expected '{first}' to appear before '{second}'.");
    }

    private static string FindRepoRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "Typedown.sln")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate Typedown.sln from the architecture test output directory.");
    }
}
