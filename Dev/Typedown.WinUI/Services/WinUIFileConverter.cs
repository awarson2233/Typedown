using System.Text;
using Microsoft.Web.WebView2.Core;
using Typedown.Core.Enums;
using Typedown.Core.Interfaces;
using Typedown.Core.Models;
using Windows.Foundation;

namespace Typedown.WinUI.Services
{
    internal sealed class WinUIFileConverter : IFileConverter
    {
        private readonly IUiDispatcher dispatcher;
        private readonly IWindowContext windowContext;
        private readonly WinUIWebViewEnvironmentService webViewEnvironmentService;

        public WinUIFileConverter(IUiDispatcher dispatcher, IWindowContext windowContext, WinUIWebViewEnvironmentService webViewEnvironmentService)
        {
            this.dispatcher = dispatcher ?? throw new ArgumentNullException(nameof(dispatcher));
            this.windowContext = windowContext ?? throw new ArgumentNullException(nameof(windowContext));
            this.webViewEnvironmentService = webViewEnvironmentService ?? throw new ArgumentNullException(nameof(webViewEnvironmentService));
        }

        public Task<MemoryStream> HtmlToPdf(string html, PdfPrintSettings? settings = null)
        {
            return dispatcher.RunAsync(() => ConvertOnUiThreadAsync(html, settings)).Unwrap();
        }

        private async Task<MemoryStream> ConvertOnUiThreadAsync(string html, PdfPrintSettings? settings)
        {
            if (windowContext.WindowHandle == 0)
            {
                throw new InvalidOperationException("A WinUI window handle is required to create the WebView2 print controller.");
            }

            // Reuse the editor's environment: same browser process, command-line switches (local file
            // access) and user data folder, instead of spinning up a default environment per export.
            var environment = await webViewEnvironmentService.GetEnvironmentAsync();
            var controllerWindow = CoreWebView2ControllerWindowReference.CreateFromWindowHandle((ulong)windowContext.WindowHandle);
            var controller = await environment.CreateCoreWebView2ControllerAsync(controllerWindow);
            var tempName = $"Typedown-{Guid.NewGuid():N}";
            var tempHtmlPath = Path.Combine(Path.GetTempPath(), tempName + ".html");
            var tempPdfPath = Path.Combine(Path.GetTempPath(), tempName + ".pdf");

            try
            {
                controller.Bounds = new Rect(0, 0, 1, 1);
                var coreWebView = controller.CoreWebView2;
                // NavigateToString caps the content at 2 MB; load large documents from a temporary file instead.
                // The BOM pins the decoding to UTF-8 even when the exported HTML carries no charset meta tag.
                await File.WriteAllTextAsync(tempHtmlPath, html ?? string.Empty, new UTF8Encoding(encoderShouldEmitUTF8Identifier: true));
                await NavigateAndWait(coreWebView, new Uri(tempHtmlPath).AbsoluteUri);
                await coreWebView.PrintToPdfAsync(tempPdfPath, CreatePrintSettings(environment, settings));
                return new MemoryStream(await File.ReadAllBytesAsync(tempPdfPath));
            }
            finally
            {
                controller.Close();
                TryDelete(tempHtmlPath);
                TryDelete(tempPdfPath);
            }
        }

        private static Task NavigateAndWait(CoreWebView2 coreWebView, string uri)
        {
            var navigationCompleted = new TaskCompletionSource<object?>(TaskCreationOptions.RunContinuationsAsynchronously);

            void OnNavigationCompleted(object? sender, CoreWebView2NavigationCompletedEventArgs args)
            {
                coreWebView.NavigationCompleted -= OnNavigationCompleted;
                if (args.IsSuccess)
                {
                    navigationCompleted.TrySetResult(null);
                }
                else
                {
                    navigationCompleted.TrySetException(new InvalidOperationException($"WebView2 failed to load HTML for PDF export: {args.WebErrorStatus}."));
                }
            }

            coreWebView.NavigationCompleted += OnNavigationCompleted;
            coreWebView.Navigate(uri);
            return navigationCompleted.Task;
        }

        private static CoreWebView2PrintSettings? CreatePrintSettings(CoreWebView2Environment environment, PdfPrintSettings? settings)
        {
            if (settings is null)
            {
                return null;
            }

            var webViewSettings = environment.CreatePrintSettings();
            webViewSettings.Orientation = settings.Orientation == PrintOrientation.Landscape
                ? CoreWebView2PrintOrientation.Landscape
                : CoreWebView2PrintOrientation.Portrait;

            if (settings.ScaleFactor.HasValue)
            {
                webViewSettings.ScaleFactor = settings.ScaleFactor.Value;
            }

            if (settings.PageSize.HasValue)
            {
                webViewSettings.PageWidth = settings.PageSize.Value.Width;
                webViewSettings.PageHeight = settings.PageSize.Value.Height;
            }

            if (settings.Margin.HasValue)
            {
                webViewSettings.MarginLeft = settings.Margin.Value.Left;
                webViewSettings.MarginTop = settings.Margin.Value.Top;
                webViewSettings.MarginRight = settings.Margin.Value.Right;
                webViewSettings.MarginBottom = settings.Margin.Value.Bottom;
            }

            webViewSettings.ShouldPrintHeaderAndFooter = settings.ShouldPrintHeaderAndFooter;
            webViewSettings.HeaderTitle = settings.Header;
            webViewSettings.FooterUri = settings.Footer;

            return webViewSettings;
        }

        private static void TryDelete(string path)
        {
            try
            {
                File.Delete(path);
            }
            catch
            {
            }
        }
    }
}
