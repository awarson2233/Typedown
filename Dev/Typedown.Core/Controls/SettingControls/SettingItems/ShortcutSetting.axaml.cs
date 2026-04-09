using PropertyChanged;
﻿using System;
using Avalonia.Input;
using Avalonia.Metadata;
using Avalonia.Data.Converters;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reflection;
using Typedown.Core.Models;
using Typedown.Core.Utilities;
using Typedown.Core.ViewModels;
using Avalonia;
using Avalonia.Interactivity;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Threading;

namespace Typedown.Core.Controls.SettingControls.SettingItems
{
[DoNotNotify]
        public sealed partial class ShortcutSetting : UserControl
    {
        public static readonly StyledProperty<string> SearchTextProperty = AvaloniaProperty.Register<ShortcutSetting, string>(nameof(SearchText), string.Empty);
        private string SearchText { get => GetValue(SearchTextProperty); set => SetValue(SearchTextProperty, value); }

        public static readonly StyledProperty<ShortcutSettingCategoryModel> FliterCategoryProperty = AvaloniaProperty.Register<ShortcutSetting, ShortcutSettingCategoryModel>(nameof(FliterCategory), null);
        private ShortcutSettingCategoryModel FliterCategory { get => (ShortcutSettingCategoryModel)GetValue(FliterCategoryProperty); set => SetValue(FliterCategoryProperty, value); }

        public static readonly StyledProperty<List<ShortcutSettingCategoryModel>> FliterCategoriesProperty = AvaloniaProperty.Register<ShortcutSetting, List<ShortcutSettingCategoryModel>>(nameof(FliterCategories), null);
        private List<ShortcutSettingCategoryModel> FliterCategories { get => (List<ShortcutSettingCategoryModel>)GetValue(FliterCategoriesProperty); set => SetValue(FliterCategoriesProperty, value); }

        private List<ShortcutSettingItemModel> AllSettingItems { get; set; }

        private ObservableCollection<ShortcutSettingItemModel> SettingItems { get; } = new();

        private readonly CompositeDisposable disposables = new();

        public ShortcutSetting()
        {
            InitializeComponent();
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            LoadAllShortcutSettingItems();
            // TODO: Avalonia doesn't have this.Binding() or Dispatcher.RunIdleAsync
            // Using Dispatcher.UIThread.Post instead
            Dispatcher.UIThread.Post(() => UpdateFilteredSettingItems());
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
                .Select(x => new ShortcutSettingItemModel(target, x))
                .ToList();
            FliterCategories = AllSettingItems.Select(x => x.Category).ToHashSet().Select(x => new ShortcutSettingCategoryModel(x, x)).ToList();
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

[DoNotNotify]
        public partial class ShortcutSettingItemModel : INotifyPropertyChanged
    {
        public SettingsViewModel Target { get; }

        public PropertyInfo Property { get; }

        public string DisplayName { get; }

        public string Description { get; }

        public string Category { get; }

        public ShortcutKey ShortcutKey { get => (ShortcutKey)Property.GetValue(Target); set => Property.SetValue(Target, value); }

        public ShortcutSettingItemModel(SettingsViewModel target, PropertyInfo property)
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

[DoNotNotify]
        public class ShortcutSettingCategoryModel
    {
        public string DisplayName { get; }

        public string Category { get; }

        public ShortcutSettingCategoryModel(string displayName, string category)
        {
            DisplayName = displayName;
            Category = category;
        }
    }
}
