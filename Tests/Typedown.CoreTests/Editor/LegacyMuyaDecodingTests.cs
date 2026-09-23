using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Typedown.Core.Editor;
using Typedown.Core.Editor.Legacy;
using Typedown.Core.Models;

namespace Typedown.CoreTests.Editor;

/// <summary>页面 → 宿主方向：信封解析、diffmsg 重组与各消息到类型化事件的映射。</summary>
[TestClass]
public sealed class LegacyMuyaDecodingTests
{
    private static JsonElement Parse(string json)
    {
        using var document = JsonDocument.Parse(json);
        return document.RootElement.Clone();
    }

    private static T Decode<T>(string name, string args) where T : LegacyInbound
    {
        var inbound = LegacyMuyaProtocol.DecodeMessage(name, Parse(args));
        Assert.IsInstanceOfType(inbound, typeof(T));
        return (T)inbound!;
    }

    private static T DecodeEvent<T>(string name, string args) where T : EditorEvent
    {
        var inbound = Decode<LegacyTypedEvent>(name, args);
        Assert.IsInstanceOfType(inbound.Event, typeof(T));
        return (T)inbound.Event;
    }

    // ── 信封 ─────────────────────────────────────────────────────────

    [TestMethod]
    public void Envelope_ParsesInvokeMessageAndDiff()
    {
        Assert.IsTrue(LegacyMuyaProtocol.TryParseEnvelope("""{"type":"invoke","id":"12","name":"GetSettings","args":null}""", out var invoke));
        Assert.AreEqual(("invoke", "12", "GetSettings"), (invoke.Type, invoke.Id, invoke.Name));
        Assert.AreEqual(JsonValueKind.Null, invoke.Args.ValueKind);

        Assert.IsTrue(LegacyMuyaProtocol.TryParseEnvelope("""{"type":"diffmsg","name":"StateChange","diff":true,"args":"xy","start":3,"end":5}""", out var diff));
        Assert.IsTrue(diff.Diff);
        Assert.AreEqual((3, 5), (diff.Start, diff.End));
        Assert.AreEqual("xy", diff.Args.GetString());
    }

    [TestMethod]
    public void Envelope_RejectsMalformedMessages()
    {
        Assert.IsFalse(LegacyMuyaProtocol.TryParseEnvelope(null, out _));
        Assert.IsFalse(LegacyMuyaProtocol.TryParseEnvelope("not json", out _));
        Assert.IsFalse(LegacyMuyaProtocol.TryParseEnvelope("[1,2]", out _));
        Assert.IsFalse(LegacyMuyaProtocol.TryParseEnvelope("""{"name":"X"}""", out _));
    }

    [TestMethod]
    public void UnknownOrRetiredMessages_AreDropped()
    {
        Assert.IsNull(LegacyMuyaProtocol.DecodeMessage("NoSuchMessage", Parse("{}")));
        Assert.IsNull(LegacyMuyaProtocol.DecodeMessage("KeyDown", Parse("null")));
        Assert.IsNull(LegacyMuyaProtocol.DecodeMessage("OpenURI", Parse("""{"uri":"  "}""")));
    }

    // ── diffmsg ─────────────────────────────────────────────────────

    [TestMethod]
    public void DiffChannel_ReassemblesFragmentsAgainstPreviousText()
    {
        var channel = new LegacyDiffChannel();
        Assert.IsTrue(channel.TryApply("StateChange", Parse("\"{\\\"a\\\":1}\""), diff: false, 0, 0, out var full));
        Assert.AreEqual("""{"a":1}""", full);

        Assert.IsTrue(channel.TryApply("StateChange", Parse("\"23\""), diff: true, 5, 6, out var patched));
        Assert.AreEqual("""{"a":23}""", patched);

        Assert.IsTrue(channel.TryApply("StateChange", Parse("\"4\""), diff: true, 5, 7, out var again));
        Assert.AreEqual("""{"a":4}""", again);
    }

