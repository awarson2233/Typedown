using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Web.WebView2.Core;
using Typedown.Core.Editor;
using Typedown.Core.Interfaces;
using Typedown.Core.Models;
using Typedown.Core.ViewModels;
using Typedown.WinUI.Services;
using Typedown.WinUI.Utilities;
using Windows.Foundation;

namespace Typedown.WinUI.Controls
{
    /// <summary>
    /// WebView2 编辑器宿主。只负责 WebView2 生命周期、报文的物理收发、主题背景与键盘输入，
    /// 不持有正文状态、不解析协议：报文一律交给 <see cref="LegacyMuyaSession"/>，文档状态由编辑会话与 Core 的 ViewModel 持有。
    /// </summary>
    public sealed class WinUIEditorHost : UserControl, ILegacyMuyaChannel, IDisposable
    {
        private readonly WebView2 webView;
        private readonly IServiceProvider? serviceProvider;
        private readonly LegacyMuyaSession? session;
        private readonly WinUIKeyboardAccelerator? accelerator;
        private readonly WinUIWebViewEnvironmentService? webViewEnvironmentService;
        private readonly string? editorIndex;
        private readonly CompositeDisposable disposables = new();
        private readonly StartupNavigationTraceState startupNavigationTraceState = new();

        private Task? coreInitializationTask;
        private CancellationTokenSource? loadCancellation;
        private bool coreInitialized;
        private bool isLoaded;
        private bool coreEventsAttached;
        private bool editorNavigationStarted;
        private XamlRoot? observedXamlRoot;
        private double observedRasterizationScale;
        private int loadVersion;

        public event EventHandler<WinUIEditorContextMenuRequestedEventArgs>? ContextMenuRequested;

        public WinUIEditorHost(IServiceProvider? serviceProvider = null)
        {
            using (StartupTrace.Phase("WinUIEditorHost ctor"))
            {
                this.serviceProvider = serviceProvider;
                session = serviceProvider?.GetService<LegacyMuyaSession>();
                accelerator = serviceProvider?.GetService<IKeyboardAccelerator>() as WinUIKeyboardAccelerator;
                webViewEnvironmentService = serviceProvider?.GetService<WinUIWebViewEnvironmentService>();
                editorIndex = ResolveEditorIndexPath();

                webView = new WebView2
                {
                    HorizontalAlignment = HorizontalAlignment.Stretch,
                    Opacity = 0,
                    VerticalAlignment = VerticalAlignment.Stretch
                };

                if (session is not null)
                {
                    EnsureViewModelsSubscribed();
                    SubscribeSession(session);
                }

                Content = webView;
                Loaded += OnLoaded;
                Unloaded += OnUnloaded;
                SizeChanged += OnHostSizeChanged;
            }
        }

        // ── 编辑会话 ──────────────────────────────────────────────────────────

        /// <summary>
        /// ViewModel 是 Scoped 且惰性解析的，而它们在构造函数里订阅编辑会话的事件。
        /// 页面一上来就会报选区、字数与目录，所以要在页面加载前把这些 ViewModel 提前实例化。
        /// </summary>
        private void EnsureViewModelsSubscribed()
        {
            if (serviceProvider?.GetService<AppViewModel>() is not AppViewModel appViewModel)
            {
                return;
            }

            _ = appViewModel.EditorViewModel;
            _ = appViewModel.FileViewModel;
            _ = appViewModel.FloatViewModel;
            _ = appViewModel.FormatViewModel;
            _ = appViewModel.ParagraphViewModel;
            _ = appViewModel.UIViewModel;
        }

        private void SubscribeSession(LegacyMuyaSession editorSession)
        {
            disposables.Add(editorSession.StateChanged
                .Where(state => state == EditorSessionState.Ready)
                .Subscribe(_ => OnEditorReady()));
            disposables.Add(editorSession.Events.OfType<DocumentLoaded>()
                .Subscribe(_ => RecordBridgeMilestone("FileLoaded")));

            // 焦点在 WebView2 里时 XAML 收不到 KeyDown，页面按下发的快捷键表拦下和弦后回传到这里补触发。
            disposables.Add(editorSession.Events.OfType<ShortcutPressed>()
                .Subscribe(x => accelerator?.Emit(x.Key, x.Modifiers)));

            if (accelerator is not null)
            {
                disposables.Add(accelerator.RegistrationsChanged.Subscribe(_ => PostKeymap()));
                PostKeymap();
            }
        }

