using PropertyChanged;
﻿using Typedown.Core.ViewModels;
using Avalonia;
using Avalonia.Interactivity;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;

namespace Typedown.Core.Controls.SettingControls.SettingItems
{
[DoNotNotify]
        public sealed partial class EditorSetting : UserControl
    {
        public AppViewModel ViewModel => DataContext as AppViewModel;

        public SettingsViewModel Settings => ViewModel?.SettingsViewModel;

        // DecimalFormatter is WinUI-only (Windows.Globalization.NumberFormatting).
        // In Avalonia, NumericUpDown handles formatting directly via FormatString property.
        // These are kept as simple format string hints for the XAML side.
        public string FontSizeFormat => "F0";
        public string LineHeightFormat => "F1";
        public string IntegerFormat => "F0";

        public EditorSetting()
        {
            InitializeComponent();
        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
        }
    }
}