    [TestMethod]
    public void DiffChannel_RejectsOutOfRangeAndResets()
    {
        var channel = new LegacyDiffChannel();
        Assert.IsTrue(channel.TryApply("M", Parse("\"abc\""), false, 0, 0, out _));
        Assert.IsFalse(channel.TryApply("M", Parse("\"x\""), true, 2, 9, out _));
        Assert.IsFalse(channel.TryApply("M", Parse("null"), false, 0, 0, out _));

        channel.Reset();
        Assert.IsTrue(channel.TryApply("M", Parse("\"x\""), true, 2, 9, out var afterReset));
        Assert.AreEqual("x", afterReset);
    }

    // ── 正文、光标、状态 ──────────────────────────────────────────────

    [TestMethod]
    public void TextCursorAndLoad_MapToAdapterInputs()
    {
        Assert.AreEqual("# 正文", Decode<LegacyTextChanged>("MarkdownChange", """{"text":"# 正文"}""").Text);
        Assert.AreEqual(string.Empty, Decode<LegacyFileLoaded>("FileLoaded", """{"text":null}""").Text);
        var cursor = Decode<LegacyCursorChanged>("CursorChange", """{"cursor":{"anchor":{"line":1,"ch":2},"focus":{"line":3,"ch":4}}}""").Cursor;
        Assert.AreEqual(new CursorState(new(1, 2), new(3, 4)), cursor);
        Assert.IsNull(LegacyMuyaProtocol.DecodeMessage("CursorChange", Parse("""{"cursor":null}""")));
    }

    [TestMethod]
    public void StateChange_MapsStatsAndOutlineWithOpaqueHeadingIds()
    {
        var state = Decode<LegacyStateChanged>(
            "StateChange",
            """{"state":{"wordCount":{"word":3,"character":12},"toc":[{"content":"一","lvl":1,"slug":"yi"},{"content":"源码","lvl":2,"slug":0.25}],"cur":{"content":"源码","lvl":2,"slug":0.25}}}""");

        var stats = state.Events.OfType<StatsChanged>().Single();
        Assert.AreEqual((12, 3), (stats.Characters, stats.Words));

        var outline = state.Events.OfType<OutlineChanged>().Single();
        CollectionAssert.AreEqual(
            new[] { new OutlineItem(new HeadingId("yi"), 1, "一"), new OutlineItem(new HeadingId("0.25"), 2, "源码") },
            outline.Items.ToArray());
        Assert.AreEqual(new HeadingId("0.25"), outline.Current?.Id);
    }

    [TestMethod]
    public void StateChange_WithoutStateStillEndsTheEcho()
    {
        Assert.AreEqual(0, Decode<LegacyStateChanged>("StateChange", "{}").Events.Count);
    }

    // ── 选区 ─────────────────────────────────────────────────────────

    [TestMethod]
    public void MuyaSelection_UsesIsCollapsedFirst()
    {
        var selection = Decode<LegacySelectionChanged>("SelectionChange", """{"selection":{"isCollapsed":false},"selectionText":"ab"}""");
        Assert.IsTrue(selection.Event.HasText);
        Assert.AreEqual("ab", selection.Event.Text);
        Assert.IsFalse(selection.SourceMode);
    }

    [TestMethod]
    public void MuyaSelection_ComparesOffsetsThenPaths()
    {
        const string samePath = """{"selection":{"start":{"offset":1},"end":{"offset":1},"anchorPath":[0,1],"focusPath":[0,1]}}""";
        const string otherPath = """{"selection":{"start":{"offset":1},"end":{"offset":1},"anchorPath":[0,1],"focusPath":[0,2]}}""";
        const string otherOffset = """{"selection":{"start":{"offset":1},"end":{"offset":4}}}""";

        Assert.IsFalse(Decode<LegacySelectionChanged>("SelectionChange", samePath).Event.HasText);
        Assert.IsTrue(Decode<LegacySelectionChanged>("SelectionChange", otherPath).Event.HasText);
        Assert.IsTrue(Decode<LegacySelectionChanged>("SelectionChange", otherOffset).Event.HasText);
    }

