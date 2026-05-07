using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI;
using System.Numerics;
using System.ComponentModel;
using Typedown.Presentation.Interfaces;
using Typedown.Presentation.Utilities;
using Typedown.Presentation.ViewModels;
using Windows.System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Typedown.WinUI.Controls;

public sealed partial class FindReplace : UserControl
{
    private readonly TextBox searchTextBox = new() { PlaceholderText = "Find" };
    private readonly TextBox replaceTextBox = new() { PlaceholderText = "Replace" };
    private readonly Grid replaceRow = new() { ColumnSpacing = 8 };
    private readonly Grid backgroundGrid = new();
    private readonly Grid dialogGrid = new();
    private readonly FontIcon chevronIcon = new() { FontSize = 14, Glyph = "\uE70D" };
    private readonly RotateTransform chevronRotateTransform = new() { CenterX = 7, CenterY = 7 };
    private ToggleMenuFlyoutItem? caseSensitiveItem;
    private ToggleMenuFlyoutItem? wholeWordItem;
    private ToggleMenuFlyoutItem? regexpItem;
    private AppViewModel? viewModel;
    private FloatViewModel? floatViewModel;
    private EditorViewModel? editorViewModel;
    private SettingsViewModel? settingsViewModel;
    private bool updatingSearchText;

    public AppViewModel? ViewModel => viewModel;

    public FloatViewModel? Float => floatViewModel;

    public EditorViewModel? Editor => editorViewModel;

    public SettingsViewModel? Settings => settingsViewModel;

    public IEditorCommandSink? EditorCommandSink => Editor?.EditorCommandSink;

    public FindReplace()
    {
        Content = BuildContent();
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
        DataContextChanged += OnDataContextChanged;
        KeyDown += OnKeyDown;
    }

    private FrameworkElement BuildContent()
    {
        var root = new Grid();
        var shadow = new ThemeShadow();
        shadow.Receivers.Add(backgroundGrid);

        var host = new Grid
        {
            Padding = new Thickness(4, 0, 4, 0),
            Margin = new Thickness(16),
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Top
        };

        dialogGrid.Width = Settings?.FindReplaceDialogWidth ?? 600;
        dialogGrid.Padding = new Thickness(8);
        dialogGrid.ColumnSpacing = 8;
        dialogGrid.Background = GetResource(
            "TeachingTipBackgroundBrush",
            GetResource<Brush>("LayerFillColorDefaultBrush", new SolidColorBrush(Colors.White)));
        dialogGrid.BorderBrush = GetResource(
            "TeachingTipBorderBrush",
            GetResource<Brush>("ControlStrokeColorDefaultBrush", new SolidColorBrush(Colors.Gray)));
        dialogGrid.BorderThickness = GetResource("TeachingTipContentBorderThicknessUntargeted", new Thickness(1));
        dialogGrid.CornerRadius = GetResource("OverlayCornerRadius", new CornerRadius(4));
        dialogGrid.Shadow = shadow;
        dialogGrid.Translation += new Vector3(0, 0, 32);

        dialogGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        dialogGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        dialogGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        dialogGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        chevronIcon.RenderTransform = chevronRotateTransform;
        var switchButton = CreateIconButton(chevronIcon);
        switchButton.Click += OnSwitchButtonClick;
        dialogGrid.Children.Add(switchButton);

        var searchRow = new Grid { ColumnSpacing = 8 };
        searchRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        for (var i = 0; i < 4; i++)
        {
            searchRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        }

        searchTextBox.PlaceholderText = Locale.GetString("Find");
        searchTextBox.TextChanged += OnSearchTextChanged;
        searchTextBox.KeyDown += OnTextBoxSearchKeyDown;
        searchRow.Children.Add(searchTextBox);

        var nextButton = CreateIconButton("\uE74B");
        ToolTipService.SetToolTip(nextButton, Locale.GetString("FindNext"));
        nextButton.Click += (_, _) => Editor?.FindCommand.Execute("next");
        Grid.SetColumn(nextButton, 1);
        searchRow.Children.Add(nextButton);

        var previousButton = CreateIconButton("\uE74A");
        ToolTipService.SetToolTip(previousButton, Locale.GetString("FindPrevious"));
        previousButton.Click += (_, _) => Editor?.FindCommand.Execute("prev");
        Grid.SetColumn(previousButton, 2);
        searchRow.Children.Add(previousButton);

        var optionsButton = CreateIconButton("\uE9E9");
        ToolTipService.SetToolTip(optionsButton, Locale.GetString("MoreOptions"));
        optionsButton.Flyout = BuildOptionsFlyout();
        Grid.SetColumn(optionsButton, 3);
        searchRow.Children.Add(optionsButton);

        var closeButton = CreateIconButton("\uF78A");
        ToolTipService.SetToolTip(closeButton, Locale.GetString("Close"));
        closeButton.Click += OnCloseButtonClick;
        Grid.SetColumn(closeButton, 4);
        searchRow.Children.Add(closeButton);

        Grid.SetColumn(searchRow, 1);
        dialogGrid.Children.Add(searchRow);

        replaceRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        replaceRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        replaceRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        replaceTextBox.PlaceholderText = Locale.GetString("Replace");
        replaceTextBox.KeyDown += OnTextBoxReplaceKeyDown;
        replaceRow.Children.Add(replaceTextBox);

        var replaceButton = new Button { MinWidth = 100, Content = Locale.GetString("Replace") };
        replaceButton.Click += OnReplaceButtonClick;
        Grid.SetColumn(replaceButton, 1);
        replaceRow.Children.Add(replaceButton);

        var replaceAllButton = new Button { MinWidth = 100, Content = Locale.GetString("ReplaceAll") };
        replaceAllButton.Click += OnReplaceAllButtonClick;
        Grid.SetColumn(replaceAllButton, 2);
        replaceRow.Children.Add(replaceAllButton);

        Grid.SetColumn(replaceRow, 1);
        Grid.SetRow(replaceRow, 1);
        dialogGrid.Children.Add(replaceRow);

        host.Children.Add(dialogGrid);
        root.Children.Add(backgroundGrid);
        root.Children.Add(host);
        return root;
    }

