using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using System;
using System.Linq;
using System.Reactive.Disposables;
using System.Windows.Input;
using Microsoft.UI.Xaml;
using Typedown.Core.Models;
using Typedown.Core.Utilities;
using Typedown.Presentation.Interfaces;
using Typedown.Presentation.ViewModels;
using Windows.System;

namespace Typedown.WinUI.Controls;

public abstract partial class MenuBarItemBase : Microsoft.UI.Xaml.Controls.MenuBarItem
{
    private readonly CompositeDisposable shortcutRegistrations = new();
    private AppViewModel? configuredViewModel;

    protected MenuBarItemBase(string title)
    {
        Title = title;
        DataContextChanged += OnDataContextChanged;
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
    }

    protected void AddPlaceholder(string text)
    {
        Items.Add(new MenuFlyoutItem { Text = text });
    }

    protected AppViewModel? ViewModel => DataContext as AppViewModel;

    protected abstract void ConfigureCommands(AppViewModel? viewModel);

    protected void InitializeMenu()
    {
        ConfigureFor(ViewModel);
    }

    protected void ReleaseMenu()
    {
        shortcutRegistrations.Clear();
        configuredViewModel = null;
        ConfigureCommands(null);
    }

    private void ConfigureFor(AppViewModel? viewModel)
    {
        if (ReferenceEquals(configuredViewModel, viewModel))
        {
            return;
        }

        shortcutRegistrations.Clear();
        configuredViewModel = viewModel;
        ConfigureCommands(viewModel);
    }

    protected static void SetCommand(MenuFlyoutItem item, ICommand? command, object? parameter = null)
    {
        item.Command = command;
        if (parameter is not null)
        {
            item.CommandParameter = parameter;
        }

        item.IsEnabled = command is not null;
    }

    protected void SetShortcut(MenuFlyoutItem item, ShortcutKey? shortcut)
    {
        item.KeyboardAccelerators.Clear();
        item.KeyboardAcceleratorTextOverride = string.Empty;

        if (!HasShortcutKey(shortcut))
        {
            return;
        }

        var activeShortcut = shortcut!;
        item.KeyboardAcceleratorTextOverride = activeShortcut.GetShortcutKeyText();
        RegisterShortcut(activeShortcut, () =>
        {
            if (item.Command?.CanExecute(item.CommandParameter) == true)
            {
                item.Command.Execute(item.CommandParameter);
            }
        });
    }

    protected static void SetCommand(ToggleMenuFlyoutItem item, ICommand? command, object? parameter = null)
    {
        item.Command = command;
        if (parameter is not null)
        {
            item.CommandParameter = parameter;
        }

        item.IsEnabled = command is not null;
    }

    protected void SetShortcut(ToggleMenuFlyoutItem item, ShortcutKey? shortcut, Action? invoke = null)
    {
        item.KeyboardAccelerators.Clear();
        item.KeyboardAcceleratorTextOverride = string.Empty;

        if (!HasShortcutKey(shortcut))
        {
            return;
        }

        var activeShortcut = shortcut!;
        item.KeyboardAcceleratorTextOverride = activeShortcut.GetShortcutKeyText();
        RegisterShortcut(activeShortcut, () =>
        {
            if (invoke is not null)
            {
                invoke();
            }
            else if (item.Command?.CanExecute(item.CommandParameter) == true)
            {
                item.Command.Execute(item.CommandParameter);
            }
        });
    }

    private static bool HasShortcutKey(ShortcutKey? shortcut)
    {
        return shortcut is not null && shortcut.Key != KeyboardKey.None;
    }

    private void RegisterShortcut(ShortcutKey shortcut, Action invoke)
    {
        var accelerator = ViewModel?.ServiceProvider.GetService<IKeyboardAccelerator>();
        if (accelerator is null)
        {
            return;
        }

        shortcutRegistrations.Add(accelerator.Register(shortcut, (_, args) =>
        {
            invoke();
            args.Handled = true;
        }));
    }

    protected MenuFlyoutItem? FindMenuItem(string text)
    {
        return Items
            .SelectMany(Flatten)
            .OfType<MenuFlyoutItem>()
            .FirstOrDefault(item => string.Equals(item.Text, text, StringComparison.Ordinal));
    }

    protected void DisableMenuItem(string text)
    {
        if (FindMenuItem(text) is { } item)
        {
            item.Command = null;
            item.IsEnabled = false;
        }
    }

    private void OnDataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args)
    {
        ConfigureFor(args.NewValue as AppViewModel);
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        ConfigureFor(ViewModel);
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        shortcutRegistrations.Clear();
    }

    private static System.Collections.Generic.IEnumerable<object> Flatten(object item)
    {
        yield return item;

        if (item is MenuFlyoutSubItem subItem)
        {
            foreach (var child in subItem.Items.SelectMany(Flatten))
            {
                yield return child;
            }
        }
    }
}
