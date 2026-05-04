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
            "Config.SetAppDataPathProvider(platformServices.AppDataPathProvider);",
            "new ServiceCollection()",
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
    public void WinUIFileExport_PersistsConfigurationCrudWithoutConverterRegistration()
    {
        var appSource = File.ReadAllText(Path.Combine(RepoRoot, "Dev", "Typedown.WinUI", "App.xaml.cs"));
        var exportSource = File.ReadAllText(Path.Combine(RepoRoot, "Dev", "Typedown.WinUI", "Services", "WinUIFileExport.cs"));

        AssertHasTypeReference(exportSource, "AppDbContext");
        AssertHasTypeReference(exportSource, "IAppDataPathProvider");
        AssertHasTypeReference(exportSource, "AddExportConfig");
        AssertHasTypeReference(exportSource, "RemoveExportConfig");
        AssertHasTypeReference(exportSource, "SaveExportConfig");
        AssertHasTypeReference(exportSource, "GetExportConfig");
        AssertHasTypeReference(exportSource, "UpdateExportConfigs");
        AssertDoesNotContain(exportSource, "configuration is not wired");
        AssertNoTypeReference(exportSource, "IFileConverter");
        AssertNoTypeReference(appSource, "AddSingleton<IFileConverter");
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

    [TestMethod]
    public void WinUISettingsNavigation_PassesPresentationViewModelsIntoIncludedSettingPages()
    {
        var mainPageSource = File.ReadAllText(Path.Combine(RepoRoot, "Dev", "Typedown.WinUI", "Views", "MainPage.xaml.cs"));
        var settingsPageSource = File.ReadAllText(Path.Combine(RepoRoot, "Dev", "Typedown.WinUI", "Pages", "SettingsPage.xaml.cs"));
        var settingPagesRoot = Path.Combine(RepoRoot, "Dev", "Typedown.WinUI", "Pages", "SettingPages");

        AssertContainsInOrder(
            mainPageSource,
            "frame.Navigate(typeof(Pages.SettingsPage)",
            "new Pages.SettingsNavigationParameter(viewModel, viewModel.SettingsViewModel, pageName)");

        AssertHasTypeReference(settingsPageSource, "SettingsNavigationParameter");
        AssertHasTypeReference(settingsPageSource, "AppViewModel");
        AssertHasTypeReference(settingsPageSource, "SettingsViewModel");
        AssertContainsInOrder(settingsPageSource, "ViewModel = navigationParameter.AppViewModel;", "SettingsViewModel = navigationParameter.SettingsViewModel;");
        AssertContainsInOrder(settingsPageSource, "typeof(GeneralPage)", "typeof(ViewPage)", "typeof(EditorPage)", "return navigationParameter.WithPage(pageName, query);");
        AssertDoesNotContain(settingsPageSource, "LegacyCopied");

        foreach (var pageName in new[] { "GeneralPage", "ViewPage", "EditorPage" })
        {
            var pageSource = File.ReadAllText(Path.Combine(settingPagesRoot, $"{pageName}.xaml.cs"));

            AssertHasTypeReference(pageSource, "SettingsNavigationParameter");
            AssertHasTypeReference(pageSource, "AppViewModel");
            AssertHasTypeReference(pageSource, "SettingsViewModel");
            AssertContainsInOrder(pageSource, "ViewModel = parameter.AppViewModel;", "SettingsViewModel = parameter.SettingsViewModel;");
            AssertDoesNotContain(pageSource, "LegacyCopied");
        }
    }

    [TestMethod]
    public void WinUISettingsRoute_UsesPresentationLocaleForStableTitles()
    {
        var routeSource = File.ReadAllText(Path.Combine(RepoRoot, "Dev", "Typedown.WinUI", "Pages", "Route.cs"));
        var settingsPageXaml = File.ReadAllText(Path.Combine(RepoRoot, "Dev", "Typedown.WinUI", "Pages", "SettingsPage.xaml"));
        var settingsPageSource = File.ReadAllText(Path.Combine(RepoRoot, "Dev", "Typedown.WinUI", "Pages", "SettingsPage.xaml.cs"));

        AssertHasTypeReference(routeSource, "Typedown.Presentation.Utilities");
        AssertContainsInOrder(routeSource, "Locale.GetString(key, Locale.ResourceSource.SettingsResources)", "fallback");
        AssertHasTypeReference(routeSource, "General.Title");
        AssertHasTypeReference(routeSource, "View.Title");
        AssertHasTypeReference(routeSource, "Editor.Title");
        AssertDoesNotContain(routeSource, "LegacyCopied");

        AssertDoesNotContain(settingsPageXaml, "Content=\"General\"");
        AssertDoesNotContain(settingsPageXaml, "Content=\"View\"");
        AssertDoesNotContain(settingsPageXaml, "Content=\"Editor\"");
        AssertContainsInOrder(settingsPageSource, "ApplyNavigationLabels();", "item.Content = Route.GetSettingsPageTitle");
    }

    [TestMethod]
    public void WinUIFindReplace_IsWiredToPresentationViewModelsWithoutLegacyCopied()
    {
        var winUIRoot = Path.Combine(RepoRoot, "Dev", "Typedown.WinUI");
        var editorContainerXaml = File.ReadAllText(Path.Combine(winUIRoot, "Controls", "EditorControls", "EditorContainer.xaml"));
        var editorContainerSource = File.ReadAllText(Path.Combine(winUIRoot, "Controls", "EditorControls", "EditorContainer.xaml.cs"));
        var editorHostSource = File.ReadAllText(Path.Combine(winUIRoot, "Controls", "WinUIEditorHost.cs"));
        var sessionSource = File.ReadAllText(Path.Combine(winUIRoot, "Controls", "WinUIEditorDocumentSession.cs"));
        var findReplaceXaml = File.ReadAllText(Path.Combine(winUIRoot, "Controls", "FloatControls", "FindReplace.xaml"));
        var findReplaceSource = File.ReadAllText(Path.Combine(winUIRoot, "Controls", "FloatControls", "FindReplace.xaml.cs"));
        var floatViewModelSource = File.ReadAllText(Path.Combine(RepoRoot, "Dev", "Typedown.Presentation", "ViewModels", "FloatViewModel.cs"));

        foreach (var source in new[] { editorContainerXaml, editorContainerSource, findReplaceXaml, findReplaceSource })
        {
            AssertDoesNotContain(source, "LegacyCopied");
        }

        AssertHasTypeReference(editorContainerSource, "FloatViewModel");
        AssertHasTypeReference(editorContainerSource, "FindReplaceDialogOpen");
        AssertHasTypeReference(editorContainerSource, "PropertyChanged");
        AssertContainsInOrder(editorContainerSource, "FindReplaceDialogOpen", "UpdateFindReplaceState");
        AssertContainsInOrder(editorHostSource, "BuildFindShortcutScript", "OpenFindReplace");
        AssertHasTypeReference(sessionSource, "OpenFindReplace");
        AssertContainsInOrder(floatViewModelSource, "\"OpenFindReplace\"", "OnOpenFindReplace");
        AssertContainsInOrder(floatViewModelSource, "OnFindReplaceDialogOpenChange", "open == FindReplaceDialogState.None", "\"SearchOpenChange\"");

        AssertHasTypeReference(findReplaceSource, "FloatViewModel");
        AssertHasTypeReference(findReplaceSource, "EditorViewModel");
        AssertHasTypeReference(findReplaceSource, "EditorCommandSink");
        AssertContainsInOrder(findReplaceSource, "OnSearchTextChanged", "Editor.SearchValue");
        AssertContainsInOrder(findReplaceSource, "SendReplace", "EditorCommandSink", "\"Replace\"");
        AssertContainsInOrder(findReplaceSource, "searchTextBox.Focus", "searchTextBox.SelectAll");

        AssertHasTypeReference(findReplaceXaml, "FindReplace");
        AssertHasTypeReference(editorContainerXaml, "FindReplacePopup");
        AssertContainsInOrder(editorContainerSource, "EnsureFindReplaceDialog", "new FindReplace");
        AssertContainsInOrder(editorContainerSource, "FindReplacePopup.IsOpen", "VisualStateManager.GoToState");
    }

    [TestMethod]
    public void ActiveWinUIMenuBar_UsesPresentationViewModelsAndCommands()
    {
        var menuRoot = Path.Combine(RepoRoot, "Dev", "Typedown.WinUI", "Controls", "EditorControls");
        var menuBarSource = File.ReadAllText(Path.Combine(menuRoot, "MenuBar.xaml.cs"));
        var menuBarXaml = File.ReadAllText(Path.Combine(menuRoot, "MenuBar.xaml"));
        var menuStubSource = File.ReadAllText(Path.Combine(menuRoot, "MenuBarItems", "MenuBarItemStubs.cs"));

        AssertDoesNotContain(menuRoot, $"{Path.DirectorySeparatorChar}LegacyCopied{Path.DirectorySeparatorChar}");
        AssertHasTypeReference(menuBarXaml, "FileMenuItem");
        AssertHasTypeReference(menuBarXaml, "EditMenuItem");
        AssertHasTypeReference(menuBarXaml, "ParagraphMenuItem");
        AssertHasTypeReference(menuBarXaml, "FormatMenuItem");
        AssertHasTypeReference(menuBarXaml, "ViewMenuItem");
        AssertHasTypeReference(menuBarSource, "AppViewModel");
        AssertHasTypeReference(menuBarSource, "ApplyDataContextToMenuItems");
        AssertHasTypeReference(menuStubSource, "Typedown.Presentation.ViewModels");
        AssertHasTypeReference(menuStubSource, "EditorViewModel");
        AssertHasTypeReference(menuStubSource, "FileViewModel");
        AssertHasTypeReference(menuStubSource, "ParagraphViewModel");
        AssertHasTypeReference(menuStubSource, "FormatViewModel");
        AssertHasTypeReference(menuStubSource, "FloatViewModel");
        AssertHasTypeReference(menuStubSource, "SettingsViewModel");
        AssertHasTypeReference(menuStubSource, "UndoCommand");
        AssertHasTypeReference(menuStubSource, "RedoCommand");
        AssertHasTypeReference(menuStubSource, "CutCommand");
        AssertHasTypeReference(menuStubSource, "CopyCommand");
        AssertHasTypeReference(menuStubSource, "PasteCommand");
        AssertHasTypeReference(menuStubSource, "SelectAllCommand");
        AssertHasTypeReference(menuStubSource, "FindCommand");
        AssertHasTypeReference(menuStubSource, "UpdateParagraphCommand");
        AssertHasTypeReference(menuStubSource, "SetFormatCommand");
    }

    [TestMethod]
    public void ActiveWinUIMenuBar_DoesNotLeaveAllMenuHandlersAsEmptyStubs()
    {
        var menuStubPath = Path.Combine(
            RepoRoot,
            "Dev",
            "Typedown.WinUI",
            "Controls",
            "EditorControls",
            "MenuBarItems",
            "MenuBarItemStubs.cs");
        var menuStubSource = File.ReadAllText(menuStubPath);

        AssertDoesNotContain(menuStubPath, $"{Path.DirectorySeparatorChar}LegacyCopied{Path.DirectorySeparatorChar}");
        AssertNoTypeReference(menuStubSource, @"OnLoaded\(object sender, Microsoft\.UI\.Xaml\.RoutedEventArgs e\) \{ \}");
        AssertNoTypeReference(menuStubSource, @"OnUnloaded\(object sender, Microsoft\.UI\.Xaml\.RoutedEventArgs e\) \{ \}");
        AssertHasTypeReference(menuStubSource, "DisableUnsupportedActions");
        AssertHasTypeReference(menuStubSource, "IsEnabled = false");
        AssertHasTypeReference(menuStubSource, "Command =");
    }

    [TestMethod]
    public void WinUIKeyboardShortcuts_UseWinUIRoutingAndSkipClearedShortcuts()
    {
        var winUIRoot = Path.Combine(RepoRoot, "Dev", "Typedown.WinUI");
        var keyboardSource = File.ReadAllText(Path.Combine(winUIRoot, "Services", "WinUIKeyboardAccelerator.cs"));
        var rootControlSource = File.ReadAllText(Path.Combine(winUIRoot, "Controls", "RootControl.xaml.cs"));
        var appSource = File.ReadAllText(Path.Combine(winUIRoot, "App.xaml.cs"));
        var menuStubSource = File.ReadAllText(Path.Combine(
            winUIRoot,
            "Controls",
            "EditorControls",
            "MenuBarItems",
            "MenuBarItemStubs.cs"));

        foreach (var source in new[] { keyboardSource, rootControlSource, appSource, menuStubSource })
        {
            AssertDoesNotContain(source, "SetWindowsHookEx");
            AssertDoesNotContain(source, "WH_KEYBOARD_LL");
            AssertDoesNotContain(source, "LoadLibrary(\"User32\")");
        }

        AssertHasTypeReference(keyboardSource, "Microsoft.UI.Xaml.Input");
        AssertHasTypeReference(keyboardSource, "KeyDown");
        AssertHasTypeReference(keyboardSource, "Subject<KeyEventArgs>");
        AssertDoesNotContain(keyboardSource, "Observable.Empty<KeyEventArgs>()");
        AssertContainsInOrder(rootControlSource, "AttachKeyboardAccelerator", "WinUIKeyboardAccelerator", "Attach(this)");
        AssertContainsInOrder(appSource, "rootControl.AttachKeyboardAccelerator", "GetRequiredService<IKeyboardAccelerator>()");

        AssertHasTypeReference(menuStubSource, "KeyboardAccelerator");
        AssertHasTypeReference(menuStubSource, "KeyboardAcceleratorTextOverride");
        AssertContainsInOrder(menuStubSource, "HasShortcutKey", "shortcut is not null", "shortcut.Key != KeyboardKey.None");
        AssertContainsInOrder(menuStubSource, "SetShortcut(MenuFlyoutItem", "if (!HasShortcutKey(shortcut))", "return;", "KeyboardAcceleratorTextOverride");
        AssertContainsInOrder(menuStubSource, "SetShortcut(ToggleMenuFlyoutItem", "if (!HasShortcutKey(shortcut))", "return;", "KeyboardAcceleratorTextOverride");
        AssertContainsInOrder(menuStubSource, "SetShortcut(FindItem", "settings?.ShortcutFind");
        AssertContainsInOrder(menuStubSource, "SetShortcut(StrongItem", "settings?.ShortcutStrong");

        AssertContainsInOrder(keyboardSource, "Register(ShortcutKey key", "key.Key == KeyboardKey.None", "return Disposable.Empty");
    }

    [TestMethod]
    public void WinUIImageFloatControls_AreCopiedAndAdaptedToPresentationPorts()
    {
        var winUIRoot = Path.Combine(RepoRoot, "Dev", "Typedown.WinUI");
        var floatControlsRoot = Path.Combine(winUIRoot, "Controls", "FloatControls");
        var imageSelectorXaml = File.ReadAllText(Path.Combine(floatControlsRoot, "ImageSelector.xaml"));
        var imageSelectorSource = File.ReadAllText(Path.Combine(floatControlsRoot, "ImageSelector.xaml.cs"));
        var imageToolbarXaml = File.ReadAllText(Path.Combine(floatControlsRoot, "ImageToolbar.xaml"));
        var imageToolbarSource = File.ReadAllText(Path.Combine(floatControlsRoot, "ImageToolbar.xaml.cs"));
        var floatServiceSource = File.ReadAllText(Path.Combine(winUIRoot, "Services", "WinUIFloatViewService.cs"));

        AssertHasTypeReference(imageSelectorXaml, "Typedown.WinUI.Controls.ImageSelector");
        AssertHasTypeReference(imageToolbarXaml, "Typedown.WinUI.Controls.ImageToolbar");
        AssertHasTypeReference(imageSelectorSource, "IEditorCommandSink");
        AssertHasTypeReference(imageSelectorSource, "IFilePickerService");
        AssertHasTypeReference(imageSelectorSource, "ImageAction");
        AssertContainsInOrder(imageSelectorSource, "\"ReplaceImage\"", "new { src, alt, title }");
        AssertHasTypeReference(imageSelectorSource, "anchor?.XamlRoot");
        AssertHasTypeReference(imageToolbarSource, "IEditorCommandSink");
        AssertHasTypeReference(imageToolbarSource, "IKeyboardAccelerator");
        AssertContainsInOrder(imageToolbarSource, "\"ImageEditToolbarClick\"", "Hide();");
        AssertContainsInOrder(imageToolbarSource, "disposables?.Dispose();", "new CompositeDisposable()");
        AssertHasTypeReference(floatServiceSource, "new ImageSelector");
        AssertHasTypeReference(floatServiceSource, "new ImageToolbar");
        AssertDoesNotContain(floatServiceSource, "image selector float view is not wired");
        AssertDoesNotContain(floatServiceSource, "image toolbar float view is not wired");
        AssertDoesNotContain(imageSelectorSource, "IMarkdownEditor");
        AssertDoesNotContain(imageToolbarSource, "IMarkdownEditor");
        AssertDoesNotContain(imageSelectorSource, "Windows.UI.Xaml");
        AssertDoesNotContain(imageToolbarSource, "Windows.UI.Xaml");
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
