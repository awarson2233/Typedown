using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;
using Typedown.Core.Interfaces;
using Typedown.Core.Utilities;
using Typedown.Presentation.Interfaces;
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
            AppLocale.Initialize();
            var providers = new List<IXamlMetadataProvider>() { CreateXamlMetadataProvider() };
            var xamlApp = new App(providers) { Resources = new Core.Resources() };
            xamlApp.Run();
        }

        private static IXamlMetadataProvider CreateXamlMetadataProvider()
        {
            var providerType = typeof(App).Assembly.GetType("Typedown.Typedown_XamlTypeInfo.XamlMetaDataProvider");
            return (IXamlMetadataProvider)System.Activator.CreateInstance(providerType);
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
            // Phase 6 keeps the legacy boundary: the first window's scoped dispatcher owns pipe callbacks.
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
