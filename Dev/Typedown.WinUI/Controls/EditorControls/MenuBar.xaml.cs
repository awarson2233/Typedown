namespace Typedown.WinUI.Controls;

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Typedown.Presentation.ViewModels;

public sealed partial class MenuBar : UserControl
{
    public event EventHandler<string>? NavigateRequested;

    public MenuBar()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
        FileMenuItem.NavigateRequested += OnFileMenuNavigateRequested;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        ApplyDataContextToMenuItems();
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
    }

    private void OnSizeChanged(object sender, SizeChangedEventArgs e) { }

    private void OnMenuBarPointerEvent(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e) { }

    private void OnDataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args)
    {
        ApplyDataContextToMenuItems();
    }

    private void ApplyDataContextToMenuItems()
    {
        var viewModel = DataContext as AppViewModel;
        FileMenuItem.DataContext = viewModel;
        EditMenuItem.DataContext = viewModel;
        ParagraphMenuItem.DataContext = viewModel;
        FormatMenuItem.DataContext = viewModel;
        ViewMenuItem.DataContext = viewModel;
    }

    private void OnFileMenuNavigateRequested(object? sender, string route)
    {
        NavigateRequested?.Invoke(this, route);
    }

    private void OnSettingsButtonClick(object sender, RoutedEventArgs e)
    {
        NavigateRequested?.Invoke(this, SettingsButton.CommandParameter as string ?? "Settings/General");
    }
}
