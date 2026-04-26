using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Typedown.Core.Contracts.Shell;

namespace Typedown.ArchitectureTests;

[TestClass]
public class Phase13ShellStateContractTests
{
    private static readonly string RepoRoot = FindRepoRoot();

    private static readonly string[] ForbiddenShellBoundaryTokens =
    [
        "Windows.UI.Xaml",
        "Microsoft.UI.Xaml",
        "Typedown.WinUI",
        "Typedown.XamlUI",
        "Windows.Storage",
        "Windows.UI.Xaml.Window",
        "Microsoft.UI.Xaml.Window",
        "IWindow",
        "WindowHandle",
        "HWND",
        "AppWindow",
        "CoreWindow",
        "FrameStack",
        "NavigationStack",
        "BackStack",
        "XamlRoot",
    ];

    private static readonly string[] ForbiddenIoAndLegacyUiTokens =
    [
        "File.Exists",
        "File.Read",
        "File.Write",
        "Directory.",
        "Config",
        "SettingsViewModel",
        "FilePicker",
        "FileOpenPicker",
        "FileSavePicker",
        "ContentDialog",
    ];

    [TestMethod]
    public void ShellStateContracts_StayPlatformNeutralAndAvoidLegacyDependencies()
    {
        var shellRoot = Path.Combine(RepoRoot, "Dev", "Typedown.Core.Contracts", "Shell");
        var shellFiles = Directory
            .EnumerateFiles(shellRoot, "*.cs", SearchOption.TopDirectoryOnly)
            .ToArray();

        Assert.IsTrue(shellFiles.Length >= 3, "Expected Phase 13 shell visible state contracts.");

        foreach (var file in shellFiles)
        {
            var source = File.ReadAllText(file);

            AssertNoReferences(source, ForbiddenShellBoundaryTokens);
            AssertNoReferences(source, ForbiddenIoAndLegacyUiTokens);
        }
    }

    [TestMethod]
    public void FileUiState_PreservesLegacyFileViewModelVisibleState()
    {
        var state = FileUiState.FromValues(
            workFolder: @"C:\docs",
            filePath: @"C:\docs\note.md",
            defaultImageBasePath: @"C:\images");

        Assert.AreEqual(@"C:\docs", state.WorkFolder);
        Assert.AreEqual(@"C:\docs\note.md", state.FilePath);
        Assert.AreEqual(@"C:\docs", state.ImageBasePath);
        Assert.AreEqual("note.md", state.FileName);
        Assert.IsFalse(state.IsUntitled);
        Assert.IsTrue(state.HasFilePath);
    }

    [TestMethod]
    public void FileUiState_MatchesLegacyFileNameAndImageBasePathDerivation()
    {
        var nullFile = FileUiState.FromValues(
            workFolder: null,
            filePath: null,
            defaultImageBasePath: @"C:\configured-images");

        Assert.IsNull(nullFile.FileName);
        Assert.AreEqual(@"C:\configured-images", nullFile.ImageBasePath);
        Assert.IsTrue(nullFile.IsUntitled);
        Assert.IsFalse(nullFile.HasFilePath);

        var emptyFile = FileUiState.FromValues(
            workFolder: null,
            filePath: string.Empty,
            defaultImageBasePath: @"C:\configured-images");

        Assert.AreEqual(string.Empty, emptyFile.FileName);
        Assert.AreEqual(@"C:\configured-images", emptyFile.ImageBasePath);
        Assert.IsTrue(emptyFile.IsUntitled);
        Assert.IsFalse(emptyFile.HasFilePath);
    }

    [TestMethod]
    public void ShellChromeState_ExpressesVisibleShellStateWithoutPlatformHandles()
    {
        var state = ShellChromeState.FromValues(
            title: "*note.md - Typedown",
            isSaved: false,
            displaySaved: false,
            isTopmost: true,
            captionHeight: 40,
            compactMode: true,
            currentPageName: "Main");

        Assert.AreEqual("*note.md - Typedown", state.Title);
        Assert.IsFalse(state.IsSaved);
        Assert.IsFalse(state.DisplaySaved);
        Assert.IsTrue(state.IsTopmost);
        Assert.AreEqual(40, state.CaptionHeight);
        Assert.IsTrue(state.CompactMode);
        Assert.AreEqual("Main", state.CurrentPageName);
    }

    [TestMethod]
    public void ShellDocumentState_SerializesWithLegacyCamelCaseNames()
    {
        var state = ShellDocumentState.FromValues(
            FileUiState.FromValues(@"C:\docs", @"C:\docs\note.md", @"C:\images"),
            ShellChromeState.FromValues("note.md - Typedown", true, true, false, 32, false, "Main"));

        var json = JsonSerializer.Serialize(state);

        StringAssert.Contains(json, "\"file\"");
        StringAssert.Contains(json, "\"chrome\"");
        StringAssert.Contains(json, "\"workFolder\"");
        StringAssert.Contains(json, "\"filePath\"");
        StringAssert.Contains(json, "\"imageBasePath\"");
        StringAssert.Contains(json, "\"fileName\"");
        StringAssert.Contains(json, "\"isUntitled\"");
        StringAssert.Contains(json, "\"hasFilePath\"");
        StringAssert.Contains(json, "\"title\"");
        StringAssert.Contains(json, "\"isSaved\"");
        StringAssert.Contains(json, "\"displaySaved\"");
        StringAssert.Contains(json, "\"isTopmost\"");
        StringAssert.Contains(json, "\"captionHeight\"");
        StringAssert.Contains(json, "\"compactMode\"");
        StringAssert.Contains(json, "\"currentPageName\"");
        Assert.IsFalse(json.Contains("\"File\"", StringComparison.Ordinal));
        Assert.IsFalse(json.Contains("\"Chrome\"", StringComparison.Ordinal));
        Assert.IsFalse(json.Contains("\"WorkFolder\"", StringComparison.Ordinal));
        Assert.IsFalse(json.Contains("\"FilePath\"", StringComparison.Ordinal));
        Assert.IsFalse(json.Contains("\"ImageBasePath\"", StringComparison.Ordinal));
        Assert.IsFalse(json.Contains("\"CurrentPageName\"", StringComparison.Ordinal));
    }

    private static void AssertNoReferences(string source, IReadOnlyList<string> forbiddenTokens)
    {
        foreach (var token in forbiddenTokens)
        {
            AssertNoReference(source, token);
        }
    }

    private static void AssertNoReference(string source, string text)
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
