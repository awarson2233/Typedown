using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Typedown.Core.Editor;
using Typedown.Core.Editor.Wire;
using Typedown.Core.Models;

namespace Typedown.CoreTests.Editor;

/// <summary>
/// 新引擎线协议的契约测试：读 Tests/Protocol 下与页面侧共用的样例，任何一端改了字段名、枚举值或类型名都会失败。
/// 样例文件名就是类型名；请求另有一份 <c>类型名.res.json</c> 是它的成功应答。
/// </summary>
[TestClass]
public sealed class EditorWireContractTests
{
    private const string ResponseSuffix = ".res";

    private static readonly string ProtocolDirectory = Path.Combine(AppContext.BaseDirectory, "Protocol");
    private static readonly string SamplesDirectory = Path.Combine(ProtocolDirectory, "samples");

    // ── 样例覆盖 ─────────────────────────────────────────────────────

    [TestMethod]
    public void EveryTypeName_HasASample()
    {
        foreach (var type in EditorWireTypes.All)
        {
            Assert.IsTrue(File.Exists(SamplePath(type.Name)), $"Missing sample {type.Name}.json");
            if (type.Kind == EditorWireKind.Request)
            {
                Assert.IsTrue(File.Exists(SamplePath(type.Name + ResponseSuffix)), $"Missing sample {type.Name}{ResponseSuffix}.json");
            }
        }
    }

    [TestMethod]
    public void EverySample_NamesAKnownType()
    {
        var files = Directory.GetFiles(SamplesDirectory, "*.json");
        Assert.IsTrue(files.Length >= EditorWireTypes.All.Count);
        foreach (var file in files)
        {
            var name = Path.GetFileNameWithoutExtension(file);
            var isResponse = name.EndsWith(ResponseSuffix, StringComparison.Ordinal);
            var typeName = isResponse ? name[..^ResponseSuffix.Length] : name;
            Assert.IsTrue(EditorWireTypes.TryGet(typeName, out var type), $"{Path.GetFileName(file)} names no wire type.");
            Assert.IsTrue(!isResponse || type.Kind == EditorWireKind.Request, $"{Path.GetFileName(file)}: only requests have replies.");
        }
    }

    // ── 往返 ─────────────────────────────────────────────────────────

    [TestMethod]
    public void Messages_RoundTripThroughRecords()
    {
        foreach (var type in EditorWireTypes.All)
        {
            var sample = ReadSample(type.Name);
            Assert.IsTrue(EditorWireCodec.TryReadMessage(sample, out var message, out var rejected), $"{type.Name}: {rejected?.Detail}");
            Assert.AreSame(type, message.Type);
            Assert.IsInstanceOfType(message.Payload, type.ClrType);
            AssertJsonEqual(sample, EditorWireCodec.WriteMessage(message), type.Name);
        }
    }

    [TestMethod]
    public void HostMessages_RoundTripThroughTypedEncoders()
    {
        foreach (var type in EditorWireTypes.All.Where(x => x.Direction == EditorWireDirection.HostToPage))
        {
            var sample = ReadSample(type.Name);
            Assert.IsTrue(EditorWireCodec.TryReadMessage(sample, out var message, out _));
            var encoded = message.Payload switch
            {
                DocLoad load => EditorWireCodec.EncodeDocLoad(load),
                EditorCommand command => EditorWireCodec.EncodeCommand(command),
                FlushDocument flush => EditorWireCodec.EncodeRequest(message.Id!.Value, flush).Message,
                RenderExportHtml export => EditorWireCodec.EncodeRequest(message.Id!.Value, export).Message,
                ContextAt contextAt => EditorWireCodec.EncodeRequest(message.Id!.Value, contextAt).Message,
                DocGetText => EditorWireCodec.EncodeGetText(message.Id!.Value).Message,
                _ => throw new AssertFailedException($"{type.Name} has no typed encoder."),
            };
            AssertJsonEqual(sample, encoded, type.Name);
        }
    }

    [TestMethod]
    public void PageMessages_DecodeToTheirSessionAction()
    {
        foreach (var type in EditorWireTypes.All.Where(x => x.Direction == EditorWireDirection.PageToHost))
        {
            var inbound = EditorWireCodec.Decode(ReadSample(type.Name));
            object payload = inbound switch
            {
                WireEvent e => e.Event,
                WireSignal s => s.Signal,
                WireHostRequest r => r.Request,
                _ => throw new AssertFailedException($"{type.Name} decoded to {inbound}."),
            };
            Assert.IsInstanceOfType(payload, type.ClrType, type.Name);
            Assert.AreEqual(type.Kind == EditorWireKind.Request, inbound is WireHostRequest, type.Name);
        }
    }

