using System;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;
using Typedown.Core.Editor;
using Typedown.Core.Editor.Wire;
using Typedown.Core.Interfaces;
using Typedown.Core.Utilities;

namespace Typedown.WinUI.Controls
{
    /// <summary>
    /// 新引擎页面的编辑会话。协议本身（握手、就绪门、正文镜像、请求与超时、崩溃恢复）都在 Core 的
    /// <see cref="EditorWireSession"/> 里，可以脱离 WebView2 测试；这里只接上 DI 与错误上报，
    /// WebView2 那一侧由 <see cref="WinUIEditorHost"/> 以 <see cref="IEditorWireChannel"/> 的身份提供。
    /// </summary>
    public sealed class WebViewEditorSession : EditorWireSession
    {
        private static readonly HashSet<ulong> reportedFaults = new();

        public WebViewEditorSession(IServiceProvider services, IUiDispatcher uiDispatcher)
            : base(() => services.GetRequiredService<IEditorHostCallbacks>(), uiDispatcher)
        {
        }

        /// <summary>致命错误按 message + stack 去重后上报，与旧会话上报页面未捕获异常的做法一致。</summary>
        protected override void OnPageFault(LifecycleFault fault)
        {
            if (!fault.Fatal)
            {
                return;
            }

            var content = $"{fault.Message}\n{fault.Stack}";
            lock (reportedFaults)
            {
                if (!reportedFaults.Add(Common.SimpleHash(content)))
                {
                    return;
                }
            }

            _ = Typedown.Core.Utilities.Log.Report("EditorPageFault", content);
        }
    }
}
