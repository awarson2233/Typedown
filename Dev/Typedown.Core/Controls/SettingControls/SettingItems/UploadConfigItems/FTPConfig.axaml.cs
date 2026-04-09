using PropertyChanged;
﻿using Typedown.Core.Models;
using Typedown.Core.Models.UploadConfigModels;
using Avalonia;
using Avalonia.Interactivity;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;

namespace Typedown.Core.Controls.SettingControls.SettingItems.UploadConfigItems
{
[DoNotNotify]
        public sealed partial class FTPConfig : UserControl
    {
        public static StyledProperty<ImageUploadConfig> ImageUploadConfigProperty { get; } = AvaloniaProperty.Register<FTPConfig, ImageUploadConfig>(nameof(ImageUploadConfig), null);
        public ImageUploadConfig ImageUploadConfig { get => (ImageUploadConfig)GetValue(ImageUploadConfigProperty); set => SetValue(ImageUploadConfigProperty, value); }

        public static StyledProperty<FTPConfigModel> FTPConfigModelProperty { get; } = AvaloniaProperty.Register<FTPConfig, FTPConfigModel>(nameof(FTPConfigModel), null);
        public FTPConfigModel FTPConfigModel { get => (FTPConfigModel)GetValue(FTPConfigModelProperty); set => SetValue(FTPConfigModelProperty, value); }

        public FTPConfig()
        {
            InitializeComponent();
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            FTPConfigModel = ImageUploadConfig.LoadUploadConfig() as FTPConfigModel;
        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            ImageUploadConfig.StoreUploadConfig(FTPConfigModel);
        }
    }
}
