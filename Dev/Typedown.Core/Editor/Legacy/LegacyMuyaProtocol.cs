using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using Typedown.Core.Models;

namespace Typedown.Core.Editor.Legacy
{
    /// <summary>
    /// 旧 Muya 页面协议的无状态编解码：类型化命令 ⇄ 字符串消息名 + JSON 载荷。
    /// 序列化全部走 <see cref="LegacyMuyaJsonContext"/> 源生成元数据；页面用 <c>JSON.parse</c> 读取，
    /// 所以只约定字段名（camelCase，字典键同样 camelCase）与值的类型，不约定字节形态。
    /// </summary>
    public static class LegacyMuyaProtocol
    {
        private static readonly JsonSerializerOptions serializerOptions = new()
        {
            // 正文里的中文与 HTML 原样输出；报文只经 postMessage 交给 JSON.parse，不嵌入 HTML。
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DictionaryKeyPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true,
            MaxDepth = 256,
        };

        private static readonly LegacyMuyaJsonContext json = new(serializerOptions);

        private static readonly JsonWriterOptions writerOptions = new()
        {
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            MaxDepth = 256,
        };

        private static readonly JsonDocumentOptions documentOptions = new() { MaxDepth = 256 };

        // ── 宿主 → 页面：命令 ──────────────────────────────────────────────

