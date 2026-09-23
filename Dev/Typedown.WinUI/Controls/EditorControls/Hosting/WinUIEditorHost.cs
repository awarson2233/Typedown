using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Reactive.Disposables;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Web.WebView2.Core;
using Typedown.Core.Models;
using Typedown.Core.Services;
using Typedown.Core.Utilities;
using Typedown.Core.Interfaces;
using Typedown.Core.ViewModels;
using Typedown.Services;
using Typedown.WinUI.Services;
using Typedown.WinUI.Utilities;
using Windows.Foundation;
using Windows.UI.ViewManagement;

namespace Typedown.WinUI.Controls
{
    /// <summary>
    /// WebView2 编辑器宿主。职责边界与 1.2.19 的 <c>Typedown.Controls.MarkdownEditor</c> 对齐：
    /// 只负责 WebView2 生命周期、报文收发、主题与输入，不持有正文状态、不做文件 IO、不对消息名做白名单。
    /// 协议解析统一由 <see cref="EditorBridge"/> 承担，文档状态由 Core 的 ViewModel 独占。
    /// </summary>
    public sealed class WinUIEditorHost : UserControl, IDisposable
    {
        private readonly WebView2 webView;
        private readonly IServiceProvider? serviceProvider;
        private readonly EditorBridge? bridge;
        private readonly WinUIEditorCommandSink? commandSink;
        private readonly WinUIWebViewEnvironmentService? webViewEnvironmentService;
        private readonly string? editorIndex;
        private readonly PendingRawMessageQueue pendingRawMessages = new();
        private readonly CompositeDisposable disposables = new();
        private readonly StartupNavigationTraceState startupNavigationTraceState = new();
        private readonly UISettings uiSettings = new();

        private static readonly HashSet<ulong> reportedEditorExceptions = new();

        private Task? coreInitializationTask;
        private CancellationTokenSource? loadCancellation;
        private bool coreInitialized;
        private bool isLoaded;
        private bool coreEventsAttached;
        private bool editorNavigationStarted;
        private bool isContentLoaded;
        private XamlRoot? observedXamlRoot;
        private double observedRasterizationScale;
        private int loadVersion;

        public event EventHandler<WinUIEditorContextMenuRequestedEventArgs>? ContextMenuRequested;

        public WinUIEditorHost(IServiceProvider? serviceProvider = null)
        {
            using (StartupTrace.Phase("WinUIEditorHost ctor"))
            {
                this.serviceProvider = serviceProvider;
                commandSink = serviceProvider?.GetService<IEditorCommandSink>() as WinUIEditorCommandSink;
                webViewEnvironmentService = serviceProvider?.GetService<WinUIWebViewEnvironmentService>();
                editorIndex = ResolveEditorIndexPath();

                webView = new WebView2
                {
                    HorizontalAlignment = HorizontalAlignment.Stretch,
                    Opacity = 0,
                    VerticalAlignment = VerticalAlignment.Stretch
                };

                var remoteInvoke = serviceProvider?.GetService<RemoteInvoke>();
                var transport = serviceProvider?.GetService<Transport>();
                if (remoteInvoke is not null && transport is not null)
                {
                    bridge = new EditorBridge(remoteInvoke, transport, SendRawMessage);
                    EnsurePresentationHandlersRegistered();
                    RegisterHostInvokeHandlers(remoteInvoke);
                    SubscribeHostEvents();
                }

                Content = webView;
                Loaded += OnLoaded;
                Unloaded += OnUnloaded;
                SizeChanged += OnHostSizeChanged;
            }
        }

        // ── 宿主自有的 invoke / 事件处理 ────────────────────────────────────────

        /// <summary>
        /// ViewModel 是 Scoped 且惰性解析的，而它们的 RemoteInvoke 处理器在构造函数里注册。
        /// 前端一上来就会 invoke GetSettings / GetStringResources / ResizeTable，
        /// 因此必须在桥接建立前把这些 ViewModel 提前实例化，否则首个 invoke 会找不到 handler。
        /// </summary>
        private void EnsurePresentationHandlersRegistered()
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

        private void RegisterHostInvokeHandlers(RemoteInvoke remoteInvoke)
        {
            disposables.Add(remoteInvoke.Handle("ContentLoaded", OnContentLoaded));
            disposables.Add(remoteInvoke.Handle("GetCurrentTheme", CreateCurrentThemePayload));
            disposables.Add(remoteInvoke.Handle<string>("UnhandledException", OnEditorUnhandledException));
            disposables.Add(remoteInvoke.Handle<string>("OpenNewWindow", uri => OpenUri(uri)));
        }

