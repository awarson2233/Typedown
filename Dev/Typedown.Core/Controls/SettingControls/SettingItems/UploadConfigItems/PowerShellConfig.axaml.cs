using PropertyChanged;
﻿using System;
using System.Collections.ObjectModel;
using Avalonia.Input;
using Avalonia.Metadata;
using Avalonia.Data.Converters;
using System.Collections.Generic;
using System.IO;
using Typedown.Core.Interfaces;
using Typedown.Core.Models;
using Typedown.Core.Models.UploadConfigModels;
using Typedown.Core.Utilities;
using Avalonia;
using Avalonia.Interactivity;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;

namespace Typedown.Core.Controls.SettingControls.SettingItems.UploadConfigItems
{
[DoNotNotify]
        public sealed partial class PowerShellConfig : UserControl
    {
        public static StyledProperty<ImageUploadConfig> ImageUploadConfigProperty { get; } = AvaloniaProperty.Register<PowerShellConfig, ImageUploadConfig>(nameof(ImageUploadConfig), null);
        public ImageUploadConfig ImageUploadConfig { get => (ImageUploadConfig)GetValue(ImageUploadConfigProperty); set => SetValue(ImageUploadConfigProperty, value); }

        public static StyledProperty<PowerShellModel> PowerShellConfigModelProperty { get; } = AvaloniaProperty.Register<PowerShellConfig, PowerShellModel>(nameof(PowerShellConfigModel), null);
        public PowerShellModel PowerShellConfigModel { get => (PowerShellModel)GetValue(PowerShellConfigModelProperty); set => SetValue(PowerShellConfigModelProperty, value); }

        public PowerShellConfig()
        {
            InitializeComponent();
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            PowerShellConfigModel = ImageUploadConfig.LoadUploadConfig() as PowerShellModel;
            PowerShellConfigModel.PropertyChanged += OnPowerShellConfigModelPropertyChanged;
        }

        private void OnPowerShellConfigModelPropertyChanged(object sender, global::System.ComponentModel.PropertyChangedEventArgs e)
        {
            ImageUploadConfig.StoreUploadConfig(PowerShellConfigModel);
        }

        private async void OnImportButtonClick(object sender, RoutedEventArgs e)
        {
            // TODO: Re-implement file picker with Avalonia StorageProvider API
            // FileOpenPicker doesn't exist in Avalonia
        }

        private async void OnExportButtonClick(object sender, RoutedEventArgs e)
        {
            // TODO: Re-implement file picker with Avalonia StorageProvider API
            // FileSavePicker doesn't exist in Avalonia
        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
        }
    }
}