    [TestMethod]
    public void Replies_RoundTripThroughResultRecords()
    {
        foreach (var type in EditorWireTypes.All.Where(x => x.Kind == EditorWireKind.Request))
        {
            var sample = ReadSample(type.Name + ResponseSuffix);
            var response = (WireResponse)EditorWireCodec.Decode(sample);
            var result = EditorWireCodec.ReadNullablePayload(response.Payload.Span, type.Result!);
            Assert.IsNotNull(result, type.Name);
            AssertJsonEqual(sample, EditorWireCodec.EncodeResponse(response.Id, type, result), type.Name + ResponseSuffix);
        }
    }

    [TestMethod]
    public void HostRequests_ReadTypedResults()
    {
        Assert.AreEqual(1048580L, Reply(EditorWireCodec.EncodeRequest(17, new FlushDocument()), "doc.flush"));
        Assert.AreEqual(new DocText(1048580, "# 标题\n"), Reply(EditorWireCodec.EncodeGetText(18), "doc.getText"));
        StringAssert.StartsWith(Reply(EditorWireCodec.EncodeRequest(19, new RenderExportHtml(ExportPurpose.Export, "t", null, null)), "export.renderHtml"), "<!DOCTYPE html>");
        var context = Reply(EditorWireCodec.EncodeRequest(20, new ContextAt(1, 2)), "selection.contextAt");
        CollectionAssert.AreEqual(new[] { BlockKind.Heading2 }, context!.Block.Kinds.ToArray());
        Assert.IsNull(context.SelectedImage);
    }

    [TestMethod]
    public void PageRequests_EncodeTypedReplies()
    {
        AssertJsonEqual(ReadSample("table.pickSize.res"), EditorWireCodec.EncodeTableSizeReply(3, new TableSize(3, 2)), "table.pickSize");
        AssertJsonEqual(ReadSample("clipboard.write.res"), EditorWireCodec.EncodeClipboardWriteReply(4), "clipboard.write");
        AssertJsonEqual(ReadSample("image.resolve.res"), EditorWireCodec.EncodeImageResolveReply(5, "./assets/图片.png"), "image.resolve");
        AssertJsonEqual("""{"k":"res","id":3,"ok":true,"p":null}""", EditorWireCodec.EncodeTableSizeReply(3, null), "cancel");
        AssertJsonEqual("""{"k":"res","id":5,"ok":true,"p":null}""", EditorWireCodec.EncodeImageResolveReply(5, null), "give up");
        AssertJsonEqual(
            """{"k":"res","id":6,"ok":false,"err":{"code":"unknownType","message":"no"}}""",
            EditorWireCodec.EncodeFailure(6, EditorWireError.UnknownType, "no"),
            "failure");
    }

    // ── 类型名表 ─────────────────────────────────────────────────────

    [TestMethod]
    public void EveryContractMessage_IsInTheTypeTable()
    {
        var contract = typeof(EditorCommand).Assembly.GetExportedTypes()
            .Where(t => t.Namespace == "Typedown.Core.Editor" && !t.IsAbstract
                && (typeof(EditorCommand).IsAssignableFrom(t) || typeof(EditorEvent).IsAssignableFrom(t) || typeof(EditorRequest).IsAssignableFrom(t)))
            .ToList();
        Assert.IsTrue(contract.Count > 50);
        foreach (var type in contract)
        {
            Assert.IsTrue(
                EditorWireTypes.TryGet(type, out _) || EditorWireTypes.SessionTranslated.ContainsKey(type),
                $"{type.Name} has no wire type name.");
        }

        foreach (var (contractType, wireType) in EditorWireTypes.SessionTranslated)
        {
            Assert.IsFalse(EditorWireTypes.TryGet(contractType, out _), $"{contractType.Name} is translated, it must not be on the wire itself.");
            Assert.IsTrue(EditorWireTypes.TryGet(wireType, out _), $"{wireType.Name} must be in the table.");
        }
    }

