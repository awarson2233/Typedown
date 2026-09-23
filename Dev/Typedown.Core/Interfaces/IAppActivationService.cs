using System;

namespace Typedown.Core.Interfaces
{
    public enum AppActivationKind
    {
        FirstLaunch,
        ForwardedToExistingInstance,
        ForwardFailedStartNewInstance,
        OpenFileRequest
    }

    public enum AppActivationSource
    {
        InitialLaunch,
        Redirected
    }

    public sealed class AppActivationRequest
    {
        public AppActivationRequest(
            AppActivationKind kind,
            string[] commandLineArgs,
            AppActivationSource source = AppActivationSource.InitialLaunch)
        {
            Kind = kind;
            CommandLineArgs = commandLineArgs ?? Array.Empty<string>();
            Source = source;
        }

        public AppActivationKind Kind { get; }

        public string[] CommandLineArgs { get; }

        public AppActivationSource Source { get; }
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
