using System;
using System.Buffers;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

namespace Typedown.Core.Editor.Wire
{
    /// <summary>一条命令、事件或请求：类型名表中的一项、请求 id 与载荷 record。</summary>
    public sealed record EditorWireMessage(EditorWireType Type, long? Id, object Payload);

    /// <summary>
    /// 新引擎线协议的编解码。信封 <c>{k, t, id?, p}</c> 用 <see cref="Utf8JsonWriter"/> / <see cref="Utf8JsonReader"/> 手写，
    /// 载荷按 <see cref="EditorWireTypes"/> 里的 <see cref="JsonTypeInfo"/> 读写，不依赖 STJ 多态，也不要求 <c>t</c> 出现在第一个字段。
    /// 这一层无状态、不认识 WebView2：会话把收到的报文字符串交给 <see cref="Decode"/>，把要发的报文字符串交给宿主。
    /// </summary>
    public static class EditorWireCodec
    {
        /// <summary>版本号与请求 id 的上限：JS <c>number</c> 能精确表示的最大整数。</summary>
        public const long MaxSafeInteger = (1L << 53) - 1;

        private static readonly JsonWriterOptions writerOptions = new()
        {
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            MaxDepth = 64,
        };

        private static readonly JsonReaderOptions readerOptions = new() { MaxDepth = 64 };

        // ── 宿主 → 页面 ─────────────────────────────────────────────────

        /// <summary>
        /// 编码一条契约命令。<see cref="LoadDocument"/> 要先经 <see cref="DocumentMirror.Load"/> 补上版本号，
        /// 再用 <see cref="EncodeDocLoad"/> 发出，这里不接受。
        /// </summary>
        public static string EncodeCommand(EditorCommand command)
        {
            ArgumentNullException.ThrowIfNull(command);
            if (command is LoadDocument)
            {
                throw new ArgumentException($"{nameof(LoadDocument)} needs a version: encode the result of {nameof(DocumentMirror)}.{nameof(DocumentMirror.Load)} with {nameof(EncodeDocLoad)}.", nameof(command));
            }

            return WriteMessage(new EditorWireMessage(RequireType(command.GetType(), EditorWireKind.Command), null, command));
        }

        public static string EncodeDocLoad(DocLoad load)
        {
            ArgumentNullException.ThrowIfNull(load);
            return WriteMessage(new EditorWireMessage(RequireType(typeof(DocLoad), EditorWireKind.Command), null, load));
        }

        /// <summary>编码一个契约请求；返回的调用对象带着报文和读应答的方法。</summary>
        public static EditorWireCall<TResult> EncodeRequest<TResult>(long id, EditorRequest<TResult> request)
        {
            ArgumentNullException.ThrowIfNull(request);
            Func<object?, TResult> project = request switch
            {
                FlushDocument => value => (TResult)(object)((DocVersion)value!).Version,
                RenderExportHtml => value => (TResult)(object)((ExportHtml)value!).Html,
                ContextAt => value => (TResult)value!,
                _ => throw new NotSupportedException($"Editor request {request.GetType().Name} has no wire encoding."),
            };
            return Call(id, request, project);
        }

        /// <summary>会话失步时取页面全文（<c>doc.getText</c>）。</summary>
        public static EditorWireCall<DocText> EncodeGetText(long id) => Call(id, new DocGetText(), value => (DocText)value!);

        /// <summary><c>table.pickSize</c> 的应答；<c>null</c> 表示用户取消。</summary>
        public static string EncodeTableSizeReply(long id, TableSize? size) =>
            EncodeResponse(id, EditorWireTypes.Get(typeof(TablePickSize)), size);

        /// <summary><c>clipboard.write</c> 的应答。</summary>
        public static string EncodeClipboardWriteReply(long id) =>
            EncodeResponse(id, EditorWireTypes.Get(typeof(ClipboardWrite)), new WireEmpty());

        /// <summary><c>image.resolve</c> 的应答；<c>null</c> 表示放弃插入。</summary>
        public static string EncodeImageResolveReply(long id, string? src) =>
            EncodeResponse(id, EditorWireTypes.Get(typeof(ImageResolve)), src is null ? null : new ImageResolved(src));

        /// <summary>失败应答 <c>{"k":"res","id":…,"ok":false,"err":{code, message}}</c>。</summary>
        public static string EncodeFailure(long id, EditorWireError error, string message)
        {
            RequireId(id);
            return Write(writer =>
            {
                writer.WriteStartObject();
                writer.WriteString("k", "res");
                writer.WriteNumber("id", id);
                writer.WriteBoolean("ok", false);
                writer.WritePropertyName("err");
                JsonSerializer.Serialize(writer, new WireErrorBody(error, message), EditorWireTypes.ErrorBody);
                writer.WriteEndObject();
            });
        }