    [TestMethod]
    public void TypeTable_IsABijection()
    {
        var all = EditorWireTypes.All;
        Assert.AreEqual(all.Count, all.Select(x => x.Name).Distinct(StringComparer.Ordinal).Count());
        Assert.AreEqual(all.Count, all.Select(x => x.ClrType).Distinct().Count());
        foreach (var type in all)
        {
            Assert.IsTrue(EditorWireTypes.TryGet(type.Name, out var byName));
            Assert.IsTrue(EditorWireTypes.TryGet(type.ClrType, out var byType));
            Assert.AreSame(type, byName);
            Assert.AreSame(type, byType);
            StringAssert.Matches(type.Name, new System.Text.RegularExpressions.Regex("^[a-z]+\\.[a-z][A-Za-z]*$"));
        }
    }

    [TestMethod]
    public void EveryType_HasJsonTypeInfo()
    {
        foreach (var type in EditorWireTypes.All)
        {
            Assert.AreEqual(type.ClrType, type.Payload.Type, type.Name);
            Assert.AreEqual(type.Kind == EditorWireKind.Request, type.Result is not null, type.Name);
            Assert.AreEqual(type.Kind == EditorWireKind.Command, type.Direction == EditorWireDirection.HostToPage && type.Kind != EditorWireKind.Request, type.Name);
        }

        Assert.AreEqual(typeof(EditorInitState), EditorWireTypes.InitState.Type);
    }

    // ── 枚举与初始态 ─────────────────────────────────────────────────

    [TestMethod]
    public void EnumWireValues_MatchTheSharedTable()
    {
        using var shared = JsonDocument.Parse(File.ReadAllText(Path.Combine(ProtocolDirectory, "enums.json")));
        var expected = shared.RootElement.EnumerateObject()
            .ToDictionary(x => x.Name, x => x.Value.EnumerateArray().Select(v => v.GetString()!).ToArray());

        var reachable = ReachableEnums().Where(t => t != typeof(KeyboardKey) && t != typeof(KeyboardModifiers)).ToList();
        CollectionAssert.AreEquivalent(expected.Keys.ToArray(), reachable.Select(t => t.Name).ToArray());
        foreach (var enumType in reachable)
        {
            var actual = Enum.GetValues(enumType).Cast<Enum>().Select(ToWireString).ToArray();
            CollectionAssert.AreEqual(expected[enumType.Name], actual, enumType.Name);
        }
    }

    [TestMethod]
    public void KeyboardEnums_AreWrittenAsIntegers()
    {
        Assert.AreEqual("83", EditorWireTypes.ToWireValue(KeyboardKey.S));
        Assert.AreEqual("5", EditorWireTypes.ToWireValue(KeyboardModifiers.Control | KeyboardModifiers.Shift));
    }

    [TestMethod]
    public void InitState_RoundTripsAndBecomesAScript()
    {
        var sample = File.ReadAllText(Path.Combine(ProtocolDirectory, "init-state.json"));
        var state = JsonSerializer.Deserialize(sample, EditorWireTypes.InitState)!;
        Assert.AreEqual(EditorWireTypes.ProtocolVersion, state.Protocol);
        Assert.AreEqual(KeyboardKey.S, state.Keymap[0].Key);
        AssertJsonEqual(sample, EditorWireCodec.EncodeInitState(state), "init-state");

        var script = EditorWireCodec.EncodeInitScript(state);
        StringAssert.StartsWith(script, "window.__typedownInit = {");
        StringAssert.EndsWith(script, "};");
    }

    // ── 编码细节 ─────────────────────────────────────────────────────

    [TestMethod]
    public void HostEncoding_KeepsChineseAndHtmlUnescaped()
    {
        var message = EditorWireCodec.EncodeDocLoad(new DocLoad(1, "# 标题 <b>&</b> \u2028", "C:\\文档"));
        StringAssert.Contains(message, "标题 <b>&</b>");
        StringAssert.Contains(message, "文档");
        Assert.AreEqual("# 标题 <b>&</b> \u2028", JsonNode.Parse(message)!["p"]!["text"]!.GetValue<string>());
    }

    [TestMethod]
    public void EncodeCommand_RequiresTheMirrorForLoads()
    {
        Assert.ThrowsException<ArgumentException>(() => EditorWireCodec.EncodeCommand(new LoadDocument("x", "")));
    }

