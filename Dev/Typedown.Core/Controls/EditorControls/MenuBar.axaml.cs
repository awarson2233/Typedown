using PropertyChanged;
﻿using System;
using System.Collections.ObjectModel;
using Avalonia.Input;
using Avalonia.Metadata;
using Avalonia.Data.Converters;
using System.Reactive.Disposables;
using Typedown.Core.Utilities;
using Typedown.Core.ViewModels;
using Avalonia.Controls;
using Avalonia;
using Avalonia.Interactivity;
using Avalonia.Controls.Primitives;

namespace Typedown.Core.Controls
{
[DoNotNotify]
        public sealed partial class MenuBar : UserControl
    {
        public AppViewModel ViewModel => DataContext as AppViewModel;
        public SettingsViewModel Settings => ViewModel?.SettingsViewModel;

        private readonly CompositeDisposable disposables = new();

        public MenuBar()
        {
            InitializeComponent();
        }

        private void OnSizeChanged(object sender, SizeChangedEventArgs e)
        {
            var titleGrid = this.FindControl<Grid>("TitleGrid");
            var menuBarControl = this.FindControl<Control>("MenuBarControl");
            var titleTextBlock = this.FindControl<TextBlock>("TitleTextBlock");
            if (titleGrid != null)
            {
                if (Bounds.Width / 2 > (menuBarControl?.Bounds.Width ?? 0) + (titleTextBlock?.Bounds.Width ?? 0) / 2 + 16)
                {
                    titleGrid.Margin = new(0);
                    Grid.SetColumn(titleGrid, 0);
                    Grid.SetColumnSpan(titleGrid, 3);
                }
                else
                {
                    titleGrid.Margin = new(0, 0, 46 * 3, 0);
                    Grid.SetColumn(titleGrid, 1);
                    Grid.SetColumnSpan(titleGrid, 2);
                }
            }
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            if (Settings.AppCompactMode)
            {
                var uiViewModel = ViewModel.UIViewModel;
                var oldCaptionHeight = uiViewModel.CaptionHeight;
                uiViewModel.CaptionHeight = 40;
                disposables.Add(Disposable.Create(() => uiViewModel.CaptionHeight = oldCaptionHeight));
            }
        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            disposables.Dispose();
        }

        private bool IsCollapsed(bool boolean) => boolean ? false : true;

        private DateTime prevLeftButtonPressedTime = DateTime.Now;

        private void OnMenuBarPointerEvent(object sender, PointerEventArgs e)
        {
            // TODO: Re-implement window drag/title bar interaction for Avalonia
            // Original code used WinUI-specific PInvoke, XamlWindow, and Dispatcher.RunIdleAsync
        }
    }
}
