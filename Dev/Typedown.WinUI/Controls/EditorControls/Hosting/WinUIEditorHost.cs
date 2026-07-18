using System;
using System.Globalization;
using System.IO;
using System.Net.Sockets;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.Foundation;
using Microsoft.Web.WebView2.Core;
using Typedown.Presentation.Interfaces;
using Typedown.WinUI.Services;
using Typedown.WinUI.Utilities;

namespace Typedown.WinUI.Controls
{
    public sealed class WinUIEditorHost : UserControl
    {
        private readonly WebView2 webView;

        private readonly IEditorHostSink hostSink;
        private readonly IServiceProvider? serviceProvider;
        private readonly WinUIEditorCommandSink? commandSink;
        private readonly WinUIWebViewEnvironmentService? webViewEnvironmentService;
        private readonly string? editorIndex;
        private readonly IEditorDocumentSession documentSession;
        private readonly WinUIEditorHostController hostController;
        private readonly WinUIEditorBridgeAdapter bridgeAdapter;
        private string status;
        private string latestRawWebMessage;
        private readonly PendingRawMessageQueue pendingRawMessages = new();
        private Task? coreInitializationTask;
        private CancellationTokenSource? loadCancellation;
        private bool coreInitialized;
        private bool isLoaded;
        private bool coreEventsAttached;
        private bool editorNavigationStarted;
        private int loadVersion;
        private readonly StartupNavigationTraceState startupNavigationTraceState = new();

        public event EventHandler<WinUIEditorContextMenuRequestedEventArgs>? ContextMenuRequested;

        public WinUIEditorHost(IServiceProvider? serviceProvider = null)
        {
            using (StartupTrace.Phase("WinUIEditorHost ctor"))
            {
                this.serviceProvider = serviceProvider;
                commandSink = serviceProvider?.GetService<IEditorCommandSink>() as WinUIEditorCommandSink;
                webViewEnvironmentService = serviceProvider?.GetService<WinUIWebViewEnvironmentService>();
                hostSink = new WinUIEditorHostSink(this);
                editorIndex = ResolveEditorIndexPath();
                documentSession = CreateDocumentSession(editorIndex is null ? null : ResolveBasePath(editorIndex));
                hostController = new WinUIEditorHostController(documentSession, hostSink);
                bridgeAdapter = new WinUIEditorBridgeAdapter(documentSession);
                webView = new WebView2
                {
                    HorizontalAlignment = HorizontalAlignment.Stretch,
                    Opacity = 0,
                    VerticalAlignment = VerticalAlignment.Stretch
                };

                status = "Loaded=False; FileLoaded=False; MarkdownLength=0; LastEvent=Waiting";
                latestRawWebMessage = bridgeAdapter.LastRawMessage;

                Content = webView;
                Loaded += OnLoaded;
                Unloaded += OnUnloaded;
            }
        }

        public string Status => status;

        public string LatestRawWebMessage => latestRawWebMessage;

        public string? InitialFilePath
        {
            get => hostController.InitialFilePath;
            set => hostController.InitialFilePath = value;
        }

        // Host entrypoints: LoadFile(), Save(), SaveAs().

