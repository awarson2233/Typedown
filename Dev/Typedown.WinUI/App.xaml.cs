using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.UI.Xaml.Media;
using Typedown.Core;
using Typedown.Presentation;
using Typedown.Presentation.Interfaces;
using Typedown.WinUI.Controls;
using Typedown.WinUI.Services;
using Typedown.WinUI.Utilities;
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
            WinUILocale.Initialize();
            window ??= new Window();
            platformServices ??= new WinUIPlatformServices(window);
            uiServices ??= new ServiceCollection()
                .AddSingleton(platformServices.WindowContext)
                .AddSingleton(platformServices.UiDispatcher)
                .AddSingleton(platformServices.DialogService)
                .AddSingleton(platformServices.FilePickerService)
                .AddSingleton(platformServices.AppActivationService)
                .AddSingleton(platformServices.AppDataPathProvider)
                .AddSingleton<IClipboard, WinUIClipboard>()
                .AddSingleton<IFileExport, WinUIFileExport>()
                .AddSingleton<IFileOperation, WinUIFileOperation>()
                .AddSingleton<IFloatViewService, WinUIFloatViewService>()
                .AddSingleton<IKeyboardAccelerator, WinUIKeyboardAccelerator>()
                .AddSingleton<IEditorCommandSink, WinUIEditorCommandSink>()
                .AddSingleton<IEditorSettingsNotifier, WinUIEditorSettingsNotifier>()
                .AddSingleton<ITableDialogService, WinUITableDialogService>()
                .AddSingleton<IWindowService, WinUIWindowService>()
                .AddTypedownCore()
                .AddTypedownPresentation()
                .BuildServiceProvider();
            platformServices.WindowContext.Title = "Typedown";
            ConfigureNativeTitleBar(window);

            if (window.Content is not RootControl rootControl)
            {
                rootControl = new RootControl();
                window.Content = rootControl;
                window.SetTitleBar(rootControl.TitleBarElement);
            }

            rootControl.MainPageNavigationParameter = new MainPageNavigationContext(platformServices, uiServices);
            platformServices.WindowContext.ViewRoot = rootControl;
            platformServices.AppActivationService.StartListening(platformServices.UiDispatcher);
            _ = platformServices.AppActivationService.Activate(Environment.GetCommandLineArgs());
            platformServices.WindowContext.Activate();
        }

        private static void ConfigureNativeTitleBar(Window targetWindow)
        {
            targetWindow.ExtendsContentIntoTitleBar = true;
            targetWindow.SystemBackdrop = new MicaBackdrop();

            var titleBar = targetWindow.AppWindow.TitleBar;
            titleBar.ExtendsContentIntoTitleBar = true;
            titleBar.ButtonBackgroundColor = Microsoft.UI.Colors.Transparent;
            titleBar.ButtonInactiveBackgroundColor = Microsoft.UI.Colors.Transparent;
            titleBar.ButtonHoverBackgroundColor = Microsoft.UI.ColorHelper.FromArgb(32, 128, 128, 128);
            titleBar.ButtonPressedBackgroundColor = Microsoft.UI.ColorHelper.FromArgb(48, 128, 128, 128);
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
