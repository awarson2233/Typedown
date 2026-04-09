using Typedown.Core.Utilities;
using Typedown.Core.ViewModels;
using Avalonia.Controls;
using Avalonia.Interactivity;

using PropertyChanged;

namespace Typedown.Core.Pages
{
    [DoNotNotify]
    public sealed partial class MainPage : UserControl
    {
        public AppViewModel AppViewModel => this.GetService<AppViewModel>();

        public MainPage()
        {
            InitializeComponent();
        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
        }
    }
}