        /// <summary>
        /// 把命令翻译成一条页面消息。撤销、重做、清空历史由适配器在宿主侧完成，没有对应的页面消息，返回 <c>null</c>。
        /// </summary>
        /// <param name="searchSelection">页面上次报来的选区对象（Muya 的 selection 或 CodeMirror 的 cursor），查找时原样带回。</param>
        /// <param name="imageStyle">最近一次打开图片工具条时那张图的 style 属性。</param>
        public static string? EncodeCommand(EditorCommand command, JsonElement searchSelection = default, string? imageStyle = null)
        {
            ArgumentNullException.ThrowIfNull(command);
            return command switch
            {
                LoadDocument c => Message("LoadFile", new LoadFileArgs(c.Text, c.BasePath), json.LoadFileArgs),
                ImportHtml c => Message("ImportFile", new ImportFileArgs("html", c.Html), json.ImportFileArgs),
                Undo or Redo or ClearUndoHistory => null,
                SelectAll => NullMessage("SelectAll"),
                DeleteSelection => NullMessage("DeleteSelection"),
                Copy c => Message("Copy", new ClipboardTypeArgs(LegacyMuyaVocabulary.GetCopyTypeName(c.Format)), json.ClipboardTypeArgs),
                Cut => Message("Cut", new ClipboardTypeArgs("normal"), json.ClipboardTypeArgs),
                Paste c => Message("Paste", new PasteArgs(LegacyMuyaVocabulary.GetPasteTypeName(c.Format), c.Text, c.Html), json.PasteArgs),
                ToggleInlineMark c => Message("Format", LegacyMuyaVocabulary.GetFormatName(c.Mark), json.String),
                ClearInlineMarks => Message("Format", LegacyMuyaVocabulary.ClearFormatName, json.String),
                SetBlockKind c => LegacyMuyaVocabulary.TryGetParagraphName(c.Kind, out var paragraph)
                    ? Message("UpdateParagraph", paragraph, json.String)
                    : null,
                PromoteHeading => Message("UpdateParagraph", LegacyMuyaVocabulary.PromoteHeadingName, json.String),
                DemoteHeading => Message("UpdateParagraph", LegacyMuyaVocabulary.DemoteHeadingName, json.String),
                InsertParagraph c => Message("InsertParagraph", c.Position == ParagraphPosition.Before ? "before" : "after", json.String),
                DeleteParagraph => NullMessage("DeleteParagraph"),
                DuplicateParagraph => NullMessage("Duplicate"),
                BlockMenuClosed => NullMessage("FrontMenuClosed"),
                InsertTable c => Message("InsertTable", new InsertTableArgs(c.Rows, c.Columns), json.InsertTableArgs),
                EditTable c => EncodeEditTable(c.Edit),
                InsertImage c => Message("InsertImage", new InsertImageArgs(c.Src, c.Title, c.Alt), json.InsertImageArgs),
                ReplaceImage c => Message(
                    "ReplaceImage",
                    new ReplaceImageArgs(c.Src, c.Alt, c.Title, c.Target == ImageTarget.Selected),
                    json.ReplaceImageArgs),
                ApplyImageToolbarAction c => Message(
                    "ImageEditToolbarClick",
                    new ImageToolbarClickArgs(LegacyMuyaVocabulary.GetImageToolbarActionName(c.Action), null, null),
                    json.ImageToolbarClickArgs),
                SetImageZoom c => Message(
                    "ImageEditToolbarClick",
                    new ImageToolbarClickArgs("updateImage", "style", LegacyMuyaVocabulary.ComposeZoomStyle(imageStyle, c.Percent)),
                    json.ImageToolbarClickArgs),
                Search c => Message(
                    "Search",
                    new SearchArgs(c.Query, new SearchOptionArgs(
                        c.Options.CaseSensitive,
                        c.Options.WholeWord,
                        c.Options.Regex,
                        searchSelection.ValueKind == JsonValueKind.Undefined ? EmptyObject : searchSelection)),
                    json.SearchArgs),
                FindMatch c => Message("Find", new FindArgs(c.Direction == SearchDirection.Next ? "next" : "prev"), json.FindArgs),
                Replace c => Message(
                    "Replace",
                    new ReplaceArgs(c.Query, c.Replacement, new ReplaceOptionArgs(!c.All, c.Options.CaseSensitive, c.Options.WholeWord, c.Options.Regex)),
                    json.ReplaceArgs),
                EndSearch => Message("SearchOpenChange", new SearchOpenChangeArgs(0), json.SearchOpenChangeArgs),
                RevealHeading c => Message("ScrollTo", new ScrollToArgs(c.Id.Value), json.ScrollToArgs),
                ApplySettings c => Message("SettingsChanged", ToSettingsChangedArgs(c.Changes), json.SettingsChangedArgs),
                ApplyTheme c => Message("ThemeChanged", ToThemeArgs(c.Theme), json.ThemeArgs),
                SetKeymap c => Message(
                    "SetShortcuts",
                    new ShortcutsArgs(c.Chords.Select(x => new ChordArgs((int)x.Key, (int)x.Modifiers)).ToList()),
                    json.ShortcutsArgs),
                ScrollTo c => Message("OnScroll", new ScrollArgs(c.X, c.Y), json.ScrollArgs),
                RefreshViewport => NullMessage("RefreshScrollState"),
                _ => throw new NotSupportedException($"Editor command {command.GetType().Name} has no legacy Muya translation."),
            };
        }

        /// <summary>整篇替换正文并恢复光标（撤销、重做、崩溃恢复共用）。</summary>
        public static string EncodeSetMarkdown(string? text, CursorState? cursor, string basePath) =>
            Message("SetMarkdown", new SetMarkdownArgs(text, cursor, basePath), json.SetMarkdownArgs);

        /// <summary>请页面生成导出 HTML；页面在回调里把 context 原样带回，宿主据 <paramref name="requestId"/> 找回请求。</summary>
        public static string EncodeExportRequest(RenderExportHtml request, long requestId)
        {
            ArgumentNullException.ThrowIfNull(request);
            var options = request.Options is { } o
                ? new ExportOptionsArgs(o.ExtraHead, o.ExtraBody, o.Header, o.Footer)
                : null;
            return Message(
                "Export",
                new ExportArgs(
                    request.Purpose == ExportPurpose.Print ? "print" : "export",
                    request.Title,
                    new ExportContextArgs(requestId),
                    request.BasePath,
                    options),
                json.ExportArgs);
        }

        // ── 宿主 → 页面：invoke 应答 ──────────────────────────────────────

        public static string EncodeNullReply(string id) => Reply(id, writer => writer.WriteNullValue());

        public static string EncodeBoolReply(string id, bool value) => Reply(id, writer => writer.WriteBooleanValue(value));

