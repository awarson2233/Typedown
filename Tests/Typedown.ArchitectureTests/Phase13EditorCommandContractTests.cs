using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Typedown.ArchitectureTests;

[TestClass]
public class Phase13EditorCommandContractTests
{
    private static readonly string RepoRoot = FindRepoRoot();

    [TestMethod]
    public void WinUIEditorHostContracts_AreShellLocalAndAvoidPresentationLeak()
    {
        var source = File.ReadAllText(Path.Combine(RepoRoot, "Dev", "Typedown.WinUI", "Controls", "EditorHostContracts.cs"));

        StringAssert.Contains(source, "namespace Typedown.WinUI.Controls");
        AssertNoTypeReference(source, "Typedown.Core.Contracts");
        AssertNoTypeReference(source, "Typedown.Presentation.ViewModels");
        AssertNoTypeReference(source, "Microsoft.UI.Xaml");
        AssertNoTypeReference(source, "Microsoft.Web.WebView2");
    }

    [TestMethod]
    public void WinUIEditorHost_NoLongerReferencesRemovedCoreEditorContracts()
    {
        var winuiControls = Directory
            .EnumerateFiles(Path.Combine(RepoRoot, "Dev", "Typedown.WinUI", "Controls"), "WinUIEditor*.cs", SearchOption.TopDirectoryOnly)
            .Select(File.ReadAllText);

        foreach (var source in winuiControls)
        {
            AssertNoTypeReference(source, "Typedown.Core.Contracts.Editor");
        }
    }

    private static void AssertNoTypeReference(string source, string text)
    {
        Assert.IsFalse(source.Contains(text, StringComparison.Ordinal), $"Unexpected reference: {text}");
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
