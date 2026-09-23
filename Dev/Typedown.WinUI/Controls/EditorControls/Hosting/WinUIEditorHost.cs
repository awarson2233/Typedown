using System;
using System.Collections.Generic;
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
using Typedown.Core.Editor.Wire;
using Typedown.Core.Interfaces;
using Typedown.Core.Models;
using Typedown.Core.ViewModels;
using Typedown.WinUI.Services;
using Typedown.WinUI.Utilities;
using Windows.Foundation;

namespace Typedown.WinUI.Controls
{
    /// <summary>
    /// WebView2 编辑器宿主。只负责 WebView2 生命周期、页面加载、报文的物理收发、主题背景与键盘输入，
    /// 不持有正文状态、不解析协议：报文一律交给 <see cref="WebViewEditorSession"/>，文档状态由编辑会话与 Core 的 ViewModel 持有。
    /// </summary>
    public sealed partial class WinUIEditorHost : UserControl, IEditorWireChannel, IDisposable
    {
        /// <summary>编辑器页面所在的虚拟主机：<c>Resources/Statics</c> 映射成 <c>https://typedown.editor/</c>。</summary>
        private const string EditorHostName = "typedown.editor";

        private static readonly Uri VirtualHostIndex = new($"https://{EditorHostName}/index.html");

        private readonly WebView2 webView;
        private readonly IServiceProvider? serviceProvider;
        private readonly WebViewEditorSession? session;
        private readonly WinUIKeyboardAccelerator? accelerator;
        private readonly WinUIWebViewEnvironmentService? webViewEnvironmentService;
        private readonly string? staticsFolder;
        private readonly CompositeDisposable disposables = new();
        private readonly StartupNavigationTraceState startupNavigationTraceState = new();

        private Task? coreInitializationTask;
        private CancellationTokenSource? loadCancellation;
        private Uri? editorUri;
        private string? initScriptId;
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
                session = serviceProvider?.GetService<WebViewEditorSession>();
                accelerator = serviceProvider?.GetService<IKeyboardAccelerator>() as WinUIKeyboardAccelerator;
                webViewEnvironmentService = serviceProvider?.GetService<WinUIWebViewEnvironmentService>();
                staticsFolder = ResolveStaticsFolder();

                // 页面报 doc.rendered 之前保持透明，不出现空编辑区的一帧。
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

        private void SubscribeSession(WebViewEditorSession editorSession)
        {
            disposables.Add(editorSession.StateChanged
                .Where(state => state == EditorSessionState.Ready)
                .Subscribe(_ => RecordBridgeMilestone("ContentLoaded")));

            // 页面出错（协议版本不一致、崩溃过多不再重载）时等不到 doc.rendered，不再藏着 WebView。
            disposables.Add(editorSession.StateChanged
                .Where(state => state == EditorSessionState.Faulted)
                .Subscribe(_ => webView.Opacity = 1));
            disposables.Add(editorSession.Events.OfType<DocumentLoaded>()
                .Subscribe(_ => OnDocumentRendered()));

            // 页面发 view.shortcut 时已经吞掉了这个按键，宿主必须补触发对应的菜单命令（Ctrl+Z 即 Undo）。
            disposables.Add(editorSession.Events.OfType<ShortcutPressed>()
                .Subscribe(x => accelerator?.Emit(x.Key, x.Modifiers)));

            if (accelerator is not null)
            {
                disposables.Add(accelerator.RegistrationsChanged.Subscribe(_ => PostKeymap()));
                PostKeymap();
            }
        }

        /// <summary>
        /// 下发宿主认领的全部和弦，页面在捕获阶段拦下命中的按键并以 view.shortcut 回报。
        /// 例外是剪贴板、全选与删除：新引擎页面还没实现这几条命令，它们的和弦不下发，交给浏览器与 CM6 原生处理，
        /// 否则页面吞掉按键后宿主转发的命令落空，Delete、Ctrl+C 之类会整个失灵。
        /// </summary>
        private void PostKeymap()
        {
            if (accelerator is null)
            {
                return;
            }

            var pageNative = PageNativeChords();
            session?.Post(new SetKeymap(accelerator.RegisteredShortcuts
                .Select(chord => new KeyChord(chord.Key, chord.Modifiers))
                .Where(chord => !pageNative.Contains(chord))
                .ToList()));
        }

