using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Navigation;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using Typedown.Core;
using Typedown.Core.Utilities;
using Typedown.Presentation.ViewModels;
using Typedown.WinUI.Utilities;

namespace Typedown.WinUI.Views
{
    public partial class MainPage : Page
    {
        public AppViewModel? ViewModel { get; private set; }

        private readonly CompositeDisposable viewModelBindings = new();
        private TextBlock? compactTitleTextBlock;
        private FrameworkElement? compactTitleGrid;
        private FrameworkElement? compactSettingsButton;
        private FrameworkElement? compactLeftDragBar;
        private FrameworkElement? compactRightDragBar;
        private bool animationEnabled = true;

        public MainPage()
        {
            using (StartupTrace.Phase("MainPage.InitializeComponent"))
            {
                this.InitializeComponent();
            }
            NavigationCacheMode = NavigationCacheMode.Required;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            MenuBar.NavigateRequested -= OnMenuBarNavigateRequested;
            MenuBar.NavigateRequested += OnMenuBarNavigateRequested;

            StatusBar.SidePaneOpenChanged -= OnStatusBarSidePaneOpenChanged;
            StatusBar.SidePaneOpenChanged += OnStatusBarSidePaneOpenChanged;

            ApplyCurrentShellState();
        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            // Do not unsubscribe events here, because the page stays in Cache Mode
            // MenuBar.NavigateRequested -= OnMenuBarNavigateRequested;
            // StatusBar.SidePaneOpenChanged -= OnStatusBarSidePaneOpenChanged;
        }

        private void OnStatusBarSidePaneOpenChanged(object? sender, bool isOpen)
        {
            if (ViewModel?.SettingsViewModel is { } settings && settings.SidePaneOpen != isOpen)
            {
                settings.SidePaneOpen = isOpen;
            }
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
            NavigationTransitionInfo transition = animationEnabled
                ? new SlideNavigationTransitionInfo { Effect = SlideNavigationTransitionEffect.FromRight }
                : new SuppressNavigationTransitionInfo();
            frame.Navigate(typeof(Pages.SettingsPage), new Pages.SettingsNavigationParameter(viewModel, viewModel.SettingsViewModel, pageName), transition);
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            using var phase = StartupTrace.Phase("MainPage.OnNavigatedTo");
            AttachViewModel(ResolveViewModel(e.Parameter));
            DataContext = ViewModel;
            ApplyCurrentShellState();
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

        private void AttachViewModel(AppViewModel nextViewModel)
        {
            if (ReferenceEquals(ViewModel, nextViewModel))
            {
                return;
            }

            viewModelBindings.Clear();
            ViewModel = nextViewModel;

            var settings = nextViewModel.SettingsViewModel;
            viewModelBindings.Add(settings.WhenPropertyChanged(nameof(SettingsViewModel.SidePaneOpen))
                .Cast<bool>()
                .StartWith(settings.SidePaneOpen)
                .Subscribe(ApplySidePaneOpen));

            viewModelBindings.Add(settings.WhenPropertyChanged(nameof(SettingsViewModel.StatusBarOpen))
                .Cast<bool>()
                .StartWith(settings.StatusBarOpen)
                .Subscribe(ApplyStatusBarVisibility));

            viewModelBindings.Add(settings.WhenPropertyChanged(nameof(SettingsViewModel.AnimationEnable))
                .Cast<bool>()
                .StartWith(settings.AnimationEnable)
                .Subscribe(ApplyAnimationEnabled));

            viewModelBindings.Add(settings.WhenPropertyChanged(nameof(SettingsViewModel.AppCompactMode))
                .Cast<bool>()
                .StartWith(settings.AppCompactMode)
                .Subscribe(ApplyCompactMode));

            viewModelBindings.Add(nextViewModel.UIViewModel.WhenPropertyChanged(nameof(UIViewModel.MainWindowTitle))
                .Cast<string>()
                .StartWith(nextViewModel.UIViewModel.MainWindowTitle)
                .Subscribe(UpdateCompactTitle));
        }

        private void ApplyCurrentShellState()
        {
            if (ViewModel is null)
            {
                return;
            }

            var settings = ViewModel.SettingsViewModel;
            ApplyAnimationEnabled(settings.AnimationEnable);
            ApplySidePaneOpen(settings.SidePaneOpen);
            ApplyStatusBarVisibility(settings.StatusBarOpen);
            ApplyCompactMode(settings.AppCompactMode);
            UpdateCompactTitle(ViewModel.UIViewModel.MainWindowTitle);
        }

        private void ApplyAnimationEnabled(bool isEnabled)
        {
            animationEnabled = isEnabled;
            MainContent.SetAnimationEnabled(isEnabled);
        }

        private void ApplySidePaneOpen(bool isOpen)
        {
            MainContent.SetSidePaneOpen(isOpen);
            StatusBar.SetSidePaneOpen(isOpen);
        }

        private void ApplyStatusBarVisibility(bool isVisible)
        {
            StatusBar.Visibility = isVisible ? Visibility.Visible : Visibility.Collapsed;
            StatusBar.IsHitTestVisible = isVisible;
        }

        private void ApplyCompactMode(bool isCompact)
        {
            EnsureMenuBarShellElements();

            if (compactTitleGrid is not null)
            {
                compactTitleGrid.Visibility = isCompact ? Visibility.Visible : Visibility.Collapsed;
            }

            if (compactSettingsButton is not null)
            {
                compactSettingsButton.Visibility = isCompact ? Visibility.Collapsed : Visibility.Visible;
            }

            if (compactLeftDragBar is not null)
            {
                compactLeftDragBar.Visibility = isCompact ? Visibility.Visible : Visibility.Collapsed;
            }

            if (compactRightDragBar is not null)
            {
                compactRightDragBar.Visibility = isCompact ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        private void UpdateCompactTitle(string? title)
        {
            EnsureMenuBarShellElements();

            if (compactTitleTextBlock is not null)
            {
                compactTitleTextBlock.Text = string.IsNullOrWhiteSpace(title) ? Config.AppName : title;
            }
        }

        private void EnsureMenuBarShellElements()
        {
            compactTitleTextBlock ??= MenuBar.FindName("TitleTextBlock") as TextBlock;
            compactTitleGrid ??= MenuBar.FindName("TitleGrid") as FrameworkElement;
            compactSettingsButton ??= MenuBar.FindName("SettingsButton") as FrameworkElement;
            compactLeftDragBar ??= MenuBar.FindName("LeftDragBar") as FrameworkElement;
            compactRightDragBar ??= MenuBar.FindName("RightDragBar") as FrameworkElement;
        }
    }
}
