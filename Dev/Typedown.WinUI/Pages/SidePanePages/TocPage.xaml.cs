using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using Typedown.Presentation.ViewModels;

namespace Typedown.WinUI.Pages.SidePanePages
{
    public sealed partial class TocPage : Page
    {
        public AppViewModel ViewModel => (DataContext as AppViewModel)!;

        public EditorViewModel Editor => ViewModel.EditorViewModel;

        public TocPage()
        {
            InitializeComponent();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            if (e.Parameter is AppViewModel viewModel)
            {
                DataContext = viewModel;
                Bindings.Update();
            }
        }
    }
}
