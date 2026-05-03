using Typedown.Presentation.Interfaces;
using Typedown.Presentation.Utilities;

namespace Typedown.WinUI.Services
{
    internal sealed class WinUITableDialogService : ITableDialogService
    {
        private readonly IWindowContext windowContext;

        public WinUITableDialogService(IWindowContext windowContext)
        {
            this.windowContext = windowContext ?? throw new ArgumentNullException(nameof(windowContext));
        }

        public Task<TableDialogResult> OpenInsertTableDialogAsync()
        {
            return OpenTableDialogAsync(Locale.GetDialogString("InsertTableTitle"));
        }

        public Task<TableDialogResult> OpenResizeTableDialogAsync()
        {
            return OpenTableDialogAsync(Locale.GetDialogString("ResizeTableTitle"));
        }

        private async Task<TableDialogResult> OpenTableDialogAsync(string title)
        {
            var rows = CreateNumberBox("Rows", 4);
            var columns = CreateNumberBox("Columns", 3);

            var content = new StackPanel
            {
                Spacing = 8,
                Children =
                {
                    rows,
                    columns
                }
            };

            var dialog = new ContentDialog
            {
                Title = title,
                Content = content,
                CloseButtonText = Locale.GetDialogString("Cancel"),
                PrimaryButtonText = Locale.GetDialogString("Ok"),
                DefaultButton = ContentDialogButton.Primary,
                XamlRoot = ResolveXamlRoot()
            };

            var result = await dialog.ShowAsync();
            return result == ContentDialogResult.Primary
                ? new TableDialogResult { Rows = (int)rows.Value, Columns = (int)columns.Value }
                : null;
        }

        private static NumberBox CreateNumberBox(string headerKey, double value)
        {
            return new NumberBox
            {
                Header = Locale.GetString(headerKey),
                Value = value,
                Minimum = 1,
                SpinButtonPlacementMode = NumberBoxSpinButtonPlacementMode.Inline
            };
        }

        private XamlRoot ResolveXamlRoot()
        {
            if (windowContext.ViewRoot is XamlRoot xamlRoot)
            {
                return xamlRoot;
            }

            if (windowContext.ViewRoot is FrameworkElement element && element.XamlRoot is not null)
            {
                return element.XamlRoot;
            }

            throw new InvalidOperationException("WinUI table dialog service requires a XamlRoot-backed view root.");
        }
    }
}
