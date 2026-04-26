using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;
using Typedown.Core.Interfaces;
using Typedown.Core.Utilities;
using Typedown.Windows;
using Typedown.XamlUI;
using Windows.UI.Xaml.Markup;

namespace Typedown
{
    public class App : XamlApplication
    {
        private App(IEnumerable<IXamlMetadataProvider> providers) : base(providers) { }

        public static void Launch()
        {
            var activationService = Injection.ServiceProvider.GetRequiredService<IAppActivationService>();
            var activationResult = activationService.Activate(System.Environment.GetCommandLineArgs());
            if (activationResult.Kind == AppActivationKind.FirstLaunch
                || activationResult.Kind == AppActivationKind.ForwardFailedStartNewInstance)
                LaunchNewApplication();
        }

        public static void LaunchNewApplication()
        {
            var providers = new List<IXamlMetadataProvider>() { new Core.Typedown_Core_XamlTypeInfo.XamlMetaDataProvider() };
            var xamlApp = new App(providers) { Resources = new Core.Resources() };
            xamlApp.Run();
        }

        protected override async void OnLaunched()
        {
            base.OnLaunched();
            if (!await EnvCheck.EnsureWebView2Installed())
            {
                Exit();
                return;
            }

            var window = new MainWindow();
            window.Show(ShowWindowCommand.SW_HIDE);

            var activationService = Injection.ServiceProvider.GetRequiredService<IAppActivationService>();
            activationService.ActivationRequested += HandleActivationRequested;
            activationService.StartListening(window.ServiceProvider.GetRequiredService<IUiDispatcher>());
        }

        private static nint HandleActivationRequested(AppActivationRequest request)
        {
            return request.Kind == AppActivationKind.OpenFileRequest
                ? Utilities.Common.OpenNewWindow(request.CommandLineArgs)
                : default;
        }
    }
}