    private static Button CreateIconButton(string glyph)
    {
        return CreateIconButton(new FontIcon { FontSize = 14, Glyph = glyph });
    }

    private static Button CreateIconButton(IconElement icon)
    {
        var button = new Button
        {
            Width = 32,
            Height = 32,
            Padding = new Thickness(0),
            Content = icon
        };

        if (Application.Current.Resources.TryGetValue("EllipsisIconButtonStyle", out var style) && style is Style buttonStyle)
        {
            button.Style = buttonStyle;
        }

        return button;
    }

    private static T GetResource<T>(string key, T fallback)
    {
        return Application.Current.Resources.TryGetValue(key, out var value) && value is T typedValue
            ? typedValue
            : fallback;
    }

    private MenuFlyout BuildOptionsFlyout()
    {
        var flyout = new MenuFlyout { Placement = FlyoutPlacementMode.Top };
        caseSensitiveItem = CreateOptionItem("CaseSensitive", nameof(SettingsViewModel.SearchIsCaseSensitive));
        wholeWordItem = CreateOptionItem("WholeWord", nameof(SettingsViewModel.SearchIsWholeWord));
        regexpItem = CreateOptionItem("UseRegexp", nameof(SettingsViewModel.SearchIsRegexp));

        flyout.Opening += (_, _) => UpdateOptionItems();
        flyout.Items.Add(caseSensitiveItem);
        flyout.Items.Add(wholeWordItem);
        flyout.Items.Add(regexpItem);
        return flyout;
    }

    private ToggleMenuFlyoutItem CreateOptionItem(string text, string settingName)
    {
        var item = new ToggleMenuFlyoutItem { Text = Locale.GetString(text) };
        item.Click += (_, _) =>
        {
            var settings = Settings;
            if (settings is null)
            {
                return;
            }

            switch (settingName)
            {
                case nameof(SettingsViewModel.SearchIsCaseSensitive):
                    settings.SearchIsCaseSensitive = item.IsChecked;
                    break;
                case nameof(SettingsViewModel.SearchIsWholeWord):
                    settings.SearchIsWholeWord = item.IsChecked;
                    break;
                case nameof(SettingsViewModel.SearchIsRegexp):
                    settings.SearchIsRegexp = item.IsChecked;
                    break;
            }

            Editor?.OnSearch();
            UpdateOptionItems();
        };
        return item;
    }

    private void UpdateOptionItems()
    {
        if (Settings is not { } settings)
        {
            return;
        }

        if (caseSensitiveItem is not null)
        {
            caseSensitiveItem.IsChecked = settings.SearchIsCaseSensitive;
        }

        if (wholeWordItem is not null)
        {
            wholeWordItem.IsChecked = settings.SearchIsWholeWord;
        }

        if (regexpItem is not null)
        {
            regexpItem.IsChecked = settings.SearchIsRegexp;
        }
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        AttachViewModel(DataContext as AppViewModel);
        if (Settings is not null)
        {
            dialogGrid.Width = Settings.FindReplaceDialogWidth;
        }

        UpdateOptionItems();
        searchTextBox.Focus(FocusState.Pointer);
        searchTextBox.SelectAll();
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        AttachViewModel(null);
    }