        private void PostKeymap()
        {
            if (accelerator is null)
            {
                return;
            }

            session?.Post(new SetKeymap(accelerator.RegisteredShortcuts
                .Select(chord => new KeyChord(chord.Key, chord.Modifiers))
                .ToList()));
        }

        private void OnEditorReady()
        {
            webView.Opacity = 1;
            RecordBridgeMilestone("ContentLoaded");
        }

        // ── ILegacyMuyaChannel ────────────────────────────────────────────────

        bool ILegacyMuyaChannel.TryPost(string message)
        {
            try
            {
                webView.CoreWebView2?.PostWebMessageAsString(message);
                return webView.CoreWebView2 is not null;
            }
            catch
            {
                return false;
            }
        }

        void ILegacyMuyaChannel.Reload() => webView.CoreWebView2?.Reload();

        EditorTheme ILegacyMuyaChannel.GetCurrentTheme() => CreateCurrentTheme();

        // ── 主题 ──────────────────────────────────────────────────────────────

        private EditorTheme CreateCurrentTheme() => EditorThemeFactory.Create(ActualTheme);

        private void ApplyNativeEditorBackground(EditorColor background)
        {
            webView.DefaultBackgroundColor = Windows.UI.Color.FromArgb(
                ToByte(background.A * 255),
                ToByte(background.R),
                ToByte(background.G),
                ToByte(background.B));
        }

        // ── 报文接收 ──────────────────────────────────────────────────────────

        private async void OnWebMessageReceived(CoreWebView2 sender, CoreWebView2WebMessageReceivedEventArgs e)
        {
            if (session is null)
            {
                return;
            }

            string? message;
            try
            {
                message = e.TryGetWebMessageAsString();
            }
            catch
            {
                return;
            }

            await session.ReceiveAsync(message);
        }

        private void RecordBridgeMilestone(string? eventName)
        {
            var milestone = startupNavigationTraceState.RecordBridgeMilestone(eventName);
            var navigationId = startupNavigationTraceState.StartupNavigationId;
            switch (milestone)
            {
                case StartupBridgeMilestone.ContentLoaded:
                    StartupTrace.BridgeContentLoaded(navigationId);
                    break;
                case StartupBridgeMilestone.FileLoaded:
                    StartupTrace.BridgeFileLoaded(navigationId);
                    break;
            }
        }

        // ── WebView2 生命周期 ─────────────────────────────────────────────────

