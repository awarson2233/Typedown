using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Typedown.Core.Contracts.Settings;

namespace Typedown.ArchitectureTests;

[TestClass]
public class Phase13SettingsContractTests
{
    private static readonly string RepoRoot = FindRepoRoot();

    [TestMethod]
    public void SettingsContracts_StayPlatformNeutralAndAvoidLegacyDependencies()
    {
        var settingsFiles = Directory
            .EnumerateFiles(Path.Combine(RepoRoot, "Dev", "Typedown.Core.Contracts", "Settings"), "*.cs", SearchOption.TopDirectoryOnly)
            .ToArray();

        Assert.IsTrue(settingsFiles.Length >= 3, "Expected Phase 13 settings and shortcut contracts.");

        foreach (var file in settingsFiles)
        {
            var source = File.ReadAllText(file);

            AssertNoTypeReference(source, "Windows.System.VirtualKey");
            AssertNoTypeReference(source, "Windows.UI.Xaml");
            AssertNoTypeReference(source, "Microsoft.UI.Xaml");
            AssertNoTypeReference(source, "Typedown.WinUI");
            AssertNoTypeReference(source, "Typedown.XamlUI");
            AssertNoTypeReference(source, "Typedown.Core.ViewModels");
            AssertNoTypeReference(source, "Typedown.Core.Models");
            AssertNoTypeReference(source, "Config.");
            AssertNoTypeReference(source, "File.");
        }
    }

    [TestMethod]
    public void ShortcutDto_ExpressesLegacyModifiersAndStableNumericKeyCodes()
    {
        var shortcut = new EditorShortcutKey(
            EditorShortcutModifierFlags.Control
                | EditorShortcutModifierFlags.Shift
                | EditorShortcutModifierFlags.Alt
                | EditorShortcutModifierFlags.Windows,
            78);

        Assert.AreEqual(0, (int)EditorShortcutModifierFlags.None);
        Assert.AreEqual(1, (int)EditorShortcutModifierFlags.Control);
        Assert.AreEqual(2, (int)EditorShortcutModifierFlags.Alt);
        Assert.AreEqual(4, (int)EditorShortcutModifierFlags.Shift);
        Assert.AreEqual(8, (int)EditorShortcutModifierFlags.Windows);
        Assert.IsTrue(shortcut.Modifiers.HasFlag(EditorShortcutModifierFlags.Control));
        Assert.IsTrue(shortcut.Modifiers.HasFlag(EditorShortcutModifierFlags.Shift));
        Assert.IsTrue(shortcut.Modifiers.HasFlag(EditorShortcutModifierFlags.Alt));
        Assert.IsTrue(shortcut.Modifiers.HasFlag(EditorShortcutModifierFlags.Windows));
        Assert.AreEqual(78, shortcut.VirtualKeyCode);

        var keyType = typeof(EditorShortcutKey).GetProperty(nameof(EditorShortcutKey.VirtualKeyCode))?.PropertyType;
        Assert.AreEqual(typeof(int), keyType);
    }

    [TestMethod]
    public void DefaultShortcuts_MatchLegacySettingsViewModelSnapshot()
    {
        var shortcuts = TypedownDefaultShortcuts.All;

        AssertShortcut(shortcuts, "NewFile", EditorShortcutModifierFlags.Control, 78);
        AssertShortcut(shortcuts, "NewWindow", EditorShortcutModifierFlags.Control | EditorShortcutModifierFlags.Shift, 78);
        AssertShortcut(shortcuts, "OpenFile", EditorShortcutModifierFlags.Control, 79);
        AssertShortcut(shortcuts, "Save", EditorShortcutModifierFlags.Control, 83);
        AssertShortcut(shortcuts, "SaveAs", EditorShortcutModifierFlags.Control | EditorShortcutModifierFlags.Shift, 83);
        AssertShortcut(shortcuts, "Print", EditorShortcutModifierFlags.Alt | EditorShortcutModifierFlags.Shift, 80);
        AssertShortcut(shortcuts, "Close", EditorShortcutModifierFlags.Control, 87);
        AssertShortcut(shortcuts, "Undo", EditorShortcutModifierFlags.Control, 90);
        AssertShortcut(shortcuts, "Redo", EditorShortcutModifierFlags.Control, 89);
        AssertShortcut(shortcuts, "Cut", EditorShortcutModifierFlags.Control, 88);
        AssertShortcut(shortcuts, "Copy", EditorShortcutModifierFlags.Control, 67);
        AssertShortcut(shortcuts, "Paste", EditorShortcutModifierFlags.Control, 86);
        AssertShortcut(shortcuts, "SelectAll", EditorShortcutModifierFlags.Control, 65);
        AssertShortcut(shortcuts, "Find", EditorShortcutModifierFlags.Control, 70);
        AssertShortcut(shortcuts, "Replace", EditorShortcutModifierFlags.Control, 72);
        AssertShortcut(shortcuts, "Heading1", EditorShortcutModifierFlags.Control, 49);
        AssertShortcut(shortcuts, "Paragraph", EditorShortcutModifierFlags.Control, 48);
        AssertShortcut(shortcuts, "Table", EditorShortcutModifierFlags.Control | EditorShortcutModifierFlags.Shift, 84);
        AssertShortcut(shortcuts, "Strong", EditorShortcutModifierFlags.Control, 66);
        AssertShortcut(shortcuts, "Emphasis", EditorShortcutModifierFlags.Control, 73);
        AssertShortcut(shortcuts, "Underline", EditorShortcutModifierFlags.Control, 85);
        AssertShortcut(shortcuts, "Hyperlink", EditorShortcutModifierFlags.Control, 75);
        AssertShortcut(shortcuts, "SidePane", EditorShortcutModifierFlags.Control | EditorShortcutModifierFlags.Shift, 76);
        AssertShortcut(shortcuts, "FocusMode", EditorShortcutModifierFlags.None, 119);
        AssertShortcut(shortcuts, "TypewriterMode", EditorShortcutModifierFlags.None, 120);
    }

