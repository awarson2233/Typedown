using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using Typedown.WinUI.Pages.SettingPages;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Navigation;

namespace Typedown.WinUI.Pages
{
    public sealed partial class SettingsPage : Page
    {
        public ObservableCollection<SettingsBreadcrumbBarItem> BreadcrumbBarItems { get; } = new();

        public SettingsPage()
        {
            InitializeComponent();
            ContentFrame.Navigated += OnNavigated;
        }

        private void OnNavigationViewSelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
        {
            if (sender.SelectedItem is not NavigationViewItem item)
            {
                return;
            }

            var pageName = item.Tag as string;
            var pageType = Route.GetSettingsPageType(pageName);
            if (pageType != null && pageType != ContentFrame.SourcePageType)
            {
                BreadcrumbBarItems.Clear();
                ContentFrame.Navigate(pageType, null, args.RecommendedNavigationTransitionInfo);
                ContentFrame.BackStack.Clear();
            }
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            var pageType = Route.GetSettingsPageType(e.Parameter as string) ?? typeof(GeneralPage);
            if (ContentFrame.SourcePageType != pageType)
            {
                ContentFrame.Navigate(pageType, null, new SuppressNavigationTransitionInfo());
            }
        }

        private void OnSizeChanged(object sender, SizeChangedEventArgs e)
        {
            var state = ActualWidth >= 1008 ? "Large" : ActualWidth >= 640 ? "Medium" : "Small";
            VisualStateManager.GoToState(this, state, false);
        }

        private void OnNavigated(object sender, NavigationEventArgs e)
        {
            var item = NavigationView.MenuItems.OfType<NavigationViewItem>().Where(x => Route.GetSettingsPageType(x.Tag as string) == ContentFrame.SourcePageType).FirstOrDefault();
            if (item != null) NavigationView.SelectedItem = item;
            switch (e.NavigationMode)
            {
                case NavigationMode.Forward:
                case NavigationMode.New:
            if (e.Content is Page page)
            {
                BreadcrumbBarItems.Add(new(page));
            }
                    break;
                case NavigationMode.Back:
                    BreadcrumbBarItems.RemoveAt(BreadcrumbBarItems.Count - 1);
                    break;
            }
        }

        private void Navigate(string args)
        {
            var path = args?.Split('?')[0].TrimStart('/').Split('/');
            if (path != null && path.Length > 1)
            {
                var type = Route.GetSettingsPageType(path[1]);
            var query = args?.Contains("?") == true ? args.Substring(args.IndexOf("?") + 1) : "";
                if (type != null && type != ContentFrame.SourcePageType)
                {
                    ContentFrame.Navigate(type, query, GetTransition());
                }
            }
        }

        public NavigationTransitionInfo GetTransition() => new SlideNavigationTransitionInfo()
        {
            Effect = SlideNavigationTransitionEffect.FromRight
        };

        private void OnLoaded(object sender, RoutedEventArgs e) { }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
             Bindings?.StopTracking();
        }

        private void OnBreadcrumbBarItemClicked(BreadcrumbBar sender, BreadcrumbBarItemClickedEventArgs args)
        {
            var count = BreadcrumbBarItems.Count - args.Index - 1;
            for (int i = 0; i < count; i++)
                ContentFrame.GoBack();
        }

        public void SetPageTitle(Page page, string title)
        {
            var item = BreadcrumbBarItems.Where(x => x.Page == page).FirstOrDefault();
            if (item != null)
                item.Title = title;
        }
    }

    public partial class SettingsBreadcrumbBarItem
    {
        public Page Page { get; }

        public Type PageType { get; }

        public string Title { get; set; }

        public SettingsBreadcrumbBarItem(Page page, string? title = null)
        {
            Page = page;
            PageType = Page.GetType();
            Title = title ?? Route.GetSettingsPageTitle(PageType);
        }
    }
}
