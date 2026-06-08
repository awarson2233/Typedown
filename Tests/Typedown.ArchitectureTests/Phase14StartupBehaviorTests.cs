using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Typedown.ArchitectureTests;

[TestClass]
public class Phase14StartupBehaviorTests
{
    private static readonly string RepoRoot = FindRepoRoot();

    [TestMethod]
    public void WinUIStartup_FollowOpenedFileFolderPrefersStartupFileDirectoryAndFallsBackToOpenLast()
    {
        var fileViewModel = File.ReadAllText(Path.Combine(RepoRoot, "Dev", "Typedown.Presentation", "ViewModels", "FileViewModel.cs"));

        AssertContains(fileViewModel, "private string? startupOpenedFilePath;");
        AssertContains(fileViewModel, "startupOpenedFilePath = null;");
        AssertContains(fileViewModel, "startupOpenedFilePath = FilePath;");
        AssertContains(fileViewModel, "case FolderStartupAction.FollowOpenedFileFolder:");
        AssertContains(fileViewModel, "Path.GetDirectoryName(startupOpenedFilePath)");
        AssertContains(fileViewModel, "return await ResolveOpenLastFolderAsync();");
    }

    [TestMethod]
    public void GeneralSettingsPage_UsesEnumBackedFolderStartupOptionsAndOnlyShowsCustomFolderPickerForOpenFolder()
    {
        var generalPage = File.ReadAllText(Path.Combine(RepoRoot, "Dev", "Typedown.WinUI", "Pages", "SettingPages", "GeneralPage.xaml"));
        var generalPageCodeBehind = File.ReadAllText(Path.Combine(RepoRoot, "Dev", "Typedown.WinUI", "Pages", "SettingPages", "GeneralPage.xaml.cs"));

        AssertContains(generalPage, "ItemsSource=\"{x:Bind enums:Enumerable.FolderStartupActions}\"");
        AssertContains(generalPage, "Visibility=\"{x:Bind local:GeneralPage.IsStartupOpenFolderItemLoad(Settings.FolderStartupAction), Mode=OneWay}\"");
        AssertContains(generalPageCodeBehind, "return action == FolderStartupAction.OpenFolder ? Visibility.Visible : Visibility.Collapsed;");
        AssertContains(generalPageCodeBehind, "subscribedSettings.PropertyChanged += OnSettingsPropertyChanged;");
        AssertContains(generalPageCodeBehind, "e.PropertyName == nameof(SettingsViewModel.Language)");
        AssertContains(generalPageCodeBehind, "Bindings.Update();");
        AssertContains(generalPageCodeBehind, "Bindings.StopTracking();");
        AssertDoesNotContain(generalPage, "StartupOpenFolder, Mode=TwoWay}\" x:Load=");
    }

    [TestMethod]
    public void KeepRun_CancelsSystemCloseAndHidesWindowWithoutDestroyingIt()
    {
        var appSource = File.ReadAllText(Path.Combine(RepoRoot, "Dev", "Typedown.WinUI", "App.xaml.cs"));
        var windowContextSource = File.ReadAllText(Path.Combine(RepoRoot, "Dev", "Typedown.WinUI", "Services", "WinUIWindowContext.cs"));

        AssertContains(appSource, "appWindow.Closing -= OnAppWindowClosing;");
        AssertContains(appSource, "appWindow.Closing += OnAppWindowClosing;");
        AssertContains(appSource, "private void OnAppWindowClosing(AppWindow sender, AppWindowClosingEventArgs args)");
        AssertContains(appSource, "args.Cancel = true;");
        AssertContains(appSource, "_ = HandleAppWindowClosingAsync(sender);");
        AssertContains(appSource, "if (appViewModel?.SettingsViewModel.KeepRun == true)");
        AssertContains(appSource, "sender.Hide();");
        AssertContains(windowContextSource, "window.AppWindow.Show();");
        AssertContains(windowContextSource, "window.Activate();");
    }

    [TestMethod]
    public void BlankEditorSaveAndClose_UsesStructuredLoadPayloadAndUnsavedPrompt()
    {
        var fileViewModel = File.ReadAllText(Path.Combine(RepoRoot, "Dev", "Typedown.Presentation", "ViewModels", "FileViewModel.cs"));
        var appSource = File.ReadAllText(Path.Combine(RepoRoot, "Dev", "Typedown.WinUI", "App.xaml.cs"));

        AssertContains(fileViewModel, "EditorCommandSink?.Send(\"LoadFile\", new { text = EditorViewModel.Markdown, basePath = ImageBasePath });");
        AssertDoesNotContain(fileViewModel, "EditorCommandSink?.Send(\"LoadFile\", EditorViewModel.Markdown);");
        AssertContains(fileViewModel, "EventCenter.GetObservable<EditorEventArgs>(\"Save\")");
        AssertContains(fileViewModel, "EventCenter.GetObservable<EditorEventArgs>(\"SaveAs\")");
        AssertContains(fileViewModel, "EventCenter.GetObservable<EditorEventArgs>(\"Close\")");
        AssertContains(fileViewModel, "private bool saveAsOpened;");
        AssertContains(fileViewModel, "if (saveAsOpened)");
        AssertContains(appSource, "private bool allowWindowClose;");
        AssertContains(appSource, "_ = HandleAppWindowClosingAsync(sender);");
        AssertContains(appSource, "private async Task HandleAppWindowClosingAsync(AppWindow sender)");
        AssertContains(appSource, "await appViewModel.FileViewModel.AskToSave()");
        AssertContains(appSource, "catch (Exception ex)");
        AssertContains(appSource, "Debug.WriteLine(ex);");
        AssertContains(appSource, "window?.Close();");
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