        private void SubscribeHostEvents()
        {
            if (serviceProvider?.GetService<EventCenter>() is not EventCenter eventCenter)
            {
                return;
            }

            disposables.Add(eventCenter.GetObservable<EditorEventArgs>("OpenURI")
                .Subscribe(x => OpenUri(x.Args?["uri"]?.ToString())));
            disposables.Add(eventCenter.GetObservable<EditorEventArgs>("FileLoaded")
                .Subscribe(_ => RecordBridgeMilestone("FileLoaded")));

            // 前端若以页面级 contextmenu 监听上报坐标，这里同样接得住；
            // 原生 CoreWebView2.ContextMenuRequested 是首选路径，两者最终汇到同一个处理。
            disposables.Add(eventCenter.GetObservable<EditorEventArgs>("OpenContextMenu")
                .Subscribe(x => RaiseContextMenuRequested(
                    x.Args?["x"]?.ToObject<double>() ?? 0,
                    x.Args?["y"]?.ToObject<double>() ?? 0)));

            // 焦点在 WebView2 里时 XAML 收不到 KeyDown，页面按下发的快捷键表拦下和弦后回传到这里补触发。
            disposables.Add(eventCenter.GetObservable<EditorEventArgs>("KeyDown")
                .Subscribe(x => OnEditorKeyDown(
                    x.Args?["key"]?.ToObject<int>() ?? 0,
                    x.Args?["modifiers"]?.ToObject<int>() ?? 0)));

            if (serviceProvider?.GetService<IKeyboardAccelerator>() is WinUIKeyboardAccelerator accelerator)
            {
                disposables.Add(accelerator.RegistrationsChanged.Subscribe(_ => SendShortcutTable()));
            }
        }

        private void OnEditorKeyDown(int key, int modifiers)
        {
            if (serviceProvider?.GetService<IKeyboardAccelerator>() is WinUIKeyboardAccelerator accelerator)
            {
                accelerator.Emit((KeyboardKey)key, (KeyboardModifiers)modifiers);
            }
        }

        private void SendShortcutTable()
        {
            if (serviceProvider?.GetService<IKeyboardAccelerator>() is not WinUIKeyboardAccelerator accelerator)
            {
                return;
            }

            var shortcuts = accelerator.RegisteredShortcuts
                .Select(chord => new { key = (int)chord.Key, modifiers = (int)chord.Modifiers })
                .ToList();
            bridge?.Send("SetShortcuts", new { shortcuts });
        }

        private void OnContentLoaded()
        {
            isContentLoaded = true;
            webView.Opacity = 1;
            RecordBridgeMilestone("ContentLoaded");
            commandSink?.ResendLatestTheme(this);
            SendShortcutTable();
        }

        private void OnEditorUnhandledException(string error)
        {
            webView.CoreWebView2?.Reload();
            try
            {
                var hash = Common.SimpleHash(error);
                if (reportedEditorExceptions.Add(hash))
                {
                    _ = Log.Report("WebViewUnhandledException", error);
                }
            }
            catch
            {
                // 上报失败不应影响自愈重载。
            }
        }

        private bool OpenUri(string? uri)
        {
            if (string.IsNullOrWhiteSpace(uri))
            {
                return false;
            }

            try
            {
                if (!UriHelper.IsWebUrl(uri) && UriHelper.TryGetLocalPath(uri, out var localPath))
                {
                    var appViewModel = serviceProvider?.GetService<AppViewModel>();
                    var currentFolder = Path.GetDirectoryName(appViewModel?.FileViewModel.FilePath ?? string.Empty);
                    if (!string.IsNullOrWhiteSpace(currentFolder))
                    {
                        var fullPath = Path.GetFullPath(Path.Combine(currentFolder, localPath));
                        if (File.Exists(fullPath))
                        {
                            if (FileTypeHelper.IsMarkdownFile(fullPath) && appViewModel is not null)
                            {
                                appViewModel.FileViewModel.NewWindowCommand.Execute(fullPath);
                            }
                            else
                            {
                                Common.OpenUrl(fullPath);
                            }

                            return true;
                        }
                    }
                }

                Common.OpenUrl(uri);
                return true;
            }
            catch
            {
                return false;
            }
        }

        // ── 主题 ──────────────────────────────────────────────────────────────

