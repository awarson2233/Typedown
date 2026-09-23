using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

namespace Typedown.Core.Editor.Wire
{
    /// <summary>报文种类，即信封的 <c>k</c>。</summary>
    public enum EditorWireKind
    {
        /// <summary><c>cmd</c>：宿主→页面，即发即走。</summary>
        Command,

        /// <summary><c>evt</c>：页面→宿主，即发即走。</summary>
        Event,

        /// <summary><c>req</c>：需要对方应答。</summary>
        Request,

        /// <summary><c>res</c>：回应对方的 <c>req</c>。</summary>
        Response,
    }

    /// <summary>报文由哪一端发出。</summary>
    public enum EditorWireDirection
    {
        HostToPage,
        PageToHost,
    }

    /// <summary>
    /// 类型名表中的一项：线上类型名 <paramref name="Name"/> 与载荷 record <paramref name="ClrType"/> 一一对应。
    /// 请求另带应答载荷的元数据；<paramref name="ResultNullable"/> 为真时应答的 <c>p</c> 可以是 <c>null</c>。
    /// </summary>
    public sealed record EditorWireType(
        string Name,
        Type ClrType,
        EditorWireKind Kind,
        EditorWireDirection Direction,
        JsonTypeInfo Payload,
        JsonTypeInfo? Result,
        bool ResultNullable);

    /// <summary>
    /// 类型名 ↔ 载荷 record 的双射，外加每个类型的 <see cref="JsonTypeInfo"/>。契约 record 不标注自己在线上叫什么，
    /// 名字只在这张表里出现；页面侧的 <c>bridge/protocol.ts</c> 是同一张表的 TypeScript 版本。
    /// </summary>
    public static class EditorWireTypes
    {
        /// <summary>线协议版本，写在初始态与 <c>lifecycle.ready</c> 里。</summary>
        public const int ProtocolVersion = 1;

        private static readonly JsonSerializerOptions options = CreateOptions();
        private static readonly EditorWireType[] all = CreateTable();
        private static readonly Dictionary<string, EditorWireType> byName = all.ToDictionary(x => x.Name, StringComparer.Ordinal);
        private static readonly Dictionary<Type, EditorWireType> byType = all.ToDictionary(x => x.ClrType);

        /// <summary>
        /// 由会话在契约与线上 record 之间转换、自身不上线的契约类型，值是对应的线上 record：
        /// 装载要补版本号，正文变化只带增量，选区事件多带正文偏移。
        /// </summary>
        public static IReadOnlyDictionary<Type, Type> SessionTranslated { get; } = new Dictionary<Type, Type>
        {
            [typeof(LoadDocument)] = typeof(DocLoad),
            [typeof(DocumentChanged)] = typeof(DocChanged),
            [typeof(SelectionChanged)] = typeof(SelectionReport),
        };

        /// <summary>
        /// 线协议的序列化选项：<see cref="EditorWireJsonContext"/> 的源生成元数据，中文与 HTML 原样输出，
        /// 非空且没有默认值的构造参数是必填字段，缺失时反序列化失败（按 <see cref="EditorWireError.InvalidPayload"/> 处理）。
        /// </summary>
        public static JsonSerializerOptions Options => options;

        public static IReadOnlyList<EditorWireType> All => all;

        public static JsonTypeInfo<EditorInitState> InitState { get; } = (JsonTypeInfo<EditorInitState>)options.GetTypeInfo(typeof(EditorInitState));

        internal static JsonTypeInfo<WireErrorBody> ErrorBody { get; } = (JsonTypeInfo<WireErrorBody>)options.GetTypeInfo(typeof(WireErrorBody));

        public static bool TryGet(string name, out EditorWireType type) => byName.TryGetValue(name, out type!);

        public static bool TryGet(Type clrType, out EditorWireType type) => byType.TryGetValue(clrType, out type!);

        public static EditorWireType Get(Type clrType) =>
            TryGet(clrType, out var type)
                ? type
                : throw new NotSupportedException($"{clrType.Name} has no wire type name.");

        /// <summary>契约枚举值在线上的写法（camelCase 字符串，键盘枚举是整数）。</summary>
        public static string ToWireValue<TEnum>(TEnum value) where TEnum : struct, Enum =>
            JsonSerializer.Serialize(value, (JsonTypeInfo<TEnum>)options.GetTypeInfo(typeof(TEnum)));

        private static JsonSerializerOptions CreateOptions()
        {
            var result = new JsonSerializerOptions(EditorWireJsonContext.Default.Options)
            {
                // 报文只经 postMessage 交给 JSON.parse，不嵌入 HTML。
                Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
                TypeInfoResolver = EditorWireJsonContext.Default.WithAddedModifier(RequireNonNullableParameters),
            };
            result.MakeReadOnly();
            return result;
        }

        /// <summary>构造参数非空且没有默认值即为必填；可空参数与带默认值的参数缺失时取默认值。</summary>
        private static void RequireNonNullableParameters(JsonTypeInfo typeInfo)
        {
            foreach (var property in typeInfo.Properties)
            {
                if (property.AssociatedParameter is { HasDefaultValue: false, IsNullable: false })
                {
                    property.IsRequired = true;
                }
            }
        }

