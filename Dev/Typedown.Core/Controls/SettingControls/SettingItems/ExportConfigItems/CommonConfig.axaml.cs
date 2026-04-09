using PropertyChanged;
﻿using Typedown.Core.Models;
using Typedown.Core.Models.UploadConfigModels;
using Typedown.Core.Pages.SettingPages;
using Typedown.Core.Utilities;
using Avalonia;
using Avalonia.Interactivity;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Markup.Xaml;

namespace Typedown.Core.Controls.SettingControls.SettingItems.ExportConfigItems
{
    [DoNotNotify]
        public sealed partial class CommonConfig : UserControl
    {
        public static StyledProperty<ExportConfig> ExportConfigProperty { get; } = AvaloniaProperty.Register<CommonConfig, ExportConfig>(nameof(ExportConfig), null);
        public ExportConfig ExportConfig { get => (ExportConfig)GetValue(ExportConfigProperty); set => SetValue(ExportConfigProperty, value); }

        public static StyledProperty<Control> DetailProperty { get; } = AvaloniaProperty.Register<CommonConfig, Control>(nameof(Detail), null);
        public Control Detail { get => (Control)GetValue(DetailProperty); set => SetValue(DetailProperty, value); }

        public CommonConfig()
        {
            this.InitializeComponent();
        }

        private void OnDeleteButtonClick(object sender, RoutedEventArgs e)
        {
            this.GetAncestor<ExportConfigPage>()?.DeleteConfigAsync();
        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
        }
    }
}
