using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Typedown.WinUI.Controls;

public sealed partial class SearchPane : UserControl
{
    public event EventHandler? Close;

    public SearchPane()
    {
        InitializeComponent();
    }

    private void OnLoaded(object sender, RoutedEventArgs e) { }

    private void OnUnloaded(object sender, RoutedEventArgs e) { }

    private void OnSearchTextBoxLostFocus(object sender, RoutedEventArgs e) { }

    private void OnCloseButtonClick(object sender, RoutedEventArgs e)
    {
        Close?.Invoke(this, EventArgs.Empty);
    }
}
