using System;
using System.Threading.Tasks;
using Typedown.Core.Controls;
using Typedown.Core.Interfaces;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml;

namespace Typedown.Services
{
    public class DialogService : IDialogService
    {
        private readonly IWindowContext windowContext;

        public DialogService(IWindowContext windowContext)
        {
            this.windowContext = windowContext;
        }

        public async Task<DialogButton> ShowAsync(DialogRequest request)
        {
            if (windowContext.ViewRoot is not XamlRoot viewRoot)
                throw new InvalidOperationException("Window view root is not available for dialog display.");

            var dialog = AppContentDialog.Create();
            dialog.Title = request.Title;
            dialog.Content = request.Content;
            dialog.CloseButtonText = request.CloseButtonText;
            dialog.PrimaryButtonText = request.PrimaryButtonText;
            dialog.SecondaryButtonText = request.SecondaryButtonText;
            dialog.DefaultButton = MapDefaultButton(request.DefaultButton);

            return MapResult(await dialog.ShowAsync(viewRoot));
        }

        private static ContentDialogButton MapDefaultButton(DialogDefaultButton button)
        {
            return button switch
            {
                DialogDefaultButton.Close => ContentDialogButton.Close,
                DialogDefaultButton.Primary => ContentDialogButton.Primary,
                DialogDefaultButton.Secondary => ContentDialogButton.Secondary,
                _ => ContentDialogButton.None
            };
        }

        private static DialogButton MapResult(ContentDialogResult result)
        {
            return result switch
            {
                ContentDialogResult.Primary => DialogButton.Primary,
                ContentDialogResult.Secondary => DialogButton.Secondary,
                _ => DialogButton.None
            };
        }
    }
}
