using System;
using System.Collections.Generic;

namespace Typedown.WinUI.Controls
{
    /// <summary>
    /// 暂存在 <c>CoreWebView2</c> 尚未创建期间产生的 host → editor 报文。
    /// 这是 WinUI3 宿主相对 1.2.19 的唯一补充：UWP 版的 WebView 控制器在 Loaded 时已同步就绪，
    /// 而 <c>EnsureCoreWebView2Async</c> 是异步的，导航前发出的命令会直接丢失。
    /// 注意：这里只覆盖"CoreWebView2 还没建好"这一段窗口，不做"页面是否已就绪"的门控——
    /// 前端的初始状态一律由 GetSettings invoke 主动拉取，invoke 应答必须立即发出，不能排队。
    /// </summary>
    internal sealed class PendingRawMessageQueue
    {
        internal const int DefaultCapacity = 128;
        internal const int DefaultMaxRetryCount = 3;

        private readonly Queue<PendingRawMessage> messages = new();
        private readonly int capacity;
        private readonly int maxRetryCount;

        public PendingRawMessageQueue(int capacity = DefaultCapacity, int maxRetryCount = DefaultMaxRetryCount)
        {
            this.capacity = Math.Max(1, capacity);
            this.maxRetryCount = Math.Max(1, maxRetryCount);
        }

        public int Count => messages.Count;

        public void Enqueue(string payload)
        {
            if (messages.Count >= capacity)
            {
                _ = messages.Dequeue();
            }

            messages.Enqueue(new PendingRawMessage(payload, 0));
        }

        public void Clear()
        {
            messages.Clear();
        }

        public void Flush(Func<string, bool> sender)
        {
            var remaining = messages.Count;
            while (remaining > 0 && messages.Count > 0)
            {
                remaining--;
                var message = messages.Dequeue();
                if (sender(message.Payload))
                {
                    continue;
                }

                // 发送失败时把这条放回队首并中断本轮，保持报文顺序；
                // diffmsg 是有状态协议，乱序重放会让前端的 prevDic 永久错位。
                var failedMessage = message with { RetryCount = message.RetryCount + 1 };
                if (failedMessage.RetryCount < maxRetryCount)
                {
                    var remainingMessages = messages.ToArray();
                    messages.Clear();
                    messages.Enqueue(failedMessage);
                    foreach (var remainingMessage in remainingMessages)
                    {
                        messages.Enqueue(remainingMessage);
                    }

                    break;
                }
            }
        }

        private sealed record PendingRawMessage(string Payload, int RetryCount);
    }
}
