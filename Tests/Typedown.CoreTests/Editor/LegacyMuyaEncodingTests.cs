using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Typedown.Core.Editor;
using Typedown.Core.Editor.Legacy;
using Typedown.Core.Models;

namespace Typedown.CoreTests.Editor;

/// <summary>
/// 宿主 → 页面方向的旧线格式：页面用 <c>JSON.parse</c> 读，所以按「解析后字段与值相同」比较，不比字节。
/// 期望值是页面实际读取的形状（消息名、camelCase 字段名、值类型）。
/// </summary>
[TestClass]
public sealed class LegacyMuyaEncodingTests
{
    private static readonly SearchOptions Options = new(CaseSensitive: true, WholeWord: false, Regex: true);

    internal static void AssertJson(string expected, string? actual)
    {
        Assert.IsNotNull(actual);
        using var expectedDocument = JsonDocument.Parse(expected);
        using var actualDocument = JsonDocument.Parse(actual);
        Assert.IsTrue(
            JsonElement.DeepEquals(expectedDocument.RootElement, actualDocument.RootElement),
            $"Expected {expected}{Environment.NewLine}Actual   {actual}");
    }

    private static JsonElement Parse(string json)
    {
        using var document = JsonDocument.Parse(json);
        return document.RootElement.Clone();
    }

    [TestMethod]
    public void LoadDocument_KeepsTextVerbatimAndUnescaped()
    {
        var text = "# 标题\n<b>\"a\"</b> & \u2028";
        var message = LegacyMuyaProtocol.EncodeCommand(new LoadDocument(text, @"C:\文档"));

        AssertJson(
            """{"name":"LoadFile","args":{"text":"# 标题\n<b>\"a\"</b> & \u2028","basePath":"C:\\文档"}}""",
            message);
        StringAssert.Contains(message, "标题");
        StringAssert.Contains(message, "<b>");
    }

    [TestMethod]
    public void HistoryCommands_HaveNoPageMessage()
    {
        Assert.IsNull(LegacyMuyaProtocol.EncodeCommand(new Undo()));
        Assert.IsNull(LegacyMuyaProtocol.EncodeCommand(new Redo()));
        Assert.IsNull(LegacyMuyaProtocol.EncodeCommand(new ClearUndoHistory()));
    }

    [TestMethod]
    public void ArgumentlessCommands_SendNullArgs()
    {
        AssertJson("""{"name":"SelectAll","args":null}""", LegacyMuyaProtocol.EncodeCommand(new SelectAll()));
        AssertJson("""{"name":"Duplicate","args":null}""", LegacyMuyaProtocol.EncodeCommand(new DuplicateParagraph()));
        AssertJson("""{"name":"FrontMenuClosed","args":null}""", LegacyMuyaProtocol.EncodeCommand(new BlockMenuClosed()));
        AssertJson("""{"name":"RefreshScrollState","args":null}""", LegacyMuyaProtocol.EncodeCommand(new RefreshViewport()));
    }

    [TestMethod]
    public void ClipboardCommands_UseMuyaTypeNames()
    {
        AssertJson("""{"name":"Copy","args":{"type":"normal"}}""", LegacyMuyaProtocol.EncodeCommand(new Copy(CopyFormat.Rich)));
        AssertJson("""{"name":"Copy","args":{"type":"copyAsMarkdown"}}""", LegacyMuyaProtocol.EncodeCommand(new Copy(CopyFormat.Markdown)));
        AssertJson("""{"name":"Cut","args":{"type":"normal"}}""", LegacyMuyaProtocol.EncodeCommand(new Cut()));
        AssertJson(
            """{"name":"Paste","args":{"type":"pasteAsPlainText","text":"t","html":"<p>h</p>"}}""",
            LegacyMuyaProtocol.EncodeCommand(new Paste(PasteFormat.PlainText, "t", "<p>h</p>")));
    }

    [TestMethod]
    public void FormatAndParagraphCommands_UseMuyaNames()
    {
        AssertJson("""{"name":"Format","args":"inline_code"}""", LegacyMuyaProtocol.EncodeCommand(new ToggleInlineMark(InlineMark.InlineCode)));
        AssertJson("""{"name":"Format","args":"clear"}""", LegacyMuyaProtocol.EncodeCommand(new ClearInlineMarks()));
        AssertJson("""{"name":"UpdateParagraph","args":"heading 3"}""", LegacyMuyaProtocol.EncodeCommand(new SetBlockKind(BlockKind.Heading3)));
        AssertJson("""{"name":"UpdateParagraph","args":"upgrade heading"}""", LegacyMuyaProtocol.EncodeCommand(new PromoteHeading()));
        AssertJson("""{"name":"InsertParagraph","args":"before"}""", LegacyMuyaProtocol.EncodeCommand(new InsertParagraph(ParagraphPosition.Before)));
        Assert.IsNull(LegacyMuyaProtocol.EncodeCommand(new SetBlockKind(BlockKind.HtmlBlock)));
    }