    [TestMethod]
    public void MuyaSelection_MapsBlockContextAndSelectedImage()
    {
        var selection = Decode<LegacySelectionChanged>(
            "SelectionChange",
            """{"selection":{"isCollapsed":true,"selectedImage":{"token":{"src":"a.png","alt":"A","title":"T"}}},"menuState":{"isMultiline":true,"isCodeFences":false,"affiliation":{"h2":true,"ul":true},"isTaskList":true}}""");

        var rich = selection.Event.Rich!;
        CollectionAssert.AreEqual(new[] { BlockKind.Heading2, BlockKind.TaskList }, rich.Block.Kinds.ToArray());
        Assert.IsTrue(rich.Block.MultipleBlocks);
        Assert.AreEqual(new ImageInfo("a.png", "A", "T"), rich.SelectedImage);
        Assert.AreEqual(JsonValueKind.Object, selection.PageSelection.ValueKind);
    }

    [TestMethod]
    public void CodeMirrorSelection_ComparesAnchorAndHead()
    {
        var caret = Decode<LegacySelectionChanged>("CodeMirrorSelectionChange", """{"cursor":{"anchor":{"line":2,"ch":1},"head":{"line":2,"ch":1}},"selectionText":""}""");
        Assert.IsFalse(caret.Event.HasText);
        Assert.IsTrue(caret.SourceMode);
        Assert.IsNull(caret.Event.Rich);

        var range = Decode<LegacySelectionChanged>("CodeMirrorSelectionChange", """{"cursor":{"anchor":{"line":2,"ch":1},"head":{"line":2,"ch":6}},"selectionText":"hello"}""");
        Assert.IsTrue(range.Event.HasText);
        Assert.AreEqual(2, range.PageSelection.GetProperty("head").GetProperty("line").GetInt32());
    }

    [TestMethod]
    public void SelectionFormats_MapToInlineMarks()
    {
        var marks = DecodeEvent<MarksChanged>("SelectionFormats", """{"formats":[{"type":"strong"},{"type":"html_tag","tag":"mark"},{"type":"link"}]}""");
        CollectionAssert.AreEqual(new[] { InlineMark.Strong, InlineMark.Highlight, InlineMark.Link }, marks.Marks.ToArray());
    }

    // ── 视图 ─────────────────────────────────────────────────────────

    [TestMethod]
    public void ViewEvents_MapToTypedEvents()
    {
        var viewport = DecodeEvent<ViewportChanged>("OnScroll", """{"viewportWidth":800,"viewportHeight":600,"maximumX":0,"maximumY":2000,"scrollX":0,"scrollY":42.5}""");
        Assert.AreEqual(new ViewportChanged(800, 600, 0, 2000, 0, 42.5), viewport);

        var key = DecodeEvent<ShortcutPressed>("KeyDown", """{"key":83,"modifiers":1}""");
        Assert.AreEqual(new ShortcutPressed(KeyboardKey.S, KeyboardModifiers.Control), key);

        Assert.AreEqual("https://example.com/路径", DecodeEvent<LinkOpenRequested>("OpenURI", """{"uri":"https://example.com/路径"}""").Uri);
    }

    // ── 浮层 ─────────────────────────────────────────────────────────

    [TestMethod]
    public void FloatRequests_CarryAnchor()
    {
        var menu = DecodeEvent<BlockMenuRequested>("OpenFrontMenu", """{"boundingClientRect":{"x":1,"y":2,"width":3,"height":4}}""");
        Assert.AreEqual(new EditorRect(1, 2, 3, 4), menu.Anchor);
        Assert.IsNull(DecodeEvent<FormatPickerRequested>("OpenFormatPicker", "{}").Anchor);

        var editor = DecodeEvent<ImageEditorRequested>("OpenImageSelector", """{"boundingClientRect":{"x":0,"y":0,"width":1,"height":1},"imageInfo":{"src":"s","alt":null}}""");
        Assert.AreEqual(new ImageInfo("s", string.Empty, string.Empty), editor.Image);
    }

    [TestMethod]
    public void ImageToolbar_RemembersImageStyle()
    {
        var toolbar = Decode<LegacyImageToolbarOpened>("OpenImageToolbar", """{"boundingClientRect":{"x":0,"y":0,"width":1,"height":1},"attrs":{"style":"zoom:50%;"}}""");
        Assert.AreEqual("zoom:50%;", toolbar.Style);
        Assert.IsNull(Decode<LegacyImageToolbarOpened>("OpenImageToolbar", "{}").Style);
    }

