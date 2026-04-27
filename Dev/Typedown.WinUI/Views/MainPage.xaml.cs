using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Navigation;
using Typedown.UI.ViewModels;
using Typedown.WinUI.Services;

namespace Typedown.WinUI.Views
{
    public partial class MainPage : Page
    {
        public MainPageViewModel ViewModel { get; private set; }

        public MainPage()
        {
            this.InitializeComponent();
            ViewModel = new MainPageViewModel();
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
            frame.Navigate(typeof(Pages.SettingsPage), pageName, new SlideNavigationTransitionInfo
            {
                Effect = SlideNavigationTransitionEffect.FromRight
            });
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            var services = ResolvePlatformServices(e.Parameter);
            ViewModel = ResolveViewModel(e.Parameter);
            ViewModel.ApplyPlatformServiceSummary(
                WinUIContractsProbe.Describe(services),
                services.ServiceNames);
        }

        private static WinUIPlatformServices ResolvePlatformServices(object? parameter)
        {
            var platformProperty = parameter?.GetType().GetProperty("PlatformServices");
            return platformProperty?.GetValue(parameter) as WinUIPlatformServices
                ?? parameter as WinUIPlatformServices
                ?? ((App)Application.Current).PlatformServices;
        }

        private static MainPageViewModel ResolveViewModel(object? parameter)
        {
            var providerProperty = parameter?.GetType().GetProperty("UiServices");
            var provider = providerProperty?.GetValue(parameter) as IServiceProvider;
            return provider?.GetService<MainPageViewModel>() ?? new MainPageViewModel();
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
