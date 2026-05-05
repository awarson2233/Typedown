using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using Typedown.WinUI.Pages.SettingPages;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Navigation;
using Typedown.Presentation.ViewModels;

namespace Typedown.WinUI.Pages
{
    public sealed class SettingsNavigationParameter
    {
        public SettingsNavigationParameter(AppViewModel appViewModel, SettingsViewModel settingsViewModel, string? pageName = null, string? query = null)
        {
            AppViewModel = appViewModel;
            SettingsViewModel = settingsViewModel;
            PageName = pageName;
            Query = query;
        }

        public AppViewModel AppViewModel { get; }

        public SettingsViewModel SettingsViewModel { get; }

        public string? PageName { get; }

        public string? Query { get; }

        public SettingsNavigationParameter WithPage(string? pageName, string? query = null) => new(AppViewModel, SettingsViewModel, pageName, query);
    }

    public sealed partial class SettingsPage : Page
    {
        public ObservableCollection<SettingsBreadcrumbBarItem> BreadcrumbBarItems { get; } = new();

        public AppViewModel? ViewModel { get; private set; }

        public SettingsViewModel? SettingsViewModel { get; private set; }

        private SettingsNavigationParameter? navigationParameter;

        private bool isUpdatingNavigationSelection;

        public SettingsPage()
        {
            InitializeComponent();
            ApplyNavigationLabels();
            ContentFrame.Navigated += OnNavigated;
        }

        private void OnNavigationViewSelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
        {
            if (isUpdatingNavigationSelection)
            {
                return;
            }

            if (sender.SelectedItem is not NavigationViewItem item)
            {
                return;
            }

            var pageName = item.Tag as string;
            var pageType = Route.GetSettingsPageType(pageName);
            if (pageType != null && pageType != ContentFrame.SourcePageType)
            {
                BreadcrumbBarItems.Clear();
                ContentFrame.Navigate(pageType, CreatePageParameter(pageName), args.RecommendedNavigationTransitionInfo);
                ContentFrame.BackStack.Clear();
            }
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            navigationParameter = ResolveNavigationParameter(e.Parameter);
            ViewModel = navigationParameter.AppViewModel;
            SettingsViewModel = navigationParameter.SettingsViewModel;
            DataContext = ViewModel;

            var pageType = Route.GetSettingsPageType(navigationParameter.PageName) ?? typeof(GeneralPage);
            if (ContentFrame.SourcePageType != pageType)
            {
                ContentFrame.Navigate(pageType, CreatePageParameter(navigationParameter.PageName), new SuppressNavigationTransitionInfo());
            }
        }

        private void OnSizeChanged(object sender, SizeChangedEventArgs e)
        {
            var state = ActualWidth >= 1008 ? "Large" : ActualWidth >= 640 ? "Medium" : "Small";
            VisualStateManager.GoToState(this, state, false);
        }

        private void OnNavigated(object sender, NavigationEventArgs e)
        {
            var item = NavigationView.MenuItems
                .OfType<NavigationViewItem>()
                .Where(x => Route.GetSettingsPageType(x.Tag as string) == GetNavigationSelectionPageType(ContentFrame.SourcePageType))
                .FirstOrDefault();

            if (item != null)
            {
                isUpdatingNavigationSelection = true;
                try
                {
                    NavigationView.SelectedItem = item;
                }
                finally
                {
                    isUpdatingNavigationSelection = false;
                }
            }

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
                    ContentFrame.Navigate(type, CreatePageParameter(path[1], query), GetTransition());
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

        private void ApplyNavigationLabels()
        {
            foreach (var item in NavigationView.MenuItems.OfType<NavigationViewItem>())
            {
                item.Content = Route.GetSettingsPageTitle(Route.GetSettingsPageType(item.Tag as string));
            }
        }

        private SettingsNavigationParameter ResolveNavigationParameter(object? parameter)
        {
            if (parameter is SettingsNavigationParameter settingsParameter)
            {
                return settingsParameter;
            }

            if (parameter is string pageName && ViewModel != null && SettingsViewModel != null)
            {
                return navigationParameter?.WithPage(pageName) ?? new SettingsNavigationParameter(ViewModel, SettingsViewModel, pageName);
            }

            throw new InvalidOperationException("Typedown settings navigation requires presentation view models.");
        }

        private object? CreatePageParameter(string? pageName, string? query = null)
        {
            if (navigationParameter == null)
            {
                return query;
            }

            var pageType = Route.GetSettingsPageType(pageName);
            if (pageType == typeof(GeneralPage)
                || pageType == typeof(ViewPage)
                || pageType == typeof(EditorPage)
                || pageType == typeof(ImagePage)
                || pageType == typeof(ImageUploadPage)
                || pageType == typeof(ExportPage)
                || pageType == typeof(ExportConfigPage)
                || pageType == typeof(ShortcutPage)
                || pageType == typeof(UploadConfigPage))
            {
                return navigationParameter.WithPage(pageName, query);
            }

            return query;
        }

        private static Type? GetNavigationSelectionPageType(Type? pageType)
        {
            if (pageType == typeof(ShortcutPage))
            {
                return typeof(GeneralPage);
            }

            if (pageType == typeof(ImageUploadPage) || pageType == typeof(UploadConfigPage))
            {
                return typeof(ImagePage);
            }

            return pageType;
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