        private HashSet<KeyChord> PageNativeChords()
        {
            var result = new HashSet<KeyChord>();
            if (serviceProvider?.GetService<SettingsViewModel>() is not { } settings)
            {
                return result;
            }

            foreach (var shortcut in new[]
            {
                settings.ShortcutCut,
                settings.ShortcutCopy,
                settings.ShortcutPaste,
                settings.ShortcutPasteAsPlainText,
                settings.ShortcutSelectAll,
                settings.ShortcutDelete,
            })
            {
                if (shortcut is not null && shortcut.Key != KeyboardKey.None)
                {
                    result.Add(new KeyChord(shortcut.Key, shortcut.Modifiers));
                }
            }

            return result;
        }

        private void OnDocumentRendered()
        {
            webView.Opacity = 1;
            RecordBridgeMilestone("FileLoaded");
            RecordBridgeMilestone("DocumentRendered");

            // 页面装载后已报过一次视口；XAML 布局此时才算定型，再请页面补报一次，滚动条按最终尺寸画。
            RequestScrollStateRefresh();
        }

        // ── IEditorWireChannel ────────────────────────────────────────────────

        bool IEditorWireChannel.TryPost(string message)
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

        async Task IEditorWireChannel.NavigateAsync(string initScript)
        {
            var core = webView.CoreWebView2 ?? throw new InvalidOperationException("CoreWebView2 is not initialized.");
            var target = editorUri ?? throw new InvalidOperationException("The editor page location is unknown.");

            // 初始态每次导航前换成最新值：先移除上一次注入的脚本，页面重载拿到的总是当下状态。
            if (initScriptId is { } previous)
            {
                core.RemoveScriptToExecuteOnDocumentCreated(previous);
                initScriptId = null;
            }

            initScriptId = await core.AddScriptToExecuteOnDocumentCreatedAsync(initScript);
            core.Navigate(target.AbsoluteUri);
            editorNavigationStarted = true;
        }

        EditorTheme IEditorWireChannel.GetCurrentTheme() => CreateCurrentTheme();

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

        private void OnWebMessageReceived(CoreWebView2 sender, CoreWebView2WebMessageReceivedEventArgs e)
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

            try
            {
                session.Receive(message);
            }
            catch (Exception ex)
            {
                // 会话的缺陷不能冒泡到 WebView2 的事件处理器里。
                System.Diagnostics.Trace.WriteLine($"[editor] 处理页面报文失败：{ex}");
            }
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
                case StartupBridgeMilestone.DocumentRendered:
                    StartupTrace.BridgeDocumentRendered(navigationId);
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

                await EnsureCoreInitializedAsync(cancellationToken);
                if (!IsCurrentLoad(currentLoadVersion, cancellationToken))
                {
                    return;
                }

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

                editorUri = await ResolveEditorUriAsync(cancellationToken);
                if (!IsCurrentLoad(currentLoadVersion, cancellationToken))
                {
                    return;
                }

                if (editorUri is null)
                {
                    System.Diagnostics.Trace.WriteLine(
                        "[editor] 编辑器页面的静态产物缺失：先在 Dev\\Typedown.Editor 下运行 yarn build。");
                    webView.Opacity = 1;
                    return;
                }

                if (session is null)
                {
                    return;
                }

