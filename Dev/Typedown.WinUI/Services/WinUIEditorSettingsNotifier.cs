using System.Collections.Generic;
using Typedown.Presentation.Interfaces;

namespace Typedown.WinUI.Services
{
    internal sealed class WinUIEditorSettingsNotifier : IEditorSettingsNotifier
    {
        private readonly IEditorCommandSink editorCommandSink;

        public WinUIEditorSettingsNotifier(IEditorCommandSink editorCommandSink)
        {
            this.editorCommandSink = editorCommandSink ?? throw new ArgumentNullException(nameof(editorCommandSink));
        }

        public void NotifySettingsChanged(IReadOnlyDictionary<string, object> settings)
        {
            editorCommandSink.Send("SettingsChanged", settings);
        }
    }
}
