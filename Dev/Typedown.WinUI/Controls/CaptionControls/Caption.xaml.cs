namespace Typedown.WinUI.Controls;

public sealed partial class Caption : UserControl
{
    public Caption()
    {
        InitializeComponent();
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        VisualStateManager.GoToState(this, "BackCollapsed", false);
    }

    private void OnUnloaded(object sender, RoutedEventArgs e) { }
}
