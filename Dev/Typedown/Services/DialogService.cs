using System;
using System.Threading.Tasks;
using Typedown.Core.Controls;
using Typedown.Core.Interfaces;
using Typedown.Core.ViewModels;
using Windows.UI.Xaml.Controls;

namespace Typedown.Services
{
    public class DialogService : IDialogService
    {
        private readonly AppViewModel appViewModel;

        public DialogService(AppViewModel appViewModel)
        {
            this.appViewModel = appViewModel;
        }

        public async Task<DialogButton> ShowAsync(DialogRequest request)
        {
            if (appViewModel.XamlRoot == null)
                throw new InvalidOperationException("XamlRoot is not available for dialog display.");

            var dialog = AppContentDialog.Create();
            dialog.Title = request.Title;
            dialog.Content = request.Content;
            dialog.CloseButtonText = request.CloseButtonText;
            dialog.PrimaryButtonText = request.PrimaryButtonText;
            dialog.SecondaryButtonText = request.SecondaryButtonText;
            dialog.DefaultButton = MapDefaultButton(request.DefaultButton);

            return MapResult(await dialog.ShowAsync(appViewModel.XamlRoot));
        }

        private static ContentDialogButton MapDefaultButton(DialogButton button)
        {
            return button switch
            {
                DialogButton.Primary => ContentDialogButton.Primary,
                DialogButton.Secondary => ContentDialogButton.Secondary,
                DialogButton.Close => ContentDialogButton.Close,
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
