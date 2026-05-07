using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.UI.Xaml.Media;
using Microsoft.Windows.AppLifecycle;
using System.Diagnostics;
using System.Linq;
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
using FileActivatedEventArgsContract = Windows.ApplicationModel.Activation.IFileActivatedEventArgs;

namespace Typedown.WinUI
{
    /// <summary>
    /// Provides application-specific behavior to supplement the default Application class.
    /// </summary>
    public partial class App : Application
    {
        private Window? window;
        private WinUIPlatformServices? platformServices;
        private ServiceProvider? rootServices;
        private IServiceScope? uiScope;
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
            using (StartupTrace.Phase("App.InitializeComponent"))
            {
                this.InitializeComponent();
            }
        }

        /// <summary>
        /// Invoked when the application is launched normally by the end user.  Other entry points
        /// will be used such as when the application is launched to open a specific file.
        /// </summary>
        /// <param name="e">Details about the launch request and process.</param>
        protected override void OnLaunched(LaunchActivatedEventArgs e)
        {
            StartupTrace.Mark("App.OnLaunched entered");
            var startupCommandLineArgs = ResolveStartupCommandLineArgs();
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

            using (StartupTrace.Phase("SQLite Batteries.Init"))
            {
                Batteries.Init();
            }

            if (window is null)
            {
                window = new Window();
                window.Closed += OnWindowClosed;
                var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(window);
                var windowId = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(hwnd);
                var appWindow = Microsoft.UI.Windowing.AppWindow.GetFromWindowId(windowId);
                appWindow.SetIcon("Assets/logo.ico");
            }

            platformServices ??= new WinUIPlatformServices(window);
            platformServices.WebViewEnvironmentService.StartPrewarm();
            Config.SetAppDataPathProvider(platformServices.AppDataPathProvider);
            if (rootServices is null)
            {
                using (StartupTrace.Phase("Build service provider"))
                {
                    rootServices = new ServiceCollection()
                        .AddSingleton(platformServices.WindowContext)
                        .AddSingleton(platformServices.UiDispatcher)
                        .AddSingleton(platformServices.DialogService)
                        .AddSingleton(platformServices.FilePickerService)
                        .AddSingleton(platformServices.AppActivationService)
                        .AddSingleton(platformServices.AppDataPathProvider)
                        .AddSingleton(platformServices.WebViewEnvironmentService)
                        .AddSingleton<IClipboard, WinUIClipboard>()
                        .AddSingleton<IFileConverter, WinUIFileConverter>()
                        .AddSingleton<IFileExport, WinUIFileExport>()
                        .AddSingleton<IFileOperation, WinUIFileOperation>()
                        .AddScoped<IFloatViewService, WinUIFloatViewService>()
                        .AddSingleton<IKeyboardAccelerator, WinUIKeyboardAccelerator>()
                        .AddSingleton<IEditorCommandSink, WinUIEditorCommandSink>()
                        .AddSingleton<IPowerShellService, WinUIPowerShellService>()
                        .AddSingleton<IEditorSettingsNotifier, WinUIEditorSettingsNotifier>()
                        .AddSingleton<ITableDialogService, WinUITableDialogService>()
                        .AddSingleton<IWindowService, WinUIWindowService>()
                        .AddTypedownCore()
                        .AddTypedownPresentation()
                        .BuildServiceProvider();
                }
            }

            if (uiScope is null)
            {
                uiScope = rootServices.CreateScope();
                uiServices = uiScope.ServiceProvider;
            }

            uiServices.GetRequiredService<AppViewModel>().CommandLineArgs = startupCommandLineArgs;

            platformServices.WindowContext.Title = "Typedown";
            ConfigureNativeTitleBar(window);

            if (window.Content is not RootControl rootControl)
            {
                using (StartupTrace.Phase("RootControl create"))
                {
                    rootControl = new RootControl();
                }
                window.Content = rootControl;
                window.SetTitleBar(rootControl.TitleBarElement);
            }

            this.rootControl = rootControl;

            rootControl.AttachKeyboardAccelerator(uiServices!.GetRequiredService<IKeyboardAccelerator>());
            rootControl.MainPageNavigationParameter = new MainPageNavigationContext(platformServices, uiServices!);
            platformServices.WindowContext.ViewRoot = rootControl;
            using (StartupTrace.Phase("Attach shell bindings"))
            {
                AttachShellBindings(rootControl);
            }
            platformServices.AppActivationService.StartListening(platformServices.UiDispatcher);
            StartupTrace.Mark("Activation service listening");
            _ = platformServices.AppActivationService.Activate(startupCommandLineArgs);
            StartupTrace.Mark("App activation request dispatched");
            platformServices.WindowContext.Activate();
            StartupTrace.Mark("Window activated");
        }

        private static string[] ResolveStartupCommandLineArgs()
        {
            var baseProcessPath = Environment.ProcessPath
                ?? Environment.GetCommandLineArgs().FirstOrDefault()
                ?? "Typedown.WinUI";

            try
            {
                var activationArgs = AppInstance.GetCurrent().GetActivatedEventArgs();
                if (activationArgs?.Kind == ExtendedActivationKind.File
                    && activationArgs.Data is FileActivatedEventArgsContract fileArgs)
                {
                    var filePaths = fileArgs.Files
                        .Select(x => x.Path)
                        .Where(x => !string.IsNullOrWhiteSpace(x))
                        .ToArray();

                    if (filePaths.Length > 0)
                    {
                        return [baseProcessPath, .. filePaths];
                    }
                }
            }
            catch
            {
                // Fall back to the raw command line when packaged activation data is unavailable.
            }

            return Environment.GetCommandLineArgs();
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

        private void OnWindowClosed(object sender, WindowEventArgs args)
        {
            if (sender is Window closedWindow)
            {
                closedWindow.Closed -= OnWindowClosed;
            }

            shellBindings.Dispose();
            uiScope?.Dispose();
            rootServices?.Dispose();
            uiScope = null;
            rootServices = null;
            uiServices = null;
            appViewModel = null;
            rootControl = null;
            platformServices = null;
            window = null;
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
            shellBindings.Add(appViewModel.FileViewModel.NewWindowCommand.OnExecute.Subscribe(OpenNewWindowInNewProcess));

            ApplyAppTheme(settings.AppTheme);
            ApplyMicaEffect(settings.UseMicaEffect);
            ApplyTopmost(settings.Topmost);
            ApplyEditorBackground(settings);
            rootControl.SetAnimationEnabled(settings.AnimationEnable);
            SyncActualTheme(rootControl.ActualTheme);
        }

        private static void OpenNewWindowInNewProcess(string? filePath)
        {
            var processPath = Environment.ProcessPath;
            if (string.IsNullOrWhiteSpace(processPath))
            {
                throw new InvalidOperationException("Environment.ProcessPath is unavailable for WinUI new-window launch.");
            }

            var startInfo = new ProcessStartInfo
            {
                FileName = processPath,
                UseShellExecute = true
            };

            if (!string.IsNullOrWhiteSpace(filePath))
            {
                startInfo.ArgumentList.Add(filePath);
            }

            Process.Start(startInfo);
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