    [TestMethod]
    public void OptionalFields_AreOmittedAndDefaulted()
    {
        AssertJsonEqual("""{"k":"cmd","t":"search.set","p":{"options":{"caseSensitive":false,"wholeWord":false,"regex":false}}}""",
            EditorWireCodec.EncodeCommand(new Search(null, new SearchOptions(false, false, false))), "search.set");
        var inbound = (WireEvent)EditorWireCodec.Decode("""{"k":"evt","t":"float.blockMenu","p":{}}""");
        Assert.AreEqual(new BlockMenuRequested(null), inbound.Event);
        var noPayload = (WireEvent)EditorWireCodec.Decode("""{"k":"evt","t":"float.tooltipDismissed"}""");
        Assert.IsInstanceOfType(noPayload.Event, typeof(TooltipDismissed));
    }

    [TestMethod]
    public void Decode_ReadsTheEnvelopeInAnyOrderAndIgnoresUnknownFields()
    {
        var inbound = EditorWireCodec.Decode("""{"p":{"canRedo":true,"extra":[1,{"a":2}],"canUndo":false},"future":{"x":1},"t":"history.changed","k":"evt"}""");
        Assert.AreEqual(new HistoryChanged(false, true), ((WireEvent)inbound).Event);
    }

    [DataTestMethod]
    [DataRow("", EditorWireError.InvalidPayload, DisplayName = "empty")]
    [DataRow("[1]", EditorWireError.InvalidPayload, DisplayName = "not an object")]
    [DataRow("{\"k\":\"evt\"", EditorWireError.InvalidPayload, DisplayName = "truncated")]
    [DataRow("""{"k":"what","t":"history.changed","p":{}}""", EditorWireError.InvalidPayload, DisplayName = "unknown kind")]
    [DataRow("""{"k":"evt","t":"history.rewind","p":{}}""", EditorWireError.UnknownType, DisplayName = "unknown type name")]
    [DataRow("""{"k":"cmd","t":"format.toggle","p":{"mark":"strong"}}""", EditorWireError.UnknownType, DisplayName = "host-only type sent by the page")]
    [DataRow("""{"k":"req","t":"history.changed","id":1,"p":{"canUndo":true,"canRedo":true}}""", EditorWireError.UnknownType, DisplayName = "kind does not match the type")]
    [DataRow("""{"k":"evt","t":"history.changed","p":{"canUndo":true}}""", EditorWireError.InvalidPayload, DisplayName = "missing required field")]
    [DataRow("""{"k":"evt","t":"view.openLink","p":{"uri":null}}""", EditorWireError.InvalidPayload, DisplayName = "null for a non-nullable field")]
    [DataRow("""{"k":"evt","t":"float.tableTools","p":{"axis":"diagonal"}}""", EditorWireError.InvalidPayload, DisplayName = "unknown enum value")]
    [DataRow("""{"k":"evt","t":"float.tableTools","p":{"axis":1}}""", EditorWireError.InvalidPayload, DisplayName = "enum as integer")]
    [DataRow("""{"k":"evt","t":"stats.changed","p":{"characters":"1","words":2}}""", EditorWireError.InvalidPayload, DisplayName = "wrong value type")]
    [DataRow("""{"k":"evt","t":"stats.changed","p":null}""", EditorWireError.InvalidPayload, DisplayName = "null payload")]
    [DataRow("""{"k":"req","t":"table.pickSize","p":{}}""", EditorWireError.InvalidPayload, DisplayName = "request without id")]
    [DataRow("""{"k":"res","ok":true,"p":{}}""", EditorWireError.InvalidPayload, DisplayName = "response without id")]
    public void Decode_RejectsBrokenMessages(string message, EditorWireError expected)
    {
        var rejected = EditorWireCodec.Decode(message) as WireRejected;
        Assert.IsNotNull(rejected, message);
        Assert.AreEqual(expected, rejected.Error, rejected.Detail);
    }

    [TestMethod]
    public void Decode_KeepsTheIdOfARejectedRequestSoTheSessionCanReply()
    {
        var rejected = (WireRejected)EditorWireCodec.Decode("""{"k":"req","id":9,"t":"table.resize","p":{}}""");
        Assert.AreEqual((EditorWireKind?)EditorWireKind.Request, rejected.Kind);
        Assert.AreEqual(9L, rejected.Id);
        Assert.AreEqual(EditorWireError.UnknownType, rejected.Error);
    }

