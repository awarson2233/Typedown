using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Typedown.WinUI.Utilities;

namespace Typedown.WinUI.Controls;

public sealed partial class LeftPane : UserControl
{
    public static readonly DependencyProperty IsSearchPaneOpenProperty =
        DependencyProperty.Register(nameof(IsSearchPaneOpen), typeof(bool), typeof(LeftPane), new PropertyMetadata(false));

    public bool IsSearchPaneOpen
    {
        get => (bool)GetValue(IsSearchPaneOpenProperty);
        set => SetValue(IsSearchPaneOpenProperty, value);
    }

    public LeftPane()
    {
        using (StartupTrace.Phase("LeftPane.InitializeComponent"))
        {
            InitializeComponent();
        }
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (NavigationView.MenuItems.Count > 0)
        {
            NavigationView.SelectedItem = NavigationView.MenuItems[0];
        }
    }

    private void OnSizeChanged(object sender, SizeChangedEventArgs e)
    {
        FrameClip.Rect = new Windows.Foundation.Rect(0, 0, Frame.ActualWidth, Frame.ActualHeight);
    }

    private void OnSelectionChanged(
        Microsoft.UI.Xaml.Controls.NavigationView sender,
        Microsoft.UI.Xaml.Controls.NavigationViewSelectionChangedEventArgs args)
    {
        if (args.SelectedItem is not Microsoft.UI.Xaml.Controls.NavigationViewItem item ||
            item.Tag is not string pageName)
        {
            return;
        }

        var pageType = Pages.SidePanePages.Route.GetSidePanePageType(pageName);
        if (pageType is null)
        {
            return;
        }

        var transition = args.RecommendedNavigationTransitionInfo ?? new Microsoft.UI.Xaml.Media.Animation.SuppressNavigationTransitionInfo();
        using (StartupTrace.Phase($"LeftPane navigate {pageName}"))
        {
            Frame.Navigate(pageType, DataContext, transition);
        }
    }

    private void OnSearchPaneClose(object? sender, EventArgs e)
    {
        IsSearchPaneOpen = false;
    }

    private void OnSearchButtonClick(object sender, RoutedEventArgs e)
    {
        IsSearchPaneOpen = true;
    }
}
