using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using Typedown.Core.Enums;
using Typedown.Core.Models;
using Typedown.Core.Utilities;
using Typedown.Presentation.Services;
using Typedown.WinUI.Controls;
using Typedown.Presentation.Utilities;
using Typedown.Presentation.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Typedown.WinUI.Controls.SettingControls.SettingItems
{
    public sealed partial class ImageUploadSetting : UserControl
    {
        public AppViewModel ViewModel => DataContext as AppViewModel;

        public SettingsViewModel Settings => ViewModel?.SettingsViewModel;

        public ImageUpload ImageUpload => this.GetService<ImageUpload>();

        public ImageUploadSetting()
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
            var methodComboBox = new ComboBox
            {
                Header = Locale.GetString("Type"),
                MinWidth = 240,
                ItemsSource = Typedown.Core.Enums.Enumerable.ImageUploadMethods.Where(method => method != ImageUploadMethod.None),
                SelectedItem = ImageUploadMethod.FTP
            };

            var content = new StackPanel { Spacing = 12 };
            content.Children.Add(nameTextBox);
            content.Children.Add(methodComboBox);

            var dialog = new ContentDialog
            {
                XamlRoot = XamlRoot,
                Title = Locale.GetString("View.Image.UploadConfigs.Title", Locale.ResourceSource.SettingsResources),
                Content = content,
                PrimaryButtonText = Locale.GetDialogString("OK"),
                CloseButtonText = Locale.GetDialogString("Cancel"),
                DefaultButton = ContentDialogButton.Primary
            };

            if (await dialog.ShowAsync() != ContentDialogResult.Primary)
                return;

            var configName = string.IsNullOrWhiteSpace(nameTextBox.Text) ? string.Empty : nameTextBox.Text;
            var method = methodComboBox.SelectedItem is ImageUploadMethod selectedMethod ? selectedMethod : ImageUploadMethod.FTP;
            await ImageUpload.AddImageUploadConfig(configName, method);
        }

        internal static void OnConfigItemClick(object sender, EventArgs e)
        {
            var buttonItem = sender as ButtonSettingItem;
            if (buttonItem?.GetAncestor<ImageUploadSetting>() is not ImageUploadSetting uploadSetting)
                return;
            var config = (sender as ButtonSettingItem).Tag as ImageUploadConfig;
            uploadSetting.ViewModel.NavigateCommand.Execute($"Settings/UploadConfig?{config.Id}");
        }

        private async void OnDeleteClick(object sender, RoutedEventArgs e)
        {
            var item = (sender as MenuFlyoutItem).DataContext as ImageUploadConfig;
            if (item != null)
                await ImageUpload.RemoveImageUploadConfig(item.Id);
        }

        public static string GetConfigItemDescription(ImageUploadMethod method, bool isEnable)
        {
            var list = new List<string>();
            var field = method.GetType().GetField(method.ToString());
            var attribute = field.GetCustomAttribute(typeof(LocaleAttribute)) as LocaleAttribute;
            list.Add(attribute.Text);
            list.Add(isEnable ? Locale.GetString("On") : Locale.GetString("Off"));
            return string.Join(", ", list);
        }

        public Visibility ConfigItemsTitleVisibility(ObservableCollection<ImageUploadConfig> configs)
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
