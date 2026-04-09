using System;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Typedown.Core.Controls.SettingControls.SettingItems.UploadConfigItems;
using Typedown.Core.Enums;
using Typedown.Core.Models;
using Typedown.Core.Services;
using Typedown.Core.Utilities;
using Typedown.Core.ViewModels;
using Avalonia;
using Avalonia.Interactivity;
using Avalonia.Controls;

namespace Typedown.Core.Pages.SettingPages
{
    public sealed partial class UploadConfigPage : UserControl
    {
        public static readonly StyledProperty<ImageUploadConfig> ImageUploadConfigProperty =
            AvaloniaProperty.Register<UploadConfigPage, ImageUploadConfig>(nameof(ImageUploadConfig), null);
        private ImageUploadConfig ImageUploadConfig { get => GetValue(ImageUploadConfigProperty); set => SetValue(ImageUploadConfigProperty, value); }

        public AppViewModel ViewModel => DataContext as AppViewModel;

        public Lazy<ImageUpload> UploadService { get; }

        private int configId;

        private readonly CompositeDisposable disposables = new();

        public UploadConfigPage()
        {
            UploadService = new(() => this.GetService<ImageUpload>());
            InitializeComponent();
        }

        /// <summary>
        /// Call this to initialize the page with a config ID parameter.
        /// Replaces WinUI OnNavigatedTo(NavigationEventArgs e).
        /// </summary>
        public void Initialize(string parameter)
        {
            int.TryParse(parameter, out configId);
        }

        private async void OnLoaded(object sender, RoutedEventArgs e)
        {
            ImageUploadConfig = await UploadService.Value.GetImageUploadConfig(configId);
            if (ImageUploadConfig != null)
                disposables.Add(ImageUploadConfig.WhenPropertyChanged(nameof(ImageUploadConfig.Name)).Cast<string>().StartWith(ImageUploadConfig.Name).Subscribe(UpdateTitle));
        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            if (ImageUploadConfig != null)
            {
                var config = ImageUploadConfig;
                var service = UploadService.Value;
                _ = Task.Run(async () => await service.SaveImageUploadConfig(config));
            }
            disposables.Clear();
        }

        private void UpdateTitle(string title)
        {
            this.GetAncestor<SettingsPage>()?.SetPageTitle(this, title);
        }

        public Control GetUploadConfigItem(ImageUploadMethod method)
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
                // TODO: Navigate back in Avalonia
            }
        }
    }
}
