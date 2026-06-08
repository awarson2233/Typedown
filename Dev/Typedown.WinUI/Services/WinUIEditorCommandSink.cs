using System;
using Typedown.Presentation.Interfaces;
using Typedown.WinUI.Controls;

namespace Typedown.WinUI.Services
{
    internal sealed class WinUIEditorCommandSink : IEditorCommandSink
    {
        private readonly object gate = new();
        private WinUIEditorHost? activeHost;
        private PendingCommand? latestThemeCommand;

        public bool Send(string name, object? args)
        {
            WinUIEditorHost? host;
            lock (gate)
            {
                if (StringComparer.Ordinal.Equals(name, "ThemeChanged"))
                {
                    latestThemeCommand = new PendingCommand(name, args);
                }

                host = activeHost;
            }

            return host?.SendCommand(name, args) == true;
        }

        public void RegisterActiveHost(WinUIEditorHost host)
        {
            PendingCommand? themeCommand;
            lock (gate)
            {
                activeHost = host;
                themeCommand = latestThemeCommand;
            }

            TrySend(host, themeCommand);
        }

        internal void ResendLatestTheme(WinUIEditorHost host)
        {
            PendingCommand? themeCommand;
            lock (gate)
            {
                if (!ReferenceEquals(activeHost, host))
                {
                    return;
                }

                themeCommand = latestThemeCommand;
            }

            TrySend(host, themeCommand);
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

        private static bool TrySend(WinUIEditorHost host, PendingCommand? command)
        {
            return command is not null && host.SendCommand(command.Name, command.Args);
        }

        private sealed record PendingCommand(string Name, object? Args);
    }
}
