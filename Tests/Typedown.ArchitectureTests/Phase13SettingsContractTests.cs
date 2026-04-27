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
            .EnumerateFiles(Path.Combine(RepoRoot, "Dev", "Typedown.Core", "Settings"), "*.cs", SearchOption.TopDirectoryOnly)
            .ToArray();

        Assert.IsTrue(settingsFiles.Length >= 3, "Expected Phase 13 settings and shortcut contracts.");

        foreach (var file in settingsFiles)
        {
            var source = File.ReadAllText(file);

            AssertNoTypeReference(source, "Windows.System.VirtualKey");
            AssertNoTypeReference(source, "Windows.UI.Xaml");
            AssertNoTypeReference(source, "Microsoft.UI.Xaml");
            AssertNoTypeReference(source, "Microsoft.Web.WebView2");
            AssertNoTypeReference(source, "Windows.Storage.Pickers");
            AssertNoTypeReference(source, "Typedown.WinUI");
            AssertNoTypeReference(source, "Typedown.XamlUI");
            AssertNoTypeReference(source, "Typedown.Core.Legacy");
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

        Assert.AreEqual(69, shortcuts.Count, "Shortcut snapshot must cover every legacy SettingsViewModel.Shortcut property.");
        AssertShortcut(shortcuts, "NewFile", EditorShortcutModifierFlags.Control, 78);
        AssertShortcut(shortcuts, "NewWindow", EditorShortcutModifierFlags.Control | EditorShortcutModifierFlags.Shift, 78);
        AssertShortcut(shortcuts, "OpenFile", EditorShortcutModifierFlags.Control, 79);
        AssertNoShortcut(shortcuts, "OpenFolder");
        AssertNoShortcut(shortcuts, "ClearRecentFiles");
        AssertShortcut(shortcuts, "Save", EditorShortcutModifierFlags.Control, 83);
        AssertShortcut(shortcuts, "SaveAs", EditorShortcutModifierFlags.Control | EditorShortcutModifierFlags.Shift, 83);
        AssertNoShortcut(shortcuts, "ExportSettings");
        AssertShortcut(shortcuts, "Print", EditorShortcutModifierFlags.Alt | EditorShortcutModifierFlags.Shift, 80);
        AssertShortcut(shortcuts, "Settings", EditorShortcutModifierFlags.Control, 188);
        AssertShortcut(shortcuts, "Close", EditorShortcutModifierFlags.Control, 87);
        AssertShortcut(shortcuts, "Undo", EditorShortcutModifierFlags.Control, 90);
        AssertShortcut(shortcuts, "Redo", EditorShortcutModifierFlags.Control, 89);
        AssertShortcut(shortcuts, "Cut", EditorShortcutModifierFlags.Control, 88);
        AssertShortcut(shortcuts, "Copy", EditorShortcutModifierFlags.Control, 67);
        AssertShortcut(shortcuts, "Paste", EditorShortcutModifierFlags.Control, 86);
        AssertNoShortcut(shortcuts, "CopyAsPlainText");
        AssertShortcut(shortcuts, "CopyAsMarkdown", EditorShortcutModifierFlags.Control | EditorShortcutModifierFlags.Shift, 67);
        AssertNoShortcut(shortcuts, "CopyAsHTMLCode");
        AssertShortcut(shortcuts, "PasteAsPlainText", EditorShortcutModifierFlags.Control | EditorShortcutModifierFlags.Shift, 86);
        AssertShortcut(shortcuts, "Delete", EditorShortcutModifierFlags.None, 46);
        AssertShortcut(shortcuts, "SelectAll", EditorShortcutModifierFlags.Control, 65);
        AssertShortcut(shortcuts, "Find", EditorShortcutModifierFlags.Control, 70);
        AssertShortcut(shortcuts, "FindNext", EditorShortcutModifierFlags.None, 114);
        AssertShortcut(shortcuts, "FindPrevious", EditorShortcutModifierFlags.Shift, 114);
        AssertShortcut(shortcuts, "Replace", EditorShortcutModifierFlags.Control, 72);
        AssertShortcut(shortcuts, "Heading1", EditorShortcutModifierFlags.Control, 49);
        AssertShortcut(shortcuts, "Heading2", EditorShortcutModifierFlags.Control, 50);
        AssertShortcut(shortcuts, "Heading3", EditorShortcutModifierFlags.Control, 51);
        AssertShortcut(shortcuts, "Heading4", EditorShortcutModifierFlags.Control, 52);
        AssertShortcut(shortcuts, "Heading5", EditorShortcutModifierFlags.Control, 53);
        AssertShortcut(shortcuts, "Heading6", EditorShortcutModifierFlags.Control, 54);
        AssertShortcut(shortcuts, "Paragraph", EditorShortcutModifierFlags.Control, 48);
        AssertShortcut(shortcuts, "IncreaseHeadingLevel", EditorShortcutModifierFlags.Control, 187);
        AssertShortcut(shortcuts, "DecreaseHeadingLevel", EditorShortcutModifierFlags.Control, 189);
        AssertShortcut(shortcuts, "Table", EditorShortcutModifierFlags.Control | EditorShortcutModifierFlags.Shift, 84);
        AssertShortcut(shortcuts, "CodeFences", EditorShortcutModifierFlags.Control | EditorShortcutModifierFlags.Shift, 75);
        AssertShortcut(shortcuts, "MathBlock", EditorShortcutModifierFlags.Control | EditorShortcutModifierFlags.Shift, 77);
        AssertShortcut(shortcuts, "Quote", EditorShortcutModifierFlags.Control | EditorShortcutModifierFlags.Shift, 81);
        AssertShortcut(shortcuts, "OrderedList", EditorShortcutModifierFlags.Control | EditorShortcutModifierFlags.Shift, 219);
        AssertShortcut(shortcuts, "UnorderedList", EditorShortcutModifierFlags.Control | EditorShortcutModifierFlags.Shift, 221);
        AssertShortcut(shortcuts, "TaskList", EditorShortcutModifierFlags.Control | EditorShortcutModifierFlags.Shift, 88);
        AssertShortcut(shortcuts, "InsertParagraphBefore", EditorShortcutModifierFlags.Control | EditorShortcutModifierFlags.Shift, 13);
        AssertShortcut(shortcuts, "InsertParagraphAfter", EditorShortcutModifierFlags.Control, 13);
        AssertNoShortcut(shortcuts, "VegaChart");
        AssertNoShortcut(shortcuts, "FlowChart");
        AssertNoShortcut(shortcuts, "SequenceDiagram");
        AssertNoShortcut(shortcuts, "PlantUMLDiagram");
        AssertNoShortcut(shortcuts, "Mermaid");
        AssertNoShortcut(shortcuts, "LinkReferences");
        AssertNoShortcut(shortcuts, "FootNote");
        AssertNoShortcut(shortcuts, "HorizontalLine");
        AssertNoShortcut(shortcuts, "Toc");
        AssertNoShortcut(shortcuts, "YAMLFrontMatter");
        AssertShortcut(shortcuts, "Strong", EditorShortcutModifierFlags.Control, 66);
        AssertShortcut(shortcuts, "Emphasis", EditorShortcutModifierFlags.Control, 73);
        AssertShortcut(shortcuts, "Underline", EditorShortcutModifierFlags.Control, 85);
        AssertShortcut(shortcuts, "InlineCode", EditorShortcutModifierFlags.Control | EditorShortcutModifierFlags.Shift, 192);
        AssertNoShortcut(shortcuts, "InlineMath");
        AssertShortcut(shortcuts, "Strikethrough", EditorShortcutModifierFlags.Alt | EditorShortcutModifierFlags.Shift, 53);
        AssertNoShortcut(shortcuts, "Highlight");
        AssertShortcut(shortcuts, "Hyperlink", EditorShortcutModifierFlags.Control, 75);
        AssertShortcut(shortcuts, "Image", EditorShortcutModifierFlags.Control | EditorShortcutModifierFlags.Shift, 73);
        AssertShortcut(shortcuts, "ClearFormat", EditorShortcutModifierFlags.Control, 220);
        AssertShortcut(shortcuts, "SidePane", EditorShortcutModifierFlags.Control | EditorShortcutModifierFlags.Shift, 76);
        AssertShortcut(shortcuts, "SourceCodeMode", EditorShortcutModifierFlags.Control, 191);
        AssertShortcut(shortcuts, "FocusMode", EditorShortcutModifierFlags.None, 119);
        AssertShortcut(shortcuts, "TypewriterMode", EditorShortcutModifierFlags.None, 120);
        AssertNoShortcut(shortcuts, "StatusBar");
    }

    [TestMethod]
    public void ShortcutModifierFlags_PreserveLegacyPersistenceBitValues()
    {
        var shortcuts = TypedownDefaultShortcuts.All;

        Assert.AreEqual(1, (int)shortcuts["Save"]!.Modifiers, "Legacy VirtualKeyModifiers.Control is bit 1.");
        Assert.AreEqual(2 | 4, (int)shortcuts["Print"]!.Modifiers, "Legacy VirtualKeyModifiers.Menu/Alt plus Shift is 6.");
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
        IReadOnlyDictionary<string, EditorShortcutKey?> shortcuts,
        string name,
        EditorShortcutModifierFlags modifiers,
        int virtualKeyCode)
    {
        Assert.IsTrue(shortcuts.TryGetValue(name, out var shortcut), $"Missing shortcut: {name}");
        Assert.IsNotNull(shortcut, $"Expected {name} to have a legacy default shortcut.");
        Assert.AreEqual(modifiers, shortcut.Modifiers, $"Unexpected modifiers for {name}.");
        Assert.AreEqual(virtualKeyCode, shortcut.VirtualKeyCode, $"Unexpected key code for {name}.");
    }

    private static void AssertNoShortcut(IReadOnlyDictionary<string, EditorShortcutKey?> shortcuts, string name)
    {
        Assert.IsTrue(shortcuts.TryGetValue(name, out var shortcut), $"Missing shortcut: {name}");
        Assert.IsNull(shortcut, $"Expected {name} to preserve the legacy null shortcut default.");
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