        public static string EncodeThemeReply(string id, EditorTheme theme) => Reply(id, ToThemeArgs(theme), json.ThemeArgs);

        public static string EncodeTableSizeReply(string id, TableSize? size) => size is { } s
            ? Reply(id, new TableSizeArgs(s.Rows, s.Columns), json.TableSizeArgs)
            : EncodeNullReply(id);

        /// <summary>字符串资源表。键按旧序列化器的规则转成 camelCase，页面据此拼 CSS 变量名。</summary>
        public static string EncodeStringResourcesReply(string id, IReadOnlyDictionary<string, string> resources) =>
            Reply(id, new Dictionary<string, string>(resources), json.DictionaryStringString);

        /// <summary>GetSettings 应答：全部编辑器设置 + 正文 + 图片基准目录。</summary>
        public static string EncodeStartupReply(string id, EditorSettings settings, string markdown, string basePath)
        {
            ArgumentNullException.ThrowIfNull(settings);
            var reply = new StartupReply(
                settings.SourceCode.GetValueOrDefault(),
                settings.Typewriter.GetValueOrDefault(),
                settings.FocusMode.GetValueOrDefault(),
                settings.SearchIsCaseSensitive.GetValueOrDefault(),
                settings.SearchIsRegexp.GetValueOrDefault(),
                settings.SearchIsWholeWord.GetValueOrDefault(),
                settings.FontSize.GetValueOrDefault(),
                settings.LineHeight.GetValueOrDefault(),
                settings.AutoPairBracket.GetValueOrDefault(),
                settings.AutoPairQuote.GetValueOrDefault(),
                settings.TrimUnnecessaryCodeBlockEmptyLines.GetValueOrDefault(),
                settings.PreferLooseListItem.GetValueOrDefault(),
                settings.AutoPairMarkdownSyntax.GetValueOrDefault(),
                settings.EditorAreaWidth ?? string.Empty,
                settings.TabSize.GetValueOrDefault(),
                settings.SpellcheckEnabled.GetValueOrDefault(),
                markdown,
                basePath);
            return Reply(id, reply, json.StartupReply);
        }

        public static string EncodeErrorReply(string id, string message)
        {
            return Write(writer =>
            {
                writer.WriteStartObject();
                writer.WriteString("name", id);
                writer.WritePropertyName("args");
                writer.WriteStartObject();
                writer.WriteNumber("code", 1);
                writer.WriteString("msg", message);
                writer.WriteEndObject();
                writer.WriteEndObject();
            });
        }

        // ── 页面 → 宿主 ─────────────────────────────────────────────────

        /// <summary>解析信封；不是合法 JSON 对象或缺少 type 时返回 <c>false</c>，与此前一样静默丢弃。</summary>
        public static bool TryParseEnvelope(string? message, out LegacyEnvelope envelope)
        {
            envelope = null!;
            if (string.IsNullOrEmpty(message))
            {
                return false;
            }

            try
            {
                using var document = JsonDocument.Parse(message, documentOptions);
                var root = document.RootElement;
                if (root.ValueKind != JsonValueKind.Object)
                {
                    return false;
                }

                var type = ReadString(root, "type");
                if (string.IsNullOrWhiteSpace(type))
                {
                    return false;
                }

                var args = root.TryGetProperty("args", out var argsElement) ? argsElement.Clone() : default;
                envelope = new LegacyEnvelope(
                    type,
                    ReadString(root, "id") ?? string.Empty,
                    ReadString(root, "name") ?? string.Empty,
                    args,
                    root.TryGetProperty("diff", out var diff) && diff.ValueKind == JsonValueKind.True,
                    ReadInt32(root, "start"),
                    ReadInt32(root, "end"));
                return true;
            }
            catch (JsonException)
            {
                return false;
            }
        }

        /// <summary>把一段 JSON 文本解析成独立的元素（diffmsg 拼回全文后用）。</summary>
        public static bool TryParseArgs(string text, out JsonElement args)
        {
            try
            {
                using var document = JsonDocument.Parse(text, documentOptions);
                args = document.RootElement.Clone();
                return true;
            }
            catch (JsonException)
            {
                args = default;
                return false;
            }
        }

