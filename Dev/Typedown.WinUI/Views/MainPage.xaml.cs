using Microsoft.Extensions.DependencyInjection;
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
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            var services = ResolvePlatformServices(e.Parameter);
            ViewModel = ResolveViewModel(e.Parameter);
            ViewModel.ApplyPlatformServiceSummary(
                Phase10ContractsProbe.Describe(services),
                services.ServiceNames);
            Bindings.Update();
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
    }
}
