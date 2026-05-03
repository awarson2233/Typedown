using System.Text.RegularExpressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Typedown.ArchitectureTests;

[TestClass]
public class Phase15PresentationBoundaryTests
{
    private static readonly string RepoRoot = FindRepoRoot();

    private static readonly string[] RequiredPresentationPorts =
    [
        "IClipboard",
        "IFileExport",
        "IFileOperation",
        "IFloatViewService",
        "IKeyboardAccelerator",
        "IEditorCommandSink",
        "IEditorSettingsNotifier",
        "ITableDialogService",
        "IWindowService"
    ];

    [TestMethod]
    public void WinUICompositionRoot_RegistersPresentationViewModelPorts()
    {
        var appSource = File.ReadAllText(Path.Combine(RepoRoot, "Dev", "Typedown.WinUI", "App.xaml.cs"));

        foreach (var port in RequiredPresentationPorts)
        {
            AssertHasTypeReference(appSource, $"AddSingleton<{port},");
        }

        AssertNoTypeReference(appSource, "AddSingleton<IFileConverter");
        AssertNoTypeReference(appSource, "AddSingleton<IPowerShellService");
        AssertContainsInOrder(
            appSource,
            ".AddSingleton<IClipboard, WinUIClipboard>()",
            ".AddSingleton<IFileExport, WinUIFileExport>()",
            ".AddSingleton<IFileOperation, WinUIFileOperation>()",
            ".AddSingleton<IFloatViewService, WinUIFloatViewService>()",
            ".AddSingleton<IKeyboardAccelerator, WinUIKeyboardAccelerator>()",
            ".AddSingleton<IEditorCommandSink, WinUIEditorCommandSink>()",
            ".AddSingleton<IEditorSettingsNotifier, WinUIEditorSettingsNotifier>()",
            ".AddSingleton<ITableDialogService, WinUITableDialogService>()",
            ".AddSingleton<IWindowService, WinUIWindowService>()",
            ".AddTypedownCore()",
            ".AddTypedownPresentation()");
    }

    [TestMethod]
    public void WinUIProject_ProvidesAdaptersForPresentationViewModelPorts()
    {
        var servicesRoot = Path.Combine(RepoRoot, "Dev", "Typedown.WinUI", "Services");

        AssertServiceImplementsPort(servicesRoot, "WinUIClipboard.cs", "WinUIClipboard", "IClipboard");
        AssertServiceImplementsPort(servicesRoot, "WinUIFileExport.cs", "WinUIFileExport", "IFileExport");
        AssertServiceImplementsPort(servicesRoot, "WinUIFileOperation.cs", "WinUIFileOperation", "IFileOperation");
        AssertServiceImplementsPort(servicesRoot, "WinUIFloatViewService.cs", "WinUIFloatViewService", "IFloatViewService");
        AssertServiceImplementsPort(servicesRoot, "WinUIKeyboardAccelerator.cs", "WinUIKeyboardAccelerator", "IKeyboardAccelerator");
        AssertServiceImplementsPort(servicesRoot, "WinUIEditorCommandSink.cs", "WinUIEditorCommandSink", "IEditorCommandSink");
        AssertServiceImplementsPort(servicesRoot, "WinUIEditorSettingsNotifier.cs", "WinUIEditorSettingsNotifier", "IEditorSettingsNotifier");
        AssertServiceImplementsPort(servicesRoot, "WinUITableDialogService.cs", "WinUITableDialogService", "ITableDialogService");
        AssertServiceImplementsPort(servicesRoot, "WinUIWindowService.cs", "WinUIWindowService", "IWindowService");
    }

    [TestMethod]
    public void WinUILocale_InitializesPresentationLocalizationFromWinUIReswFallback()
    {
        var appSource = File.ReadAllText(Path.Combine(RepoRoot, "Dev", "Typedown.WinUI", "App.xaml.cs"));
        var localePath = Path.Combine(RepoRoot, "Dev", "Typedown.WinUI", "Utilities", "WinUILocale.cs");

        Assert.IsTrue(File.Exists(localePath), $"Expected WinUI localization initializer {localePath}.");

        var localeSource = File.ReadAllText(localePath);
        AssertContainsInOrder(appSource, "WinUILocale.Initialize();", "new ServiceCollection()");
        AssertContainsInOrder(
            localeSource,
            "PresentationLocale.StringResolver = GetString;",
            "LocaleAttribute.StringResolver = key => PresentationLocale.GetString(key);");
        AssertHasTypeReference(localeSource, "Typedown.Presentation.Utilities");
        AssertHasTypeReference(localeSource, "Typedown.Core.Utilities");
        AssertHasTypeReference(localeSource, "ResourceManager.Current.MainResourceMap");
        AssertHasTypeReference(localeSource, "AppContext.BaseDirectory");
        AssertHasTypeReference(localeSource, "Resources");
        AssertHasTypeReference(localeSource, "Strings");
        AssertHasTypeReference(localeSource, ".resw");
        AssertHasTypeReference(localeSource, "CommonResources");
        AssertHasTypeReference(localeSource, "DialogResources");
        AssertHasTypeReference(localeSource, "SettingsResources");
        AssertHasTypeReference(localeSource, "Resources");
        AssertHasTypeReference(localeSource, "Replace('.', '/')");
    }

