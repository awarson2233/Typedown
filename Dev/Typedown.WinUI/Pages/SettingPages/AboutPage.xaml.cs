using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;

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
    }
}