    [TestMethod]
    public void TableTools_MapBarTypeToAxis()
    {
        Assert.AreEqual(TableAxis.Column, DecodeEvent<TableToolsRequested>("OpenTableTools", """{"tableInfo":{"barType":"bottom"}}""").Axis);
        Assert.AreEqual(TableAxis.Row, DecodeEvent<TableToolsRequested>("OpenTableTools", """{"tableInfo":{"barType":"left"}}""").Axis);
    }

    [TestMethod]
    public void Tooltip_MapsResourceKeyToKindAndDropsUnknownKeys()
    {
        var tooltip = DecodeEvent<TooltipRequested>("OpenToolTip", """{"open":true,"tooltip":"CtrlAndClickOpenLink","boundingClientRect":{"x":5,"y":6,"width":7,"height":8}}""");
        Assert.AreEqual(TooltipKind.CtrlClickToOpenLink, tooltip.Kind);
        Assert.AreEqual(new EditorRect(5, 6, 7, 8), tooltip.Anchor);

        Assert.IsNull(LegacyMuyaProtocol.DecodeMessage("OpenToolTip", Parse("""{"open":true,"tooltip":"SomethingNew"}""")));
        DecodeEvent<TooltipDismissed>("OpenToolTip", """{"open":false,"tooltip":"CopyContent"}""");
    }

    [TestMethod]
    public void EveryTooltipKind_HasAPageResourceKey()
    {
        var mapped = new[] { "CopyContent", "CtrlAndClickOpenLink", "ResizeTable", "AlignLeft", "AlignCenter", "AlignRight", "DeleteTable" }
            .Select(key => LegacyMuyaVocabulary.TryParseTooltip(key, out var kind) ? kind : (TooltipKind?)null)
            .ToList();
        CollectionAssert.AreEquivalent(Enum.GetValues<TooltipKind>().Cast<TooltipKind?>().ToList(), mapped);
    }

    // ── invoke 载荷 ─────────────────────────────────────────────────

    [TestMethod]
    public void InvokePayloads_AreRead()
    {
        CollectionAssert.AreEqual(new[] { "CopyContent", "AlignLeft" }, LegacyMuyaProtocol.ReadStringResourceNames(Parse("""{"names":["CopyContent",null,"AlignLeft"]}""")).ToArray());
        Assert.AreEqual<(string?, long?)>(("<html/>", 9L), LegacyMuyaProtocol.ReadExportCallback(Parse("""{"html":"<html/>","context":{"requestId":9}}""")));
        Assert.AreEqual<(string?, long?)>((null, null), LegacyMuyaProtocol.ReadExportCallback(Parse("""{"html":null}""")));
        Assert.AreEqual(("text/html", "<b>x</b>"), LegacyMuyaProtocol.ReadClipboardWrite(Parse("""{"type":"text/html","data":"<b>x</b>"}""")));
        Assert.AreEqual("boom", LegacyMuyaProtocol.ReadStringArgument(Parse("\"boom\"")));
        Assert.IsNull(LegacyMuyaProtocol.ReadStringArgument(Parse("{}")));
    }

    // ── 词汇表 ───────────────────────────────────────────────────────

    [TestMethod]
    public void ParagraphAndFormatNames_RoundTrip()
    {
        foreach (var kind in Enum.GetValues<BlockKind>())
        {
            if (LegacyMuyaVocabulary.TryGetParagraphName(kind, out var name))
            {
                Assert.IsTrue(LegacyMuyaVocabulary.TryParseParagraphName(name, out var parsed));
                Assert.AreEqual(kind, parsed);
            }
        }

        foreach (var mark in Enum.GetValues<InlineMark>())
        {
            Assert.IsTrue(LegacyMuyaVocabulary.TryParseFormatName(LegacyMuyaVocabulary.GetFormatName(mark), out var parsed));
            Assert.AreEqual(mark, parsed);
        }
    }

    [TestMethod]
    public void ComposeZoomStyle_ReplacesOnlyTheZoomDeclaration()
    {
        Assert.AreEqual("zoom:75%;", LegacyMuyaVocabulary.ComposeZoomStyle(null, 75));
        Assert.AreEqual("width:20px;zoom:100%;", LegacyMuyaVocabulary.ComposeZoomStyle("zoom:50%;width:20px;", 100));
    }
}
