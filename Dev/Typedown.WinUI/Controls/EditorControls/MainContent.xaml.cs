using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Typedown.WinUI.Controls;

public sealed partial class MainContent : UserControl
{
    public static readonly DependencyProperty IsLeftPaneLoadProperty =
        DependencyProperty.Register(nameof(IsLeftPaneLoad), typeof(bool), typeof(MainContent), new PropertyMetadata(true));

    public bool IsLeftPaneLoad
    {
        get => (bool)GetValue(IsLeftPaneLoadProperty);
        set => SetValue(IsLeftPaneLoadProperty, value);
    }

    public bool IsSidePaneOpen => IsLeftPaneLoad;

    private bool animationEnabled = true;

    public MainContent()
    {
        InitializeComponent();
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        VisualStateManager.GoToState(this, IsLeftPaneLoad ? "SidePaneExpand" : "SidePaneCollapse", false);
    }

    private void OnUnloaded(object sender, RoutedEventArgs e) { }

    private void OnSizeChanged(object sender, SizeChangedEventArgs e) { }

    public void SetSidePaneOpen(bool isOpen)
    {
        IsLeftPaneLoad = isOpen;
        if (IsLoaded)
        {
            VisualStateManager.GoToState(this, isOpen ? "SidePaneExpand" : "SidePaneCollapse", animationEnabled);
        }
    }

    public static double GetColumnWidthNegative(GridLength length) => -length.Value;

    public void SetAnimationEnabled(bool isEnabled)
    {
        animationEnabled = isEnabled;
    }
}