    [TestMethod]
    public void ReadReply_MapsErrorCodesToExceptions()
    {
        var call = EditorWireCodec.EncodeRequest(7, new FlushDocument());
        Assert.ThrowsException<NotSupportedException>(() => call.ReadReply(Failure(7, "unknownType")));
        Assert.ThrowsException<OperationCanceledException>(() => call.ReadReply(Failure(7, "canceled")));
        var failed = Assert.ThrowsException<EditorRequestFailedException>(() => call.ReadReply(Failure(7, "failed")));
        Assert.AreEqual(EditorWireError.Failed, failed.Code);
        Assert.AreEqual("boom", failed.Message);
        var unknownCode = Assert.ThrowsException<EditorRequestFailedException>(() => call.ReadReply(Failure(7, "exploded")));
        Assert.AreEqual(EditorWireError.Failed, unknownCode.Code);

        var nullReply = (EditorWireReply)EditorWireCodec.Decode("""{"k":"res","id":7,"ok":true,"p":null}""");
        Assert.AreEqual(EditorWireError.InvalidPayload, Assert.ThrowsException<EditorRequestFailedException>(() => call.ReadReply(nullReply)).Code);
        var badReply = (EditorWireReply)EditorWireCodec.Decode("""{"k":"res","id":7,"ok":true,"p":{"version":"x"}}""");
        Assert.AreEqual(EditorWireError.InvalidPayload, Assert.ThrowsException<EditorRequestFailedException>(() => call.ReadReply(badReply)).Code);

        var contextAt = EditorWireCodec.EncodeRequest(8, new ContextAt(0, 0));
        Assert.IsNull(contextAt.ReadReply((EditorWireReply)EditorWireCodec.Decode("""{"k":"res","id":8,"ok":true,"p":null}""")));
    }

    // ── 工具 ─────────────────────────────────────────────────────────

    private static string SamplePath(string name) => Path.Combine(SamplesDirectory, name + ".json");

    private static string ReadSample(string name) => File.ReadAllText(SamplePath(name));

    private static T Reply<T>(EditorWireCall<T> call, string typeName) =>
        call.ReadReply((EditorWireReply)EditorWireCodec.Decode(ReadSample(typeName + ResponseSuffix)));

    private static EditorWireReply Failure(long id, string code) =>
        (EditorWireReply)EditorWireCodec.Decode("{\"k\":\"res\",\"id\":" + id + ",\"ok\":false,\"err\":{\"code\":\"" + code + "\",\"message\":\"boom\"}}");

    private static void AssertJsonEqual(string expected, string actual, string context)
    {
        Assert.IsTrue(
            JsonNode.DeepEquals(JsonNode.Parse(expected), JsonNode.Parse(actual)),
            $"{context}{Environment.NewLine}Expected {expected.Trim()}{Environment.NewLine}Actual   {actual}");
    }

    private static string ToWireString(Enum value)
    {
        var json = (string)typeof(EditorWireTypes).GetMethod(nameof(EditorWireTypes.ToWireValue))!
            .MakeGenericMethod(value.GetType())
            .Invoke(null, [value])!;
        return JsonSerializer.Deserialize<string>(json)!;
    }

    /// <summary>从类型名表（载荷与应答）、错误体与初始态出发，沿公开属性能走到的全部枚举类型。</summary>
    private static IReadOnlyCollection<Type> ReachableEnums()
    {
        var roots = EditorWireTypes.All.SelectMany(x => new[] { x.ClrType, x.Result?.Type })
            .Append(typeof(WireErrorBody))
            .Append(typeof(EditorInitState))
            .OfType<Type>();
        var seen = new HashSet<Type>();
        var enums = new HashSet<Type>();
        var queue = new Queue<Type>(roots);
        while (queue.TryDequeue(out var type))
        {
            type = Nullable.GetUnderlyingType(type) ?? type;
            if (!seen.Add(type) || type == typeof(string) || type.IsPrimitive)
            {
                continue;
            }

            if (type.IsEnum)
            {
                enums.Add(type);
                continue;
            }

            if (type.IsGenericType)
            {
                foreach (var argument in type.GetGenericArguments())
                {
                    queue.Enqueue(argument);
                }

                if (type.Namespace?.StartsWith("System", StringComparison.Ordinal) == true)
                {
                    continue;
                }
            }

            foreach (var property in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                queue.Enqueue(property.PropertyType);
            }
        }

        return enums;
    }
}