                session.Attach(this);
                await session.LoadPageAsync();
#if DEBUG
                webView.CoreWebView2?.OpenDevToolsWindow();
#endif
            }
            catch (OperationCanceledException) when (!IsCurrentLoad(currentLoadVersion, cancellationToken))
            {
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.WriteLine($"[FATAL WEBVIEW2] {ex}");
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

        /// <summary>
        /// Debug 下先探测 Vite 开发服务器（http://localhost:3000），否则用虚拟主机加载 <c>Resources/Statics</c> 里的构建产物。
        /// 静态产物缺失时返回 <c>null</c>。
        /// </summary>
        private async Task<Uri?> ResolveEditorUriAsync(CancellationToken cancellationToken)
        {
#if DEBUG
            if (await IsDevServerAvailableAsync(cancellationToken))
            {
                return new Uri("http://localhost:3000/index.html");
            }
#else
            await Task.CompletedTask;
#endif
            return staticsFolder is null ? null : VirtualHostIndex;
        }

        private bool IsEditorPage(string? uri) =>
            editorUri is not null
            && Uri.TryCreate(uri, UriKind.Absolute, out var target)
            && Uri.Compare(target, editorUri, UriComponents.SchemeAndServer, UriFormat.Unescaped, StringComparison.OrdinalIgnoreCase) == 0;

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

        private async Task EnsureCoreInitializedAsync(CancellationToken cancellationToken)
        {
            var initializationTask = coreInitializationTask ??= InitializeCoreAsync();
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

        private async Task InitializeCoreAsync()
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

            if (staticsFolder is not null)
            {
                // 页面以 https 虚拟主机加载，不再需要 file:// 与 --disable-web-security。
                webView.CoreWebView2.SetVirtualHostNameToFolderMapping(
                    EditorHostName,
                    staticsFolder,
                    CoreWebView2HostResourceAccessKind.Allow);
            }

            // 本地图片走专用主机，只拦图片上下文的请求（<img>、CSS 背景），fetch / XHR 读不到本地文件。
            webView.CoreWebView2.AddWebResourceRequestedFilter(LocalImageRequest.FilterPattern, CoreWebView2WebResourceContext.Image);

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
            webView.CoreWebView2.WebResourceRequested += OnWebResourceRequested;
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
            webView.CoreWebView2.WebResourceRequested -= OnWebResourceRequested;
            webView.PreviewKeyDown -= OnPreviewKeyDown;
            coreEventsAttached = false;
        }

        // ── 本地图片 ──────────────────────────────────────────────────────────

        /// <summary>
        /// 页面把本地图片写成 <c>https://typedown.image/&lt;盘符&gt;/&lt;路径段&gt;</c>（docs/editor-protocol.md 第 3 节），
        /// 路径校验在 <see cref="LocalImageRequest"/>；这里读文件并回应。读文件放到线程池，CoreWebView2 的调用回到 UI 线程。
        /// </summary>
        private async void OnWebResourceRequested(CoreWebView2 sender, CoreWebView2WebResourceRequestedEventArgs e)
        {
            var request = LocalImageRequest.Parse(e.Request.Method, e.Request.Uri);
            if (request.Kind == LocalImageRequestKind.NotLocalImage)
            {
                return;
            }

            var deferral = e.GetDeferral();
            try
            {
                if (request.Kind != LocalImageRequestKind.Accepted || request.Path is not { } path || request.ContentType is not { } contentType)
                {
                    e.Response = request.Kind == LocalImageRequestKind.MethodNotAllowed
                        ? sender.Environment.CreateWebResourceResponse(null, 405, "Method Not Allowed", LocalImageHeaders(null))
                        : sender.Environment.CreateWebResourceResponse(null, 403, "Forbidden", LocalImageHeaders(null));
                    return;
                }

                var head = string.Equals(e.Request.Method, "HEAD", StringComparison.OrdinalIgnoreCase);
                var (status, bytes) = await Task.Run(() => ReadLocalImage(path, head));
                if (status != 200)
                {
                    e.Response = sender.Environment.CreateWebResourceResponse(null, status, status == 404 ? "Not Found" : "Forbidden", LocalImageHeaders(null));
                    return;
                }

                var stream = new Windows.Storage.Streams.InMemoryRandomAccessStream();
                if (bytes.Length > 0)
                {
                    await stream.WriteAsync(System.Runtime.InteropServices.WindowsRuntime.WindowsRuntimeBufferExtensions.AsBuffer(bytes));
                    stream.Seek(0);
                }

                e.Response = sender.Environment.CreateWebResourceResponse(stream, 200, "OK", LocalImageHeaders(contentType));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.WriteLine($"[editor] 本地图片请求失败：{ex}");
            }
            finally
            {
                deferral.Complete();
            }
        }

        private static (int Status, byte[] Bytes) ReadLocalImage(string path, bool headOnly)
        {
            try
            {
                var info = new FileInfo(path);
                if (!info.Exists)
                {
                    return (404, []);
                }

                if (info.Length > LocalImageRequest.MaxBytes)
                {
                    return (403, []);
                }

                return (200, headOnly ? [] : File.ReadAllBytes(path));
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                return (404, []);
            }
        }

        private static string LocalImageHeaders(string? contentType)
        {
            var headers = "Cache-Control: no-cache\r\nX-Content-Type-Options: nosniff\r\nContent-Security-Policy: default-src 'none'; style-src 'unsafe-inline'";
            return contentType is null ? headers : $"Content-Type: {contentType}\r\n{headers}";
        }

        private void OnNavigationStarting(CoreWebView2 sender, CoreWebView2NavigationStartingEventArgs e)
        {
            if (!IsEditorPage(e.Uri))
            {
                // 编辑器页面之外的导航（页面里的链接、拖进来的文件）会丢掉整个编辑器，一律拦下；链接由页面以 view.openLink 交给宿主打开。
                e.Cancel = true;
                return;
            }

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
                System.Diagnostics.Trace.WriteLine($"[editor] 编辑器页面导航失败：{e.WebErrorStatus}");
                webView.Opacity = 1;
            }
        }

