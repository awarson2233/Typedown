using System.Reflection;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Typedown.Core.Contracts.Editor;

namespace Typedown.ArchitectureTests;

[TestClass]
public class Phase13EditorCommandContractTests
{
    private static readonly string RepoRoot = FindRepoRoot();

    [TestMethod]
    public void EditorCommandContracts_StayPlatformNeutralAndAvoidUiFrameworkTypes()
    {
        var editorFiles = Directory
            .EnumerateFiles(Path.Combine(RepoRoot, "Dev", "Typedown.Core.Contracts", "Editor"), "*.cs", SearchOption.TopDirectoryOnly)
            .ToArray();

        Assert.IsTrue(editorFiles.Length >= 18, "Expected Phase 13 editor command contracts.");

        foreach (var file in editorFiles)
        {
            var source = File.ReadAllText(file);

            AssertNoTypeReference(source, "Windows.UI.Xaml");
            AssertNoTypeReference(source, "Microsoft.UI.Xaml");
            AssertNoTypeReference(source, "Typedown.WinUI");
            AssertNoTypeReference(source, "Typedown.XamlUI");
            AssertNoTypeReference(source, "ICommand");
            AssertNoTypeReference(source, "FrameworkElement");
            AssertNoTypeReference(source, "WebView2");
            AssertNoTypeReference(source, "Newtonsoft");
            AssertNoTypeReference(source, "JObject");
        }
    }

    [TestMethod]
    public void EditorCommandNames_PreserveLegacyPostMessageNames()
    {
        var names = typeof(EditorUiCommandNames)
            .GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy)
            .Where(field => field.IsLiteral && !field.IsInitOnly && field.FieldType == typeof(string))
            .ToDictionary(field => field.Name, field => (string)field.GetRawConstantValue()!);

        CollectionAssert.AreEquivalent(
            new[]
            {
                "Undo",
                "Redo",
                "Cut",
                "Copy",
                "Paste",
                "Delete",
                "SelectAll",
                "Find",
                "Format",
                "UpdateParagraph",
                "InsertParagraph",
                "DeleteParagraph",
                "Duplicate",
                "InsertTable",
                "SearchOpenChange",
                "ScrollTo",
            },
            names.Keys.ToArray());

        Assert.AreEqual("Undo", names["Undo"]);
        Assert.AreEqual("Redo", names["Redo"]);
        Assert.AreEqual("Cut", names["Cut"]);
        Assert.AreEqual("Copy", names["Copy"]);
        Assert.AreEqual("Paste", names["Paste"]);
        Assert.AreEqual("DeleteSelection", names["Delete"]);
        Assert.AreEqual("SelectAll", names["SelectAll"]);
        Assert.AreEqual("Find", names["Find"]);
        Assert.AreEqual("Format", names["Format"]);
        Assert.AreEqual("UpdateParagraph", names["UpdateParagraph"]);
        Assert.AreEqual("InsertParagraph", names["InsertParagraph"]);
        Assert.AreEqual("DeleteParagraph", names["DeleteParagraph"]);
        Assert.AreEqual("Duplicate", names["Duplicate"]);
        Assert.AreEqual("InsertTable", names["InsertTable"]);
        Assert.AreEqual("SearchOpenChange", names["SearchOpenChange"]);
        Assert.AreEqual("ScrollTo", names["ScrollTo"]);
    }

    [TestMethod]
    public void EditorCommandFactories_UseLegacyNamesAndPayloadShapes()
    {
        AssertCommand(EditorUiCommands.CreateUndo(), "Undo", "null");
        AssertCommand(EditorUiCommands.CreateRedo(), "Redo", "null");
        AssertCommand(EditorUiCommands.CreateCut("text/html"), "Cut", """{"type":"text/html"}""");
        AssertCommand(EditorUiCommands.CreateCopy("text/plain"), "Copy", """{"type":"text/plain"}""");
        AssertCommand(EditorUiCommands.CreatePaste("text/html", "plain", "html"), "Paste", """{"type":"text/html","text":"plain","html":"html"}""");
        AssertCommand(EditorUiCommands.CreateDelete(), "DeleteSelection", "null");
        AssertCommand(EditorUiCommands.CreateSelectAll(), "SelectAll", "null");
        AssertCommand(EditorUiCommands.CreateFind("next"), "Find", """{"action":"next"}""");
        AssertCommand(EditorUiCommands.CreateFormat("strong"), "Format", "\"strong\"");
        AssertCommand(EditorUiCommands.CreateUpdateParagraph("h2"), "UpdateParagraph", "\"h2\"");
        AssertCommand(EditorUiCommands.CreateInsertParagraph("before"), "InsertParagraph", "\"before\"");
        AssertCommand(EditorUiCommands.CreateDeleteParagraph(), "DeleteParagraph", "null");
        AssertCommand(EditorUiCommands.CreateDuplicate(), "Duplicate", "null");
        AssertCommand(EditorUiCommands.CreateInsertTable(3, 4), "InsertTable", """{"rows":3,"columns":4}""");
        AssertCommand(EditorUiCommands.CreateSearchOpenChange(EditorSearchPanelState.Replace), "SearchOpenChange", """{"open":2}""");
        AssertCommand(EditorUiCommands.CreateScrollTo("intro"), "ScrollTo", """{"slug":"intro"}""");
    }

    [TestMethod]
    public void PayloadRecords_SerializeWithCamelCaseLegacyFieldsOnly()
    {
        AssertJson(new EditorFindActionRequest("next"), """{"action":"next"}""");
        AssertJson(new EditorParagraphCommandRequest("blockquote"), """{"type":"blockquote"}""");
        AssertJson(new EditorClipboardCommandRequest("text/html"), """{"type":"text/html"}""");
        AssertJson(new EditorPasteCommandRequest("text/html", "plain", "html"), """{"type":"text/html","text":"plain","html":"html"}""");
        AssertJson(new EditorInsertTableRequest(2, 5), """{"rows":2,"columns":5}""");
        AssertJson(new EditorSearchOpenChangeRequest(1), """{"open":1}""");
        AssertJson(new EditorScrollToRequest("heading"), """{"slug":"heading"}""");
    }

    private static void AssertCommand(EditorUiCommand command, string name, string argsJson)
    {
        Assert.AreEqual(name, command.Name);
        Assert.AreEqual(argsJson, JsonSerializer.Serialize(command.Args));

        var json = JsonSerializer.Serialize(command);
        StringAssert.Contains(json, "\"name\"");
        StringAssert.Contains(json, "\"args\"");
        Assert.IsFalse(json.Contains("\"Name\"", StringComparison.Ordinal));
        Assert.IsFalse(json.Contains("\"Args\"", StringComparison.Ordinal));
    }

    private static void AssertJson<T>(T value, string expectedJson)
    {
        var json = JsonSerializer.Serialize(value);

        Assert.AreEqual(expectedJson, json);
        Assert.IsFalse(json.Contains("Type", StringComparison.Ordinal));
        Assert.IsFalse(json.Contains("Rows", StringComparison.Ordinal));
        Assert.IsFalse(json.Contains("Columns", StringComparison.Ordinal));
        Assert.IsFalse(json.Contains("Action", StringComparison.Ordinal));
        Assert.IsFalse(json.Contains("Open", StringComparison.Ordinal));
        Assert.IsFalse(json.Contains("Slug", StringComparison.Ordinal));
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
