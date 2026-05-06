using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reflection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using Typedown.Core.Models;
using Typedown.Core.Utilities;
using Typedown.Presentation.Utilities;
using Typedown.Presentation.ViewModels;
using Typedown.WinUI.Controls;

namespace Typedown.WinUI.Pages.SettingPages
{
    [Locale("Shortcut.Title")]
    public sealed partial class ShortcutPage : Page
    {
        private static DependencyProperty SearchTextProperty { get; } = DependencyProperty.Register(nameof(SearchText), typeof(string), typeof(ShortcutPage), new(string.Empty));
        private string SearchText { get => (string)GetValue(SearchTextProperty); set => SetValue(SearchTextProperty, value); }

        private static DependencyProperty FliterCategoryProperty { get; } = DependencyProperty.Register(nameof(FliterCategory), typeof(ShortcutPageCategoryModel), typeof(ShortcutPage), null);
        private ShortcutPageCategoryModel FliterCategory { get => (ShortcutPageCategoryModel)GetValue(FliterCategoryProperty); set => SetValue(FliterCategoryProperty, value); }

        private static DependencyProperty FliterCategoriesProperty { get; } = DependencyProperty.Register(nameof(FliterCategories), typeof(List<ShortcutPageCategoryModel>), typeof(ShortcutPage), null);
        private List<ShortcutPageCategoryModel> FliterCategories { get => (List<ShortcutPageCategoryModel>)GetValue(FliterCategoriesProperty); set => SetValue(FliterCategoriesProperty, value); }

        public AppViewModel? ViewModel { get; private set; }

        public SettingsViewModel? SettingsViewModel { get; private set; }

        private List<ShortcutPageItemModel> AllSettingItems { get; set; }

        private ObservableCollection<ShortcutPageItemModel> SettingItems { get; } = new();

        private readonly CompositeDisposable disposables = new();

        public ShortcutPage()
        {
            NavigationCacheMode = NavigationCacheMode.Enabled;
            InitializeComponent();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            if (e.Parameter is SettingsNavigationParameter parameter)
            {
                ViewModel = parameter.AppViewModel;
                SettingsViewModel = parameter.SettingsViewModel;
                DataContext = ViewModel;
                Bindings.Update();
            }
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            LoadAllShortcutSettingItems();
            disposables.Add(this.Binding(new(nameof(SearchText))).Merge(this.Binding(new(nameof(FliterCategory))))
                .Throttle(TimeSpan.FromMilliseconds(100))
                .Subscribe(_ => QueueUpdateFilteredSettingItems()));
            QueueUpdateFilteredSettingItems();
        }

        private void QueueUpdateFilteredSettingItems()
        {
            DispatcherQueue?.TryEnqueue(UpdateFilteredSettingItems);
        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            disposables.Clear();
            SettingItems.Clear();
        }

        public void LoadAllShortcutSettingItems()
        {
            SettingItems.Clear();
            var target = this.GetService<SettingsViewModel>();
            AllSettingItems = typeof(SettingsViewModel)
                .GetProperties()
                .Where(x => x.PropertyType == typeof(ShortcutKey))
                .Select(x => new ShortcutPageItemModel(target, x))
                .ToList();
            FliterCategories = AllSettingItems.Select(x => x.Category).ToHashSet().Select(x => new ShortcutPageCategoryModel(x, x)).ToList();
            FliterCategories.Insert(0, new(Locale.GetString("All"), null));
            FliterCategory = FliterCategories[0];
        }

        public void UpdateFilteredSettingItems()
        {
            if (IsLoaded)
            {
                var newItems = AllSettingItems
                    .Where(x => string.IsNullOrEmpty(FliterCategory?.Category) || x.Category == FliterCategory.Category)
                    .Where(x => string.IsNullOrEmpty(SearchText) || x.DisplayName.ToLower().Contains(SearchText.ToLower()) || x.Description.ToLower().Contains(SearchText.ToLower()))
                    .ToList();
                SettingItems.UpdateCollection(newItems, (a, b) => a == b);
            }
            else
            {
                SettingItems.Clear();
            }
        }
    }

    public partial class ShortcutPageItemModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        public SettingsViewModel Target { get; }

        public PropertyInfo Property { get; }

        public string DisplayName { get; }

        public string Description { get; }

        public string Category { get; }

        public ShortcutKey ShortcutKey
        {
            get => (ShortcutKey)Property.GetValue(Target);
            set
            {
                if (EqualityComparer<ShortcutKey>.Default.Equals(ShortcutKey, value))
                    return;

                Property.SetValue(Target, value);
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ShortcutKey)));
            }
        }

        public ShortcutPageItemModel(SettingsViewModel target, PropertyInfo property)
        {
            Target = target;
            Property = property;
            var texts = Property.GetCustomAttribute<LocaleAttribute>()?.Texts.ToList();
            if (texts == null || !texts.Any())
            {
                DisplayName = Property.Name;
                Description = "Unknown";
                Category = "Unknown";
            }
            else
            {
                DisplayName = string.IsNullOrEmpty(texts.Last()) ? Property.Name : texts.Last();
                Description = string.Join(" / ", texts.Take(texts.Count - 1).Append(DisplayName));
                Category = texts.First();
            }
        }
    }

    public class ShortcutPageCategoryModel
    {
        public string DisplayName { get; }

        public string Category { get; }

        public ShortcutPageCategoryModel(string displayName, string category)
        {
            DisplayName = displayName;
            Category = category;
        }
    }
}
