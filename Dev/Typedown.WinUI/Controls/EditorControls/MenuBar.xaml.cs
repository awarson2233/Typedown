namespace Typedown.WinUI.Controls;

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Typedown.Presentation.ViewModels;
using Typedown.WinUI.Utilities;

public sealed partial class MenuBar : UserControl
{
    public event EventHandler<string>? NavigateRequested;

    public MenuBar()
    {
        using (StartupTrace.Phase("MenuBar.InitializeComponent"))
        {
            InitializeComponent();
        }
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

    private void OnDataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args)
    {
        ApplyDataContextToMenuItems();
    }

    private void ApplyDataContextToMenuItems()
    {
        var viewModel = DataContext as AppViewModel;
        SetDataContextIfChanged(FileMenuItem, viewModel);
        SetDataContextIfChanged(EditMenuItem, viewModel);
        SetDataContextIfChanged(ParagraphMenuItem, viewModel);
        SetDataContextIfChanged(FormatMenuItem, viewModel);
        SetDataContextIfChanged(ViewMenuItem, viewModel);
    }

    private static void SetDataContextIfChanged(FrameworkElement element, AppViewModel? viewModel)
    {
        if (!ReferenceEquals(element.DataContext, viewModel))
        {
            element.DataContext = viewModel;
        }
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
