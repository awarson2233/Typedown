using System;
using System.IO;
using Typedown.Core.Interfaces;

namespace Typedown.WinUI.Services
{
    internal sealed class WinUIAppActivationService : IAppActivationService
    {
        private readonly IWindowContext windowContext;

        public WinUIAppActivationService(IWindowContext windowContext)
        {
            this.windowContext = windowContext ?? throw new ArgumentNullException(nameof(windowContext));
        }

        public event Func<AppActivationRequest, nint>? ActivationRequested;

        public AppActivationResult Activate(string[] commandLineArgs)
        {
            var kind = ResolveKind(commandLineArgs);
            var request = new AppActivationRequest(kind, commandLineArgs ?? Array.Empty<string>());
            var requestedHandle = windowContext.WindowHandle;

            if (ActivationRequested is not null)
            {
                foreach (var subscriber in ActivationRequested.GetInvocationList())
                {
                    if (subscriber is Func<AppActivationRequest, nint> handler)
                    {
                        var candidateHandle = handler(request);
                        if (candidateHandle != default)
                        {
                            requestedHandle = candidateHandle;
                        }
                    }
                }
            }

            return new AppActivationResult(kind, requestedHandle);
        }

        public void StartListening(IUiDispatcher dispatcher)
        {
        }

        private static AppActivationKind ResolveKind(string[]? commandLineArgs)
        {
            if (commandLineArgs is null || commandLineArgs.Length == 0)
            {
                return AppActivationKind.FirstLaunch;
            }

            return File.Exists(commandLineArgs[0])
                ? AppActivationKind.OpenFileRequest
                : AppActivationKind.FirstLaunch;
        }
    }
}
