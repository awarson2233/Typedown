using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using Typedown.Core.Enums;
using Typedown.Core.Interfaces;
using Typedown.Presentation.Interfaces;
using Typedown.Core.Models;
using Typedown.Core.Utilities;
using Typedown.WinUI.Controls;
using Typedown.Presentation.Utilities;
using Typedown.Presentation.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Typedown.WinUI.Controls.SettingControls.SettingItems
{
    public sealed partial class ExportSetting : UserControl
    {
        public AppViewModel ViewModel => DataContext as AppViewModel;

        public SettingsViewModel Settings => ViewModel?.SettingsViewModel;

        public IFileExport FileExport => this.GetService<IFileExport>();

        public ExportSetting()
        {
            InitializeComponent();
        }

        private void OnAddButtonClick(object sender, RoutedEventArgs e)
        {
            AddConfigItem();
        }

        private async void AddConfigItem()
        {
            var nameTextBox = new TextBox
            {
                Header = Locale.GetDialogString("Name"),
                Text = Locale.GetString("New")
            };
            var typeComboBox = new ComboBox
            {
                Header = Locale.GetString("Type"),
                MinWidth = 240,
                ItemsSource = Typedown.Core.Enums.Enumerable.ExportTypes,
                SelectedItem = ExportType.PDF
            };

            var content = new StackPanel { Spacing = 12 };
            content.Children.Add(nameTextBox);
            content.Children.Add(typeComboBox);

            var dialog = new ContentDialog
            {
                XamlRoot = XamlRoot,
                Title = Locale.GetString("Export.AddConfig.Title", Locale.ResourceSource.SettingsResources),
                Content = content,
                PrimaryButtonText = Locale.GetDialogString("OK"),
                CloseButtonText = Locale.GetDialogString("Cancel"),
                DefaultButton = ContentDialogButton.Primary
            };

            if (await dialog.ShowAsync() != ContentDialogResult.Primary)
                return;

            var configName = string.IsNullOrWhiteSpace(nameTextBox.Text) ? string.Empty : nameTextBox.Text;
            var exportType = typeComboBox.SelectedItem is ExportType selectedType ? selectedType : ExportType.PDF;
            await FileExport.AddExportConfig(configName, exportType);
        }

        internal static void OnConfigItemClick(object sender, EventArgs e)
        {
            var buttonItem = sender as ButtonSettingItem;
            if (buttonItem?.GetAncestor<ExportSetting>() is not ExportSetting exportSetting)
                return;
            var config = buttonItem.DataContext as ExportConfig;
            exportSetting.ViewModel.NavigateCommand.Execute($"Settings/ExportConfig?{config.Id}");
        }

        private async void OnDeleteClick(object sender, RoutedEventArgs e)
        {
            var item = (sender as MenuFlyoutItem).DataContext as ExportConfig;
            if (item != null)
                await FileExport.RemoveExportConfig(item.Id);
        }

        public static string GetConfigItemDescription(ExportType method)
        {
            var list = new List<string>();
            var field = method.GetType().GetField(method.ToString());
            var attribute = field.GetCustomAttribute(typeof(LocaleAttribute)) as LocaleAttribute;
            list.Add(attribute.Text);
            return string.Join(", ", list);
        }

        public Visibility ConfigItemsTitleVisibility(ObservableCollection<ExportConfig> configs)
        {
            return configs.Any() ? Visibility.Visible : Visibility.Collapsed;
        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            Bindings?.StopTracking();
            ConfigItemMenuFlyout.Items.Clear();
        }
    }
}
