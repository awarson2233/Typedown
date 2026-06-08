using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.Windows.AppLifecycle;
using Typedown.Presentation.Interfaces;
using Windows.ApplicationModel.Activation;
using Windows.Storage;

namespace Typedown.WinUI.Services
{
    internal sealed class WinUIAppActivationService : IAppActivationService, IDisposable
    {
        private readonly IWindowContext windowContext;
        private IUiDispatcher? dispatcher;
        private bool isListening;
        private bool disposed;

        public WinUIAppActivationService(IWindowContext windowContext)
        {
            this.windowContext = windowContext ?? throw new ArgumentNullException(nameof(windowContext));
        }

        public event Func<AppActivationRequest, nint>? ActivationRequested;

        public AppActivationResult Activate(string[] commandLineArgs)
        {
            var userArgs = commandLineArgs?.Skip(1).ToArray() ?? Array.Empty<string>();
            var kind = ResolveKind(userArgs);
            var request = new AppActivationRequest(kind, userArgs);
            return DispatchActivationRequest(request);
        }

        public void StartListening(IUiDispatcher dispatcher)
        {
            if (disposed)
            {
                throw new ObjectDisposedException(nameof(WinUIAppActivationService));
            }

            this.dispatcher = dispatcher ?? throw new ArgumentNullException(nameof(dispatcher));

            if (isListening)
            {
                return;
            }

            AppInstance.GetCurrent().Activated += OnAppInstanceActivated;
            isListening = true;
        }

        public void StopListening()
        {
            if (!isListening)
            {
                return;
            }

            AppInstance.GetCurrent().Activated -= OnAppInstanceActivated;
            isListening = false;
            dispatcher = null;
        }

        public void Dispose()
        {
            if (disposed)
            {
                return;
            }

            StopListening();
            ActivationRequested = null;
            disposed = true;
        }

        private async void OnAppInstanceActivated(object? sender, AppActivationArguments args)
        {
            try
            {
                var request = CreateActivationRequest(args);
                var activeDispatcher = dispatcher;
                if (activeDispatcher is null)
                {
                    DispatchActivationRequest(request);
                    return;
                }

                await activeDispatcher.RunAsync(() => DispatchActivationRequest(request));
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"WinUI app activation dispatch failed: {ex}");
            }
        }

        private AppActivationResult DispatchActivationRequest(AppActivationRequest request)
        {
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

            return new AppActivationResult(request.Kind, requestedHandle);
        }

        private static AppActivationRequest CreateActivationRequest(AppActivationArguments args)
        {
            var filePaths = GetActivatedFilePaths(args).ToArray();
            if (filePaths.Length > 0)
            {
                return new AppActivationRequest(AppActivationKind.OpenFileRequest, filePaths);
            }

            var commandLineArgs = GetCommandLineArgs(args).ToArray();
            return new AppActivationRequest(ResolveKind(commandLineArgs, AppActivationKind.ForwardedToExistingInstance), commandLineArgs);
        }

        private static IEnumerable<string> GetActivatedFilePaths(AppActivationArguments args)
        {
            if (args.Data is IFileActivatedEventArgs fileArgs)
            {
                foreach (var file in fileArgs.Files.OfType<StorageFile>())
                {
                    if (!string.IsNullOrEmpty(file.Path))
                    {
                        yield return file.Path;
                    }
                }
            }
        }

        private static IEnumerable<string> GetCommandLineArgs(AppActivationArguments args)
        {
            if (args.Data is CommandLineActivatedEventArgs commandLineArgs)
            {
                return ParseArgumentString(commandLineArgs.Operation.Arguments);
            }

            if (args.Data is LaunchActivatedEventArgs launchArgs)
            {
                return ParseArgumentString(launchArgs.Arguments);
            }

            return Array.Empty<string>();
        }

        private static AppActivationKind ResolveKind(string[]? commandLineArgs, AppActivationKind emptyKind = AppActivationKind.FirstLaunch)
        {
            if (commandLineArgs is null || commandLineArgs.Length == 0)
            {
                if (emptyKind == AppActivationKind.FirstLaunch)
                {
                    return AppActivationKind.FirstLaunch;
                }

                return emptyKind;
            }

            if (emptyKind == AppActivationKind.FirstLaunch)
            {
                return File.Exists(commandLineArgs[0])
                    ? AppActivationKind.OpenFileRequest
                    : AppActivationKind.FirstLaunch;
            }

            return commandLineArgs.Any(File.Exists)
                ? AppActivationKind.OpenFileRequest
                : emptyKind;
        }

        private static string[] ParseArgumentString(string? arguments)
        {
            if (string.IsNullOrWhiteSpace(arguments))
            {
                return Array.Empty<string>();
            }

            var result = new List<string>();
            var current = new StringBuilder(arguments.Length);
            var inQuotes = false;
            var hasArgument = false;

            for (var i = 0; i < arguments.Length; i++)
            {
                var character = arguments[i];
                if (character == '\\')
                {
                    var slashStart = i;
                    while (i < arguments.Length && arguments[i] == '\\')
                    {
                        i++;
                    }

                    var slashCount = i - slashStart;
                    if (i < arguments.Length && arguments[i] == '"')
                    {
                        current.Append('\\', slashCount / 2);
                        hasArgument = true;
                        if (slashCount % 2 == 0)
                        {
                            inQuotes = !inQuotes;
                        }
                        else
                        {
                            current.Append('"');
                        }
                    }
                    else
                    {
                        current.Append('\\', slashCount);
                        hasArgument = true;
                        i--;
                    }

                    continue;
                }

                if (character == '"')
                {
                    inQuotes = !inQuotes;
                    hasArgument = true;
                    continue;
                }

                if (char.IsWhiteSpace(character) && !inQuotes)
                {
                    AddCurrentArgument(result, current, ref hasArgument);
                    continue;
                }

                current.Append(character);
                hasArgument = true;
            }

            AddCurrentArgument(result, current, ref hasArgument);
            return result.ToArray();
        }

        private static void AddCurrentArgument(List<string> result, StringBuilder current, ref bool hasArgument)
        {
            if (!hasArgument)
            {
                return;
            }

            result.Add(current.ToString());
            current.Clear();
            hasArgument = false;
        }
    }
}