        private async void OnLoaded(object sender, RoutedEventArgs e)
        {
            loadCancellation?.Cancel();
            loadCancellation?.Dispose();
            loadCancellation = new CancellationTokenSource();
            var cancellationToken = loadCancellation.Token;
            var currentLoadVersion = ++loadVersion;

            isLoaded = true;
            ObserveXamlRoot(XamlRoot);

            try
            {
                var theme = CreateCurrentTheme();
                ApplyNativeEditorBackground(theme.Background);

                if (coreInitialized && editorNavigationStarted)
                {
                    AttachCoreWebView();
                    session?.Attach(this);
                    if (session?.State == EditorSessionState.Ready)
                    {
                        webView.Opacity = 1;
                    }

                    return;
                }

                await EnsureCoreInitializedAsync(theme.Background, cancellationToken);
                if (!IsCurrentLoad(currentLoadVersion, cancellationToken))
                {
                    return;
                }

                AttachCoreWebView();
                session?.Attach(this);

                // 右键菜单走原生路径：默认菜单必须保持启用，ContextMenuRequested 才会触发，
                // 再由 Handled = true 抑制浏览器菜单并改用 XAML Flyout。
                webView.CoreWebView2.Settings.AreDefaultContextMenusEnabled = true;
                webView.CoreWebView2.Settings.AreBrowserAcceleratorKeysEnabled = false;
#if DEBUG
                webView.CoreWebView2.Settings.AreDevToolsEnabled = true;
#else
                webView.CoreWebView2.Settings.AreDevToolsEnabled = false;
#endif
                webView.CoreWebView2.Settings.IsBuiltInErrorPageEnabled = false;
                webView.CoreWebView2.Settings.IsStatusBarEnabled = false;
                webView.CoreWebView2.Settings.IsZoomControlEnabled = false;
#if DEBUG
                webView.Opacity = 1;
                var useDevServer = await IsDevServerAvailableAsync(cancellationToken);
                if (!IsCurrentLoad(currentLoadVersion, cancellationToken))
                {
                    return;
                }

                if (useDevServer)
                {
                    webView.CoreWebView2.Navigate("http://localhost:3000");
                    editorNavigationStarted = true;
                }
                else
                {
                    if (editorIndex is null)
                    {
                        System.Diagnostics.Debug.WriteLine(
                            "[WEBVIEW2] Local Dev Server offline and static bundle is missing. Run yarn build in Dev\\Typedown.Editor.");
                        return;
                    }

                    webView.CoreWebView2.Navigate(new Uri(editorIndex).AbsoluteUri);
                    editorNavigationStarted = true;
                }
                webView.CoreWebView2.OpenDevToolsWindow();
#else
                webView.Opacity = 0;
                if (editorIndex is null)
                {
                    return;
                }

                webView.CoreWebView2.Navigate(new Uri(editorIndex).AbsoluteUri);
                editorNavigationStarted = true;
#endif
            }
            catch (OperationCanceledException) when (!IsCurrentLoad(currentLoadVersion, cancellationToken))
            {
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[FATAL WEBVIEW2] {ex}");
            }
        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            isLoaded = false;
            loadVersion++;
            loadCancellation?.Cancel();
            loadCancellation?.Dispose();
            loadCancellation = null;
            session?.Detach(this);
            ObserveXamlRoot(null);
            DetachCoreWebView();
        }

        // ── 视口同步 ────────────────────────────────────────────────────────────

        /// <summary>
        /// WinUI3 的 WebView2 在 XAML 布局之后才异步把新尺寸与缩放推给内核，页面自己的 resize 可能早于最终视口触发。
        /// 宿主尺寸或缩放一变，就请页面按当前视口重报一次滚动状态，免得宿主拿着过渡期的溢出量画出滚动条。
        /// </summary>
        private void RequestScrollStateRefresh()
        {
            session?.Post(new RefreshViewport());
        }

        private void OnHostSizeChanged(object sender, SizeChangedEventArgs e)
        {
            RequestScrollStateRefresh();
        }

        private void ObserveXamlRoot(XamlRoot? root)
        {
            if (ReferenceEquals(observedXamlRoot, root))
            {
                return;
            }

            if (observedXamlRoot is not null)
            {
                observedXamlRoot.Changed -= OnXamlRootChanged;
            }

            observedXamlRoot = root;
            if (root is null)
            {
                return;
            }

            observedRasterizationScale = root.RasterizationScale;
            root.Changed += OnXamlRootChanged;
        }

        private void OnXamlRootChanged(XamlRoot sender, XamlRootChangedEventArgs args)
        {
            // 窗口拖到另一块缩放不同的显示器时，DIP 尺寸可能不变、SizeChanged 不触发，但取整误差已经变了。
            if (sender.RasterizationScale == observedRasterizationScale)
            {
                return;
            }

            observedRasterizationScale = sender.RasterizationScale;
            RequestScrollStateRefresh();
        }

        private bool IsCurrentLoad(int version)
        {
            return isLoaded && version == loadVersion;
        }

        private bool IsCurrentLoad(int version, CancellationToken cancellationToken)
        {
            return IsCurrentLoad(version) && !cancellationToken.IsCancellationRequested;
        }

        private async Task EnsureCoreInitializedAsync(EditorColor background, CancellationToken cancellationToken)
        {
            var initializationTask = coreInitializationTask ??= InitializeCoreAsync(background);
            try
            {
                await initializationTask.WaitAsync(cancellationToken);
            }
            catch when (initializationTask.IsFaulted || initializationTask.IsCanceled)
            {
                if (ReferenceEquals(coreInitializationTask, initializationTask))
                {
                    coreInitializationTask = null;
                }

                throw;
            }
        }

