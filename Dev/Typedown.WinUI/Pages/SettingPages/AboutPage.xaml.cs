using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using System;
using Typedown.WinUI.Controls;
using Windows.System;

namespace Typedown.WinUI.Pages.SettingPages
{
    public sealed partial class AboutPage : Page
    {
        public string VersionText { get; } = typeof(AboutPage).Assembly.GetName().Version?.ToString() ?? "";

        public AboutPage()
        {
            NavigationCacheMode = NavigationCacheMode.Enabled;
            this.InitializeComponent();
        }

        private async void OnOpenSourceSoftwareClick(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
        {
            await Launcher.LaunchUriAsync(new Uri("https://typedown.ownbox.cn/credits"));
        }

        private async void OnMicrosoftStorePageClick(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
        {
            await Launcher.LaunchUriAsync(new Uri("https://www.microsoft.com/store/apps/9P8TCW4H2HB4"));
        }

        private async void OnFeedbackClick(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
        {
            await FeedbackDialog.OpenFeedbackDialog(XamlRoot);
        }
    }
}
