using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.UI.Xaml.Media;
using Typedown.Core;
using Typedown.Core.Enums;
using Typedown.Core.Interfaces;
using Typedown.Core.Utilities;
using Typedown.Presentation.ViewModels;
using Typedown.Presentation;
using Typedown.Presentation.Interfaces;
using Typedown.WinUI.Controls;
using Typedown.WinUI.Services;
using Typedown.WinUI.Utilities;
using Typedown.WinUI.Views;
using SQLitePCL;
using Microsoft.UI.Xaml;
using System.Reactive.Disposables;
using System.Reactive.Linq;

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
        private CompositeDisposable shellBindings = new();
        private RootControl? rootControl;
        private AppViewModel? appViewModel;

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

            // Set SQLite temp directory before any connection is created, so
            // Microsoft.Data.Sqlite does not probe ApplicationData.Current (which
            // throws APPMODEL_ERROR_NO_PACKAGE in unpackaged WinUI 3 apps).
            var tmp = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "Typedown",
                "temp");
            Directory.CreateDirectory(tmp);
            Environment.SetEnvironmentVariable("SQLITE_TMPDIR", tmp);

            Batteries.Init();

            window ??= new Window();
            platformServices ??= new WinUIPlatformServices(window);
            Config.SetAppDataPathProvider(platformServices.AppDataPathProvider);
            uiServices ??= new ServiceCollection()
                .AddSingleton(platformServices.WindowContext)
                .AddSingleton(platformServices.UiDispatcher)
                .AddSingleton(platformServices.DialogService)
                .AddSingleton(platformServices.FilePickerService)
                .AddSingleton(platformServices.AppActivationService)
                .AddSingleton(platformServices.AppDataPathProvider)
                .AddSingleton<IClipboard, WinUIClipboard>()
                .AddSingleton<IFileConverter, WinUIFileConverter>()
                .AddSingleton<IFileExport, WinUIFileExport>()
                .AddSingleton<IFileOperation, WinUIFileOperation>()
                .AddSingleton<IFloatViewService, WinUIFloatViewService>()
                .AddSingleton<IKeyboardAccelerator, WinUIKeyboardAccelerator>()
                .AddSingleton<IEditorCommandSink, WinUIEditorCommandSink>()
                .AddSingleton<IPowerShellService, WinUIPowerShellService>()
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

            this.rootControl = rootControl;

            rootControl.AttachKeyboardAccelerator(uiServices.GetRequiredService<IKeyboardAccelerator>());
            rootControl.MainPageNavigationParameter = new MainPageNavigationContext(platformServices, uiServices);
            platformServices.WindowContext.ViewRoot = rootControl;
            AttachShellBindings(rootControl);
            platformServices.AppActivationService.StartListening(platformServices.UiDispatcher);
            _ = platformServices.AppActivationService.Activate(Environment.GetCommandLineArgs());
            platformServices.WindowContext.Activate();
        }

        private static void ConfigureNativeTitleBar(Window targetWindow)
        {
            targetWindow.ExtendsContentIntoTitleBar = true;

            var titleBar = targetWindow.AppWindow.TitleBar;
            titleBar.ExtendsContentIntoTitleBar = true;
            titleBar.ButtonBackgroundColor = Microsoft.UI.Colors.Transparent;
            titleBar.ButtonInactiveBackgroundColor = Microsoft.UI.Colors.Transparent;
            titleBar.ButtonHoverBackgroundColor = Microsoft.UI.ColorHelper.FromArgb(32, 128, 128, 128);
            titleBar.ButtonPressedBackgroundColor = Microsoft.UI.ColorHelper.FromArgb(48, 128, 128, 128);
        }

        private void AttachShellBindings(RootControl rootControl)
        {
            shellBindings.Dispose();
            shellBindings = new CompositeDisposable();

            appViewModel = uiServices?.GetService<AppViewModel>();
            if (appViewModel is null)
            {
                return;
            }

            rootControl.ActualThemeChanged -= OnRootControlActualThemeChanged;
            rootControl.ActualThemeChanged += OnRootControlActualThemeChanged;
            shellBindings.Add(Disposable.Create(() => rootControl.ActualThemeChanged -= OnRootControlActualThemeChanged));

            var settings = appViewModel.SettingsViewModel;

            shellBindings.Add(settings.WhenPropertyChanged(nameof(SettingsViewModel.AppTheme))
                .Cast<AppTheme>()
                .StartWith(settings.AppTheme)
                .Subscribe(theme =>
                {
                    ApplyAppTheme(theme);
                    ApplyEditorBackground(settings);
                }));

            shellBindings.Add(settings.WhenPropertyChanged(nameof(SettingsViewModel.UseMicaEffect))
                .Cast<bool>()
                .StartWith(settings.UseMicaEffect)
                .Subscribe(enable =>
                {
                    ApplyMicaEffect(enable);
                    ApplyEditorBackground(settings);
                }));

            shellBindings.Add(settings.WhenPropertyChanged(nameof(SettingsViewModel.UseEditorMicaEffect))
                .Cast<bool>()
                .StartWith(settings.UseEditorMicaEffect)
                .Subscribe(_ => ApplyEditorBackground(settings)));

            shellBindings.Add(settings.WhenPropertyChanged(nameof(SettingsViewModel.Topmost))
                .Cast<bool>()
                .StartWith(settings.Topmost)
                .Subscribe(ApplyTopmost));

            shellBindings.Add(settings.WhenPropertyChanged(nameof(SettingsViewModel.AnimationEnable))
                .Cast<bool>()
                .StartWith(settings.AnimationEnable)
                .Subscribe(rootControl.SetAnimationEnabled));

            ApplyAppTheme(settings.AppTheme);
            ApplyMicaEffect(settings.UseMicaEffect);
            ApplyTopmost(settings.Topmost);
            ApplyEditorBackground(settings);
            rootControl.SetAnimationEnabled(settings.AnimationEnable);
            SyncActualTheme(rootControl.ActualTheme);
        }

        private void ApplyAppTheme(AppTheme theme)
        {
            if (rootControl is null)
            {
                return;
            }

            rootControl.RequestedTheme = theme switch
            {
                AppTheme.Light => ElementTheme.Light,
                AppTheme.Dark => ElementTheme.Dark,
                _ => ElementTheme.Default
            };

            if (theme != AppTheme.Default)
            {
                appViewModel?.UIViewModel.SetActualTheme(theme);
            }
        }

        private void ApplyMicaEffect(bool enable)
        {
            if (window is null)
            {
                return;
            }

            window.SystemBackdrop = Config.IsMicaSupported && enable ? new MicaBackdrop() : null;
        }

        private void ApplyTopmost(bool isTopmost)
        {
            if (window?.AppWindow.Presenter is OverlappedPresenter presenter)
            {
                presenter.IsAlwaysOnTop = isTopmost;
            }
        }

        private void ApplyEditorBackground(SettingsViewModel settings)
        {
            var actualTheme = rootControl?.ActualTheme ?? ElementTheme.Default;
            var useTransparentBackground = Config.IsMicaSupported && settings.UseMicaEffect && settings.UseEditorMicaEffect;
            var color = useTransparentBackground
                ? Colors.Transparent
                : actualTheme == ElementTheme.Light
                    ? Microsoft.UI.ColorHelper.FromArgb(0xFF, 0xF9, 0xF9, 0xF9)
                    : Microsoft.UI.ColorHelper.FromArgb(0xFF, 0x28, 0x28, 0x28);

            Resources["TypedownEditorBackgroundBrush"] = new SolidColorBrush(color);
        }

        private void OnRootControlActualThemeChanged(FrameworkElement sender, object args)
        {
            SyncActualTheme(sender.ActualTheme);

            if (appViewModel is not null)
            {
                ApplyEditorBackground(appViewModel.SettingsViewModel);
            }
        }

        private void SyncActualTheme(ElementTheme actualTheme)
        {
            if (actualTheme != ElementTheme.Light && actualTheme != ElementTheme.Dark)
            {
                return;
            }

            appViewModel?.UIViewModel.SetActualTheme(actualTheme == ElementTheme.Light ? AppTheme.Light : AppTheme.Dark);
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
