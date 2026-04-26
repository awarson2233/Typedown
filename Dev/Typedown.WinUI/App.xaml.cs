using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml.Navigation;
using Typedown.UI.Composition;
using Typedown.WinUI.Services;
using Typedown.WinUI.Views;

namespace Typedown.WinUI
{
    /// <summary>
    /// Provides application-specific behavior to supplement the default Application class.
    /// </summary>
    public partial class App : Application
    {
        private Window? window;
        private WinUIPlatformServices? platformServices;
        private IServiceProvider? uiServices;

        /// <summary>
        /// Initializes the singleton application object.  This is the first line of authored code
        /// executed, and as such is the logical equivalent of main() or WinMain().
        /// </summary>
        public App()
        {
            this.InitializeComponent();
        }

        /// <summary>
        /// Invoked when the application is launched normally by the end user.  Other entry points
        /// will be used such as when the application is launched to open a specific file.
        /// </summary>
        /// <param name="e">Details about the launch request and process.</param>
        protected override void OnLaunched(LaunchActivatedEventArgs e)
        {
            window ??= new Window();
            platformServices ??= new WinUIPlatformServices(window);
            uiServices ??= new ServiceCollection()
                .AddTypedownUI()
                .BuildServiceProvider();
            platformServices.WindowContext.Title = "Typedown WinUI3 Phase 10b Platform Services";

            if (window.Content is not Frame rootFrame)
            {
                rootFrame = new Frame();
                rootFrame.NavigationFailed += OnNavigationFailed;
                window.Content = rootFrame;
            }

            platformServices.WindowContext.ViewRoot = rootFrame;
            _ = rootFrame.Navigate(typeof(MainPage), new MainPageNavigationContext(platformServices, uiServices));
            platformServices.AppActivationService.StartListening(platformServices.UiDispatcher);
            _ = platformServices.AppActivationService.Activate(Environment.GetCommandLineArgs());
            platformServices.WindowContext.Activate();
        }

        internal WinUIPlatformServices PlatformServices => platformServices ?? throw new InvalidOperationException("Platform services are not initialized.");

        private sealed record MainPageNavigationContext(
            WinUIPlatformServices PlatformServices,
            IServiceProvider UiServices);

        /// <summary>
        /// Invoked when Navigation to a certain page fails
        /// </summary>
        /// <param name="sender">The Frame which failed navigation</param>
        /// <param name="e">Details about the navigation failure</param>
        void OnNavigationFailed(object sender, NavigationFailedEventArgs e)
        {
            throw new Exception("Failed to load Page " + e.SourcePageType.FullName);
        }
    }
}
