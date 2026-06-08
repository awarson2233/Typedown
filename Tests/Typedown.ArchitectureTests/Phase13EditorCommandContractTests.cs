using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Typedown.ArchitectureTests;

[TestClass]
public class Phase13EditorCommandContractTests
{
    private static readonly string RepoRoot = FindRepoRoot();

    [TestMethod]
    public void WinUIEditorHostContracts_AreShellLocalAndAvoidPresentationLeak()
    {
        var source = File.ReadAllText(Path.Combine(RepoRoot, "Dev", "Typedown.WinUI", "Controls", "EditorControls", "Hosting", "EditorHostContracts.cs"));

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
            .EnumerateFiles(Path.Combine(RepoRoot, "Dev", "Typedown.WinUI", "Controls", "EditorControls", "Hosting"), "WinUIEditor*.cs", SearchOption.TopDirectoryOnly)
            .Select(File.ReadAllText);

        foreach (var source in winuiControls)
        {
            AssertNoTypeReference(source, "Typedown.Core.Contracts.Editor");
        }
    }

    [TestMethod]
    public void WinUIEditorHost_ForwardsSaveAndCloseShortcutsFromWebView()
    {
        var hostSource = File.ReadAllText(Path.Combine(RepoRoot, "Dev", "Typedown.WinUI", "Controls", "EditorControls", "Hosting", "WinUIEditorHost.cs"));
        var sessionSource = File.ReadAllText(Path.Combine(RepoRoot, "Dev", "Typedown.WinUI", "Controls", "EditorControls", "Hosting", "WinUIEditorDocumentSession.cs"));

        AssertHasTypeReference(hostSource, "event.defaultPrevented || event.repeat");
        AssertHasTypeReference(hostSource, "name = 'Save'");
        AssertHasTypeReference(hostSource, "name = 'SaveAs'");
        AssertHasTypeReference(hostSource, "name = 'Close'");
        AssertHasTypeReference(hostSource, "key === 's'");
        AssertHasTypeReference(hostSource, "key === 'w'");
        AssertHasTypeReference(sessionSource, "or \"Save\"");
        AssertHasTypeReference(sessionSource, "or \"SaveAs\"");
        AssertHasTypeReference(sessionSource, "or \"Close\"");
    }

    [TestMethod]
    public void EditorThemeRuntime_NormalizesPayloadAndReplaysLatestThemeAfterContentReady()
    {
        var themeSource = File.ReadAllText(Path.Combine(RepoRoot, "Dev", "Typedown.Editor", "src", "services", "theme.ts"));
        var commandSinkSource = File.ReadAllText(Path.Combine(RepoRoot, "Dev", "Typedown.WinUI", "Services", "WinUIEditorCommandSink.cs"));
        var hostSource = File.ReadAllText(Path.Combine(RepoRoot, "Dev", "Typedown.WinUI", "Controls", "EditorControls", "Hosting", "WinUIEditorHost.cs"));

        StringAssert.Contains(themeSource, "function normalizeTheme");
        StringAssert.Contains(themeSource, "function normalizeColor");
        StringAssert.Contains(themeSource, "Number.isFinite(numberValue)");
        StringAssert.Contains(themeSource, "Math.min(Math.max(numberValue, min), max)");
        StringAssert.Contains(themeSource, "payload?.accentColor");
        StringAssert.Contains(themeSource, "payload?.background");
        StringAssert.Contains(themeSource, "formatRgba");
        AssertNoTypeReference(themeSource, "theme?.toLowerCase()");

        StringAssert.Contains(commandSinkSource, "latestThemeCommand = new PendingCommand(name, args);");
        StringAssert.Contains(commandSinkSource, "ResendLatestTheme");
        StringAssert.Contains(hostSource, "commandSink?.ResendLatestTheme(this);");
    }

    private static void AssertHasTypeReference(string source, string text)
    {
        Assert.IsTrue(source.Contains(text, StringComparison.Ordinal), $"Expected reference: {text}");
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