        private async void OnLoaded(object sender, RoutedEventArgs e)
        {
            loadCancellation?.Cancel();
            loadCancellation?.Dispose();
            loadCancellation = new CancellationTokenSource();
            var cancellationToken = loadCancellation.Token;
            var currentLoadVersion = ++loadVersion;

            isLoaded = true;
            commandSink?.RegisterActiveHost(this);

            if (editorIndex is null)
            {
                status = "Editor static bundle is missing. Run yarn build in Dev\\Typedown.Editor to generate Dev\\Typedown.WinUI\\Resources\\Statics\\index.html.";
                return;
            }

            try
            {
                var themePayload = CreateCurrentThemePayload();
                ApplyNativeEditorBackground(themePayload.Background);

                if (coreInitialized && editorNavigationStarted)
                {
                    AttachCoreWebView();
                    FlushPendingRawMessages();
                    if (bridgeAdapter.IsContentLoaded)
                    {
                        webView.Opacity = 1;
                    }

                    status = bridgeAdapter.StatusText;
                    latestRawWebMessage = bridgeAdapter.LastRawMessage;
                    TrySendPendingLoadFile();
                    return;
                }

                await EnsureCoreInitializedAsync(themePayload.Background, cancellationToken);
                if (!IsCurrentLoad(currentLoadVersion, cancellationToken))
                {
                    return;
                }

                bridgeAdapter.ResetForNavigation();
                hostController.ResetForNavigation();
                AttachCoreWebView();
                if (!string.IsNullOrWhiteSpace(InitialFilePath))
                {
                    var loadResult = hostController.LoadFile(InitialFilePath);
                    if (!loadResult.Success)
                    {
                        status = $"Initial file load failed: {loadResult.Message}";
                    }
                }
                status = bridgeAdapter.StatusText;
                latestRawWebMessage = bridgeAdapter.LastRawMessage;

                webView.CoreWebView2.Settings.AreDefaultContextMenusEnabled = false;
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
                    status = "Editor host navigating to Local Dev Server (http://localhost:3000) with HMR enabled.";
                    editorNavigationStarted = true;
                    webView.CoreWebView2.Navigate("http://localhost:3000");
                }
                else
                {
                    status = $"Local Dev Server offline. Navigating to fallback static bundle: {editorIndex}";
                    editorNavigationStarted = true;
                    webView.CoreWebView2.Navigate(new Uri(editorIndex).AbsoluteUri);
                }
                webView.CoreWebView2.OpenDevToolsWindow();
#else
                webView.Opacity = 0;
                status = $"Editor host navigating to {editorIndex}";
                editorNavigationStarted = true;
                webView.CoreWebView2.Navigate(new Uri(editorIndex).AbsoluteUri);
#endif
            }
            catch (OperationCanceledException) when (!IsCurrentLoad(currentLoadVersion, cancellationToken))
            {
            }
            catch (Exception ex)
            {
                if (IsCurrentLoad(currentLoadVersion, cancellationToken))
                {
                    status = $"WebView2 initialization failed: {ex.GetType().Name}: {ex.Message}";
                }
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
            DetachCoreWebView();
        }

        private async void OnWebMessageReceived(CoreWebView2 sender, CoreWebView2WebMessageReceivedEventArgs e)
        {
            var messageLoadVersion = loadVersion;
            var wasContentLoaded = bridgeAdapter.IsContentLoaded;
            var messageStr = e.TryGetWebMessageAsString();

            if (!string.IsNullOrEmpty(messageStr) && messageStr.Contains("\"name\":\"OpenContextMenu\""))
            {
                try
                {
                    using var document = JsonDocument.Parse(messageStr);
                    var root = document.RootElement;
                    if (root.TryGetProperty("args", out var argsElement))
                    {
                        var x = argsElement.GetProperty("x").GetDouble();
                        var y = argsElement.GetProperty("y").GetDouble();
                        ContextMenuRequested?.Invoke(this, new WinUIEditorContextMenuRequestedEventArgs(new Point(x, y)));
                        return;
                    }
                }
                catch { }
            }

            var receiveTask = bridgeAdapter.ReceiveAsync(
                messageStr,
                payload => IsCurrentLoad(messageLoadVersion) && SendRawMessage(payload));
            var bridgeMilestoneName = StartupTrace.IsEnabled ? bridgeAdapter.LastEventName : null;
            await receiveTask;
            if (!IsCurrentLoad(messageLoadVersion))
            {
                return;
            }

            status = bridgeAdapter.StatusText;
            latestRawWebMessage = bridgeAdapter.LastRawMessage;
            if (bridgeMilestoneName is not null)
            {
                RecordBridgeMilestone(bridgeMilestoneName);
            }

            if (!wasContentLoaded && bridgeAdapter.IsContentLoaded)
            {
                webView.Opacity = 1;
                hostController.MarkEditorReady();
                FlushPendingRawMessages();
                commandSink?.ResendLatestTheme(this);
                TrySendPendingLoadFile();
            }
        }