        private async Task InitializeCoreAsync(EditorColor background)
        {
            var environment = await GetEnvironmentAsync();
            StartupTrace.EnsureCoreWebView2Start();
            try
            {
                await webView.EnsureCoreWebView2Async(environment);
            }
            finally
            {
                StartupTrace.EnsureCoreWebView2Stop();
            }

            await webView.CoreWebView2.AddScriptToExecuteOnDocumentCreatedAsync(BuildDocumentCreatedScript(background));
            coreInitialized = true;
        }

#if DEBUG
        private static async Task<bool> IsDevServerAvailableAsync(CancellationToken cancellationToken)
        {
            using var timeoutCancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeoutCancellation.CancelAfter(TimeSpan.FromMilliseconds(200));

            try
            {
                using var tcpClient = new TcpClient();
                await tcpClient.ConnectAsync("127.0.0.1", 3000, timeoutCancellation.Token);
                return true;
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                return false;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch
            {
                return false;
            }
        }
#endif

        /// <summary>
        /// 环境一律由 <see cref="WinUIWebViewEnvironmentService"/> 创建——命令行开关与用户数据目录都在那里，
        /// 这里不另开一条裸 CreateAsync 的兜底路径，否则两条路径建出来的环境配置会不一致。
        /// </summary>
        private Task<CoreWebView2Environment> GetEnvironmentAsync()
        {
            if (webViewEnvironmentService is null)
            {
                throw new InvalidOperationException(
                    $"{nameof(WinUIWebViewEnvironmentService)} is required to host the editor.");
            }

            return webViewEnvironmentService.GetEnvironmentAsync();
        }

        /// <summary>
        /// 只做一件事：在首帧之前把文档背景刷成与宿主一致的颜色，避免 WebView2 的白底闪烁。
        /// 快捷键一律走 <see cref="IKeyboardAccelerator"/>，不在这里硬编码。
        /// </summary>
        private static string BuildDocumentCreatedScript(EditorColor background)
        {
            var color = string.Create(
                CultureInfo.InvariantCulture,
                $"rgba({background.R}, {background.G}, {background.B}, {background.A})");

            return string.Create(
                CultureInfo.InvariantCulture,
                $$"""
                (() => {
                    const color = '{{color}}';
                    const apply = () => {
                        if (!document.documentElement) {
                            return;
                        }
                        document.documentElement.style.backgroundColor = color;
                        if (document.body) {
                            document.body.style.backgroundColor = color;
                        }
                    };
                    document.addEventListener('DOMContentLoaded', apply, { once: true });
                    apply();
                })();
                """);
        }

        private static byte ToByte(double value)
        {
            return (byte)Math.Clamp((int)Math.Round(value), 0, 255);
        }

        private void AttachCoreWebView()
        {
            if (coreEventsAttached || webView.CoreWebView2 is null)
            {
                return;
            }

            webView.CoreWebView2.WebMessageReceived += OnWebMessageReceived;
            webView.CoreWebView2.NavigationStarting += OnNavigationStarting;
            webView.CoreWebView2.ContentLoading += OnContentLoading;
            webView.CoreWebView2.DOMContentLoaded += OnDomContentLoaded;
            webView.CoreWebView2.NavigationCompleted += OnNavigationCompleted;
            webView.CoreWebView2.ProcessFailed += OnProcessFailed;
            webView.CoreWebView2.ContextMenuRequested += OnCoreContextMenuRequested;
            webView.PreviewKeyDown += OnPreviewKeyDown;
            coreEventsAttached = true;
        }

        private void DetachCoreWebView()
        {
            if (!coreEventsAttached || webView.CoreWebView2 is null)
            {
                return;
            }

            webView.CoreWebView2.WebMessageReceived -= OnWebMessageReceived;
            webView.CoreWebView2.NavigationStarting -= OnNavigationStarting;
            webView.CoreWebView2.ContentLoading -= OnContentLoading;
            webView.CoreWebView2.DOMContentLoaded -= OnDomContentLoaded;
            webView.CoreWebView2.NavigationCompleted -= OnNavigationCompleted;
            webView.CoreWebView2.ProcessFailed -= OnProcessFailed;
            webView.CoreWebView2.ContextMenuRequested -= OnCoreContextMenuRequested;
            webView.PreviewKeyDown -= OnPreviewKeyDown;
            coreEventsAttached = false;
        }

        private void OnNavigationStarting(CoreWebView2 sender, CoreWebView2NavigationStartingEventArgs e)
        {
            session?.OnNavigationStarting();
            startupNavigationTraceState.NavigationStarting(e.NavigationId);
            StartupTrace.NavigationStarting(e.NavigationId, StartupTrace.IsEnabled ? e.Uri : null);
        }

        private void OnContentLoading(CoreWebView2 sender, CoreWebView2ContentLoadingEventArgs e)
        {
            StartupTrace.ContentLoading(e.NavigationId);
        }

        private void OnDomContentLoaded(CoreWebView2 sender, CoreWebView2DOMContentLoadedEventArgs e)
        {
            StartupTrace.DomContentLoaded(e.NavigationId);
        }

        private void OnNavigationCompleted(CoreWebView2 sender, CoreWebView2NavigationCompletedEventArgs e)
        {
            StartupTrace.NavigationCompleted(e.NavigationId, e.IsSuccess, (int)e.WebErrorStatus);
            if (!isLoaded)
            {
                return;
            }

            if (!e.IsSuccess)
            {
                webView.Opacity = 1;
            }
        }

        /// <summary>
        /// 渲染进程崩溃后页面只剩一块空白，此前宿主不做任何恢复。现在交给编辑会话决定是否重载：
        /// 重载后页面经启动握手拿回正文镜像，光标由会话在握手前补发。
        /// 其余失败（浏览器进程退出、渲染进程无响应、GPU 进程等）不在这里自动重载。
        /// </summary>
        private void OnProcessFailed(CoreWebView2 sender, CoreWebView2ProcessFailedEventArgs e)
        {
            webView.Opacity = 1;
            if (e.ProcessFailedKind != CoreWebView2ProcessFailedKind.RenderProcessExited || session is null)
            {
                return;
            }

            if (session.OnRenderProcessFailed())
            {
                try
                {
                    sender.Reload();
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[WEBVIEW2] Reload after render process exit failed: {ex}");
                }
            }
        }

        private void OnCoreContextMenuRequested(CoreWebView2 sender, CoreWebView2ContextMenuRequestedEventArgs e)
        {
            e.Handled = true;
            ContextMenuRequested?.Invoke(this, new WinUIEditorContextMenuRequestedEventArgs(new Point(e.Location.X, e.Location.Y)));
        }

        private void OnPreviewKeyDown(object sender, Microsoft.UI.Xaml.Input.KeyRoutedEventArgs e)
        {
            if (accelerator is not null)
            {
                var args = accelerator.Emit((KeyboardKey)(int)e.Key, WinUIKeyboardAccelerator.GetCurrentModifiers());
                e.Handled = args.Handled;
            }
        }

        private static string? ResolveEditorIndexPath()
        {
            var outputPath = Path.Combine(AppContext.BaseDirectory, "Resources", "Statics", "index.html");
            if (File.Exists(outputPath))
            {
                return outputPath;
            }

            var directory = new DirectoryInfo(AppContext.BaseDirectory);
            while (directory is not null)
            {
                var candidate = Path.Combine(directory.FullName, "Dev", "Typedown.WinUI", "Resources", "Statics", "index.html");
                if (File.Exists(candidate))
                {
                    return candidate;
                }

                directory = directory.Parent;
            }

            return null;
        }

        public void Dispose()
        {
            ObserveXamlRoot(null);
            DetachCoreWebView();
            session?.Detach(this);
            disposables.Dispose();
            loadCancellation?.Cancel();
            loadCancellation?.Dispose();
            loadCancellation = null;
        }
    }

    public sealed class WinUIEditorContextMenuRequestedEventArgs : EventArgs
    {
        public WinUIEditorContextMenuRequestedEventArgs(Point position)
        {
            Position = position;
        }

        public Point Position { get; }
    }
}
