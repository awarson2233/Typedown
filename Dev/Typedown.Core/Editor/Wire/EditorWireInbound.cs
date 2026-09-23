using System;
using System.Text.Json;

namespace Typedown.Core.Editor.Wire
{
    /// <summary>
    /// 一条入站报文解码后的结果，按会话要做的动作分类：外发契约事件、自己消化的事件、应答页面请求、了结自己的请求、拒收。
    /// </summary>
    public abstract record EditorWireInbound
    {
        private protected EditorWireInbound()
        {
        }
    }

    /// <summary>
    /// 契约事件，原样发给 ViewModel。唯一的例外是 <see cref="DocumentLoaded"/>：会话先用
    /// <see cref="DocumentMirror.IsCurrentLoad"/> 丢掉过期的回声。
    /// </summary>
    public sealed record WireEvent(EditorEvent Event) : EditorWireInbound;

    /// <summary>会话自用的事件：生命周期、正文增量、带偏移的选区。</summary>
    public sealed record WireSignal(EditorWireSignal Signal) : EditorWireInbound;

    /// <summary>页面回问宿主的请求；会话调回调后用 <see cref="EditorWireCodec"/> 的 <c>Encode…Reply</c> 应答 <paramref name="Id"/>。</summary>
    public sealed record WireHostRequest(long Id, EditorWireHostRequest Request) : EditorWireInbound;

    /// <summary>页面对宿主某个请求的应答；交给该请求的 <see cref="EditorWireCall{TResult}.ReadReply"/>。</summary>
    public abstract record EditorWireReply(long Id) : EditorWireInbound;

    /// <summary>成功应答，<paramref name="Payload"/> 是 <c>p</c> 的原始 UTF-8 JSON（可能是 <c>null</c>）。</summary>
    public sealed record WireResponse(long Id, ReadOnlyMemory<byte> Payload) : EditorWireReply(Id);

    /// <summary>失败应答。</summary>
    public sealed record WireFailure(long Id, EditorWireError Error, string Message) : EditorWireReply(Id);

    /// <summary>
    /// 拒收的报文：信封残缺、类型名不认识或载荷无效。命令与事件只记日志；
    /// 带 <paramref name="Id"/> 的请求要回 <see cref="EditorWireCodec.EncodeFailure"/>，错误码取 <paramref name="Error"/>。
    /// </summary>
    public sealed record WireRejected(EditorWireKind? Kind, long? Id, EditorWireError Error, string Detail) : EditorWireInbound;

    /// <summary>
    /// 宿主发出的一个请求：<see cref="Message"/> 是要投递的报文，<see cref="ReadReply"/> 把页面的应答读成契约结果。
    /// 挂起表按 <see cref="Id"/> 登记，收到同 id 的 <see cref="EditorWireReply"/> 时了结。
    /// </summary>
    public sealed class EditorWireCall<TResult>
    {
        private readonly EditorWireType type;
        private readonly Func<object?, TResult> project;

        internal EditorWireCall(long id, string message, EditorWireType type, Func<object?, TResult> project)
        {
            Id = id;
            Message = message;
            this.type = type;
            this.project = project;
        }

        public long Id { get; }

        public string Message { get; }

        /// <summary>
        /// 读应答：成功时返回结果；失败时按错误码抛出——<see cref="EditorWireError.UnknownType"/> 为
        /// <see cref="NotSupportedException"/>，<see cref="EditorWireError.Canceled"/> 为 <see cref="OperationCanceledException"/>，
        /// 其余为 <see cref="EditorRequestFailedException"/>；应答载荷本身无效时同样是 <see cref="EditorWireError.InvalidPayload"/>。
        /// </summary>
        public TResult ReadReply(EditorWireReply reply)
        {
            ArgumentNullException.ThrowIfNull(reply);
            switch (reply)
            {
                case WireFailure failure:
                    throw failure.Error switch
                    {
                        EditorWireError.UnknownType => new NotSupportedException($"The editor page does not support '{type.Name}': {failure.Message}"),
                        EditorWireError.Canceled => new OperationCanceledException(failure.Message),
                        _ => new EditorRequestFailedException(failure.Error, failure.Message),
                    };
                case WireResponse response:
                    object? value;
                    try
                    {
                        value = EditorWireCodec.ReadNullablePayload(response.Payload.Span, type.Result!);
                    }
                    catch (Exception ex) when (ex is JsonException or NotSupportedException or InvalidOperationException)
                    {
                        throw new EditorRequestFailedException(EditorWireError.InvalidPayload, $"Invalid reply to '{type.Name}': {ex.Message}");
                    }

                    if (value is null && !type.ResultNullable)
                    {
                        throw new EditorRequestFailedException(EditorWireError.InvalidPayload, $"The reply to '{type.Name}' must not be null.");
                    }

                    return project(value);
                default:
                    throw new ArgumentException($"Unknown reply {reply.GetType().Name}.", nameof(reply));
            }
        }
    }
}
