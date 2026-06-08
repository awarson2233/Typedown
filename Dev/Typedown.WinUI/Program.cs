using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.Windows.AppLifecycle;
using Typedown.WinUI.Services;

namespace Typedown.WinUI
{
    internal enum WinUIAppInstanceRole
    {
        Main,
        Secondary
    }

    internal static class Program
    {
        internal const string MainInstanceKey = "Typedown.WinUI.Main";
        internal const string NewWindowArgument = "--typedown-new-window";
        private static readonly TimeSpan RedirectActivationTimeout = TimeSpan.FromSeconds(5);
        private static readonly TimeSpan RedirectActivationCancellationTimeout = TimeSpan.FromSeconds(1);

        [STAThread]
        private static void Main(string[] args)
        {
            WinRT.ComWrappersSupport.InitializeComWrappers();

            var currentInstance = AppInstance.GetCurrent();
            var initialActivationArgs = currentInstance.GetActivatedEventArgs();
            var isSecondaryWindowLaunch = HasNewWindowBypass(args);
            var instanceRole = isSecondaryWindowLaunch
                ? WinUIAppInstanceRole.Secondary
                : WinUIAppInstanceRole.Main;
            WinUIActivationBroker? activationBroker = null;

            if (!isSecondaryWindowLaunch)
            {
                activationBroker = new WinUIActivationBroker(currentInstance);
                var mainInstance = AppInstance.FindOrRegisterForKey(MainInstanceKey);
                if (!mainInstance.IsCurrent)
                {
                    if (TryRedirectActivation(mainInstance, initialActivationArgs))
                    {
                        activationBroker.Dispose();
                        return;
                    }

                    instanceRole = WinUIAppInstanceRole.Secondary;
                    mainInstance = AppInstance.FindOrRegisterForKey(MainInstanceKey);
                    if (mainInstance.IsCurrent)
                    {
                        Debug.WriteLine("Typedown activation redirection target was unavailable; current process registered as main instance on retry.");
                        instanceRole = WinUIAppInstanceRole.Main;
                        activationBroker.EnableKeyUnregistrationOnDispose();
                    }
                    else
                    {
                        activationBroker.Dispose();
                        activationBroker = null;
                    }
                }
                else
                {
                    activationBroker.EnableKeyUnregistrationOnDispose();
                }
            }

            Application.Start(_ =>
            {
                var context = new DispatcherQueueSynchronizationContext(DispatcherQueue.GetForCurrentThread());
                SynchronizationContext.SetSynchronizationContext(context);
                new App(initialActivationArgs, instanceRole, activationBroker);
            });
        }

        private static bool HasNewWindowBypass(string[] args)
        {
            return args.Any(arg => string.Equals(arg, NewWindowArgument, StringComparison.OrdinalIgnoreCase));
        }

        private static bool TryRedirectActivation(AppInstance mainInstance, AppActivationArguments? initialActivationArgs)
        {
            if (initialActivationArgs is null)
            {
                Debug.WriteLine("Typedown activation redirection skipped because activation arguments were unavailable.");
                return false;
            }

            var redirectCompletion = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            var redirectCancellation = new CancellationTokenSource();
            var disposeRedirectCancellation = true;
            var redirectCanceled = false;
            Exception? redirectError = null;

            try
            {
                ThreadPool.QueueUserWorkItem(async _ =>
                {
                    try
                    {
                        await mainInstance.RedirectActivationToAsync(initialActivationArgs)
                            .AsTask(redirectCancellation.Token)
                            .ConfigureAwait(false);
                    }
                    catch (OperationCanceledException) when (redirectCancellation.IsCancellationRequested)
                    {
                        redirectCanceled = true;
                    }
                    catch (Exception ex)
                    {
                        redirectError = ex;
                    }
                    finally
                    {
                        redirectCompletion.TrySetResult(true);
                    }
                });

                if (!redirectCompletion.Task.Wait(RedirectActivationTimeout))
                {
                    Debug.WriteLine($"Typedown activation redirection timed out after {RedirectActivationTimeout.TotalSeconds:N0} seconds; canceling pending redirect.");

                    try
                    {
                        redirectCancellation.Cancel();
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Typedown activation redirection cancellation request failed; exiting without fallback to avoid handling the same activation twice: {ex}");
                        disposeRedirectCancellation = false;
                        return true;
                    }

                    if (!redirectCompletion.Task.Wait(RedirectActivationCancellationTimeout))
                    {
                        Debug.WriteLine($"Typedown activation redirection cancellation did not complete within {RedirectActivationCancellationTimeout.TotalSeconds:N0} seconds; exiting without fallback to avoid handling the same activation twice.");
                        disposeRedirectCancellation = false;
                        return true;
                    }
                }

                if (redirectCanceled)
                {
                    Debug.WriteLine("Typedown activation redirection was canceled after timeout; continuing in this process.");
                    return false;
                }

                if (redirectError is not null)
                {
                    Debug.WriteLine($"Typedown activation redirection failed; continuing in this process: {redirectError}");
                    return false;
                }

                return true;
            }
            finally
            {
                if (disposeRedirectCancellation)
                {
                    redirectCancellation.Dispose();
                }
            }
        }
    }
}
