using PropertyChanged;
﻿using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using Typedown.Core.Utilities;
using Typedown.Core.ViewModels;
using Avalonia;
using Avalonia.Interactivity;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;

namespace Typedown.Core.Controls
{
[DoNotNotify]
        public sealed partial class LeftPane : UserControl
    {
        public static readonly StyledProperty<bool> IsSearchPaneOpenProperty = AvaloniaProperty.Register<LeftPane, bool>(nameof(IsSearchPaneOpen), false);
        public bool IsSearchPaneOpen { get => GetValue(IsSearchPaneOpenProperty); set => SetValue(IsSearchPaneOpenProperty, value); }

        public AppViewModel ViewModel => DataContext as AppViewModel;

        public SettingsViewModel Settings => ViewModel?.SettingsViewModel;

        private readonly CompositeDisposable disposables = new();

        public LeftPane()
        {
            InitializeComponent();
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            disposables.Add(Settings.WhenPropertyChanged(nameof(SettingsViewModel.SidePaneIndex))
                .Cast<int>()
                .StartWith(Settings.SidePaneIndex)
                .Subscribe(UpdateSelectedItem));
        }

        private void UpdateSelectedItem(int index)
        {
            // TODO: Implement NavigationView selection for Avalonia
        }

        private void OnSearchButtonClick(object sender, RoutedEventArgs e)
        {
            IsSearchPaneOpen = true;
        }

        private void OnSearchPaneClose(object sender, EventArgs e)
        {
            IsSearchPaneOpen = false;
        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            disposables.Clear();
        }

        private void OnSizeChanged(object sender, SizeChangedEventArgs e)
        {
            // TODO: Update FrameClip for Avalonia
        }
    }
}