        /// <summary>
        /// 解码一条页面消息。未知消息名、页面已不再发送的消息名以及载荷残缺的消息返回 <c>null</c>。
        /// </summary>
        public static LegacyInbound? DecodeMessage(string name, JsonElement args)
        {
            try
            {
                return name switch
                {
                    "MarkdownChange" => new LegacyTextChanged(Deserialize(args, json.MarkdownPayload)?.Text ?? string.Empty),
                    "FileLoaded" => new LegacyFileLoaded(Deserialize(args, json.MarkdownPayload)?.Text ?? string.Empty),
                    "CursorChange" => Deserialize(args, json.CursorPayload)?.Cursor is { } cursor ? new LegacyCursorChanged(cursor) : null,
                    "StateChange" => DecodeStateChange(args),
                    "SelectionChange" => DecodeSelectionChange(args),
                    "CodeMirrorSelectionChange" => DecodeCodeMirrorSelectionChange(args),
                    "SelectionFormats" => new LegacyTypedEvent(new MarksChanged(LegacyMuyaVocabulary.ToInlineMarks(
                        (Deserialize(args, json.SelectionFormatsPayload)?.Formats ?? [])
                            .Select(x => (x.Type, x.Tag))))),
                    "OnScroll" => Deserialize(args, json.ScrollStatePayload) is { } s
                        ? new LegacyTypedEvent(new ViewportChanged(s.ViewportWidth, s.ViewportHeight, s.MaximumX, s.MaximumY, s.ScrollX, s.ScrollY))
                        : null,
                    "KeyDown" => Deserialize(args, json.KeyDownPayload) is { } k
                        ? new LegacyTypedEvent(new ShortcutPressed((KeyboardKey)k.Key, (KeyboardModifiers)k.Modifiers))
                        : null,
                    "OpenURI" => Deserialize(args, json.OpenUriPayload)?.Uri is { } uri && !string.IsNullOrWhiteSpace(uri)
                        ? new LegacyTypedEvent(new LinkOpenRequested(uri))
                        : null,
                    "OpenFrontMenu" => new LegacyTypedEvent(new BlockMenuRequested(ReadFloat(args)?.Anchor)),
                    "OpenFormatPicker" => new LegacyTypedEvent(new FormatPickerRequested(ReadFloat(args)?.Anchor)),
                    "OpenImageSelector" => DecodeImageSelector(args),
                    "OpenImageToolbar" => DecodeImageToolbar(args),
                    "OpenTableTools" => DecodeTableTools(args),
                    "OpenToolTip" => DecodeToolTip(args),
                    _ => null,
                };
            }
            catch (Exception ex) when (ex is JsonException or InvalidOperationException or FormatException)
            {
                return null;
            }
        }

        public static IReadOnlyList<string> ReadStringResourceNames(JsonElement args) =>
            Deserialize(args, json.StringResourcesRequest)?.Names?.Where(x => x is not null).ToList() ?? [];

        /// <summary>ExportCallback / PrintHTML 的载荷：生成的 HTML 与宿主当初下发的请求号。</summary>
        public static (string? Html, long? RequestId) ReadExportCallback(JsonElement args)
        {
            var payload = Deserialize(args, json.ExportCallbackRequest);
            return (payload?.Html, payload?.Context?.RequestId);
        }

        /// <summary>SetClipboard 的载荷：MIME 类型与内容。</summary>
        public static (string? Type, string Data) ReadClipboardWrite(JsonElement args)
        {
            if (args.ValueKind != JsonValueKind.Object)
            {
                return (null, string.Empty);
            }

            var type = args.TryGetProperty("type", out var typeElement) ? ToLegacyString(typeElement) : null;
            var data = args.TryGetProperty("data", out var dataElement) ? ToLegacyString(dataElement) : null;
            return (type, data ?? string.Empty);
        }

