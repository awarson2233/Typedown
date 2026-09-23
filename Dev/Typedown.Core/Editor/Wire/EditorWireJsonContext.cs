using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Typedown.Core.Editor.Wire
{
    /// <summary>
    /// 新引擎线协议的 STJ 源生成元数据。字段名 camelCase、空值不写；契约枚举写成 camelCase 字符串，
    /// 但 <see cref="Models.KeyboardKey"/> 与 <see cref="Models.KeyboardModifiers"/> 是虚拟键码与标志位，保持整数；
    /// <see cref="HeadingId"/> 展开成它的字符串值。运行时的完整选项（转义器、必填字段规则）见 <see cref="EditorWireTypes.Options"/>。
    /// </summary>
    [JsonSourceGenerationOptions(
        PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        RespectNullableAnnotations = true,
        MaxDepth = 64,
        Converters =
        [
            typeof(HeadingIdJsonConverter),
            typeof(CamelCaseEnumConverter<InlineMark>),
            typeof(CamelCaseEnumConverter<BlockKind>),
            typeof(CamelCaseEnumConverter<ParagraphPosition>),
            typeof(CamelCaseEnumConverter<CopyFormat>),
            typeof(CamelCaseEnumConverter<PasteFormat>),
            typeof(CamelCaseEnumConverter<TableAxis>),
            typeof(CamelCaseEnumConverter<TableEdit>),
            typeof(CamelCaseEnumConverter<ImageTarget>),
            typeof(CamelCaseEnumConverter<ImageToolbarAction>),
            typeof(CamelCaseEnumConverter<SearchDirection>),
            typeof(CamelCaseEnumConverter<ExportPurpose>),
            typeof(CamelCaseEnumConverter<TooltipKind>),
            typeof(CamelCaseEnumConverter<ImageSourceKind>),
            typeof(CamelCaseEnumConverter<EditorWireError>),
        ])]
    // ── 命令 ──
    [JsonSerializable(typeof(DocLoad))]
    [JsonSerializable(typeof(ImportHtml))]
    [JsonSerializable(typeof(Undo))]
    [JsonSerializable(typeof(Redo))]
    [JsonSerializable(typeof(ClearUndoHistory))]
    [JsonSerializable(typeof(SelectAll))]
    [JsonSerializable(typeof(DeleteSelection))]
    [JsonSerializable(typeof(Copy))]
    [JsonSerializable(typeof(Cut))]
    [JsonSerializable(typeof(Paste))]
    [JsonSerializable(typeof(ToggleInlineMark))]
    [JsonSerializable(typeof(ClearInlineMarks))]
    [JsonSerializable(typeof(SetBlockKind))]
    [JsonSerializable(typeof(PromoteHeading))]
    [JsonSerializable(typeof(DemoteHeading))]
    [JsonSerializable(typeof(InsertParagraph))]
    [JsonSerializable(typeof(DeleteParagraph))]
    [JsonSerializable(typeof(DuplicateParagraph))]
    [JsonSerializable(typeof(BlockMenuClosed))]
    [JsonSerializable(typeof(InsertTable))]
    [JsonSerializable(typeof(EditTable))]
    [JsonSerializable(typeof(InsertImage))]
    [JsonSerializable(typeof(ReplaceImage))]
    [JsonSerializable(typeof(ApplyImageToolbarAction))]
    [JsonSerializable(typeof(SetImageZoom))]
    [JsonSerializable(typeof(Search))]
    [JsonSerializable(typeof(FindMatch))]
    [JsonSerializable(typeof(Replace))]
    [JsonSerializable(typeof(EndSearch))]
    [JsonSerializable(typeof(RevealHeading))]
    [JsonSerializable(typeof(ApplySettings))]
    [JsonSerializable(typeof(ApplyTheme))]
    [JsonSerializable(typeof(SetKeymap))]
    [JsonSerializable(typeof(ScrollTo))]
    [JsonSerializable(typeof(RefreshViewport))]
    // ── 事件 ──
    [JsonSerializable(typeof(LifecycleReady))]
    [JsonSerializable(typeof(LifecycleFault))]
    [JsonSerializable(typeof(DocChanged))]
    [JsonSerializable(typeof(DocumentLoaded))]
    [JsonSerializable(typeof(HistoryChanged))]
    [JsonSerializable(typeof(SelectionReport))]
    [JsonSerializable(typeof(MarksChanged))]
    [JsonSerializable(typeof(OutlineChanged))]
    [JsonSerializable(typeof(StatsChanged))]
    [JsonSerializable(typeof(SearchResultChanged))]
    [JsonSerializable(typeof(ViewportChanged))]
    [JsonSerializable(typeof(ShortcutPressed))]
    [JsonSerializable(typeof(LinkOpenRequested))]
    [JsonSerializable(typeof(BlockMenuRequested))]
    [JsonSerializable(typeof(FormatPickerRequested))]
    [JsonSerializable(typeof(ImageToolbarRequested))]
    [JsonSerializable(typeof(ImageEditorRequested))]
    [JsonSerializable(typeof(TableToolsRequested))]
    [JsonSerializable(typeof(TooltipRequested))]
    [JsonSerializable(typeof(TooltipDismissed))]
    // ── 请求与应答 ──
    [JsonSerializable(typeof(FlushDocument))]
    [JsonSerializable(typeof(DocVersion))]
    [JsonSerializable(typeof(DocGetText))]
    [JsonSerializable(typeof(DocText))]
    [JsonSerializable(typeof(RenderExportHtml))]
    [JsonSerializable(typeof(ExportHtml))]
    [JsonSerializable(typeof(ContextAt))]
    [JsonSerializable(typeof(RichSelection))]
    [JsonSerializable(typeof(TablePickSize))]
    [JsonSerializable(typeof(TableSize))]
    [JsonSerializable(typeof(ClipboardWrite))]
    [JsonSerializable(typeof(WireEmpty))]
    [JsonSerializable(typeof(ImageResolve))]
    [JsonSerializable(typeof(ImageResolved))]
    [JsonSerializable(typeof(WireErrorBody))]
    // ── 初始态 ──
    [JsonSerializable(typeof(EditorInitState))]
    public sealed partial class EditorWireJsonContext : JsonSerializerContext
    {
    }

    /// <summary>契约枚举在线上写成 camelCase 字符串；不接受整数，不认识的值按载荷无效处理。</summary>
    public sealed class CamelCaseEnumConverter<TEnum> : JsonStringEnumConverter<TEnum>
        where TEnum : struct, Enum
    {
        public CamelCaseEnumConverter()
            : base(JsonNamingPolicy.CamelCase, allowIntegerValues: false)
        {
        }
    }

    /// <summary><see cref="HeadingId"/> 在线上是不透明字符串。</summary>
    public sealed class HeadingIdJsonConverter : JsonConverter<HeadingId>
    {
        public override HeadingId Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
            reader.TokenType == JsonTokenType.String
                ? new HeadingId(reader.GetString()!)
                : throw new JsonException("A heading id must be a string.");

        public override void Write(Utf8JsonWriter writer, HeadingId value, JsonSerializerOptions options) =>
            writer.WriteStringValue(value.Value);
    }
}
