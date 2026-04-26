using System;
using System.IO;
using System.IO.Pipes;
using System.Threading;
using System.Threading.Tasks;
using Typedown.Core.Interfaces;
using Typedown.Core.Utilities;

namespace Typedown.Services
{
    public class AppActivationService : IAppActivationService, IDisposable
    {
        private const string MutexName = "Typedown.App.Mutex";
        private const string PipeName = "Typedown.App.PiPe";

        private readonly Mutex mutex = new(false, MutexName);
        private readonly object syncRoot = new();

        private Task listenLoopTask;
        private bool ownsMutex;

        public event Func<AppActivationRequest, nint> ActivationRequested;

        public AppActivationResult Activate(string[] commandLineArgs)
        {
            commandLineArgs ??= Array.Empty<string>();

            if (TryAcquirePrimaryInstance())
                return new(AppActivationKind.FirstLaunch);

            if (TryForwardToPrimaryInstance(commandLineArgs, out var windowHandle))
                return new(AppActivationKind.ForwardedToExistingInstance, windowHandle);

            return new(AppActivationKind.ForwardFailedStartNewInstance);
        }

        public void StartListening(IUiDispatcher dispatcher)
        {
            ArgumentNullException.ThrowIfNull(dispatcher);

            lock (syncRoot)
            {
                listenLoopTask ??= ListenAsync(dispatcher);
            }
        }

        private bool TryAcquirePrimaryInstance()
        {
            if (ownsMutex)
                return true;

            try
            {
                ownsMutex = mutex.WaitOne(TimeSpan.Zero);
            }
            catch (AbandonedMutexException)
            {
                ownsMutex = true;
            }

            return ownsMutex;
        }

        private async Task ListenAsync(IUiDispatcher dispatcher)
        {
            while (true)
            {
                try
                {
                    using var server = new NamedPipeServerStream(PipeName, PipeDirection.InOut, 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous);
                    await server.WaitForConnectionAsync();
                    using var reader = new StreamReader(server);
                    using var writer = new StreamWriter(server) { AutoFlush = true };
                    var request = new AppActivationRequest(AppActivationKind.OpenFileRequest, ParseArgs(await reader.ReadLineAsync()));
                    var windowHandle = await dispatcher.RunIdleAsync(() => RaiseActivationRequested(request));
                    await writer.WriteLineAsync(windowHandle.ToString());
                }
                catch (Exception)
                {
                    await Task.Delay(1000);
                }
            }
        }

        private bool TryForwardToPrimaryInstance(string[] commandLineArgs, out nint windowHandle)
        {
            windowHandle = default;

            try
            {
                using var client = new NamedPipeClientStream(".", PipeName, PipeDirection.InOut);
                client.Connect();
                using var reader = new StreamReader(client);
                using var writer = new StreamWriter(client) { AutoFlush = true };
                writer.WriteLine(string.Join("\0", commandLineArgs));

                if (long.TryParse(reader.ReadLine(), out var handle))
                {
                    windowHandle = (nint)handle;
                    if (windowHandle != default)
                        PInvoke.SetForegroundWindow(windowHandle);
                }

                return true;
            }
            catch
            {
                return false;
            }
        }

        private static string[] ParseArgs(string line)
        {
            return string.IsNullOrEmpty(line)
                ? Array.Empty<string>()
                : line.Split("\0");
        }

        private nint RaiseActivationRequested(AppActivationRequest request)
        {
            return ActivationRequested?.Invoke(request) ?? default;
        }

        public void Dispose()
        {
            if (ownsMutex)
            {
                mutex.ReleaseMutex();
                ownsMutex = false;
            }

            mutex.Dispose();
        }
    }
}