        /// <summary>参数本身是一个字符串的 invoke（UnhandledException、OpenNewWindow）。</summary>
        public static string? ReadStringArgument(JsonElement args) =>
            args.ValueKind == JsonValueKind.String ? args.GetString() : null;

        // ── 解码细节 ────────────────────────────────────────────────────

        private static LegacyInbound DecodeStateChange(JsonElement args)
        {
            var state = Deserialize(args, json.StatePayload)?.State;
            if (state is null)
            {
                return new LegacyStateChanged([]);
            }

            var items = (state.Toc ?? []).Select(ToOutlineItem).ToList();
            var current = state.Cur is { } cur ? ToOutlineItem(cur) : null;
            return new LegacyStateChanged(
            [
                new StatsChanged(state.WordCount?.Character ?? 0, state.WordCount?.Word ?? 0),
                new OutlineChanged(items, current),
            ]);
        }

        private static OutlineItem ToOutlineItem(TocItemPayload item) =>
            new(new HeadingId(ToLegacyString(item.Slug) ?? string.Empty), item.Lvl, item.Content ?? string.Empty);

        private static LegacyInbound DecodeSelectionChange(JsonElement args)
        {
            var payload = Deserialize(args, json.SelectionPayload);
            var selection = payload is not null && payload.Selection.ValueKind == JsonValueKind.Object
                ? payload.Selection
                : EmptyObject;
            var menuState = payload?.MenuState;
            var block = menuState is null
                ? BlockContext.Empty
                : LegacyMuyaVocabulary.ToBlockContext(
                    (IReadOnlyCollection<string>?)menuState.Affiliation?.Keys ?? [],
                    menuState.IsTaskList,
                    menuState.IsTable,
                    menuState.IsFootnote,
                    menuState.IsCodeFences,
                    menuState.IsCodeContent,
                    menuState.IsMultiline,
                    menuState.IsDisabled);
            var rich = new RichSelection(block, ReadSelectedImage(selection));
            var selectionChanged = new SelectionChanged(HasMuyaTextSelection(selection), payload?.SelectionText ?? string.Empty, rich);
            return new LegacySelectionChanged(selectionChanged, selection, SourceMode: false);
        }

        /// <summary>
        /// Muya 的选区是否选中了文字：优先看 isCollapsed，没有时比较首尾偏移与首尾路径。
        /// 比较方式与此前逐字相同（偏移按字符串比较，路径按 JSON 结构比较）。
        /// </summary>
        private static bool HasMuyaTextSelection(JsonElement selection)
        {
            if (selection.TryGetProperty("isCollapsed", out var collapsed)
                && collapsed.ValueKind is JsonValueKind.True or JsonValueKind.False)
            {
                return !collapsed.GetBoolean();
            }

            var startOffset = ReadOffset(selection, "start");
            var endOffset = ReadOffset(selection, "end");
            if (!string.Equals(startOffset, endOffset, StringComparison.Ordinal))
            {
                return true;
            }

            var hasAnchorPath = selection.TryGetProperty("anchorPath", out var anchorPath);
            var hasFocusPath = selection.TryGetProperty("focusPath", out var focusPath);
            if (hasAnchorPath != hasFocusPath)
            {
                return true;
            }

            return hasAnchorPath && !JsonElement.DeepEquals(anchorPath, focusPath);
        }

        private static string? ReadOffset(JsonElement selection, string edge) =>
            selection.TryGetProperty(edge, out var position)
            && position.ValueKind == JsonValueKind.Object
            && position.TryGetProperty("offset", out var offset)
                ? ToLegacyString(offset)
                : null;

        private static ImageInfo? ReadSelectedImage(JsonElement selection)
        {
            if (!selection.TryGetProperty("selectedImage", out var image) || !HasValues(image))
            {
                return null;
            }

            var token = image.ValueKind == JsonValueKind.Object && image.TryGetProperty("token", out var t) && t.ValueKind == JsonValueKind.Object
                ? t
                : default;
            return new ImageInfo(ReadTokenString(token, "src"), ReadTokenString(token, "alt"), ReadTokenString(token, "title"));
        }

