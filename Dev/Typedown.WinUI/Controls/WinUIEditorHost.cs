using System;
using System.IO;
using System.Text.Json;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Web.WebView2.Core;
using Typedown.Core.Contracts.Editor;

namespace Typedown.WinUI.Controls
{
    public sealed class WinUIEditorHost : UserControl
    {
        private readonly WebView2 webView;
        private readonly TextBlock statusText;
        private readonly TextBlock messageText;
        private readonly IEditorHostSink hostSink;
        private IEditorDocumentSession documentSession;
        private WinUIEditorBridgeAdapter bridgeAdapter;
        private bool coreInitialized;
        private bool isLoaded;
        private bool coreEventsAttached;
        private bool pendingLoadFile;
        private int loadVersion;

        public WinUIEditorHost()
        {
            documentSession = new WinUIEditorDocumentSession();
            hostSink = new WinUIEditorHostSink(this);
            bridgeAdapter = new WinUIEditorBridgeAdapter(documentSession);
            webView = new WebView2
            {
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch
            };

            statusText = new TextBlock
            {
                Text = "Loaded=False; FileLoaded=False; MarkdownLength=0; LastEvent=Waiting",
                TextWrapping = TextWrapping.Wrap
            };

            messageText = new TextBlock
            {
                Text = bridgeAdapter.LastRawMessage,
                TextWrapping = TextWrapping.Wrap
            };

            Content = BuildLayout();
            Loaded += OnLoaded;
            Unloaded += OnUnloaded;
        }

        public string Status => statusText.Text;

        public string LatestRawWebMessage => messageText.Text;

        private Grid BuildLayout()
        {
            var root = new Grid
            {
                RowSpacing = 12
            };

            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            var statusBorder = new Border
            {
                Background = new Microsoft.UI.Xaml.Media.SolidColorBrush(Windows.UI.Color.FromArgb(255, 244, 239, 229)),
                BorderBrush = new Microsoft.UI.Xaml.Media.SolidColorBrush(Windows.UI.Color.FromArgb(255, 216, 203, 184)),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(10),
                Padding = new Thickness(12),
                Child = statusText
            };

            var messageBorder = new Border
            {
                Background = new Microsoft.UI.Xaml.Media.SolidColorBrush(Windows.UI.Color.FromArgb(255, 25, 54, 59)),
                CornerRadius = new CornerRadius(10),
                Padding = new Thickness(12),
                Child = messageText
            };
            messageText.Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Windows.UI.Color.FromArgb(255, 255, 255, 255));

            Grid.SetRow(statusBorder, 0);
            Grid.SetRow(webView, 1);
            Grid.SetRow(messageBorder, 2);

            root.Children.Add(statusBorder);
            root.Children.Add(webView);
            root.Children.Add(messageBorder);
            return root;
        }

        private async void OnLoaded(object sender, RoutedEventArgs e)
        {
            isLoaded = true;
            var currentLoadVersion = ++loadVersion;

            var editorIndex = ResolveEditorIndexPath();
            if (editorIndex is null)
            {
                statusText.Text = "Editor static bundle is missing. Run yarn build in Dev\\Typedown.Editor to generate Dev\\Typedown\\Resources\\Statics\\index.html.";
                return;
            }

            try
            {
                if (!coreInitialized)
                {
                    await webView.EnsureCoreWebView2Async();
                    if (!isLoaded || currentLoadVersion != loadVersion)
                    {
                        return;
                    }

                    coreInitialized = true;
                }

                AttachCoreWebView();
                documentSession = new WinUIEditorDocumentSession(basePath: ResolveBasePath(editorIndex));
                bridgeAdapter = new WinUIEditorBridgeAdapter(documentSession);
                bridgeAdapter.ResetForNavigation();
                pendingLoadFile = true;
                statusText.Text = bridgeAdapter.StatusText;
                messageText.Text = bridgeAdapter.LastRawMessage;

                webView.CoreWebView2.Settings.AreDefaultContextMenusEnabled = false;
                webView.CoreWebView2.Settings.AreBrowserAcceleratorKeysEnabled = false;
                webView.CoreWebView2.Settings.AreDevToolsEnabled = false;
                webView.CoreWebView2.Settings.IsBuiltInErrorPageEnabled = false;
                webView.CoreWebView2.Settings.IsStatusBarEnabled = false;
                webView.CoreWebView2.Settings.IsZoomControlEnabled = false;
                webView.CoreWebView2.Navigate(new Uri(editorIndex).AbsoluteUri);
                statusText.Text = $"Editor host navigating to {editorIndex}";
            }
            catch (Exception ex)
            {
                statusText.Text = $"WebView2 initialization failed: {ex.GetType().Name}: {ex.Message}";
            }
        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            isLoaded = false;
            loadVersion++;
            pendingLoadFile = false;
            DetachCoreWebView();
        }

        private void OnWebMessageReceived(CoreWebView2 sender, CoreWebView2WebMessageReceivedEventArgs e)
        {
            var wasContentLoaded = bridgeAdapter.IsContentLoaded;
            bridgeAdapter.Receive(e.TryGetWebMessageAsString(), SendRawMessage);
            statusText.Text = bridgeAdapter.StatusText;
            messageText.Text = bridgeAdapter.LastRawMessage;

            if (!wasContentLoaded && bridgeAdapter.IsContentLoaded)
            {
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
                statusText.Text = $"Editor navigation failed: {e.WebErrorStatus}";
                return;
            }

            statusText.Text = "Editor static bundle loaded. Bridge smoke message sent from WinUI host.";
            var payload = JsonSerializer.Serialize(new
            {
                name = "WinUIHostReady",
                args = new
                {
                    shell = "Typedown.WinUI",
                    phase = "Phase 11",
                    mode = "OpenAndEditSmoke"
                }
            });
            SendRawMessage(payload);
            TrySendPendingLoadFile();
        }

        private bool SendMessage(string name, object args)
        {
            var payload = JsonSerializer.Serialize(new { name, args });
            return SendRawMessage(payload);
        }

        private void AttachCoreWebView()
        {
            if (coreEventsAttached || webView.CoreWebView2 is null)
            {
                return;
            }

            webView.CoreWebView2.WebMessageReceived += OnWebMessageReceived;
            webView.CoreWebView2.NavigationCompleted += OnNavigationCompleted;
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
            coreEventsAttached = false;
        }

        private void TrySendPendingLoadFile()
        {
            if (!isLoaded || !bridgeAdapter.IsContentLoaded || !pendingLoadFile)
            {
                return;
            }

            pendingLoadFile = !hostSink.Send(new EditorHostMessage("LoadFile", new
            {
                text = documentSession.State.Text,
                basePath = documentSession.State.BasePath
            }));
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
                var candidate = Path.Combine(directory.FullName, "Dev", "Typedown", "Resources", "Statics", "index.html");
                if (File.Exists(candidate))
                {
                    return candidate;
                }

                directory = directory.Parent;
            }

            return null;
        }
    }
}