        /// <summary>导航前注入页面的一行脚本：<c>window.__typedownInit = {...};</c>。</summary>
        public static string EncodeInitScript(EditorInitState state)
        {
            ArgumentNullException.ThrowIfNull(state);
            return $"window.__typedownInit = {EncodeInitState(state)};";
        }

        public static string EncodeInitState(EditorInitState state)
        {
            ArgumentNullException.ThrowIfNull(state);
            return Write(writer => JsonSerializer.Serialize(writer, state, EditorWireTypes.InitState));
        }

        // ── 页面 → 宿主 ─────────────────────────────────────────────────

        /// <summary>
        /// 解码一条页面发来的报文，结果按会话要做的动作分类（见 <see cref="EditorWireInbound"/> 的派生类型）。
        /// 从不抛异常：残缺、不认识或方向不对的报文一律是 <see cref="WireRejected"/>。
        /// </summary>
        public static EditorWireInbound Decode(string? message)
        {
            if (!TryParseEnvelope(message, out var envelope, out var detail))
            {
                return new WireRejected(null, null, EditorWireError.InvalidPayload, detail);
            }

            if (envelope.Kind == EditorWireKind.Response)
            {
                return DecodeReply(envelope);
            }

            if (!TryReadMessage(envelope, out var read, out var rejected))
            {
                return rejected;
            }

            if (read.Type.Direction != EditorWireDirection.PageToHost)
            {
                return new WireRejected(envelope.Kind, envelope.Id, EditorWireError.UnknownType, $"'{read.Type.Name}' is not sent by the page.");
            }

            return read.Payload switch
            {
                EditorEvent editorEvent => new WireEvent(editorEvent),
                EditorWireSignal signal => new WireSignal(signal),
                EditorWireHostRequest request => new WireHostRequest(read.Id!.Value, request),
                _ => new WireRejected(envelope.Kind, envelope.Id, EditorWireError.UnknownType, $"'{read.Type.Name}' has no inbound handling."),
            };
        }

        // ── 通用层（会话的类型化接口与契约测试共用） ───────────────────────

        /// <summary>读一条命令、事件或请求（任一方向）；应答报文请用 <see cref="Decode"/>。</summary>
        public static bool TryReadMessage(string? message, out EditorWireMessage result, out WireRejected rejected)
        {
            result = null!;
            if (!TryParseEnvelope(message, out var envelope, out var detail))
            {
                rejected = new WireRejected(null, null, EditorWireError.InvalidPayload, detail);
                return false;
            }

            if (envelope.Kind == EditorWireKind.Response)
            {
                rejected = new WireRejected(envelope.Kind, envelope.Id, EditorWireError.InvalidPayload, "A response is not a message.");
                return false;
            }

            return TryReadMessage(envelope, out result, out rejected);
        }

        /// <summary>写一条命令、事件或请求；载荷必须正是该类型名对应的 record，请求必须带 id。</summary>
        public static string WriteMessage(EditorWireMessage message)
        {
            ArgumentNullException.ThrowIfNull(message);
            var type = message.Type;
            if (message.Payload.GetType() != type.ClrType)
            {
                throw new ArgumentException($"'{type.Name}' carries {type.ClrType.Name}, not {message.Payload.GetType().Name}.", nameof(message));
            }

            if ((type.Kind == EditorWireKind.Request) != message.Id.HasValue)
            {
                throw new ArgumentException($"'{type.Name}' {(message.Id.HasValue ? "must not" : "must")} carry a request id.", nameof(message));
            }

            if (message.Id is { } id)
            {
                RequireId(id);
            }

            return Write(writer =>
            {
                writer.WriteStartObject();
                writer.WriteString("k", KindName(type.Kind));
                if (message.Id is { } requestId)
                {
                    writer.WriteNumber("id", requestId);
                }

                writer.WriteString("t", type.Name);
                writer.WritePropertyName("p");
                JsonSerializer.Serialize(writer, message.Payload, type.Payload);
                writer.WriteEndObject();
            });
        }