    [TestMethod]
    public void TableCommands_MapToActionLocationTarget()
    {
        AssertJson("""{"name":"InsertTable","args":{"rows":3,"columns":4}}""", LegacyMuyaProtocol.EncodeCommand(new InsertTable(3, 4)));
        AssertJson(
            """{"name":"EditTable","args":{"action":"insert","location":"previous","target":"row"}}""",
            LegacyMuyaProtocol.EncodeCommand(new EditTable(TableEdit.InsertRowAbove)));
        AssertJson(
            """{"name":"EditTable","args":{"action":"remove","location":"current","target":"column"}}""",
            LegacyMuyaProtocol.EncodeCommand(new EditTable(TableEdit.RemoveColumn)));
    }

    [TestMethod]
    public void ImageCommands_MatchPagePayloads()
    {
        AssertJson(
            """{"name":"InsertImage","args":{"src":"a.png","title":null,"alt":null}}""",
            LegacyMuyaProtocol.EncodeCommand(new InsertImage("a.png")));
        AssertJson(
            """{"name":"ReplaceImage","args":{"src":"b.png","alt":"alt","title":"t"}}""",
            LegacyMuyaProtocol.EncodeCommand(new ReplaceImage(ImageTarget.Edited, "b.png", "alt", "t")));
        AssertJson(
            """{"name":"ReplaceImage","args":{"src":"b.png","alt":null,"title":null,"isReplaceSelected":true}}""",
            LegacyMuyaProtocol.EncodeCommand(new ReplaceImage(ImageTarget.Selected, "b.png", null, null)));
        AssertJson(
            """{"name":"ImageEditToolbarClick","args":{"type":"center"}}""",
            LegacyMuyaProtocol.EncodeCommand(new ApplyImageToolbarAction(ImageToolbarAction.AlignCenter)));
        AssertJson(
            """{"name":"ImageEditToolbarClick","args":{"type":"updateImage","attrName":"style","attrValue":"width:10px;zoom:25%;"}}""",
            LegacyMuyaProtocol.EncodeCommand(new SetImageZoom(25), imageStyle: "width:10px;zoom:50%;"));
    }

    [TestMethod]
    public void Search_CarriesPageSelectionBack()
    {
        var selection = Parse("""{"anchor":{"line":1,"ch":2},"head":{"line":1,"ch":5}}""");
        AssertJson(
            """{"name":"Search","args":{"value":"查找","opt":{"searchIsCaseSensitive":true,"searchIsWholeWord":false,"searchIsRegexp":true,"selection":{"anchor":{"line":1,"ch":2},"head":{"line":1,"ch":5}}}}}""",
            LegacyMuyaProtocol.EncodeCommand(new Search("查找", Options), selection));
        AssertJson(
            """{"name":"Search","args":{"value":null,"opt":{"searchIsCaseSensitive":true,"searchIsWholeWord":false,"searchIsRegexp":true,"selection":{}}}}""",
            LegacyMuyaProtocol.EncodeCommand(new Search(null, Options)));
    }

    [TestMethod]
    public void FindReplaceAndEndSearch_MatchPagePayloads()
    {
        AssertJson("""{"name":"Find","args":{"action":"prev"}}""", LegacyMuyaProtocol.EncodeCommand(new FindMatch(SearchDirection.Previous)));
        AssertJson(
            """{"name":"Replace","args":{"searchValue":"a","value":"b","opt":{"isSingle":false,"searchIsCaseSensitive":true,"searchIsWholeWord":false,"searchIsRegexp":true}}}""",
            LegacyMuyaProtocol.EncodeCommand(new Replace("a", "b", All: true, Options)));
        AssertJson("""{"name":"SearchOpenChange","args":{"open":0}}""", LegacyMuyaProtocol.EncodeCommand(new EndSearch()));
    }

