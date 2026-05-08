using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Animation;
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
        if (NavigationView.SelectedItem is NavigationViewItem selectedItem)
        {
            NavigateToItem(selectedItem, new Microsoft.UI.Xaml.Media.Animation.SuppressNavigationTransitionInfo());
        }
        else if (NavigationView.MenuItems.Count > 0 &&
                 NavigationView.MenuItems[0] is NavigationViewItem firstItem)
        {
            NavigationView.SelectedItem = firstItem;
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
        if (args.SelectedItem is not Microsoft.UI.Xaml.Controls.NavigationViewItem item)
        {
            return;
        }

        NavigateToItem(item, args.RecommendedNavigationTransitionInfo);
    }

    private void NavigateToItem(NavigationViewItem item, NavigationTransitionInfo? transition = null)
    {
        if (item.Tag is not string pageName)
        {
            return;
        }

        var pageType = Pages.SidePanePages.Route.GetSidePanePageType(pageName);
        if (pageType is null)
        {
            return;
        }

        transition ??= new SuppressNavigationTransitionInfo();
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
