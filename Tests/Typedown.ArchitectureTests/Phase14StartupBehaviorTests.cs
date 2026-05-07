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

        AssertContains(fileViewModel, "private string startupOpenedFilePath = null;");
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
        AssertDoesNotContain(generalPage, "StartupOpenFolder, Mode=TwoWay}\" x:Load=");
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