        private static EditorWireType[] CreateTable() =>
        [
            // doc
            Command<DocLoad>("doc.load"),
            Command<ImportHtml>("doc.importHtml"),
            Event<DocChanged>("doc.changed"),
            Event<DocumentLoaded>("doc.rendered"),
            Request<FlushDocument, DocVersion>("doc.flush", EditorWireDirection.HostToPage),
            Request<DocGetText, DocText>("doc.getText", EditorWireDirection.HostToPage),
            // lifecycle
            Event<LifecycleReady>("lifecycle.ready"),
            Event<LifecycleFault>("lifecycle.fault"),
            // history
            Command<Undo>("history.undo"),
            Command<Redo>("history.redo"),
            Command<ClearUndoHistory>("history.clear"),
            Event<HistoryChanged>("history.changed"),
            // selection
            Command<SelectAll>("selection.selectAll"),
            Command<DeleteSelection>("selection.delete"),
            Event<SelectionReport>("selection.changed"),
            Event<MarksChanged>("selection.marks"),
            Request<ContextAt, RichSelection>("selection.contextAt", EditorWireDirection.HostToPage, resultNullable: true),
            // clipboard
            Command<Copy>("clipboard.copy"),
            Command<Cut>("clipboard.cut"),
            Command<Paste>("clipboard.paste"),
            Request<ClipboardWrite, WireEmpty>("clipboard.write", EditorWireDirection.PageToHost),
            // format
            Command<ToggleInlineMark>("format.toggle"),
            Command<ClearInlineMarks>("format.clear"),
            // block
            Command<SetBlockKind>("block.setKind"),
            Command<PromoteHeading>("block.promote"),
            Command<DemoteHeading>("block.demote"),
            Command<InsertParagraph>("block.insert"),
            Command<DeleteParagraph>("block.delete"),
            Command<DuplicateParagraph>("block.duplicate"),
            Command<BlockMenuClosed>("block.menuClosed"),
            // table
            Command<InsertTable>("table.insert"),
            Command<EditTable>("table.edit"),
            Request<TablePickSize, TableSize>("table.pickSize", EditorWireDirection.PageToHost, resultNullable: true),
            // image
            Command<InsertImage>("image.insert"),
            Command<ReplaceImage>("image.replace"),
            Command<ApplyImageToolbarAction>("image.toolbarAction"),
            Command<SetImageZoom>("image.zoom"),
            Request<ImageResolve, ImageResolved>("image.resolve", EditorWireDirection.PageToHost, resultNullable: true),
            // search
            Command<Search>("search.set"),
            Command<FindMatch>("search.step"),
            Command<Replace>("search.replace"),
            Command<EndSearch>("search.end"),
            Event<SearchResultChanged>("search.result"),
            // outline / stats
            Command<RevealHeading>("outline.reveal"),
            Event<OutlineChanged>("outline.changed"),
            Event<StatsChanged>("stats.changed"),
            // export
            Request<RenderExportHtml, ExportHtml>("export.renderHtml", EditorWireDirection.HostToPage),
            // view
            Command<ApplySettings>("view.settings"),
            Command<ApplyTheme>("view.theme"),
            Command<SetKeymap>("view.keymap"),
            Command<ScrollTo>("view.scrollTo"),
            Command<RefreshViewport>("view.refreshViewport"),
            Event<ViewportChanged>("view.viewport"),
            Event<ShortcutPressed>("view.shortcut"),
            Event<LinkOpenRequested>("view.openLink"),
            // float
            Event<BlockMenuRequested>("float.blockMenu"),
            Event<FormatPickerRequested>("float.formatPicker"),
            Event<ImageToolbarRequested>("float.imageToolbar"),
            Event<ImageEditorRequested>("float.imageEditor"),
            Event<TableToolsRequested>("float.tableTools"),
            Event<TooltipRequested>("float.tooltip"),
            Event<TooltipDismissed>("float.tooltipDismissed"),
        ];

        private static EditorWireType Command<T>(string name) =>
            new(name, typeof(T), EditorWireKind.Command, EditorWireDirection.HostToPage, TypeInfo(typeof(T)), null, false);

        private static EditorWireType Event<T>(string name) =>
            new(name, typeof(T), EditorWireKind.Event, EditorWireDirection.PageToHost, TypeInfo(typeof(T)), null, false);

        private static EditorWireType Request<T, TResult>(string name, EditorWireDirection direction, bool resultNullable = false) =>
            new(name, typeof(T), EditorWireKind.Request, direction, TypeInfo(typeof(T)), TypeInfo(typeof(TResult)), resultNullable);

        private static JsonTypeInfo TypeInfo(Type type)
        {
            try
            {
                return options.GetTypeInfo(type);
            }
            catch (NotSupportedException ex)
            {
                throw new NotSupportedException($"{type.Name} has no JsonTypeInfo in {nameof(EditorWireJsonContext)}.", ex);
            }
        }
    }
}