        private void OnNavigationStarting(CoreWebView2 sender, CoreWebView2NavigationStartingEventArgs e)
        {
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

        private async void OnNavigationCompleted(CoreWebView2 sender, CoreWebView2NavigationCompletedEventArgs e)
        {
            StartupTrace.NavigationCompleted(e.NavigationId, e.IsSuccess, (int)e.WebErrorStatus);
            if (!isLoaded)
            {
                return;
            }

            if (!e.IsSuccess)
            {
                webView.Opacity = 1;
                status = $"Editor navigation failed: {e.WebErrorStatus}";
                return;
            }

            status = "Editor static bundle loaded. Bridge smoke message sent from WinUI host.";
            var payload = JsonSerializer.Serialize(new
            {
                name = "WinUIHostReady",
                args = new
                {
                    shell = "Typedown.WinUI",
                    capability = "EditorHost",
                    mode = "OpenAndEditSmoke"
                }
            });
            SendRawMessage(payload);
            TrySendPendingLoadFile();
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

        private void OnProcessFailed(CoreWebView2 sender, CoreWebView2ProcessFailedEventArgs e)
        {
            webView.Opacity = 1;
            status = $"WebView2 process failed: {e.ProcessFailedKind}";
        }

        private bool SendMessage(string name, object? args)
        {
            var payload = JsonSerializer.Serialize(new { name, args });
            return SendRawMessage(payload, requireContentLoaded: true);
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

        private async Task<CoreWebView2Environment> GetEnvironmentAsync()
        {
            if (webViewEnvironmentService is not null)
            {
                return await webViewEnvironmentService.GetEnvironmentAsync();
            }

            StartupTrace.CoreWebView2EnvironmentCreateStart();
            try
            {
                return await CoreWebView2Environment.CreateAsync();
            }
            finally
            {
                StartupTrace.CoreWebView2EnvironmentCreateStop();
            }
        }

        private WinUIEditorDocumentSession CreateDocumentSession(string? basePath = null)
        {
            return new WinUIEditorDocumentSession(
                basePath: basePath,
                themeProvider: CreateCurrentThemePayload,
                serviceProvider: serviceProvider);
        }

        private EditorThemePayload CreateCurrentThemePayload()
        {
            var isDark = ActualTheme == ElementTheme.Dark;
            var background = isDark
                ? new EditorColorPayload(40, 40, 40, 1)
                : new EditorColorPayload(249, 249, 249, 1);

            return new EditorThemePayload
            {
                Theme = isDark ? "Dark" : "Light",
                AccentColor = new EditorColorPayload(27, 102, 107, 1),
                Background = background
            };
        }

        private void ApplyNativeEditorBackground(EditorColorPayload background)
        {
            webView.DefaultBackgroundColor = Windows.UI.Color.FromArgb(
                ToByte(background.A * 255),
                ToByte(background.R),
                ToByte(background.G),
                ToByte(background.B));
        }

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

                    document.addEventListener('keydown', event => {
                        if (event.defaultPrevented || event.repeat || !event.ctrlKey || event.altKey) {
                            return;
                        }

                        const key = String(event.key).toLowerCase();
                        let name = null;
                        if (!event.shiftKey && key === 'f') {
                            name = 'OpenFindReplace';
                        } else if (!event.shiftKey && key === 's') {
                            name = 'Save';
                        } else if (event.shiftKey && key === 's') {
                            name = 'SaveAs';
                        } else if (!event.shiftKey && key === 'w') {
                            name = 'Close';
                        }

                        if (name === null) {
                            return;
                        }

                        event.preventDefault();
                        event.stopPropagation();
                        window.chrome.webview.postMessage(JSON.stringify({
                            type: 'message',
                            name,
                            args: {}
                        }));
                    }, true);
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
            webView.CoreWebView2.ContextMenuRequested += OnContextMenuRequested;
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
            webView.CoreWebView2.ContextMenuRequested -= OnContextMenuRequested;
            webView.PreviewKeyDown -= OnPreviewKeyDown;
            coreEventsAttached = false;
        }

        private void OnContextMenuRequested(CoreWebView2 sender, CoreWebView2ContextMenuRequestedEventArgs e)
        {
            e.Handled = true;
            ContextMenuRequested?.Invoke(this, new WinUIEditorContextMenuRequestedEventArgs(new Point(e.Location.X, e.Location.Y)));
        }

        private void OnPreviewKeyDown(object sender, Microsoft.UI.Xaml.Input.KeyRoutedEventArgs e)
        {
            if (serviceProvider?.GetService<IKeyboardAccelerator>() is WinUIKeyboardAccelerator accelerator)
            {
                var args = accelerator.Emit((Typedown.Core.Models.KeyboardKey)(int)e.Key, WinUIKeyboardAccelerator.GetCurrentModifiers());
                e.Handled = args.Handled;
            }
        }

        private void TrySendPendingLoadFile()
        {
            if (!isLoaded || !bridgeAdapter.IsContentLoaded)
            {
                return;
            }

            _ = hostController.TrySendLoadFile();
        }

        public EditorPersistenceResult LoadFile(string filePath)
        {
            InitialFilePath = filePath;
            var result = hostController.LoadFile(filePath);
            if (!result.Success)
            {
                status = $"LoadFile failed: {result.Message}";
            }

            return result;
        }

        public EditorPersistenceResult Save()
        {
            var result = hostController.Save();
            if (!result.Success)
            {
                status = $"Save failed: {result.Message}";
            }

            return result;
        }

        public EditorPersistenceResult SaveAs(string filePath, bool saveCopy = false)
        {
            var result = hostController.SaveAs(filePath, saveCopy);
            if (result.Success && !saveCopy)
            {
                InitialFilePath = filePath;
            }

            if (!result.Success)
            {
                status = $"SaveAs failed: {result.Message}";
            }

            return result;
        }

        public EditorPersistenceResult ReplaceFileText(string text, string? filePath = null, string? basePath = null)
        {
            return hostController.ReplaceFileText(text, filePath, basePath);
        }

        internal bool SendLoadFile()
        {
            return hostSink.Send(EditorHostCommands.CreateLoadFile(documentSession.State));
        }

        internal bool SendThemeChanged(EditorThemePayload payload)
        {
            return hostSink.Send(EditorHostCommands.CreateThemeChanged(payload));
        }

        internal bool SendSearch(EditorSearchRequest request)
        {
            return hostSink.Send(EditorHostCommands.CreateSearch(request));
        }

        internal bool SendReplace(EditorReplaceRequest request)
        {
            return hostSink.Send(EditorHostCommands.CreateReplace(request));
        }

        internal bool SendSearchOpenChange(EditorSearchPanelState state)
        {
            return hostSink.Send(EditorHostCommands.CreateSearchOpenChange(state));
        }

        internal bool SendSettingsChanged(EditorSettingsChange change)
        {
            return hostSink.Send(EditorHostCommands.CreateSettingsChanged(change));
        }

        internal bool SendExport(EditorExportRequest request)
        {
            return hostSink.Send(EditorHostCommands.CreateExport(request));
        }

        internal bool SendCommand(string name, object? args)
        {
            return SendMessage(name, args);
        }

        internal bool SendRawMessage(string payload, bool requireContentLoaded = false)
        {
            if (webView.CoreWebView2 is null || (requireContentLoaded && !bridgeAdapter.IsContentLoaded))
            {
                pendingRawMessages.Enqueue(payload, requireContentLoaded);
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

            pendingRawMessages.Flush(TryPostRawMessage, bridgeAdapter.IsContentLoaded);
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

        private static string ResolveBasePath(string editorIndex)
        {
            var directory = Path.GetDirectoryName(editorIndex);
            return string.IsNullOrWhiteSpace(directory) ? AppContext.BaseDirectory : directory;
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