    private void OnDataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args)
    {
        AttachViewModel(args.NewValue as AppViewModel);
    }

    private void AttachViewModel(AppViewModel? nextViewModel)
    {
        if (ReferenceEquals(viewModel, nextViewModel))
        {
            return;
        }

        if (floatViewModel is not null)
        {
            floatViewModel.PropertyChanged -= OnFloatViewModelPropertyChanged;
        }

        if (editorViewModel is not null)
        {
            editorViewModel.PropertyChanged -= OnEditorViewModelPropertyChanged;
        }

        viewModel = nextViewModel;
        floatViewModel = null;
        editorViewModel = null;
        settingsViewModel = null;

        if (viewModel is not null)
        {
            floatViewModel = viewModel.FloatViewModel;
            editorViewModel = viewModel.EditorViewModel;
            settingsViewModel = viewModel.SettingsViewModel;

            floatViewModel.PropertyChanged += OnFloatViewModelPropertyChanged;
            editorViewModel.PropertyChanged += OnEditorViewModelPropertyChanged;
            UpdateSearchText(editorViewModel.SearchValue);
            UpdateOptionItems();
            SearchOpenChanged(floatViewModel.FindReplaceDialogOpen);
        }
    }

    private void OnFloatViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(FloatViewModel.FindReplaceDialogOpen) && sender is FloatViewModel floatViewModel)
        {
            SearchOpenChanged(floatViewModel.FindReplaceDialogOpen);
        }
    }

    private void OnEditorViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(EditorViewModel.SearchValue) && sender is EditorViewModel editor)
        {
            UpdateSearchText(editor.SearchValue);
        }
    }

    private void UpdateSearchText(string? searchValue)
    {
        var nextText = searchValue ?? string.Empty;
        if (searchTextBox.Text == nextText)
        {
            return;
        }

        updatingSearchText = true;
        searchTextBox.Text = nextText;
        updatingSearchText = false;
    }

    private void SearchOpenChanged(FloatViewModel.FindReplaceDialogState state)
    {
        replaceRow.Visibility = state == FloatViewModel.FindReplaceDialogState.Replace
            ? Visibility.Visible
            : Visibility.Collapsed;
        replaceRow.MaxHeight = state == FloatViewModel.FindReplaceDialogState.Replace ? 40 : 0;
        chevronRotateTransform.Angle = state == FloatViewModel.FindReplaceDialogState.Replace ? 0 : 180;

        if (state != FloatViewModel.FindReplaceDialogState.None)
        {
            searchTextBox.Focus(FocusState.Programmatic);
            searchTextBox.SelectAll();
        }
    }

    private void OnSearchTextChanged(object sender, TextChangedEventArgs e)
    {
        if (updatingSearchText || Editor is null)
        {
            return;
        }

        Editor.SearchValue = searchTextBox.Text;
    }

    private void OnCloseButtonClick(object sender, RoutedEventArgs e)
    {
        if (Float is not null)
        {
            Float.FindReplaceDialogOpen = FloatViewModel.FindReplaceDialogState.None;
        }
    }

    private void OnReplaceButtonClick(object sender, RoutedEventArgs e)
    {
        SendReplace(true);
    }

    private void OnReplaceAllButtonClick(object sender, RoutedEventArgs e)
    {
        SendReplace(false);
    }

    private void OnFindNextButtonClick(object sender, RoutedEventArgs e)
    {
        Editor?.FindCommand.Execute("next");
    }

    private void OnFindPreviousButtonClick(object sender, RoutedEventArgs e)
    {
        Editor?.FindCommand.Execute("prev");
    }

    private void OnTextBoxSearchKeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (e.Key == VirtualKey.Enter)
        {
            Editor?.FindCommand.Execute("next");
            e.Handled = true;
        }
    }

    private void OnTextBoxReplaceKeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (e.Key == VirtualKey.Enter)
        {
            SendReplace(true);
            e.Handled = true;
        }
    }

    private void OnKeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (e.Key == VirtualKey.Escape && Float is not null)
        {
            Float.FindReplaceDialogOpen = FloatViewModel.FindReplaceDialogState.None;
            e.Handled = true;
        }
    }

    private void SendReplace(bool isSingle)
    {
        var editor = Editor;
        var settings = Settings;
        if (editor is null || settings is null)
        {
            return;
        }

        EditorCommandSink?.Send("Replace", new
        {
            searchValue = editor.SearchValue,
            value = replaceTextBox.Text,
            opt = new
            {
                isSingle,
                searchIsCaseSensitive = settings.SearchIsCaseSensitive,
                searchIsWholeWord = settings.SearchIsWholeWord,
                searchIsRegexp = settings.SearchIsRegexp
            }
        });
    }

    private void OnSwitchButtonClick(object sender, RoutedEventArgs e)
    {
        if (Float is null)
        {
            return;
        }

        Float.FindReplaceDialogOpen = Float.FindReplaceDialogOpen == FloatViewModel.FindReplaceDialogState.Search
            ? FloatViewModel.FindReplaceDialogState.Replace
            : FloatViewModel.FindReplaceDialogState.Search;
    }
}
