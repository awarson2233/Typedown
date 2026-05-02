using System;

namespace Typedown.Presentation.Interfaces
{
    public enum AppActivationKind
    {
        FirstLaunch,
        ForwardedToExistingInstance,
        ForwardFailedStartNewInstance,
        OpenFileRequest
    }

    public sealed class AppActivationRequest
    {
        public AppActivationRequest(AppActivationKind kind, string[] commandLineArgs)
        {
            Kind = kind;
            CommandLineArgs = commandLineArgs ?? Array.Empty<string>();
        }

        public AppActivationKind Kind { get; }

        public string[] CommandLineArgs { get; }
    }

    public sealed class AppActivationResult
    {
        public AppActivationResult(AppActivationKind kind, nint windowHandle = default)
        {
            Kind = kind;
            WindowHandle = windowHandle;
        }

        public AppActivationKind Kind { get; }

        public nint WindowHandle { get; }
    }

    public interface IAppActivationService
    {
        event Func<AppActivationRequest, nint> ActivationRequested;

        AppActivationResult Activate(string[] commandLineArgs);

        void StartListening(IUiDispatcher dispatcher);
    }
}
