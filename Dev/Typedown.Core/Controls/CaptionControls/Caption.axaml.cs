using PropertyChanged;
﻿using System;
using System.Collections.ObjectModel;
using Avalonia.Input;
using Avalonia.Metadata;
using Avalonia.Data.Converters;
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
        public sealed partial class Caption : UserControl
    {
        public AppViewModel ViewModel => DataContext as AppViewModel;

        public SettingsViewModel Settings => ViewModel?.SettingsViewModel;

        private readonly CompositeDisposable disposables = new();

        public Caption()
        {
            InitializeComponent();
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            // TODO: Re-implement back button state observation for Avalonia
            // The original code used WhenPropertyChanged + VisualStateManager which
            // are WinUI-specific. Need to be replaced with Avalonia equivalents.
            // disposables.Add(ViewModel.GoBackCommand
            //     .WhenPropertyChanged(nameof(ViewModel.GoBackCommand.IsExecutable))
            //     .Cast<bool>()
            //     .Subscribe(x => UpdateBackButtonState(x)));
            // UpdateBackButtonState(ViewModel.GoBackCommand.IsExecutable, false);
        }

        private void UpdateBackButtonState(bool canGoBack, bool useTransitions = true)
        {
            var backButton = this.FindControl<Button>("BackButton");
            if (backButton != null)
            {
                backButton.IsVisible = canGoBack;
            }
            var titlePanel = this.FindControl<Grid>("TitlePanel");
            if (titlePanel != null)
            {
                titlePanel.Margin = canGoBack ? new Thickness(4, 0, 0, 0) : new Thickness(12, 0, 0, 0);
            }
        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            disposables.Clear();
        }
    }
}