        private static string ReadTokenString(JsonElement token, string name) =>
            token.ValueKind == JsonValueKind.Object && token.TryGetProperty(name, out var value)
                ? ToLegacyString(value) ?? string.Empty
                : string.Empty;

        private static LegacyInbound DecodeCodeMirrorSelectionChange(JsonElement args)
        {
            var payload = Deserialize(args, json.CodeMirrorSelectionPayload);
            var cursor = payload is not null && payload.Cursor.ValueKind == JsonValueKind.Object ? payload.Cursor : EmptyObject;
            if (!cursor.TryGetProperty("anchor", out var anchor) || !cursor.TryGetProperty("head", out var head))
            {
                return new LegacySelectionChanged(new SelectionChanged(false, string.Empty, null), cursor, SourceMode: true);
            }

            var hasText = ReadInt32(anchor, "line") != ReadInt32(head, "line") || ReadInt32(anchor, "ch") != ReadInt32(head, "ch");
            return new LegacySelectionChanged(
                new SelectionChanged(hasText, payload?.SelectionText ?? string.Empty, null),
                cursor,
                SourceMode: true);
        }

        private static LegacyInbound DecodeImageSelector(JsonElement args)
        {
            var payload = ReadFloat(args);
            var info = payload?.Payload.ImageInfo;
            return new LegacyTypedEvent(new ImageEditorRequested(
                payload?.Anchor,
                new ImageInfo(info?.Src ?? string.Empty, info?.Alt ?? string.Empty, info?.Title ?? string.Empty)));
        }

        private static LegacyInbound DecodeImageToolbar(JsonElement args)
        {
            var payload = ReadFloat(args);
            var attrs = payload?.Payload.Attrs ?? default(JsonElement);
            var style = attrs.ValueKind == JsonValueKind.Object && attrs.TryGetProperty("style", out var styleElement)
                ? ToLegacyString(styleElement)
                : null;
            return new LegacyImageToolbarOpened(new ImageToolbarRequested(payload?.Anchor), style);
        }

        private static LegacyInbound DecodeTableTools(JsonElement args)
        {
            var payload = ReadFloat(args);
            var axis = payload?.Payload.TableInfo?.BarType == "bottom" ? TableAxis.Column : TableAxis.Row;
            return new LegacyTypedEvent(new TableToolsRequested(payload?.Anchor, axis));
        }

        private static LegacyInbound? DecodeToolTip(JsonElement args)
        {
            var payload = ReadFloat(args);
            if (payload?.Payload.Open != true)
            {
                return new LegacyTypedEvent(new TooltipDismissed());
            }

            // 页面报的是资源键；认不出的提示整条丢弃。
            return LegacyMuyaVocabulary.TryParseTooltip(payload.Payload.Tooltip, out var kind)
                ? new LegacyTypedEvent(new TooltipRequested(kind, payload.Anchor))
                : null;
        }

        private static FloatRead? ReadFloat(JsonElement args)
        {
            if (Deserialize(args, json.FloatPayload) is not { } payload)
            {
                return null;
            }

            var anchor = payload.BoundingClientRect is { } r ? new EditorRect(r.X, r.Y, r.Width, r.Height) : (EditorRect?)null;
            return new FloatRead(payload, anchor);
        }

        private sealed record FloatRead(FloatPayload Payload, EditorRect? Anchor);

        // ── 共用工具 ────────────────────────────────────────────────────

        private static readonly JsonElement EmptyObject = CreateEmptyObject();

        private static JsonElement CreateEmptyObject()
        {
            using var document = JsonDocument.Parse("{}");
            return document.RootElement.Clone();
        }

        private static T? Deserialize<T>(JsonElement args, JsonTypeInfo<T> typeInfo) where T : class =>
            args.ValueKind is JsonValueKind.Object ? args.Deserialize(typeInfo) : null;

        private static bool HasValues(JsonElement element) => element.ValueKind switch
        {
            JsonValueKind.Object => element.EnumerateObject().Any(),
            JsonValueKind.Array => element.GetArrayLength() > 0,
            _ => false,
        };