    [TestMethod]
    public void ViewCommands_MatchPagePayloads()
    {
        AssertJson("""{"name":"ScrollTo","args":{"slug":"标题-1"}}""", LegacyMuyaProtocol.EncodeCommand(new RevealHeading(new HeadingId("标题-1"))));
        AssertJson("""{"name":"OnScroll","args":{"scrollX":0,"scrollY":120.5}}""", LegacyMuyaProtocol.EncodeCommand(new ScrollTo(0, 120.5)));
        AssertJson(
            """{"name":"SetShortcuts","args":{"shortcuts":[{"key":83,"modifiers":1}]}}""",
            LegacyMuyaProtocol.EncodeCommand(new SetKeymap([new KeyChord(KeyboardKey.S, KeyboardModifiers.Control)])));
        AssertJson(
            """{"name":"ThemeChanged","args":{"theme":"Dark","accentColor":{"r":1,"g":2,"b":3,"a":1},"background":{"r":40,"g":40,"b":40,"a":0.5}}}""",
            LegacyMuyaProtocol.EncodeCommand(new ApplyTheme(new EditorTheme(true, new EditorColor(1, 2, 3, 1), new EditorColor(40, 40, 40, 0.5)))));
    }

    [TestMethod]
    public void ApplySettings_SendsOnlyChangedFields()
    {
        AssertJson(
            """{"name":"SettingsChanged","args":{"fontSize":18,"editorAreaWidth":"80%","spellcheckEnabled":false}}""",
            LegacyMuyaProtocol.EncodeCommand(new ApplySettings(new EditorSettings { FontSize = 18, EditorAreaWidth = "80%", SpellcheckEnabled = false })));
    }

    [TestMethod]
    public void ImportHtml_UsesHtmlImportType()
    {
        AssertJson("""{"name":"ImportFile","args":{"type":"html","text":"<h1>x</h1>"}}""", LegacyMuyaProtocol.EncodeCommand(new ImportHtml("<h1>x</h1>")));
    }

