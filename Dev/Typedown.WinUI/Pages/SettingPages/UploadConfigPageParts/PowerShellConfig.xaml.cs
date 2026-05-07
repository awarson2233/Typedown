using System;
using System.Collections.Generic;
using System.IO;
using Typedown.Core.Interfaces;
using Typedown.Presentation.Interfaces;
using Typedown.Core.Models;
using Typedown.Core.Models.UploadConfigModels;
using Typedown.WinUI.Controls;
using Windows.Storage.Pickers;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Typedown.WinUI.Pages.SettingPages.UploadConfigPageParts
{
        public sealed partial class PowerShellConfig : UserControl
        {
            public static DependencyProperty ImageUploadConfigProperty { get; } = DependencyProperty.Register(nameof(ImageUploadConfig), typeof(ImageUploadConfig), typeof(PowerShellConfig), null);
        public ImageUploadConfig? ImageUploadConfig { get => (ImageUploadConfig?)GetValue(ImageUploadConfigProperty); set => SetValue(ImageUploadConfigProperty, value); }

        public static DependencyProperty PowerShellConfigModelProperty { get; } = DependencyProperty.Register(nameof(PowerShellConfigModel), typeof(PowerShellModel), typeof(PowerShellConfig), null);
        public PowerShellModel? PowerShellConfigModel { get => (PowerShellModel?)GetValue(PowerShellConfigModelProperty); set => SetValue(PowerShellConfigModelProperty, value); }

        public PowerShellConfig()
        {
            InitializeComponent();
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            PowerShellConfigModel = ImageUploadConfig?.LoadUploadConfig() as PowerShellModel;
            if (PowerShellConfigModel is not null)
            {
                PowerShellConfigModel.PropertyChanged += OnPowerShellConfigModelPropertyChanged;
            }
        }

        private void OnPowerShellConfigModelPropertyChanged(object? sender, global::System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (ImageUploadConfig is not null && PowerShellConfigModel is not null)
            {
                ImageUploadConfig.StoreUploadConfig(PowerShellConfigModel);
            }
        }

        private async void OnImportButtonClick(object sender, RoutedEventArgs e)
        {
            var filePicker = new FileOpenPicker();
            filePicker.FileTypeFilter.Add(".ps1");
            filePicker.SetOwnerWindow(this.GetService<IWindowService>().GetWindow(this));
            var file = await filePicker.PickSingleFileAsync();
            if (file != null && PowerShellConfigModel is not null)
                PowerShellConfigModel.Script = File.ReadAllText(file.Path);
        }

        private async void OnExportButtonClick(object sender, RoutedEventArgs e)
        {
            var filePicker = new FileSavePicker();
            filePicker.SuggestedFileName = ImageUploadConfig?.Name ?? string.Empty;
            filePicker.FileTypeChoices.Add("PowerShell Cmdlet File", new List<string>() { ".ps1" });
            filePicker.SetOwnerWindow(this.GetService<IWindowService>().GetWindow(this));
            var file = await filePicker.PickSaveFileAsync();
            if (file != null && PowerShellConfigModel is not null)
                File.WriteAllText(file.Path, PowerShellConfigModel.Script);
        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
             if (PowerShellConfigModel is not null)
             {
                 PowerShellConfigModel.PropertyChanged -= OnPowerShellConfigModelPropertyChanged;
             }

              Bindings?.StopTracking();
        }
    }
}
