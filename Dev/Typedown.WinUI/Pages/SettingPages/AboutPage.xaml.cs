using Microsoft.UI.Xaml.Controls;

namespace Typedown.WinUI.Pages.SettingPages
{
    public sealed partial class AboutPage : Page
    {
        public string VersionText { get; } = typeof(AboutPage).Assembly.GetName().Version?.ToString() ?? "";

        public AboutPage()
        {
            this.InitializeComponent();
        }
    }
}