    [TestMethod]
    public void SetMarkdown_CarriesCursorAndBasePath()
    {
        AssertJson(
            """{"name":"SetMarkdown","args":{"text":"a","cursor":{"anchor":{"line":0,"ch":1},"focus":{"line":2,"ch":3}},"basePath":"D:\\"}}""",
            LegacyMuyaProtocol.EncodeSetMarkdown("a", new CursorState(new(0, 1), new(2, 3)), @"D:\"));
        AssertJson(
            """{"name":"SetMarkdown","args":{"text":"a","cursor":null,"basePath":""}}""",
            LegacyMuyaProtocol.EncodeSetMarkdown("a", null, string.Empty));
    }

    [TestMethod]
    public void ExportRequest_CarriesRequestIdAndOnlyPresentOptions()
    {
        AssertJson(
            """{"name":"Export","args":{"type":"print","title":"T","context":{"requestId":7},"basePath":"C:\\d","options":{"extraHead":"<style></style>","footer":"<p>f</p>"}}}""",
            LegacyMuyaProtocol.EncodeExportRequest(
                new RenderExportHtml(ExportPurpose.Print, "T", @"C:\d", new ExportHtmlOptions("<style></style>", null, null, "<p>f</p>")),
                7));
        AssertJson(
            """{"name":"Export","args":{"type":"export","title":"T","context":{"requestId":8},"basePath":null}}""",
            LegacyMuyaProtocol.EncodeExportRequest(new RenderExportHtml(ExportPurpose.Export, "T", null, null), 8));
    }

    [TestMethod]
    public void Replies_UseInvokeReplyEnvelope()
    {
        AssertJson("""{"name":"3","args":{"code":0,"data":null}}""", LegacyMuyaProtocol.EncodeNullReply("3"));
        AssertJson("""{"name":"3","args":{"code":0,"data":true}}""", LegacyMuyaProtocol.EncodeBoolReply("3", true));
        AssertJson("""{"name":"3","args":{"code":1,"msg":"function [X] does not exist"}}""", LegacyMuyaProtocol.EncodeErrorReply("3", "function [X] does not exist"));
        AssertJson("""{"name":"3","args":{"code":0,"data":{"rows":2,"columns":5}}}""", LegacyMuyaProtocol.EncodeTableSizeReply("3", new TableSize(2, 5)));
        AssertJson("""{"name":"3","args":{"code":0,"data":null}}""", LegacyMuyaProtocol.EncodeTableSizeReply("3", null));
        AssertJson(
            """{"name":"3","args":{"code":0,"data":{"theme":"Light","accentColor":{"r":27,"g":102,"b":107,"a":1},"background":{"r":249,"g":249,"b":249,"a":1}}}}""",
            LegacyMuyaProtocol.EncodeThemeReply("3", new EditorTheme(false, new EditorColor(27, 102, 107, 1), new EditorColor(249, 249, 249, 1))));
    }

    [TestMethod]
    public void StringResourcesReply_CamelCasesKeys()
    {
        AssertJson(
            """{"name":"4","args":{"code":0,"data":{"copyContent":"复制","ctrlAndClickOpenLink":"Ctrl+单击"}}}""",
            LegacyMuyaProtocol.EncodeStringResourcesReply("4", new Dictionary<string, string>
            {
                ["CopyContent"] = "复制",
                ["CtrlAndClickOpenLink"] = "Ctrl+单击",
            }));
    }

    [TestMethod]
    public void StartupReply_CarriesAllSettingsTextAndBasePath()
    {
        var settings = new EditorSettings
        {
            SourceCode = true,
            Typewriter = false,
            FocusMode = true,
            SearchIsCaseSensitive = false,
            SearchIsRegexp = true,
            SearchIsWholeWord = false,
            FontSize = 16,
            LineHeight = 1.6,
            AutoPairBracket = true,
            AutoPairQuote = false,
            TrimUnnecessaryCodeBlockEmptyLines = true,
            PreferLooseListItem = false,
            AutoPairMarkdownSyntax = true,
            EditorAreaWidth = "1000px",
            TabSize = 4,
            SpellcheckEnabled = true,
        };

        AssertJson(
            """
            {"name":"1","args":{"code":0,"data":{
              "sourceCode":true,"typewriter":false,"focusMode":true,
              "searchIsCaseSensitive":false,"searchIsRegexp":true,"searchIsWholeWord":false,
              "fontSize":16,"lineHeight":1.6,"autoPairBracket":true,"autoPairQuote":false,
              "trimUnnecessaryCodeBlockEmptyLines":true,"preferLooseListItem":false,"autoPairMarkdownSyntax":true,
              "editorAreaWidth":"1000px","tabSize":4,"spellcheckEnabled":true,
              "markdown":"# 正文","basePath":"C:\\notes"}}}
            """,
            LegacyMuyaProtocol.EncodeStartupReply("1", settings, "# 正文", @"C:\notes"));
    }

    [TestMethod]
    public void EveryCommandType_HasLegacyTranslation()
    {
        EditorCommand[] samples =
        [
            new LoadDocument("t", "b"), new ImportHtml("h"),
            new Undo(), new Redo(), new ClearUndoHistory(),
            new SelectAll(), new DeleteSelection(), new Copy(CopyFormat.Html), new Cut(), new Paste(PasteFormat.Rich, "t", "h"),
            new ToggleInlineMark(InlineMark.Strong), new ClearInlineMarks(), new SetBlockKind(BlockKind.Quote),
            new PromoteHeading(), new DemoteHeading(), new InsertParagraph(ParagraphPosition.After),
            new DeleteParagraph(), new DuplicateParagraph(), new BlockMenuClosed(),
            new InsertTable(1, 1), new EditTable(TableEdit.RemoveRow),
            new InsertImage("s"), new ReplaceImage(ImageTarget.Edited, "s", null, null),
            new ApplyImageToolbarAction(ImageToolbarAction.Delete), new SetImageZoom(50),
            new Search("q", Options), new FindMatch(SearchDirection.Next), new Replace("q", "r", false, Options), new EndSearch(),
            new RevealHeading(new HeadingId("h")),
            new ApplySettings(new EditorSettings()), new ApplyTheme(new EditorTheme(false, default, default)),
            new SetKeymap([]), new ScrollTo(0, 0), new RefreshViewport(),
        ];

        var commandTypes = typeof(EditorCommand).Assembly.GetTypes()
            .Where(t => !t.IsAbstract && typeof(EditorCommand).IsAssignableFrom(t))
            .ToHashSet();
        CollectionAssert.AreEquivalent(commandTypes.ToList(), samples.Select(x => x.GetType()).Distinct().ToList());

        foreach (var command in samples)
        {
            var message = LegacyMuyaProtocol.EncodeCommand(command);
            if (message is not null)
            {
                using var document = JsonDocument.Parse(message);
                Assert.AreEqual(JsonValueKind.String, document.RootElement.GetProperty("name").ValueKind, command.GetType().Name);
                Assert.IsTrue(document.RootElement.TryGetProperty("args", out _), command.GetType().Name);
            }
        }
    }

    [TestMethod]
    public void Encoding_IsDeterministic()
    {
        var command = new LoadDocument("中文 <tag> \"q\" \u2029 😀", "p");
        Assert.AreEqual(LegacyMuyaProtocol.EncodeCommand(command), LegacyMuyaProtocol.EncodeCommand(command));
    }
}
