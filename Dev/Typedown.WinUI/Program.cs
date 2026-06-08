using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
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

        [STAThread]
        private static void Main(string[] args)
        {
            WinRT.ComWrappersSupport.InitializeComWrappers();

            var initialActivationArgs = AppInstance.GetCurrent().GetActivatedEventArgs();
            var isSecondaryWindowLaunch = HasNewWindowBypass(args);
            var instanceRole = isSecondaryWindowLaunch
                ? WinUIAppInstanceRole.Secondary
                : WinUIAppInstanceRole.Main;
            WinUIActivationBroker? activationBroker = null;

            if (!isSecondaryWindowLaunch)
            {
                var mainInstance = AppInstance.FindOrRegisterForKey(MainInstanceKey);
                if (!mainInstance.IsCurrent)
                {
                    RedirectActivationAndExit(mainInstance, initialActivationArgs);
                    return;
                }

                activationBroker = new WinUIActivationBroker(mainInstance);
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

        private static void RedirectActivationAndExit(AppInstance mainInstance, AppActivationArguments initialActivationArgs)
        {
            using var completion = new ManualResetEventSlim();
            Exception? redirectError = null;

            ThreadPool.QueueUserWorkItem(async _ =>
            {
                try
                {
                    await mainInstance.RedirectActivationToAsync(initialActivationArgs);
                }
                catch (Exception ex)
                {
                    redirectError = ex;
                }
                finally
                {
                    completion.Set();
                }
            });

            completion.Wait();

            if (redirectError is not null)
            {
                Debug.WriteLine($"Typedown activation redirection failed: {redirectError}");
            }
        }
    }
}
