using PropertyChanged;
using Avalonia.Controls;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Reactive.Disposables;
using Typedown.Core.Pages.SettingPages;
using Typedown.Core.Utilities;
using Typedown.Core.ViewModels;
using Avalonia.Interactivity;

namespace Typedown.Core.Pages
{
    [DoNotNotify]
    public sealed partial class SettingsPage : UserControl
    {
        public AppViewModel ViewModel => DataContext as AppViewModel;

        public SettingsViewModel Settings => ViewModel?.SettingsViewModel;

        public ObservableCollection<SettingsBreadcrumbBarItem> BreadcrumbBarItems { get; } = new();

        private readonly CompositeDisposable disposables = new();

        public SettingsPage()
        {
            InitializeComponent();
        }

        // TODO: Implement Avalonia navigation equivalent
        // WinUI NavigationView/BreadcrumbBar items need Avalonia equivalents

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            disposables.Add(ViewModel.NavigateCommand.OnExecute.Subscribe(args => Navigate(args)));
        }

        private void Navigate(string args)
        {
            // TODO: Implement navigation logic for Avalonia
        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            disposables.Clear();
        }

        public void SetPageTitle(UserControl page, string title)
        {
            var item = BreadcrumbBarItems.Where(x => x.Page == page).FirstOrDefault();
            if (item != null)
                item.Title = title;
        }
    }

    [DoNotNotify]
    public partial class SettingsBreadcrumbBarItem : INotifyPropertyChanged
    {
        public UserControl Page { get; }

        public Type PageType { get; }

        public string Title { get; set; }

        public event PropertyChangedEventHandler? PropertyChanged;

        public SettingsBreadcrumbBarItem(UserControl page, string title = null)
        {
            Page = page;
            PageType = Page.GetType();
            Title = title ?? Locale.GetTypeString(PageType);
        }
    }
}