        /// <summary>按旧实现取 JSON 值的字符串形式：字符串取值、null 为空串、其余取原文。</summary>
        private static string? ToLegacyString(JsonElement element) => element.ValueKind switch
        {
            JsonValueKind.Undefined => null,
            JsonValueKind.Null => string.Empty,
            JsonValueKind.String => element.GetString(),
            _ => element.GetRawText(),
        };

        private static string? ReadString(JsonElement element, string name) =>
            element.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String ? value.GetString() : null;

        private static int ReadInt32(JsonElement element, string name) =>
            element.ValueKind == JsonValueKind.Object
            && element.TryGetProperty(name, out var value)
            && value.ValueKind == JsonValueKind.Number
            && value.TryGetInt32(out var number)
                ? number
                : 0;

        private static SettingsChangedArgs ToSettingsChangedArgs(EditorSettings s) => new()
        {
            SourceCode = s.SourceCode,
            Typewriter = s.Typewriter,
            FocusMode = s.FocusMode,
            SearchIsCaseSensitive = s.SearchIsCaseSensitive,
            SearchIsRegexp = s.SearchIsRegexp,
            SearchIsWholeWord = s.SearchIsWholeWord,
            FontSize = s.FontSize,
            LineHeight = s.LineHeight,
            AutoPairBracket = s.AutoPairBracket,
            AutoPairQuote = s.AutoPairQuote,
            TrimUnnecessaryCodeBlockEmptyLines = s.TrimUnnecessaryCodeBlockEmptyLines,
            PreferLooseListItem = s.PreferLooseListItem,
            AutoPairMarkdownSyntax = s.AutoPairMarkdownSyntax,
            EditorAreaWidth = s.EditorAreaWidth,
            TabSize = s.TabSize,
            SpellcheckEnabled = s.SpellcheckEnabled,
        };

        private static ThemeArgs ToThemeArgs(EditorTheme theme) => new(
            theme.IsDark ? "Dark" : "Light",
            new ColorArgs(theme.Accent.R, theme.Accent.G, theme.Accent.B, theme.Accent.A),
            new ColorArgs(theme.Background.R, theme.Background.G, theme.Background.B, theme.Background.A));

        private static string EncodeEditTable(TableEdit edit)
        {
            var (action, location, target) = LegacyMuyaVocabulary.GetTableEdit(edit);
            return Message("EditTable", new EditTableArgs(action, location, target), json.EditTableArgs);
        }

        private static string Message<T>(string name, T args, JsonTypeInfo<T> typeInfo)
        {
            return Write(writer =>
            {
                writer.WriteStartObject();
                writer.WriteString("name", name);
                writer.WritePropertyName("args");
                JsonSerializer.Serialize(writer, args, typeInfo);
                writer.WriteEndObject();
            });
        }

        private static string NullMessage(string name)
        {
            return Write(writer =>
            {
                writer.WriteStartObject();
                writer.WriteString("name", name);
                writer.WriteNull("args");
                writer.WriteEndObject();
            });
        }

        private static string Reply<T>(string id, T data, JsonTypeInfo<T> typeInfo) =>
            Reply(id, writer => JsonSerializer.Serialize(writer, data, typeInfo));

        private static string Reply(string id, Action<Utf8JsonWriter> writeData)
        {
            return Write(writer =>
            {
                writer.WriteStartObject();
                writer.WriteString("name", id);
                writer.WritePropertyName("args");
                writer.WriteStartObject();
                writer.WriteNumber("code", 0);
                writer.WritePropertyName("data");
                writeData(writer);
                writer.WriteEndObject();
                writer.WriteEndObject();
            });
        }

        private static string Write(Action<Utf8JsonWriter> write)
        {
            var buffer = new ArrayBufferWriter<byte>(256);
            using (var writer = new Utf8JsonWriter(buffer, writerOptions))
            {
                write(writer);
            }

            return Encoding.UTF8.GetString(buffer.WrittenSpan);
        }
    }
}
