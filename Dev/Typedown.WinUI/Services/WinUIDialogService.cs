using System;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Typedown.Core.Interfaces;

namespace Typedown.WinUI.Services
{
    internal sealed class WinUIDialogService : IDialogService
    {
        private readonly IWindowContext windowContext;

        public WinUIDialogService(IWindowContext windowContext)
        {
            this.windowContext = windowContext ?? throw new ArgumentNullException(nameof(windowContext));
        }

        public async Task<DialogButton> ShowAsync(DialogRequest request)
        {
            request ??= new DialogRequest();

            var dialog = new ContentDialog
            {
                Title = request.Title,
                Content = request.Content,
                CloseButtonText = request.CloseButtonText ?? "Close",
                PrimaryButtonText = request.PrimaryButtonText,
                SecondaryButtonText = request.SecondaryButtonText,
                DefaultButton = MapDefaultButton(request.DefaultButton),
                XamlRoot = ResolveXamlRoot()
            };

            var result = await dialog.ShowAsync();
            return result switch
            {
                ContentDialogResult.Primary => DialogButton.Primary,
                ContentDialogResult.Secondary => DialogButton.Secondary,
                _ => DialogButton.None
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

            throw new InvalidOperationException("WinUI dialog service requires a XamlRoot-backed view root.");
        }

        private static ContentDialogButton MapDefaultButton(DialogDefaultButton defaultButton)
        {
            return defaultButton switch
            {
                DialogDefaultButton.Primary => ContentDialogButton.Primary,
                DialogDefaultButton.Secondary => ContentDialogButton.Secondary,
                DialogDefaultButton.Close => ContentDialogButton.Close,
                _ => ContentDialogButton.None
            };
        }
    }
}