    [TestMethod]
    public void ShortcutModifierFlags_PreserveLegacyPersistenceBitValues()
    {
        var shortcuts = TypedownDefaultShortcuts.All;

        Assert.AreEqual(1, (int)shortcuts["Save"].Modifiers, "Legacy VirtualKeyModifiers.Control is bit 1.");
        Assert.AreEqual(2 | 4, (int)shortcuts["Print"].Modifiers, "Legacy VirtualKeyModifiers.Menu/Alt plus Shift is 6.");
    }

    [TestMethod]
    public void SettingsUiSnapshot_CoversPureSettingsWithLegacyJsonNames()
    {
        var snapshot = new SettingsUiSnapshot
        {
            SidePaneOpen = true,
            SidePaneWidth = 320,
            StatusBarOpen = false,
            SourceCode = true,
            Typewriter = true,
            FocusMode = true,
            FontSize = 18,
            LineHeight = 1.8,
            EditorWidth = "1200px",
            Language = "en-US",
            Spellcheck = true,
            SpellcheckLanguage = "en-US",
            InsertClipboardImageAction = 1,
            InsertClipboardImageCopyPath = "./clipboard",
            InsertClipboardImageUseUploadConfigId = 7,
            InsertLocalImageAction = 2,
            InsertLocalImageCopyPath = "./local",
            InsertLocalImageUseUploadConfigId = 8,
            InsertWebImageAction = 3,
            InsertWebImageCopyPath = "./web",
            InsertWebImageUseUploadConfigId = 9,
        };

        var json = JsonSerializer.Serialize(snapshot);

        StringAssert.Contains(json, "\"sidePaneOpen\"");
        StringAssert.Contains(json, "\"sidePaneWidth\"");
        StringAssert.Contains(json, "\"statusBarOpen\"");
        StringAssert.Contains(json, "\"sourceCode\"");
        StringAssert.Contains(json, "\"typewriter\"");
        StringAssert.Contains(json, "\"focusMode\"");
        StringAssert.Contains(json, "\"fontSize\"");
        StringAssert.Contains(json, "\"lineHeight\"");
        StringAssert.Contains(json, "\"editorAreaWidth\"");
        StringAssert.Contains(json, "\"language\"");
        StringAssert.Contains(json, "\"spellcheckEnabled\"");
        StringAssert.Contains(json, "\"spellcheckLang\"");
        StringAssert.Contains(json, "\"insertClipboardImageAction\"");
        StringAssert.Contains(json, "\"insertClipboardImageCopyPath\"");
        StringAssert.Contains(json, "\"insertClipboardImageUseUploadConfigId\"");
        StringAssert.Contains(json, "\"insertLocalImageAction\"");
        StringAssert.Contains(json, "\"insertLocalImageCopyPath\"");
        StringAssert.Contains(json, "\"insertLocalImageUseUploadConfigId\"");
        StringAssert.Contains(json, "\"insertWebImageAction\"");
        StringAssert.Contains(json, "\"insertWebImageCopyPath\"");
        StringAssert.Contains(json, "\"insertWebImageUseUploadConfigId\"");
        Assert.IsFalse(json.Contains("\"fontFamily\"", StringComparison.Ordinal));
        Assert.IsFalse(json.Contains("\"imageInsertStrategy\"", StringComparison.Ordinal));
        Assert.IsFalse(json.Contains("\"SidePaneOpen\"", StringComparison.Ordinal));
        Assert.IsFalse(json.Contains("\"EditorWidth\"", StringComparison.Ordinal));
        Assert.IsFalse(json.Contains("\"Spellcheck\"", StringComparison.Ordinal));
    }

    [TestMethod]
    public void SettingsUiSnapshot_UsesExplicitJsonPropertyNames()
    {
        var properties = typeof(SettingsUiSnapshot)
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .ToArray();

        Assert.IsTrue(properties.Length >= 13, "Expected the Phase 13 settings UI snapshot fields.");

        foreach (var property in properties)
        {
            Assert.IsNotNull(
                property.GetCustomAttribute<JsonPropertyNameAttribute>(),
                $"Expected {property.Name} to lock its JSON field with JsonPropertyName.");
        }
    }

    private static void AssertShortcut(
        IReadOnlyDictionary<string, EditorShortcutKey> shortcuts,
        string name,
        EditorShortcutModifierFlags modifiers,
        int virtualKeyCode)
    {
        Assert.IsTrue(shortcuts.TryGetValue(name, out var shortcut), $"Missing shortcut: {name}");
        Assert.AreEqual(modifiers, shortcut.Modifiers, $"Unexpected modifiers for {name}.");
        Assert.AreEqual(virtualKeyCode, shortcut.VirtualKeyCode, $"Unexpected key code for {name}.");
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
