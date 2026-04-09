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
        public sealed partial class SCPConfig : UserControl
    {
        public static StyledProperty<ImageUploadConfig> ImageUploadConfigProperty { get; } = AvaloniaProperty.Register<SCPConfig, ImageUploadConfig>(nameof(ImageUploadConfig), null);
        public ImageUploadConfig ImageUploadConfig { get => (ImageUploadConfig)GetValue(ImageUploadConfigProperty); set => SetValue(ImageUploadConfigProperty, value); }

        public static StyledProperty<SCPConfigModel> SCPConfigModelProperty { get; } = AvaloniaProperty.Register<SCPConfig, SCPConfigModel>(nameof(SCPConfigModel), null);
        public SCPConfigModel SCPConfigModel { get => (SCPConfigModel)GetValue(SCPConfigModelProperty); set => SetValue(SCPConfigModelProperty, value); }


        public SCPConfig()
        {
            InitializeComponent();
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            SCPConfigModel = ImageUploadConfig.LoadUploadConfig() as SCPConfigModel;
        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            ImageUploadConfig.StoreUploadConfig(SCPConfigModel);
        }
    }
}
