namespace Typedown.WinUI.Controls;

public sealed partial class RootControl : UserControl
{
    public object? MainPageNavigationParameter { get; set; }

    public UIElement TitleBarElement => TitleDragRegion;

    public RootControl()
    {
        InitializeComponent();
        Frame.Navigated += OnFrameNavigated;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (Frame.Content is null)
        {
            Frame.Navigate(typeof(Views.MainPage), MainPageNavigationParameter);
        }

        UpdateBackButtonState(false);
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        Frame.Navigated -= OnFrameNavigated;
    }

    private void OnFrameNavigated(object sender, Microsoft.UI.Xaml.Navigation.NavigationEventArgs e)
    {
        UpdateBackButtonState(true);
    }

    private void OnBackButtonClick(object sender, RoutedEventArgs e)
    {
        if (Frame.CanGoBack)
        {
            Frame.GoBack();
        }
    }

    private void UpdateBackButtonState(bool useTransitions)
    {
        var isSettingsPage = Frame.SourcePageType == typeof(Pages.SettingsPage);
        BackButton.Visibility = isSettingsPage ? Visibility.Visible : Visibility.Collapsed;
        TitlePanel.Margin = isSettingsPage ? new Thickness(0) : new Thickness(4, 0, 0, 0);
    }
}