        /// <summary>成功应答 <c>{"k":"res","id":…,"ok":true,"p":…}</c>；<paramref name="result"/> 为 <c>null</c> 时 <c>p</c> 是 <c>null</c>。</summary>
        public static string EncodeResponse(long id, EditorWireType request, object? result)
        {
            ArgumentNullException.ThrowIfNull(request);
            RequireId(id);
            if (request.Result is not { } resultType)
            {
                throw new ArgumentException($"'{request.Name}' is not a request.", nameof(request));
            }

            if (result is null ? !request.ResultNullable : result.GetType() != resultType.Type)
            {
                throw new ArgumentException($"'{request.Name}' replies with {resultType.Type.Name}{(request.ResultNullable ? "?" : string.Empty)}.", nameof(result));
            }

            return Write(writer =>
            {
                writer.WriteStartObject();
                writer.WriteString("k", "res");
                writer.WriteNumber("id", id);
                writer.WriteBoolean("ok", true);
                writer.WritePropertyName("p");
                if (result is null)
                {
                    writer.WriteNullValue();
                }
                else
                {
                    JsonSerializer.Serialize(writer, result, resultType);
                }

                writer.WriteEndObject();
            });
        }

        /// <summary>按 <paramref name="typeInfo"/> 读一段载荷；JSON <c>null</c> 读成 <c>null</c>。</summary>
        public static object? ReadNullablePayload(ReadOnlySpan<byte> json, JsonTypeInfo typeInfo)
        {
            ArgumentNullException.ThrowIfNull(typeInfo);
            var trimmed = json.Trim(" \t\r\n"u8);
            return trimmed.IsEmpty || trimmed.SequenceEqual("null"u8) ? null : JsonSerializer.Deserialize(trimmed, typeInfo);
        }

        // ── 细节 ────────────────────────────────────────────────────────

        private static EditorWireCall<TResult> Call<TResult>(long id, object request, Func<object?, TResult> project)
        {
            var type = RequireType(request.GetType(), EditorWireKind.Request);
            return new EditorWireCall<TResult>(id, WriteMessage(new EditorWireMessage(type, id, request)), type, project);
        }

        private static EditorWireType RequireType(Type clrType, EditorWireKind kind)
        {
            var type = EditorWireTypes.Get(clrType);
            return type.Kind == kind
                ? type
                : throw new ArgumentException($"'{type.Name}' is a {type.Kind}, not a {kind}.");
        }

        private static void RequireId(long id)
        {
            if (id is < 0 or > MaxSafeInteger)
            {
                throw new ArgumentOutOfRangeException(nameof(id), id, "Request ids must be non-negative safe integers.");
            }
        }

        private static bool TryReadMessage(in Envelope envelope, out EditorWireMessage result, out WireRejected rejected)
        {
            result = null!;
            if (envelope.TypeName is not { } name)
            {
                rejected = new WireRejected(envelope.Kind, envelope.Id, EditorWireError.InvalidPayload, "The message has no type name.");
                return false;
            }

            if (!EditorWireTypes.TryGet(name, out var type) || type.Kind != envelope.Kind)
            {
                rejected = new WireRejected(envelope.Kind, envelope.Id, EditorWireError.UnknownType, $"Unknown {KindName(envelope.Kind)} '{name}'.");
                return false;
            }

            if ((envelope.Kind == EditorWireKind.Request) != envelope.Id.HasValue)
            {
                rejected = new WireRejected(envelope.Kind, envelope.Id, EditorWireError.InvalidPayload, $"'{name}' has a missing or unexpected id.");
                return false;
            }

            try
            {
                // 载荷永远是对象；缺失的 p 按空对象处理。
                var payload = envelope.Payload.IsEmpty ? "{}"u8 : envelope.Payload.Span;
                if (ReadNullablePayload(payload, type.Payload) is not { } value)
                {
                    rejected = new WireRejected(envelope.Kind, envelope.Id, EditorWireError.InvalidPayload, $"'{name}' has a null payload.");
                    return false;
                }

                result = new EditorWireMessage(type, envelope.Id, value);
                rejected = null!;
                return true;
            }
            catch (Exception ex) when (ex is JsonException or NotSupportedException or InvalidOperationException)
            {
                rejected = new WireRejected(envelope.Kind, envelope.Id, EditorWireError.InvalidPayload, $"Invalid payload for '{name}': {ex.Message}");
                return false;
            }
        }

