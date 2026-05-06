using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using Typedown.Core.Enums;
using Typedown.Core.Utilities;
using Typedown.Presentation.Services;
using Typedown.Presentation.Utilities;
using Typedown.Presentation.ViewModels;
using Typedown.WinUI.Controls;

namespace Typedown.WinUI.Pages.SettingPages
{
    public sealed partial class ImagePage : Page, INotifyPropertyChanged
    {
        public AppViewModel? ViewModel { get; private set; }

        public SettingsViewModel? SettingsViewModel { get; private set; }

        public SettingsViewModel Settings => ViewModel?.SettingsViewModel;

        public ImageUpload ImageUpload => this.GetService<ImageUpload>();

        public ObservableCollection<UploadConfigOption> UploadConfigOptions { get; } = new();

        private UploadConfigOption clipboardImageUploadConfig = UploadConfigOption.None;

        public UploadConfigOption ClipboardImageUploadConfig
        {
            get => clipboardImageUploadConfig;
            set
            {
                value ??= UploadConfigOption.None;
                if (EqualityComparer<UploadConfigOption>.Default.Equals(clipboardImageUploadConfig, value))
                    return;

                clipboardImageUploadConfig = value;
                OnPropertyChanged(nameof(ClipboardImageUploadConfig));
            }
        }

        private UploadConfigOption localImageUploadConfig = UploadConfigOption.None;

        public UploadConfigOption LocalImageUploadConfig
        {
            get => localImageUploadConfig;
            set
            {
                value ??= UploadConfigOption.None;
                if (EqualityComparer<UploadConfigOption>.Default.Equals(localImageUploadConfig, value))
                    return;

                localImageUploadConfig = value;
                OnPropertyChanged(nameof(LocalImageUploadConfig));
            }
        }

        private UploadConfigOption webImageUploadConfig = UploadConfigOption.None;

        public UploadConfigOption WebImageUploadConfig
        {
            get => webImageUploadConfig;
            set
            {
                value ??= UploadConfigOption.None;
                if (EqualityComparer<UploadConfigOption>.Default.Equals(webImageUploadConfig, value))
                    return;

                webImageUploadConfig = value;
                OnPropertyChanged(nameof(WebImageUploadConfig));
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        private readonly CompositeDisposable disposables = new();

        private readonly CompositeDisposable ImageUploadConfigsDisposables = new();

        public ImagePage()
        {
            NavigationCacheMode = NavigationCacheMode.Enabled;
            InitializeComponent();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            if (e.Parameter is SettingsNavigationParameter parameter)
            {
                ViewModel = parameter.AppViewModel;
                SettingsViewModel = parameter.SettingsViewModel;
                DataContext = ViewModel;
                Bindings.Update();
            }
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            disposables.Add(ImageUpload.ImageUploadConfigs.GetCollectionObservable().Subscribe(_ => UpdateUploadConfigOptions()));
            UpdateUploadConfigOptions();
        }

        public Visibility IsCopyImagePathSettingItemVisibility(InsertImageAction action)
        {
            return action == InsertImageAction.CopyToPath ? Visibility.Visible : Visibility.Collapsed;
        }

        public Visibility IsSelectUploadConfigSettingItemVisibility(InsertImageAction action)
        {
            return action == InsertImageAction.Upload ? Visibility.Visible : Visibility.Collapsed;
        }

        private async void UpdateUploadConfigOptions()
        {
            ImageUploadConfigsDisposables.Clear();
            foreach (var config in ImageUpload.ImageUploadConfigs)
                ImageUploadConfigsDisposables.Add(config.WhenPropertyChanged(nameof(config.IsEnable)).Subscribe(_ => UpdateUploadConfigOptions()));

            UploadConfigOptions.UpdateCollection(ImageUpload.ImageUploadConfigs
                .Where(x => x.IsEnable)
                .Select(x => new UploadConfigOption { Id = x.Id, Name = x.Name })
                .Append(UploadConfigOption.None)
                .ToList(),
                (a, b) => a.Id == b.Id);

            await Task.Yield();
            ClipboardImageUploadConfig = UploadConfigOptions.Where(x => x.Id == Settings.InsertClipboardImageUseUploadConfigId).FirstOrDefault() ?? UploadConfigOption.None;
            LocalImageUploadConfig = UploadConfigOptions.Where(x => x.Id == Settings.InsertLocalImageUseUploadConfigId).FirstOrDefault() ?? UploadConfigOption.None;
            WebImageUploadConfig = UploadConfigOptions.Where(x => x.Id == Settings.InsertWebImageUseUploadConfigId).FirstOrDefault() ?? UploadConfigOption.None;
            ImageUploadConfigsDisposables.Add(this.WhenPropertyChanged(nameof(ClipboardImageUploadConfig)).Cast<UploadConfigOption>().Subscribe(x => Settings.InsertClipboardImageUseUploadConfigId = x.Id));
            ImageUploadConfigsDisposables.Add(this.WhenPropertyChanged(nameof(LocalImageUploadConfig)).Cast<UploadConfigOption>().Subscribe(x => Settings.InsertLocalImageUseUploadConfigId = x.Id));
            ImageUploadConfigsDisposables.Add(this.WhenPropertyChanged(nameof(WebImageUploadConfig)).Cast<UploadConfigOption>().Subscribe(x => Settings.InsertWebImageUseUploadConfigId = x.Id));
        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            disposables.Clear();
            ImageUploadConfigsDisposables.Clear();
        }

        private void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public record UploadConfigOption
    {
        public static UploadConfigOption None => new() { Id = null, Name = Locale.GetString("None") };

        public int? Id { get; set; }

        public string Name { get; set; }
    }
}
