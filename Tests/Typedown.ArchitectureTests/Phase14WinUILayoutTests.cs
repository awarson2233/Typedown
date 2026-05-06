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
        AssertContains(app, "targetWindow.SystemBackdrop = new MicaBackdrop()");
        AssertContains(app, "targetWindow.AppWindow.TitleBar");
        AssertContains(app, "window.SetTitleBar(rootControl.TitleBarElement)");
        AssertContains(root, "x:Name=\"AppTitleBar\"");
        AssertContains(root, "x:Name=\"TitleDragRegion\"");
        AssertContains(root, "x:Name=\"BackButton\"");
        AssertContains(root, "Width=\"32\"");
        AssertContains(root, "Height=\"32\"");
        AssertContains(root, "Style=\"{ThemeResource TitleBarBackButtonStyle}\"");
        AssertContains(root, "Text=\"{u:LocaleString Key=AppName}\"");
        AssertContains(rootCode, "Frame.Navigate(typeof(Views.MainPage), MainPageNavigationParameter)");
        AssertContains(rootCode, "public UIElement TitleBarElement => TitleDragRegion");
        AssertContains(rootCode, "Frame.SourcePageType == typeof(Pages.SettingsPage)");
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
    public void WinUIFileMenu_UsesDynamicRecentFilesAndExportConfigs()
    {
        var winuiRoot = Path.Combine(RepoRoot, "Dev", "Typedown.WinUI");
        var fileItemXaml = File.ReadAllText(Path.Combine(winuiRoot, "Controls", "EditorControls", "MenuBarItems", "FileItem.xaml"));
        var menuCode = File.ReadAllText(Path.Combine(winuiRoot, "Controls", "EditorControls", "MenuBarItems", "FileItem.xaml.cs"));

        AssertContains(fileItemXaml, "Loaded=\"OnOpenRecentSubMenuLoaded\"");
        AssertContains(fileItemXaml, "Loaded=\"OnExportSubMenuLoaded\"");
        AssertDoesNotContain(fileItemXaml, "CommandParameter=\"pdf\"");
        AssertDoesNotContain(fileItemXaml, "CommandParameter=\"html\"");
        AssertDoesNotContain(fileItemXaml, "CommandParameter=\"text\"");

        AssertContains(menuCode, "UpdateOpenRecentItem();");
        AssertContains(menuCode, "UpdateExportItem();");
        AssertContains(menuCode, "FileRecentlyOpened");
        AssertContains(menuCode, "ExportConfigs");
        AssertContains(menuCode, "files.OpenFileCommand");
        AssertContains(menuCode, "files.ExportCommand");
        AssertContains(menuCode, "CommandParameter = file");
        AssertContains(menuCode, "CommandParameter = config");
        AssertDoesNotContain(menuCode, "NoRecentFilesItem.IsEnabled = false;");
        AssertDoesNotContain(menuCode, "NoExportConfigItem.IsEnabled = false;");
    }

    private static void AssertContains(string source, string snippet)
    {
        StringAssert.Contains(source, snippet);
    }

    private static void AssertDoesNotContain(string source, string snippet)
    {
        Assert.IsFalse(source.Contains(snippet, StringComparison.Ordinal), $"Did not expect to find snippet: {snippet}");
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
