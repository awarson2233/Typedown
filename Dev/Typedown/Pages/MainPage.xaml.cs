using Typedown.Core.Utilities;
using Typedown.Presentation.ViewModels;
using Windows.UI.Xaml.Controls;

namespace Typedown.Pages
{
    public sealed partial class MainPage : Page
    {
        public AppViewModel AppViewModel => this.GetService<AppViewModel>();

        public MainPage()
        {
            InitializeComponent();
        }

        private void OnUnloaded(object sender, global::Windows.UI.Xaml.RoutedEventArgs e)
        {
             Bindings?.StopTracking();
        }
    }
}
