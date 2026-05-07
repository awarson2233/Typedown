using Microsoft.VisualStudio.TestTools.UnitTesting;
using Typedown.Core.Enums;

namespace Typedown.ArchitectureTests;

[TestClass]
public class Phase13SettingsContractTests
{
    private static readonly string RepoRoot = FindRepoRoot();

    [TestMethod]
    public void CoreEnums_RemainPureCoreTypes()
    {
        AssertEnumMembers<ImageUploadMethod>(("None", 0), ("FTP", 1), ("Git", 2), ("OSS", 3), ("SCP", 4), ("PowerShell", 1024));
        AssertEnumMembers<InsertImageAction>(("None", 0), ("CopyToPath", 1), ("Upload", 2));
        AssertEnumMembers<ExportType>(("None", 0), ("PDF", 1), ("HTML", 2), ("Image", 3));
        AssertEnumMembers<PrintOrientation>(("Portrait", 0), ("Landscape", 1));
        AssertEnumMembers<FileStartupAction>(("None", 0), ("OpenLast", 1));
        AssertEnumMembers<FolderStartupAction>(("None", 0), ("OpenLast", 1), ("OpenFolder", 2), ("FollowOpenedFileFolder", 3));
        AssertEnumMembers<AppTheme>(("Default", 0), ("Light", 1), ("Dark", 2));
    }

    [TestMethod]
    public void PresentationSettingsViewModel_DoesNotReferenceWinUIShell()
    {
        var source = File.ReadAllText(Path.Combine(RepoRoot, "Dev", "Typedown.Presentation", "ViewModels", "SettingsViewModel.cs"));

        AssertNoTypeReference(source, "Typedown.WinUI");
        AssertNoTypeReference(source, "Microsoft.UI.Xaml");
        AssertNoTypeReference(source, "Windows.UI.Xaml");
    }

    [TestMethod]
    public void WinUISettingsPages_AreRoutedAndIncludedInProject()
    {
        var winUIRoot = Path.Combine(RepoRoot, "Dev", "Typedown.WinUI");
        var routeSource = File.ReadAllText(Path.Combine(winUIRoot, "Pages", "Route.cs"));
        var settingsPageXaml = File.ReadAllText(Path.Combine(winUIRoot, "Pages", "SettingsPage.xaml"));
        var projectSource = File.ReadAllText(Path.Combine(winUIRoot, "Typedown.WinUI.csproj"));
        var settingItemExtensionsSource = File.ReadAllText(Path.Combine(winUIRoot, "Controls", "SettingControls", "SettingItemExtensions.cs"));

        foreach (var pageName in new[] { "ExportConfig", "ImageUpload", "Shortcut", "UploadConfig" })
        {
            AssertHasTypeReference(routeSource, $"\"{pageName}\" => typeof({pageName}Page)");
        }

        foreach (var pageName in new[] { "General", "View", "Editor", "Image", "Export", "About" })
        {
            AssertHasTypeReference(settingsPageXaml, $"Tag=\"{pageName}\"");
        }

        var settingsPageCodeBehind = File.ReadAllText(Path.Combine(winUIRoot, "Pages", "SettingsPage.xaml.cs"));
        AssertHasTypeReference(settingsPageCodeBehind, "pageType == typeof(ImageUploadPage) || pageType == typeof(UploadConfigPage)");
        AssertHasTypeReference(settingsPageCodeBehind, "if (pageType == typeof(ShortcutPage))");

        foreach (var relativePath in new[]
        {
            @"Pages\SettingPages\ExportConfigPage.xaml",
            @"Pages\SettingPages\ExportConfigPage.xaml.cs",
            @"Pages\SettingPages\ImageUploadPage.xaml",
            @"Pages\SettingPages\ImageUploadPage.xaml.cs",
            @"Pages\SettingPages\ShortcutPage.xaml",
            @"Pages\SettingPages\ShortcutPage.xaml.cs",
            @"Pages\SettingPages\UploadConfigPage.xaml",
            @"Pages\SettingPages\UploadConfigPage.xaml.cs",
            @"Controls\SettingControls\SettingItems\**\*.xaml",
            @"Controls\SettingControls\SettingItems\**\*.cs"
        })
        {
            AssertNoTypeReference(projectSource, $@"Remove=""{relativePath}""");
            AssertNoTypeReference(projectSource, $@"Include=""{relativePath}""");
        }

        AssertNoTypeReference(settingItemExtensionsSource, "public class EnumNameBlock");
        AssertNoTypeReference(settingItemExtensionsSource, "public sealed partial class UnitNumberBox");
        AssertNoTypeReference(settingItemExtensionsSource, "public class PathPickerButton");
        AssertNoTypeReference(settingItemExtensionsSource, "public sealed partial class ShortcutPickerButton");

        foreach (var relativePath in new[]
        {
            @"Controls\SettingControls\CommonControls\EnumNameBlock.cs",
            @"Controls\SettingControls\CommonControls\PathPickerButton.cs",
            @"Controls\SettingControls\CommonControls\ShortcutPicker.xaml",
            @"Controls\SettingControls\CommonControls\ShortcutPicker.xaml.cs",
            @"Controls\SettingControls\CommonControls\ShortcutPickerButton.xaml",
            @"Controls\SettingControls\CommonControls\ShortcutPickerButton.xaml.cs",
            @"Controls\SettingControls\CommonControls\UnitNumberBox.xaml",
            @"Controls\SettingControls\CommonControls\UnitNumberBox.xaml.cs"
        })
        {
            Assert.IsTrue(File.Exists(Path.Combine(winUIRoot, relativePath)), $"Expected copied WinUI setting control: {relativePath}");
        }

        var shortcutPickerButtonSource = File.ReadAllText(Path.Combine(winUIRoot, "Controls", "SettingControls", "CommonControls", "ShortcutPickerButton.xaml.cs"));
        var shortcutPickerSource = File.ReadAllText(Path.Combine(winUIRoot, "Controls", "SettingControls", "CommonControls", "ShortcutPicker.xaml.cs"));
        AssertHasTypeReference(shortcutPickerButtonSource, "var picker = new ShortcutPicker(ShortcutKey)");
        AssertHasTypeReference(shortcutPickerSource, "private void OnKeyDown(object sender, KeyRoutedEventArgs args)");
    }

