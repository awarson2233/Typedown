using Windows.ApplicationModel;
using Typedown.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Typedown.Controls
{
    public sealed partial class AboutApp : UserControl
    {
        public AboutApp()
        {
            InitializeComponent();
        }

        public static string GetAppVersion()
        {
            return Config.GetAppVersion();
        }

        private async void FeedBackButton_Click(object sender, global::Windows.UI.Xaml.RoutedEventArgs e)
        {
            await FeedbackDialog.OpenFeedbackDialog(XamlRoot);
        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            Bindings?.StopTracking();
        }
    }
}
