using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using Typedown.Core.Enums;
using Typedown.Core.Models;
using Typedown.Core.Utilities;
using Typedown.Presentation.Services;
using Typedown.Presentation.Utilities;
using Typedown.Presentation.ViewModels;
using Typedown.WinUI.Controls;

namespace Typedown.WinUI.Pages.SettingPages
{
    [Locale("ImageUpload.Title")]
    public sealed partial class ImageUploadPage : Page
    {
        public AppViewModel? ViewModel { get; private set; }

        public SettingsViewModel? SettingsViewModel { get; private set; }

        public SettingsViewModel Settings => ViewModel?.SettingsViewModel;

        public ImageUpload ImageUpload => this.GetService<ImageUpload>();

        public ImageUploadPage()
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

        internal static void OnConfigItemClick(object sender, RoutedEventArgs e)
        {
            var itemElement = sender as FrameworkElement;
            if (itemElement?.GetAncestor<ImageUploadPage>() is not ImageUploadPage uploadPage)
                return;

            var config = itemElement.Tag as ImageUploadConfig;
            uploadPage.ViewModel?.NavigateCommand.Execute($"Settings/UploadConfig?{config.Id}");
        }

        private async void OnDeleteClick(object sender, RoutedEventArgs e)
        {
            var item = (sender as MenuFlyoutItem)?.DataContext as ImageUploadConfig;
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
            ConfigItemMenuFlyout.Items.Clear();
        }
    }
}
