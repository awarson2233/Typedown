namespace Typedown.WinUI.Controls;

public sealed partial class MenuBar : UserControl
{
    public event EventHandler<string>? NavigateRequested;

    public MenuBar()
    {
        InitializeComponent();
    }

    private void OnLoaded(object sender, RoutedEventArgs e) { }

    private void OnUnloaded(object sender, RoutedEventArgs e) { }

    private void OnSizeChanged(object sender, SizeChangedEventArgs e) { }

    private void OnMenuBarPointerEvent(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e) { }

    private void OnSettingsButtonClick(object sender, RoutedEventArgs e)
    {
        NavigateRequested?.Invoke(this, SettingsButton.CommandParameter as string ?? "Settings/General");
    }
}
