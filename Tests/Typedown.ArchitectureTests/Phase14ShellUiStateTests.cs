using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Typedown.UI.Composition;
using Typedown.UI.ViewModels;

namespace Typedown.ArchitectureTests;

[TestClass]
public class Phase14ShellUiStateTests
{
    private static readonly string RepoRoot = FindRepoRoot();

    [TestMethod]
    public void Phase14ShellViewModel_CanBeResolvedFromTypedownUiComposition()
    {
        var services = new ServiceCollection()
            .AddTypedownUI();

        var shellRegistration = services.SingleOrDefault(descriptor => descriptor.ServiceType == typeof(Phase14ShellViewModel));
        var mainPage = new MainPageViewModel();

        Assert.IsNotNull(shellRegistration);
        Assert.AreEqual(ServiceLifetime.Transient, shellRegistration.Lifetime);
        Assert.IsNotNull(mainPage.Shell);
    }

    [TestMethod]
    public void Phase14ShellViewModel_ExposesBaselineVisibleShellCollections()
    {
        var shell = new Phase14ShellViewModel();

        CollectionAssert.AreEqual(new[] { "File", "Edit", "View" }, shell.CommandGroups.Select(group => group.Label).ToArray());
        CollectionAssert.IsSubsetOf(new[] { "NewFile", "OpenFile", "Save", "Undo", "Redo", "SidePane", "StatusBar" }, shell.CommandGroups
            .SelectMany(group => group.Items)
            .Select(item => item.CommandName)
            .ToArray());

        CollectionAssert.AreEqual(new[] { "Document", "Outline", "Search" }, shell.SidePaneSections.Select(section => section.Key).ToArray());
        CollectionAssert.IsSubsetOf(new[] { "FileName", "SavedState", "EditorMode", "Encoding" }, shell.StatusItems.Select(item => item.Key).ToArray());

        Assert.AreEqual("Typedown", shell.AppTitle);
        Assert.AreEqual("Untitled", shell.DocumentTitle);
        Assert.IsFalse(string.IsNullOrWhiteSpace(shell.EditorPanelTitle));
        Assert.IsFalse(string.IsNullOrWhiteSpace(shell.EditorPanelDescription));
        Assert.IsTrue(shell.CommandGroups.SelectMany(group => group.Items).Any(item => item.ShortcutDisplayText == "Ctrl+N"));
        Assert.IsTrue(shell.CommandGroups.SelectMany(group => group.Items).Any(item => item.ShortcutDisplayText == "Ctrl+S"));
    }

    [TestMethod]
    public void Phase14TypedownUi_SourceStaysPlatformNeutral()
    {
        var uiRoot = Path.Combine(RepoRoot, "Dev", "Typedown.UI");
        var uiSources = Directory
            .EnumerateFiles(uiRoot, "*.cs", SearchOption.AllDirectories)
            .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase)
                && !path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase))
            .Select(File.ReadAllText)
            .ToArray();

        Assert.IsTrue(uiSources.Length > 0, "Expected Typedown.UI source files.");

        foreach (var source in uiSources)
        {
            AssertNoReference(source, "using Microsoft.UI.Xaml");
            AssertNoReference(source, "using Windows.UI.Xaml");
            AssertNoReference(source, "global using Microsoft.UI.Xaml");
            AssertNoReference(source, "global using Windows.UI.Xaml");
            AssertNoReference(source, "using Typedown.WinUI");
            AssertNoReference(source, "using Typedown.XamlUI");
            AssertNoReference(source, @"..\Typedown.WinUI\Typedown.WinUI.csproj");
            AssertNoReference(source, @"..\Typedown.XamlUI\Typedown.XamlUI.csproj");
        }
    }

    private static void AssertNoReference(string source, string token)
    {
        Assert.IsFalse(source.Contains(token, StringComparison.Ordinal), $"Unexpected reference: {token}");
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
