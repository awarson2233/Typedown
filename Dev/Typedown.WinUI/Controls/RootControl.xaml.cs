namespace Typedown.WinUI.Controls;

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Typedown.Presentation.Interfaces;
using Typedown.WinUI.Services;

public sealed partial class RootControl : UserControl
{
    public object? MainPageNavigationParameter { get; set; }

    public UIElement TitleBarElement => TitleDragRegion;

    private bool animationEnabled = true;

    public RootControl()
    {
        InitializeComponent();
        Frame.Navigated += OnFrameNavigated;
    }

    public void AttachKeyboardAccelerator(IKeyboardAccelerator keyboardAccelerator)
    {
        if (keyboardAccelerator is WinUIKeyboardAccelerator winUIKeyboardAccelerator)
        {
            winUIKeyboardAccelerator.Attach(this);
        }
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
        VisualStateManager.GoToState(this, isSettingsPage ? "BackVisible" : "BackCollapsed", useTransitions && animationEnabled);
    }

    public void SetAnimationEnabled(bool isEnabled)
    {
        animationEnabled = isEnabled;
        UpdateBackButtonState(false);
    }
}
