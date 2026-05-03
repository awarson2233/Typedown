using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Navigation;
using Typedown.Presentation.ViewModels;

namespace Typedown.WinUI.Views
{
    public partial class MainPage : Page
    {
        public AppViewModel? ViewModel { get; private set; }

        public MainPage()
        {
            this.InitializeComponent();
            MenuBar.NavigateRequested += OnMenuBarNavigateRequested;
            StatusBar.SidePaneOpenChanged += OnStatusBarSidePaneOpenChanged;
            StatusBar.SetSidePaneOpen(MainContent.IsSidePaneOpen);
        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            MenuBar.NavigateRequested -= OnMenuBarNavigateRequested;
            StatusBar.SidePaneOpenChanged -= OnStatusBarSidePaneOpenChanged;
        }

        private void OnStatusBarSidePaneOpenChanged(object? sender, bool isOpen)
        {
            MainContent.SetSidePaneOpen(isOpen);
        }

        private void OnMenuBarNavigateRequested(object? sender, string route)
        {
            if (FindParentFrame(this) is not { } frame)
            {
                return;
            }

            var parts = route.Split('/', StringSplitOptions.RemoveEmptyEntries);
            var pageName = parts.Length > 1 ? parts[1] : "General";
            var viewModel = ViewModel ?? throw new InvalidOperationException("Typedown presentation view model is not initialized.");
            frame.Navigate(typeof(Pages.SettingsPage), new Pages.SettingsNavigationParameter(viewModel, viewModel.SettingsViewModel, pageName), new SlideNavigationTransitionInfo
            {
                Effect = SlideNavigationTransitionEffect.FromRight
            });
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            ViewModel = ResolveViewModel(e.Parameter);
            DataContext = ViewModel;
        }

        private static AppViewModel ResolveViewModel(object? parameter)
        {
            var providerProperty = parameter?.GetType().GetProperty("UiServices");
            var provider = providerProperty?.GetValue(parameter) as IServiceProvider;
            return provider?.GetRequiredService<AppViewModel>()
                ?? throw new InvalidOperationException("Typedown presentation services are not initialized.");
        }

        private static Frame? FindParentFrame(DependencyObject element)
        {
            for (var current = VisualTreeHelper.GetParent(element); current != null; current = VisualTreeHelper.GetParent(current))
            {
                if (current is Frame frame)
                {
                    return frame;
                }
            }

            return null;
        }
    }
}