        private EditorThemePayload CreateCurrentThemePayload()
        {
            var isDark = ActualTheme == ElementTheme.Dark;
            var background = isDark
                ? new EditorColorPayload(40, 40, 40, 1)
                : new EditorColorPayload(249, 249, 249, 1);

            return new EditorThemePayload
            {
                Theme = isDark ? "Dark" : "Light",
                AccentColor = ResolveSystemAccentColor(),
                Background = background
            };
        }

        private EditorColorPayload ResolveSystemAccentColor()
        {
            try
            {
                var accent = uiSettings.GetColorValue(UIColorType.Accent);
                return new EditorColorPayload(accent.R, accent.G, accent.B, 1);
            }
            catch
            {
                return new EditorColorPayload(27, 102, 107, 1);
            }
        }

        private void ApplyNativeEditorBackground(EditorColorPayload background)
        {
            webView.DefaultBackgroundColor = Windows.UI.Color.FromArgb(
                ToByte(background.A * 255),
                ToByte(background.R),
                ToByte(background.G),
                ToByte(background.B));
        }

        // ── 报文收发 ──────────────────────────────────────────────────────────

        /// <summary>供 <see cref="WinUIEditorCommandSink"/> 转发 ViewModel 发起的命令。</summary>
        internal bool SendCommand(string name, object? args)
        {
            return bridge?.Send(name, args!) == true;
        }

        private bool SendRawMessage(string payload)
        {
            if (webView.CoreWebView2 is null)
            {
                pendingRawMessages.Enqueue(payload);
                return true;
            }

            return TryPostRawMessage(payload);
        }

        private void FlushPendingRawMessages()
        {
            if (webView.CoreWebView2 is null)
            {
                return;
            }

            pendingRawMessages.Flush(TryPostRawMessage);
        }

        private bool TryPostRawMessage(string payload)
        {
            try
            {
                webView.CoreWebView2?.PostWebMessageAsString(payload);
                return webView.CoreWebView2 is not null;
            }
            catch
            {
                return false;
            }
        }

        private async void OnWebMessageReceived(CoreWebView2 sender, CoreWebView2WebMessageReceivedEventArgs e)
        {
            if (bridge is null)
            {
                return;
            }

            var messageLoadVersion = loadVersion;
            var messageStr = e.TryGetWebMessageAsString();
            await bridge.ReceiveAsync(messageStr);
            _ = IsCurrentLoad(messageLoadVersion);
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
            commandSink?.RegisterActiveHost(this);
            ObserveXamlRoot(XamlRoot);

            try
            {
                var themePayload = CreateCurrentThemePayload();
                ApplyNativeEditorBackground(themePayload.Background);

                if (coreInitialized && editorNavigationStarted)
                {
                    AttachCoreWebView();
                    FlushPendingRawMessages();
                    if (isContentLoaded)
                    {
                        webView.Opacity = 1;
                    }

                    return;
                }

                await EnsureCoreInitializedAsync(themePayload.Background, cancellationToken);
                if (!IsCurrentLoad(currentLoadVersion, cancellationToken))
                {
                    return;
                }

                isContentLoaded = false;
                AttachCoreWebView();

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
            pendingRawMessages.Clear();
            commandSink?.UnregisterActiveHost(this);
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
            if (isContentLoaded)
            {
                SendCommand("RefreshScrollState", null);
            }
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

        private async Task EnsureCoreInitializedAsync(EditorColorPayload background, CancellationToken cancellationToken)
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

        private async Task InitializeCoreAsync(EditorColorPayload background)
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
        private static string BuildDocumentCreatedScript(EditorColorPayload background)
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
            isContentLoaded = false;
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
                return;
            }

            FlushPendingRawMessages();
        }

        private void OnProcessFailed(CoreWebView2 sender, CoreWebView2ProcessFailedEventArgs e)
        {
            webView.Opacity = 1;
        }

        private void OnCoreContextMenuRequested(CoreWebView2 sender, CoreWebView2ContextMenuRequestedEventArgs e)
        {
            e.Handled = true;
            RaiseContextMenuRequested(e.Location.X, e.Location.Y);
        }

        private void RaiseContextMenuRequested(double x, double y)
        {
            ContextMenuRequested?.Invoke(this, new WinUIEditorContextMenuRequestedEventArgs(new Point(x, y)));
        }

        private void OnPreviewKeyDown(object sender, Microsoft.UI.Xaml.Input.KeyRoutedEventArgs e)
        {
            if (serviceProvider?.GetService<IKeyboardAccelerator>() is WinUIKeyboardAccelerator accelerator)
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