    [TestMethod]
    public void SpellcheckSetting_IsExposedThroughWinUIPageAndPresentationEditorSettingsContract()
    {
        var editorPageXaml = File.ReadAllText(Path.Combine(RepoRoot, "Dev", "Typedown.WinUI", "Pages", "SettingPages", "EditorPage.xaml"));
        var settingsSource = File.ReadAllText(Path.Combine(RepoRoot, "Dev", "Typedown.Presentation", "ViewModels", "SettingsViewModel.cs"));

        AssertHasTypeReference(editorPageXaml, "<toolkit:SettingsExpander Header=\"{u:LocaleString Key=Editor.SpellcheckEnabled.Title}\" Description=\"{u:LocaleString Key=Editor.SpellcheckEnabled.Description}\">");
        AssertHasTypeReference(editorPageXaml, "<ToggleSwitch IsOn=\"{x:Bind Settings.SpellcheckEnabled, Mode=TwoWay}\"/>");
        AssertHasTypeReference(settingsSource, "[nameof(SpellcheckEnabled)] = \"spellcheckEnabled\"");
        AssertHasTypeReference(settingsSource, "[editorSettingNameMap[nameof(SpellcheckEnabled)]] = SpellcheckEnabled");
    }

    private static void AssertEnumMembers<TEnum>(params (string Name, int Value)[] expectedMembers)
        where TEnum : struct, Enum
    {
        var actualMembers = Enum
            .GetValues<TEnum>()
            .Select(value => (value.ToString(), Convert.ToInt32(value)))
            .ToArray();

        CollectionAssert.AreEqual(
            expectedMembers.Select(member => $"{member.Name}:{member.Value}").ToArray(),
            actualMembers.Select(member => $"{member.Item1}:{member.Item2}").ToArray(),
            $"Unexpected enum shape for {typeof(TEnum).Name}.");
    }

    private static void AssertNoTypeReference(string source, string text)
    {
        Assert.IsFalse(source.Contains(text, StringComparison.Ordinal), $"Unexpected reference: {text}");
    }

    private static void AssertHasTypeReference(string source, string text)
    {
        Assert.IsTrue(source.Contains(text, StringComparison.Ordinal), $"Expected reference: {text}");
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