    [TestMethod]
    public void WinUIEditorBridge_RoutesPresentationOwnedInvokesAndEventsThroughPresentationServices()
    {
        var controlsRoot = Path.Combine(RepoRoot, "Dev", "Typedown.WinUI", "Controls");
        var sessionSource = File.ReadAllText(Path.Combine(controlsRoot, "WinUIEditorDocumentSession.cs"));
        var adapterSource = File.ReadAllText(Path.Combine(controlsRoot, "WinUIEditorBridgeAdapter.cs"));
        var containerSource = File.ReadAllText(Path.Combine(controlsRoot, "EditorControls", "EditorContainer.xaml.cs"));
        var hostSource = File.ReadAllText(Path.Combine(controlsRoot, "WinUIEditorHost.cs"));
        var commandSinkSource = File.ReadAllText(Path.Combine(RepoRoot, "Dev", "Typedown.WinUI", "Services", "WinUIEditorCommandSink.cs"));

        AssertNoTypeReference(sessionSource, "HandleStubInvoke");
        foreach (var invokeName in new[] { "ExportCallback", "PrintHTML", "SetClipboard", "GetSettings", "GetStringResources", "ResizeTable" })
        {
            AssertContainsInOrder(sessionSource, invokeName, "InvokePresentationAsync");
        }

        AssertDoesNotContain(sessionSource, "\"ExportCallback\" => true");
        AssertDoesNotContain(sessionSource, "\"PrintHTML\" => true");
        AssertDoesNotContain(sessionSource, "\"SetClipboard\" => true");
        AssertDoesNotContain(sessionSource, "\"GetSettings\" => SettingsSnapshot.Payload");
        AssertDoesNotContain(sessionSource, "\"GetStringResources\" => CreateStringResources");
        AssertDoesNotContain(sessionSource, "\"ResizeTable\" => CreateResizeTablePayload");

        AssertHasTypeReference(sessionSource, "RemoteInvoke");
        AssertHasTypeReference(sessionSource, "EventCenter");
        AssertHasTypeReference(sessionSource, "EditorEventArgs");
        AssertHasTypeReference(sessionSource, "JToken");
        AssertHasTypeReference(adapterSource, "HandleRemoteInvokeAsync");
        AssertHasTypeReference(containerSource, "ServiceProvider");
        AssertHasTypeReference(hostSource, "WinUIEditorCommandSink");
        AssertHasTypeReference(commandSinkSource, "RegisterActiveHost");
        AssertNoTypeReference(commandSinkSource, "return false;");
    }

    private static void AssertServiceImplementsPort(string servicesRoot, string fileName, string className, string portName)
    {
        var path = Path.Combine(servicesRoot, fileName);

        Assert.IsTrue(File.Exists(path), $"Expected WinUI service adapter {path}.");

        var source = File.ReadAllText(path);
        AssertHasTypeReference(source, className);
        AssertHasTypeReference(source, portName);
        AssertHasTypeReference(source, "Typedown.Presentation.Interfaces");
    }

    private static void AssertContainsInOrder(string source, params string[] snippets)
    {
        var currentIndex = -1;
        foreach (var snippet in snippets)
        {
            var nextIndex = source.IndexOf(snippet, currentIndex + 1, StringComparison.Ordinal);
            Assert.IsTrue(nextIndex >= 0, $"Expected to find snippet: {snippet}");
            Assert.IsTrue(nextIndex > currentIndex, $"Expected snippet to appear after the previous one: {snippet}");
            currentIndex = nextIndex;
        }
    }

    private static void AssertNoTypeReference(string source, string typeName)
    {
        Assert.IsFalse(Regex.IsMatch(source, CreateBoundaryPattern(typeName)), $"Unexpected reference to {typeName}.");
    }

    private static void AssertDoesNotContain(string source, string snippet)
    {
        Assert.IsFalse(source.Contains(snippet, StringComparison.Ordinal), $"Unexpected snippet: {snippet}");
    }

    private static void AssertHasTypeReference(string source, string typeName)
    {
        Assert.IsTrue(Regex.IsMatch(source, CreateBoundaryPattern(typeName)), $"Expected reference to {typeName}.");
    }

    private static string CreateBoundaryPattern(string typeName)
    {
        return $@"(?<![A-Za-z0-9_]){Regex.Escape(typeName)}(?![A-Za-z0-9_])";
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
