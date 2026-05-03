using Typedown.Presentation.Interfaces;
using Typedown.WinUI.Controls;

namespace Typedown.WinUI.Services
{
    internal sealed class WinUIEditorCommandSink : IEditorCommandSink
    {
        private readonly object gate = new();
        private WinUIEditorHost? activeHost;

        public bool Send(string name, object args)
        {
            WinUIEditorHost? host;
            lock (gate)
            {
                host = activeHost;
            }

            return host?.SendCommand(name, args) == true;
        }

        public void RegisterActiveHost(WinUIEditorHost host)
        {
            lock (gate)
            {
                activeHost = host;
            }
        }

        public void UnregisterActiveHost(WinUIEditorHost host)
        {
            lock (gate)
            {
                if (ReferenceEquals(activeHost, host))
                {
                    activeHost = null;
                }
            }
        }
    }
}
