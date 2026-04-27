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

    public MainContent()
    {
        InitializeComponent();
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        VisualStateManager.GoToState(this, "SidePaneExpand", false);
    }

    private void OnUnloaded(object sender, RoutedEventArgs e) { }

    private void OnSizeChanged(object sender, SizeChangedEventArgs e) { }

    public void SetSidePaneOpen(bool isOpen)
    {
        VisualStateManager.GoToState(this, isOpen ? "SidePaneExpand" : "SidePaneCollapse", true);
    }

    public static double GetColumnWidthNegative(GridLength length) => -length.Value;
}
