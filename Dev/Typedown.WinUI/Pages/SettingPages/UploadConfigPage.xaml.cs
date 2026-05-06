using System;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Typedown.WinUI.Pages.SettingPages.UploadConfigPageParts;
using Typedown.Core.Enums;
using Typedown.Core.Models;
using Typedown.Core.Utilities;
using Typedown.Presentation.Interfaces;
using Typedown.Presentation.Services;
using Typedown.Presentation.Utilities;
using Typedown.WinUI.Controls;
using Typedown.Presentation.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using Windows.Storage.Pickers;

namespace Typedown.WinUI.Pages.SettingPages
{
    public sealed partial class UploadConfigPage : Page
    {
        private static DependencyProperty ImageUploadConfigProperty { get; } = DependencyProperty.Register(nameof(ImageUploadConfig), typeof(ImageUploadConfig), typeof(UploadConfigPage), null);
        private ImageUploadConfig ImageUploadConfig { get => (ImageUploadConfig)GetValue(ImageUploadConfigProperty); set => SetValue(ImageUploadConfigProperty, value); }

        public AppViewModel ViewModel => DataContext as AppViewModel;

        public Lazy<ImageUpload> UploadService { get; }

        private int configId;

        private readonly CompositeDisposable disposables = new();

        public UploadConfigPage()
        {
            UploadService = new(() => this.GetService<ImageUpload>());
            InitializeComponent();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            if (e.Parameter is SettingsNavigationParameter parameter)
            {
                DataContext = parameter.AppViewModel;
                int.TryParse(parameter.Query, out configId);
                return;
            }

            int.TryParse(e.Parameter?.ToString(), out configId);
        }

        private async void OnLoaded(object sender, RoutedEventArgs e)
        {
            ImageUploadConfig = await UploadService.Value.GetImageUploadConfig(configId);
            if (ImageUploadConfig != null)
                disposables.Add(ImageUploadConfig.WhenPropertyChanged(nameof(ImageUploadConfig.Name)).Cast<string>().StartWith(ImageUploadConfig.Name).Subscribe(UpdateTitle));
        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            _ = Dispatcher.RunIdleAsync(async _ =>
            {
                if (ImageUploadConfig != null)
                    await UploadService.Value.SaveImageUploadConfig(ImageUploadConfig);
            });
            disposables.Clear();
        }

        private void UpdateTitle(string title)
        {
            this.GetAncestor<SettingsPage>()?.SetPageTitle(this, title);
        }

        private void OnDeleteButtonClick(object sender, RoutedEventArgs e)
        {
            _ = DeleteConfigAsync();
        }

        private async void OnTestUploadButtonClick(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            try
            {
                button.IsEnabled = false;
                var filePicker = new FileOpenPicker();
                FileTypeHelper.Image.ToList().ForEach(filePicker.FileTypeFilter.Add);
                filePicker.SetOwnerWindow(this.GetService<IWindowService>().GetWindow(this));
                var file = await filePicker.PickSingleFileAsync();
                if (file == null)
                    return;

                var res = await ImageUploadConfig.LoadUploadConfig().Upload(this.GetService<IServiceProvider>(), file.Path);
                await ShowMessageAsync(Locale.GetDialogString("UploadSuccessfulTitle"), res);
            }
            catch (Exception ex)
            {
                await ShowMessageAsync(Locale.GetDialogString("UploadFailedTitle"), ex.Message);
            }
            finally
            {
                button.IsEnabled = true;
            }
        }

        public FrameworkElement GetUploadConfigItem(ImageUploadMethod method)
        {
            return method switch
            {
                ImageUploadMethod.FTP => new FTPConfig() { ImageUploadConfig = ImageUploadConfig },
                ImageUploadMethod.Git => new GitConfig() { ImageUploadConfig = ImageUploadConfig },
                ImageUploadMethod.OSS => new OSSConfig() { ImageUploadConfig = ImageUploadConfig },
                ImageUploadMethod.SCP => new SCPConfig() { ImageUploadConfig = ImageUploadConfig },
                ImageUploadMethod.PowerShell => new PowerShellConfig() { ImageUploadConfig = ImageUploadConfig },
                _ => null
            };
        }

        public async Task DeleteConfigAsync()
        {
            if (ImageUploadConfig != null)
            {
                var service = this.GetService<ImageUpload>();
                await service.RemoveImageUploadConfig(ImageUploadConfig.Id);
                ImageUploadConfig = null;
                Frame.GoBack();
            }
        }

        private async Task ShowMessageAsync(string title, string message)
        {
            var dialog = new ContentDialog
            {
                XamlRoot = XamlRoot,
                Title = title,
                Content = message,
                CloseButtonText = Locale.GetDialogString("Ok")
            };

            await dialog.ShowAsync();
        }
    }
}