        /// <summary>
        /// 渲染进程退出与页面的致命错误走同一条恢复路径：由编辑会话按 <see cref="EditorCrashRecovery"/> 决定是否重载，
        /// 重载时会话重新注入初始态并经 <see cref="IEditorWireChannel.NavigateAsync"/> 导航，doc.load 取正文镜像。
        /// 其余失败（浏览器进程退出、渲染进程无响应、GPU 进程等）不在这里自动重载。
        /// </summary>
        private void OnProcessFailed(CoreWebView2 sender, CoreWebView2ProcessFailedEventArgs e)
        {
            webView.Opacity = 1;
            if (e.ProcessFailedKind == CoreWebView2ProcessFailedKind.RenderProcessExited)
            {
                session?.OnRenderProcessExited();
            }
        }

        /// <summary>
        /// 右键菜单：抑制浏览器菜单，按点击处的上下文（<see cref="ContextAt"/>）弹 XAML 菜单。
        /// 页面还不支持 <see cref="ContextAt"/> 或请求失败时，上下文未知，菜单退回按当前选区决定菜单项。
        /// </summary>
        private async void OnCoreContextMenuRequested(CoreWebView2 sender, CoreWebView2ContextMenuRequestedEventArgs e)
        {
            e.Handled = true;
            var position = new Point(e.Location.X, e.Location.Y);
            var context = EditorContextMenuContext.Unknown;
            if (session is not null)
            {
                try
                {
                    // 宿主从不改 ZoomFactor（缩放控件已关闭），CSS px 与这里的坐标一致。
                    context = EditorContextMenuContext.Known(await session.RequestAsync(new ContextAt(position.X, position.Y)));
                }
                catch (Exception ex) when (ex is NotSupportedException or OperationCanceledException or TimeoutException or EditorRequestFailedException)
                {
                    context = EditorContextMenuContext.Unknown;
                }
            }

            if (isLoaded)
            {
                ContextMenuRequested?.Invoke(this, new WinUIEditorContextMenuRequestedEventArgs(position, context));
            }
        }

        private void OnPreviewKeyDown(object sender, Microsoft.UI.Xaml.Input.KeyRoutedEventArgs e)
        {
            if (accelerator is not null)
            {
                var args = accelerator.Emit((KeyboardKey)(int)e.Key, WinUIKeyboardAccelerator.GetCurrentModifiers());
                e.Handled = args.Handled;
            }
        }

        private static string? ResolveStaticsFolder()
        {
            var outputPath = Path.Combine(AppContext.BaseDirectory, "Resources", "Statics");
            if (File.Exists(Path.Combine(outputPath, "index.html")))
            {
                return outputPath;
            }

            var directory = new DirectoryInfo(AppContext.BaseDirectory);
            while (directory is not null)
            {
                var candidate = Path.Combine(directory.FullName, "Dev", "Typedown.WinUI", "Resources", "Statics");
                if (File.Exists(Path.Combine(candidate, "index.html")))
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

    /// <summary>右键处的富文本上下文：页面支持 <see cref="ContextAt"/> 时已知（源码模式或不在正文上为 <c>null</c>），否则未知。</summary>
    public readonly record struct EditorContextMenuContext(bool IsKnown, RichSelection? Selection)
    {
        public static EditorContextMenuContext Unknown => default;

        public static EditorContextMenuContext Known(RichSelection? selection) => new(true, selection);
    }

    public sealed class WinUIEditorContextMenuRequestedEventArgs : EventArgs
    {
        public WinUIEditorContextMenuRequestedEventArgs(Point position, EditorContextMenuContext context)
        {
            Position = position;
            Context = context;
        }

        public Point Position { get; }

        public EditorContextMenuContext Context { get; }
    }
}
