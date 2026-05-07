using System;
using System.Globalization;
using System.IO;
using System.Text.Json;
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
        private IEditorDocumentSession documentSession;
        private WinUIEditorHostController hostController;
        private WinUIEditorBridgeAdapter bridgeAdapter;
        private string status;
        private string latestRawWebMessage;
        private bool coreInitialized;
        private bool isLoaded;
        private bool coreEventsAttached;
        private bool editorNavigationStarted;
        private int loadVersion;

        public event EventHandler<WinUIEditorContextMenuRequestedEventArgs>? ContextMenuRequested;

        public WinUIEditorHost(IServiceProvider? serviceProvider = null)
        {
            using (StartupTrace.Phase("WinUIEditorHost ctor"))
            {
                this.serviceProvider = serviceProvider;
                commandSink = serviceProvider?.GetService<IEditorCommandSink>() as WinUIEditorCommandSink;
                webViewEnvironmentService = serviceProvider?.GetService<WinUIWebViewEnvironmentService>();
                hostSink = new WinUIEditorHostSink(this);
                documentSession = CreateDocumentSession();
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
            isLoaded = true;
            commandSink?.RegisterActiveHost(this);
            var currentLoadVersion = ++loadVersion;

            var editorIndex = ResolveEditorIndexPath();
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
                    if (bridgeAdapter.IsContentLoaded)
                    {
                        webView.Opacity = 1;
                    }

                    status = bridgeAdapter.StatusText;
                    latestRawWebMessage = bridgeAdapter.LastRawMessage;
                    TrySendPendingLoadFile();
                    return;
                }

                if (!coreInitialized)
                {
                    var environment = await GetEnvironmentAsync();
                    using (StartupTrace.Phase("WebView2.EnsureCoreWebView2Async"))
                    {
                        await webView.EnsureCoreWebView2Async(environment);
                    }
                    if (!isLoaded || currentLoadVersion != loadVersion)
                    {
                        return;
                    }

                    coreInitialized = true;
                    await webView.CoreWebView2.AddScriptToExecuteOnDocumentCreatedAsync(BuildInitialEditorBackgroundScript(themePayload.Background));
                    await webView.CoreWebView2.AddScriptToExecuteOnDocumentCreatedAsync(BuildFindShortcutScript());
                }

                AttachCoreWebView();
                documentSession = CreateDocumentSession(ResolveBasePath(editorIndex));
                hostController = new WinUIEditorHostController(documentSession, hostSink)
                {
                    InitialFilePath = InitialFilePath
                };
                bridgeAdapter = new WinUIEditorBridgeAdapter(documentSession);
                bridgeAdapter.ResetForNavigation();
                hostController.ResetForNavigation();
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
                webView.CoreWebView2.Settings.AreDevToolsEnabled = false;
                webView.CoreWebView2.Settings.IsBuiltInErrorPageEnabled = false;
                webView.CoreWebView2.Settings.IsStatusBarEnabled = false;
                webView.CoreWebView2.Settings.IsZoomControlEnabled = false;
                webView.Opacity = 0;
                editorNavigationStarted = true;
                webView.CoreWebView2.Navigate(new Uri(editorIndex).AbsoluteUri);
                status = $"Editor host navigating to {editorIndex}";
            }
            catch (Exception ex)
            {
                status = $"WebView2 initialization failed: {ex.GetType().Name}: {ex.Message}";
            }
        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            isLoaded = false;
            loadVersion++;
            commandSink?.UnregisterActiveHost(this);
            DetachCoreWebView();
        }

        private async void OnWebMessageReceived(CoreWebView2 sender, CoreWebView2WebMessageReceivedEventArgs e)
        {
            var wasContentLoaded = bridgeAdapter.IsContentLoaded;
            await bridgeAdapter.ReceiveAsync(e.TryGetWebMessageAsString(), SendRawMessage);
            status = bridgeAdapter.StatusText;
            latestRawWebMessage = bridgeAdapter.LastRawMessage;

            if (!wasContentLoaded && bridgeAdapter.IsContentLoaded)
            {
                webView.Opacity = 1;
                hostController.MarkEditorReady();
                TrySendPendingLoadFile();
            }
        }

        private async void OnNavigationCompleted(CoreWebView2 sender, CoreWebView2NavigationCompletedEventArgs e)
        {
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

        private bool SendMessage(string name, object? args)
        {
            var payload = JsonSerializer.Serialize(new { name, args });
            return SendRawMessage(payload);
        }

        private async Task<CoreWebView2Environment> GetEnvironmentAsync()
        {
            if (webViewEnvironmentService is not null)
            {
                return await webViewEnvironmentService.GetEnvironmentAsync();
            }

            return await CoreWebView2Environment.CreateAsync();
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

        private static string BuildInitialEditorBackgroundScript(EditorColorPayload background)
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
                        document.documentElement.style.backgroundColor = color;
                        if (document.body) {
                            document.body.style.backgroundColor = color;
                        }
                    };
                    apply();
                    document.addEventListener('DOMContentLoaded', apply, { once: true });
                })();
                """);
        }

        private static string BuildFindShortcutScript()
        {
            return """
                (() => {
                    document.addEventListener('keydown', event => {
                        if (!event.ctrlKey || event.shiftKey || event.altKey || String(event.key).toLowerCase() !== 'f') {
                            return;
                        }

                        event.preventDefault();
                        event.stopPropagation();
                        window.chrome.webview.postMessage(JSON.stringify({
                            type: 'message',
                            name: 'OpenFindReplace',
                            args: {}
                        }));
                    }, true);
                })();
                """;
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
            webView.CoreWebView2.NavigationCompleted += OnNavigationCompleted;
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
            webView.CoreWebView2.NavigationCompleted -= OnNavigationCompleted;
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

        internal bool SendRawMessage(string payload)
        {
            try
            {
                webView.CoreWebView2?.PostWebMessageAsString(payload);
                return true;
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