        private static EditorWireInbound DecodeReply(in Envelope envelope)
        {
            if (envelope.Id is not { } id || envelope.Ok is not { } ok)
            {
                return new WireRejected(EditorWireKind.Response, envelope.Id, EditorWireError.InvalidPayload, "A response needs an id and ok.");
            }

            if (ok)
            {
                return new WireResponse(id, envelope.Payload.IsEmpty ? "null"u8.ToArray() : envelope.Payload);
            }

            try
            {
                if (!envelope.Error.IsEmpty && JsonSerializer.Deserialize(envelope.Error.Span, EditorWireTypes.ErrorBody) is { } error)
                {
                    return new WireFailure(id, error.Code, error.Message ?? string.Empty);
                }
            }
            catch (JsonException)
            {
                // 不认识的错误码：仍然了结这个请求，按一般失败处理。
            }

            return new WireFailure(id, EditorWireError.Failed, envelope.Error.IsEmpty ? string.Empty : Encoding.UTF8.GetString(envelope.Error.Span));
        }

        private static string KindName(EditorWireKind kind) => kind switch
        {
            EditorWireKind.Command => "cmd",
            EditorWireKind.Event => "evt",
            EditorWireKind.Request => "req",
            EditorWireKind.Response => "res",
            _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, null),
        };

        private static EditorWireKind? ParseKind(ReadOnlySpan<byte> name) =>
            name.SequenceEqual("cmd"u8) ? EditorWireKind.Command
            : name.SequenceEqual("evt"u8) ? EditorWireKind.Event
            : name.SequenceEqual("req"u8) ? EditorWireKind.Request
            : name.SequenceEqual("res"u8) ? EditorWireKind.Response
            : null;

        /// <summary>信封的外层字段；<c>p</c> 与 <c>err</c> 保留原始 UTF-8 JSON，按类型名再反序列化。</summary>
        private readonly record struct Envelope(
            EditorWireKind Kind,
            string? TypeName,
            long? Id,
            bool? Ok,
            ReadOnlyMemory<byte> Payload,
            ReadOnlyMemory<byte> Error);

        private static bool TryParseEnvelope(string? message, out Envelope envelope, out string detail)
        {
            envelope = default;
            detail = string.Empty;
            if (string.IsNullOrEmpty(message))
            {
                detail = "The message is empty.";
                return false;
            }

            var utf8 = Encoding.UTF8.GetBytes(message);
            try
            {
                var reader = new Utf8JsonReader(utf8, readerOptions);
                if (!reader.Read() || reader.TokenType != JsonTokenType.StartObject)
                {
                    detail = "The message is not a JSON object.";
                    return false;
                }

                EditorWireKind? kind = null;
                string? typeName = null;
                long? id = null;
                bool? ok = null;
                ReadOnlyMemory<byte> payload = default;
                ReadOnlyMemory<byte> error = default;
                while (reader.Read() && reader.TokenType == JsonTokenType.PropertyName)
                {
                    if (reader.ValueTextEquals("k"u8))
                    {
                        reader.Read();
                        kind = reader.TokenType == JsonTokenType.String ? ParseKind(reader.ValueSpan) : null;
                    }
                    else if (reader.ValueTextEquals("t"u8))
                    {
                        reader.Read();
                        typeName = reader.TokenType == JsonTokenType.String ? reader.GetString() : null;
                    }
                    else if (reader.ValueTextEquals("id"u8))
                    {
                        reader.Read();
                        id = reader.TokenType == JsonTokenType.Number && reader.TryGetInt64(out var value) && value is >= 0 and <= MaxSafeInteger
                            ? value
                            : null;
                    }
                    else if (reader.ValueTextEquals("ok"u8))
                    {
                        reader.Read();
                        ok = reader.TokenType switch
                        {
                            JsonTokenType.True => true,
                            JsonTokenType.False => false,
                            _ => null,
                        };
                    }
                    else if (reader.ValueTextEquals("p"u8))
                    {
                        payload = ReadRawValue(ref reader, utf8);
                    }
                    else if (reader.ValueTextEquals("err"u8))
                    {
                        error = ReadRawValue(ref reader, utf8);
                    }
                    else
                    {
                        // 未知字段忽略。
                        reader.Read();
                        reader.Skip();
                    }
                }

                if (kind is not { } k)
                {
                    detail = "The message has no valid kind.";
                    return false;
                }

                envelope = new Envelope(k, typeName, id, ok, payload, error);
                return true;
            }
            catch (JsonException ex)
            {
                detail = $"The message is not valid JSON: {ex.Message}";
                return false;
            }
        }

        private static ReadOnlyMemory<byte> ReadRawValue(ref Utf8JsonReader reader, byte[] utf8)
        {
            reader.Read();
            var start = (int)reader.TokenStartIndex;
            reader.Skip();
            return utf8.AsMemory(start, (int)reader.BytesConsumed - start);
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
