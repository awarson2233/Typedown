using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using Typedown.Core.Models;

namespace Typedown.Core.Editor.Legacy
{
    // 旧 Muya 页面的线上载荷。字段名与顺序即线上格式：序列化按声明顺序、camelCase 输出，
    // 与此前 Newtonsoft 匿名对象的输出逐字节一致（见 CoreTests 的旧线格式对照测试）。

    // ── 宿主 → 页面 ─────────────────────────────────────────────────────

    internal sealed record LoadFileArgs(string Text, string BasePath);

    internal sealed record ImportFileArgs(string Type, string Text);

    internal sealed record SetMarkdownArgs(string? Text, CursorState? Cursor, string BasePath);

    internal sealed record ExportArgs(
        string Type,
        string Title,
        ExportContextArgs Context,
        string? BasePath,
        [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] ExportOptionsArgs? Options);

    internal sealed record ExportContextArgs(long RequestId);

    internal sealed record ExportOptionsArgs(
        [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? ExtraHead,
        [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? ExtraBody,
        [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? Header,
        [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? Footer);

    /// <summary>SettingsChanged 只带变化的项，未变化的字段不出现在线上。</summary>
    internal sealed class SettingsChangedArgs
    {
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] public bool? SourceCode { get; init; }
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] public bool? Typewriter { get; init; }
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] public bool? FocusMode { get; init; }
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] public bool? SearchIsCaseSensitive { get; init; }
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] public bool? SearchIsRegexp { get; init; }
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] public bool? SearchIsWholeWord { get; init; }
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] public double? FontSize { get; init; }
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] public double? LineHeight { get; init; }
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] public bool? AutoPairBracket { get; init; }
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] public bool? AutoPairQuote { get; init; }
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] public bool? TrimUnnecessaryCodeBlockEmptyLines { get; init; }
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] public bool? PreferLooseListItem { get; init; }
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] public bool? AutoPairMarkdownSyntax { get; init; }
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] public string? EditorAreaWidth { get; init; }
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] public int? TabSize { get; init; }
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] public bool? SpellcheckEnabled { get; init; }
    }

    internal sealed record ThemeArgs(string Theme, ColorArgs AccentColor, ColorArgs Background);

    internal sealed record ColorArgs(int R, int G, int B, double A);

    internal sealed record ShortcutsArgs(IReadOnlyList<ChordArgs> Shortcuts);

    internal sealed record ChordArgs(int Key, int Modifiers);

    internal sealed record ScrollArgs(double ScrollX, double ScrollY);

    internal sealed record SearchArgs(string? Value, SearchOptionArgs Opt);

    internal sealed record SearchOptionArgs(bool SearchIsCaseSensitive, bool SearchIsWholeWord, bool SearchIsRegexp, JsonElement Selection);

    internal sealed record FindArgs(string Action);

    internal sealed record ReplaceArgs(string? SearchValue, string Value, ReplaceOptionArgs Opt);

    internal sealed record ReplaceOptionArgs(bool IsSingle, bool SearchIsCaseSensitive, bool SearchIsWholeWord, bool SearchIsRegexp);

    internal sealed record SearchOpenChangeArgs(int Open);

    internal sealed record ScrollToArgs(string Slug);

    internal sealed record ClipboardTypeArgs(string Type);

    internal sealed record PasteArgs(string Type, string Text, string Html);

    internal sealed record InsertImageArgs(string Src, string? Title, string? Alt);

    internal sealed record InsertTableArgs(int Rows, int Columns);

    internal sealed record ReplaceImageArgs(
        string Src,
        string? Alt,
        string? Title,
        [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)] bool IsReplaceSelected);

    internal sealed record ImageToolbarClickArgs(
        string Type,
        [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? AttrName,
        [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? AttrValue);

    internal sealed record EditTableArgs(string Action, string Location, string Target);

    // ── 宿主 → 页面：invoke 应答 ─────────────────────────────────────────

    internal sealed record StartupReply(
        bool SourceCode,
        bool Typewriter,
        bool FocusMode,
        bool SearchIsCaseSensitive,
        bool SearchIsRegexp,
        bool SearchIsWholeWord,
        double FontSize,
        double LineHeight,
        bool AutoPairBracket,
        bool AutoPairQuote,
        bool TrimUnnecessaryCodeBlockEmptyLines,
        bool PreferLooseListItem,
        bool AutoPairMarkdownSyntax,
        string EditorAreaWidth,
        int TabSize,
        bool SpellcheckEnabled,
        string Markdown,
        string BasePath);

    internal sealed record TableSizeArgs(int Rows, int Columns);

    // ── 页面 → 宿主 ─────────────────────────────────────────────────────

    internal sealed record StringResourcesRequest(List<string>? Names);

    internal sealed record ExportCallbackRequest(string? Html, ExportContextArgs? Context);

    internal sealed record MarkdownPayload(string? Text);

    internal sealed record CursorPayload(CursorState? Cursor);

    internal sealed record StatePayload(ContentStatePayload? State);

    internal sealed record ContentStatePayload(WordCountPayload? WordCount, List<TocItemPayload>? Toc, TocItemPayload? Cur);

    internal sealed record WordCountPayload(int Word, int Character);

    /// <summary>源码模式的目录 slug 是 <c>Math.random()</c> 生成的数字，所以按原始 JSON 读取。</summary>
    internal sealed record TocItemPayload(string? Content, int Lvl, JsonElement Slug);

    internal sealed record SelectionPayload(JsonElement Selection, MenuStatePayload? MenuState, string? SelectionText);

    internal sealed class MenuStatePayload
    {
        public bool IsDisabled { get; set; }
        public bool IsMultiline { get; set; }
        public bool IsLooseListItem { get; set; }
        public bool IsTaskList { get; set; }
        public bool IsCodeFences { get; set; }
        public bool IsCodeContent { get; set; }
        public bool IsTable { get; set; }
        public bool IsFootnote { get; set; }
        public Dictionary<string, bool>? Affiliation { get; set; }
    }

    internal sealed record SelectionFormatsPayload(List<SelectionFormatPayload>? Formats);

    internal sealed record SelectionFormatPayload(string? Type, string? Tag);

    internal sealed record CodeMirrorSelectionPayload(JsonElement Cursor, string? SelectionText);

    internal sealed record ScrollStatePayload(
        double ViewportWidth,
        double ViewportHeight,
        double MaximumX,
        double MaximumY,
        double ScrollX,
        double ScrollY);

    internal sealed record KeyDownPayload(int Key, int Modifiers);

    internal sealed record OpenUriPayload(string? Uri);

    internal sealed record RectPayload(double X, double Y, double Width, double Height);

    internal sealed record ImageInfoPayload(string? Src, string? Alt, string? Title);

    internal sealed record TableInfoPayload(string? BarType);

    internal sealed record FloatPayload(
        RectPayload? BoundingClientRect,
        ImageInfoPayload? ImageInfo,
        TableInfoPayload? TableInfo,
        JsonElement Attrs,
        bool? Open,
        string? Tooltip);

    [JsonSourceGenerationOptions(
        PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
        DictionaryKeyPolicy = JsonKnownNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        MaxDepth = 256)]
    [JsonSerializable(typeof(LoadFileArgs))]
    [JsonSerializable(typeof(ImportFileArgs))]
    [JsonSerializable(typeof(SetMarkdownArgs))]
    [JsonSerializable(typeof(ExportArgs))]
    [JsonSerializable(typeof(SettingsChangedArgs))]
    [JsonSerializable(typeof(ThemeArgs))]
    [JsonSerializable(typeof(ShortcutsArgs))]
    [JsonSerializable(typeof(ScrollArgs))]
    [JsonSerializable(typeof(SearchArgs))]
    [JsonSerializable(typeof(FindArgs))]
    [JsonSerializable(typeof(ReplaceArgs))]
    [JsonSerializable(typeof(SearchOpenChangeArgs))]
    [JsonSerializable(typeof(ScrollToArgs))]
    [JsonSerializable(typeof(ClipboardTypeArgs))]
    [JsonSerializable(typeof(PasteArgs))]
    [JsonSerializable(typeof(InsertImageArgs))]
    [JsonSerializable(typeof(InsertTableArgs))]
    [JsonSerializable(typeof(ReplaceImageArgs))]
    [JsonSerializable(typeof(ImageToolbarClickArgs))]
    [JsonSerializable(typeof(EditTableArgs))]
    [JsonSerializable(typeof(StartupReply))]
    [JsonSerializable(typeof(TableSizeArgs))]
    [JsonSerializable(typeof(Dictionary<string, string>))]
    [JsonSerializable(typeof(bool))]
    [JsonSerializable(typeof(string))]
    [JsonSerializable(typeof(StringResourcesRequest))]
    [JsonSerializable(typeof(ExportCallbackRequest))]
    [JsonSerializable(typeof(MarkdownPayload))]
    [JsonSerializable(typeof(CursorPayload))]
    [JsonSerializable(typeof(StatePayload))]
    [JsonSerializable(typeof(SelectionPayload))]
    [JsonSerializable(typeof(SelectionFormatsPayload))]
    [JsonSerializable(typeof(CodeMirrorSelectionPayload))]
    [JsonSerializable(typeof(ScrollStatePayload))]
    [JsonSerializable(typeof(KeyDownPayload))]
    [JsonSerializable(typeof(OpenUriPayload))]
    [JsonSerializable(typeof(FloatPayload))]
    internal sealed partial class LegacyMuyaJsonContext : JsonSerializerContext
    {
    }
}
