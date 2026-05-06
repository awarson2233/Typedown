using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using Typedown.Presentation.ViewModels;
using Windows.Globalization.NumberFormatting;

namespace Typedown.WinUI.Pages.SettingPages
{
    public sealed partial class EditorPage : Page
    {
        public AppViewModel? ViewModel { get; private set; }

        public SettingsViewModel? SettingsViewModel { get; private set; }

        public SettingsViewModel Settings => ViewModel?.SettingsViewModel;

        public DecimalFormatter FontSizeFormatter { get; } = new()
        {
            FractionDigits = 0,
            NumberRounder = new IncrementNumberRounder { Increment = 0.1, RoundingAlgorithm = RoundingAlgorithm.RoundHalfUp }
        };

        public DecimalFormatter LineHeightFormatter { get; } = new()
        {
            FractionDigits = 1,
            NumberRounder = new IncrementNumberRounder { Increment = 0.01, RoundingAlgorithm = RoundingAlgorithm.RoundHalfUp }
        };

        public DecimalFormatter IntegerFormatter { get; } = new()
        {
            FractionDigits = 0,
            NumberRounder = new IncrementNumberRounder { Increment = 1, RoundingAlgorithm = RoundingAlgorithm.RoundHalfUp }
        };

        public EditorPage()
        {
            NavigationCacheMode = NavigationCacheMode.Enabled;
            InitializeComponent();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            if (e.Parameter is SettingsNavigationParameter parameter)
            {
                ViewModel = parameter.AppViewModel;
                SettingsViewModel = parameter.SettingsViewModel;
                DataContext = ViewModel;
                Bindings.Update();
            }
        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
        }
    }
}
