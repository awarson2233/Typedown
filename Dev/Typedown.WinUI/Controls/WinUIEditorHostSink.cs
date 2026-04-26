using System.Text.Json;
using Typedown.Core.Contracts.Editor;

namespace Typedown.WinUI.Controls
{
    internal sealed class WinUIEditorHostSink : IEditorHostSink
    {
        private readonly WinUIEditorHost host;

        public WinUIEditorHostSink(WinUIEditorHost host)
        {
            this.host = host;
        }

        public bool Send(EditorHostMessage message)
        {
            var payload = JsonSerializer.Serialize(new { name = message.Name, args = message.Args });
            return host.SendRawMessage(payload);
        }
    }
}
