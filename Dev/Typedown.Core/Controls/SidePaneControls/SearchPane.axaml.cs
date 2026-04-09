using PropertyChanged;
﻿using System;
using System.Collections.ObjectModel;
using Avalonia.Input;
using Avalonia.Metadata;
using Avalonia.Data.Converters;
using Typedown.Core.Utilities;
using Avalonia;
using Avalonia.Interactivity;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Threading;

namespace Typedown.Core.Controls
{
[DoNotNotify]
        public sealed partial class SearchPane : UserControl
    {
        public event EventHandler Close;

        public SearchPane()
        {
            InitializeComponent();
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            var searchTextBox = this.FindControl<TextBox>("SearchTextBox");
            Dispatcher.UIThread.Post(() => searchTextBox?.Focus());
        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {

        }

        private void OnSearchTextBoxLostFocus(object sender, RoutedEventArgs e)
        {
            var searchTextBox = this.FindControl<TextBox>("SearchTextBox");
            if (string.IsNullOrEmpty(searchTextBox?.Text))
                Close?.Invoke(this, EventArgs.Empty);
        }

        private void OnCloseButtonClick(object sender, RoutedEventArgs e)
        {
            Close?.Invoke(this, EventArgs.Empty);
        }
    }
}
