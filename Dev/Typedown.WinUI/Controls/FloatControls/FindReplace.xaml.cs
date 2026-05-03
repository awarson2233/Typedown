using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using System.ComponentModel;
using Typedown.Presentation.Interfaces;
using Typedown.Presentation.ViewModels;
using Windows.System;

namespace Typedown.WinUI.Controls;

public sealed partial class FindReplace : UserControl
{
    private readonly TextBox searchTextBox = new() { PlaceholderText = "Find" };
    private readonly TextBox replaceTextBox = new() { PlaceholderText = "Replace" };
    private readonly Grid replaceRow = new() { ColumnSpacing = 8 };
    private AppViewModel? viewModel;
    private bool updatingSearchText;

    public AppViewModel? ViewModel => viewModel ?? DataContext as AppViewModel;

    public FloatViewModel? Float => ViewModel?.FloatViewModel;

    public EditorViewModel? Editor => ViewModel?.EditorViewModel;

    public SettingsViewModel? Settings => ViewModel?.SettingsViewModel;

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
        var root = new Grid
        {
            Padding = new Thickness(20, 16, 20, 0),
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Top
        };

        var dialog = new Grid
        {
            Width = 600,
            Padding = new Thickness(8),
            ColumnSpacing = 8,
            RowSpacing = 8,
            Background = (Brush)Application.Current.Resources["TeachingTipBackgroundBrush"],
            BorderBrush = (Brush)Application.Current.Resources["TeachingTipBorderBrush"],
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(4)
        };

        dialog.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        dialog.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        dialog.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        dialog.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        var switchButton = CreateIconButton("\uE70D");
        switchButton.Click += OnSwitchButtonClick;
        dialog.Children.Add(switchButton);

        var searchRow = new Grid { ColumnSpacing = 8 };
        searchRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        for (var i = 0; i < 4; i++)
        {
            searchRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        }

        searchTextBox.TextChanged += OnSearchTextChanged;
        searchTextBox.KeyDown += OnTextBoxSearchKeyDown;
        searchRow.Children.Add(searchTextBox);

        var nextButton = CreateIconButton("\uE74B");
        nextButton.Click += (_, _) => Editor?.FindCommand.Execute("next");
        Grid.SetColumn(nextButton, 1);
        searchRow.Children.Add(nextButton);

        var previousButton = CreateIconButton("\uE74A");
        previousButton.Click += (_, _) => Editor?.FindCommand.Execute("prev");
        Grid.SetColumn(previousButton, 2);
        searchRow.Children.Add(previousButton);

        var optionsButton = CreateIconButton("\uE9E9");
        optionsButton.Flyout = BuildOptionsFlyout();
        Grid.SetColumn(optionsButton, 3);
        searchRow.Children.Add(optionsButton);

        var closeButton = CreateIconButton("\uE8BB");
        closeButton.Click += OnCloseButtonClick;
        Grid.SetColumn(closeButton, 4);
        searchRow.Children.Add(closeButton);

        Grid.SetColumn(searchRow, 1);
        dialog.Children.Add(searchRow);

        replaceRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        replaceRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        replaceRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        replaceTextBox.KeyDown += OnTextBoxReplaceKeyDown;
        replaceRow.Children.Add(replaceTextBox);

        var replaceButton = new Button { MinWidth = 100, Content = "Replace" };
        replaceButton.Click += OnReplaceButtonClick;
        Grid.SetColumn(replaceButton, 1);
        replaceRow.Children.Add(replaceButton);

        var replaceAllButton = new Button { MinWidth = 100, Content = "ReplaceAll" };
        replaceAllButton.Click += OnReplaceAllButtonClick;
        Grid.SetColumn(replaceAllButton, 2);
        replaceRow.Children.Add(replaceAllButton);

        Grid.SetColumn(replaceRow, 1);
        Grid.SetRow(replaceRow, 1);
        dialog.Children.Add(replaceRow);

        root.Children.Add(dialog);
        return root;
    }

    private static Button CreateIconButton(string glyph)
    {
        return new Button
        {
            Width = 32,
            Height = 32,
            Padding = new Thickness(0),
            Content = new FontIcon { FontSize = 14, Glyph = glyph }
        };
    }

    private MenuFlyout BuildOptionsFlyout()
    {
        var flyout = new MenuFlyout { Placement = FlyoutPlacementMode.Top };
        flyout.Items.Add(CreateOptionItem("CaseSensitive", nameof(SettingsViewModel.SearchIsCaseSensitive)));
        flyout.Items.Add(CreateOptionItem("WholeWord", nameof(SettingsViewModel.SearchIsWholeWord)));
        flyout.Items.Add(CreateOptionItem("UseRegexp", nameof(SettingsViewModel.SearchIsRegexp)));
        return flyout;
    }

    private ToggleMenuFlyoutItem CreateOptionItem(string text, string settingName)
    {
        var item = new ToggleMenuFlyoutItem { Text = text };
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
        };
        return item;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        AttachViewModel(DataContext as AppViewModel);
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

        if (viewModel is not null)
        {
            viewModel.FloatViewModel.PropertyChanged -= OnFloatViewModelPropertyChanged;
            viewModel.EditorViewModel.PropertyChanged -= OnEditorViewModelPropertyChanged;
        }

        viewModel = nextViewModel;

        if (viewModel is not null)
        {
            viewModel.FloatViewModel.PropertyChanged += OnFloatViewModelPropertyChanged;
            viewModel.EditorViewModel.PropertyChanged += OnEditorViewModelPropertyChanged;
            UpdateSearchText(viewModel.EditorViewModel.SearchValue);
            SearchOpenChanged(viewModel.FloatViewModel.FindReplaceDialogOpen);
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
