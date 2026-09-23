using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.UI.Xaml.Media;
using Microsoft.Windows.AppLifecycle;
using System.Diagnostics;
using System.Linq;
using Typedown.Core;
using Typedown.Core.Editor;
using Typedown.Core.Enums;
using Typedown.Core.Interfaces;
using Typedown.Core.Services;
using Typedown.Core.Utilities;
using Typedown.Core.ViewModels;
using Typedown.WinUI.Controls;
using Typedown.WinUI.Services;
using Typedown.WinUI.Utilities;
using Typedown.WinUI.Views;
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
        private readonly AppActivationArguments? initialActivationArgs;
        private readonly WinUIAppInstanceRole instanceRole;
        private readonly WinUIActivationBroker? activationBroker;
        private Window? window;
        private WinUIPlatformServices? platformServices;
        private ServiceProvider? rootServices;
        private IServiceScope? uiScope;
        private IServiceProvider? uiServices;
        private CompositeDisposable shellBindings = new();
        private RootControl? rootControl;
        private AppViewModel? appViewModel;
        private bool allowWindowClose;
        private bool windowCloseInProgress;

        /// <summary>
        /// Initializes the singleton application object.  This is the first line of authored code
        /// executed, and as such is the logical equivalent of main() or WinMain().
        /// </summary>
        public App()
            : this(null, WinUIAppInstanceRole.Main, null)
        {
        }

        internal App(
            AppActivationArguments? initialActivationArgs,
            WinUIAppInstanceRole instanceRole,
            WinUIActivationBroker? activationBroker)
        {
            this.initialActivationArgs = initialActivationArgs;
            this.instanceRole = instanceRole;
            this.activationBroker = activationBroker;
            Config.SetAppDataPathProvider(new WinUIAppDataPathProvider());
            WinUILocale.ApplyPersistedLanguageOverride();

            StartupTrace.AppInitializeComponentStart();
            try
            {
                this.InitializeComponent();
            }
            finally
            {
                StartupTrace.AppInitializeComponentStop();
            }
        }

        /// <summary>
        /// Invoked when the application is launched normally by the end user.  Other entry points
        /// will be used such as when the application is launched to open a specific file.
        /// </summary>
        /// <param name="e">Details about the launch request and process.</param>
        protected override void OnLaunched(LaunchActivatedEventArgs e)
        {
            StartupTrace.AppOnLaunchedStart();
            try
            {
                OnLaunchedCore();
            }
            finally
            {
                StartupTrace.AppOnLaunchedStop();
            }
        }

        private void OnLaunchedCore()
        {
            var startupCommandLineArgs = ResolveStartupCommandLineArgs();
            WinUILocale.Initialize();

            if (window is null)
            {
                window = new Window();
                window.Closed += OnWindowClosed;
                var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(window);
                var windowId = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(hwnd);
                var appWindow = Microsoft.UI.Windowing.AppWindow.GetFromWindowId(windowId);
                appWindow.SetIcon(Path.Combine(AppContext.BaseDirectory, "Assets", "logo.ico"));
                appWindow.Closing -= OnAppWindowClosing;
                appWindow.Closing += OnAppWindowClosing;
            }

            platformServices ??= new WinUIPlatformServices(window, activationBroker);
            platformServices.WebViewEnvironmentService.StartPrewarm();
            Config.SetAppDataPathProvider(platformServices.AppDataPathProvider);
            WinUILocale.ApplyPersistedLanguageOverride();
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
                        // 语义上属于窗口：keyEvents 是共享 Subject。目前每个窗口独占一个进程和一个 uiScope，
                        // Scoped 与单例运行时等价；将来若同一进程承载多个窗口，单例会让快捷键触发所有窗口的菜单项。
                        .AddScoped<IKeyboardAccelerator, WinUIKeyboardAccelerator>()
                        .AddScoped<LegacyMuyaSession>()
                        .AddScoped<IEditorSession>(sp => sp.GetRequiredService<LegacyMuyaSession>())
                        .AddSingleton<IPowerShellService, WinUIPowerShellService>()
                        .AddSingleton<ITableDialogService, WinUITableDialogService>()
                        .AddSingleton<IWindowService, WinUIWindowService>()
                        .AddTypedownCore()
                        .BuildServiceProvider();
                }
            }

            if (uiScope is null)
            {
                uiScope = rootServices.CreateScope();
                uiServices = uiScope.ServiceProvider;
            }

            if (uiServices is null)
            {
                throw new InvalidOperationException("Typedown UI services are not initialized.");
            }

            uiServices.GetRequiredService<AppViewModel>().CommandLineArgs = startupCommandLineArgs;
            StartStartupDocumentPrefetch(uiServices, startupCommandLineArgs);

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

            rootControl.AttachKeyboardAccelerator(uiServices.GetRequiredService<IKeyboardAccelerator>());
            rootControl.MainPageNavigationParameter = new MainPageNavigationContext(platformServices, uiServices);
            platformServices.WindowContext.ViewRoot = rootControl;
            StartupTrace.ShellBindingsStart();
            try
            {
                using (StartupTrace.Phase("Attach shell bindings"))
                {
                    AttachShellBindings(rootControl);
                }
            }
            catch
            {
                StartupTrace.ShellBindingsFailure();
                throw;
            }
            finally
            {
                StartupTrace.ShellBindingsStop();
            }

            platformServices.AppActivationService.StartListening(platformServices.UiDispatcher);
            StartupTrace.Mark("Activation service listening");
            _ = platformServices.AppActivationService.Activate(startupCommandLineArgs);
            StartupTrace.Mark("App activation request dispatched");
            StartupTrace.WindowActivateStart();
            try
            {
                platformServices.WindowContext.Activate();
            }
            finally
            {
                StartupTrace.WindowActivateStop();
            }
        }

        /// <summary>
        /// 启动文档在这里就开始读（线程池），与 WebView2 环境预热、XAML 构建并行；
        /// 页面握手时 <see cref="EditorViewModel.PrepareStartupAsync"/> 直接取读好的快照。
        /// </summary>
        private static void StartStartupDocumentPrefetch(IServiceProvider services, string[] commandLineArgs)
        {
            using (StartupTrace.Phase("Start startup document prefetch"))
            {
                var settings = services.GetRequiredService<SettingsViewModel>();
                var target = StartupDocumentTarget.Resolve(commandLineArgs, settings.FileStartupAction, settings.LastFilePath);
                services.GetRequiredService<StartupDocumentPrefetch>().Start(target);
            }
        }

        private string[] ResolveStartupCommandLineArgs()
        {
            var baseProcessPath = Environment.ProcessPath
                ?? Environment.GetCommandLineArgs().FirstOrDefault()
                ?? "Typedown.WinUI";

            try
            {
                if (initialActivationArgs?.Kind == ExtendedActivationKind.File
                    && initialActivationArgs.Data is FileActivatedEventArgsContract fileArgs)
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

            return Environment.GetCommandLineArgs()
                .Where(x => !string.Equals(x, Program.NewWindowArgument, StringComparison.OrdinalIgnoreCase))
                .ToArray();
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

        private void OnAppWindowClosing(AppWindow sender, AppWindowClosingEventArgs args)
        {
            if (allowWindowClose)
            {
                allowWindowClose = false;
                return;
            }

            args.Cancel = true;
            _ = HandleAppWindowClosingAsync(sender);
        }

        private async Task HandleAppWindowClosingAsync(AppWindow sender)
        {
            if (windowCloseInProgress)
            {
                return;
            }

            windowCloseInProgress = true;
            try
            {
                if (appViewModel is not null && !await appViewModel.FileViewModel.AskToSave())
                {
                    return;
                }

                if (instanceRole == WinUIAppInstanceRole.Main
                    && appViewModel?.SettingsViewModel.KeepRun == true)
                {
                    sender.Hide();
                    return;
                }

                allowWindowClose = true;
                window?.Close();
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
            }
            finally
            {
                windowCloseInProgress = false;
            }
        }

        private void OnWindowClosed(object sender, WindowEventArgs args)
        {
            if (sender is Window closedWindow)
            {
                closedWindow.Closed -= OnWindowClosed;
                closedWindow.AppWindow.Closing -= OnAppWindowClosing;
            }

            shellBindings.Dispose();
            (platformServices?.AppActivationService as IDisposable)?.Dispose();
            activationBroker?.Dispose();
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

            var appThemeChanges = settings.WhenPropertyChanged(nameof(SettingsViewModel.AppTheme), x => x.AppTheme)
                .StartWith(settings.AppTheme);
            var micaEffectChanges = settings.WhenPropertyChanged(nameof(SettingsViewModel.UseMicaEffect), x => x.UseMicaEffect)
                .StartWith(settings.UseMicaEffect);
            var editorMicaEffectChanges = settings.WhenPropertyChanged(nameof(SettingsViewModel.UseEditorMicaEffect), x => x.UseEditorMicaEffect)
                .StartWith(settings.UseEditorMicaEffect);

            shellBindings.Add(appThemeChanges.Subscribe(ApplyAppTheme));
            shellBindings.Add(micaEffectChanges.Subscribe(ApplyMicaEffect));
            shellBindings.Add(Observable.CombineLatest(
                    appThemeChanges,
                    micaEffectChanges,
                    editorMicaEffectChanges,
                    (_, _, _) => settings)
                .Subscribe(ApplyEditorBackground));

            shellBindings.Add(settings.WhenPropertyChanged(nameof(SettingsViewModel.Topmost), x => x.Topmost)
                .StartWith(settings.Topmost)
                .Subscribe(ApplyTopmost));

            shellBindings.Add(settings.WhenPropertyChanged(nameof(SettingsViewModel.AnimationEnable), x => x.AnimationEnable)
                .StartWith(settings.AnimationEnable)
                .Subscribe(rootControl.SetAnimationEnabled));
            shellBindings.Add(appViewModel.FileViewModel.NewWindowCommand.OnExecute.Subscribe(OpenNewWindowInNewProcess));

            if (platformServices is not null)
            {
                var activationService = platformServices.AppActivationService;
                activationService.ActivationRequested += OnActivationRequested;
                shellBindings.Add(Disposable.Create(() => activationService.ActivationRequested -= OnActivationRequested));
            }

            SyncActualTheme(rootControl.ActualTheme);
        }

        private nint OnActivationRequested(AppActivationRequest request)
        {
            if (platformServices is null)
            {
                return default;
            }

            if (request.Source == AppActivationSource.InitialLaunch)
            {
                return platformServices.WindowContext.WindowHandle;
            }

            platformServices.WindowContext.Activate();
            var filePath = CommandLine.GetOpenFilePath(request.CommandLineArgs);
            if (!string.IsNullOrWhiteSpace(filePath))
            {
                _ = OpenRedirectedFileAsync(filePath);
            }

            return platformServices.WindowContext.WindowHandle;
        }

        private async Task OpenRedirectedFileAsync(string filePath)
        {
            try
            {
                if (appViewModel?.FileViewModel is { } fileViewModel)
                {
                    await fileViewModel.OpenFile(filePath);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
            }
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

            startInfo.ArgumentList.Add(Program.NewWindowArgument);

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

            var requestedTheme = theme switch
            {
                AppTheme.Light => ElementTheme.Light,
                AppTheme.Dark => ElementTheme.Dark,
                _ => ElementTheme.Default
            };

            rootControl.RequestedTheme = requestedTheme;

            if (theme != AppTheme.Default)
            {
                appViewModel?.UIViewModel.SetActualTheme(theme);
                NotifyActiveEditorThemeChanged(requestedTheme);
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
            NotifyActiveEditorThemeChanged(sender.ActualTheme);

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

        private void NotifyActiveEditorThemeChanged(ElementTheme actualTheme)
        {
            if (actualTheme != ElementTheme.Light && actualTheme != ElementTheme.Dark)
            {
                return;
            }

            uiServices?.GetService<IEditorSession>()?.Post(new ApplyTheme(EditorThemeFactory.Create(actualTheme)));
        }

        internal WinUIPlatformServices PlatformServices => platformServices ?? throw new InvalidOperationException("Platform services are not initialized.");

        internal sealed record MainPageNavigationContext(
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

