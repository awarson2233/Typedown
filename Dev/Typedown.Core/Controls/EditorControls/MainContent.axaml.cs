using PropertyChanged;
using System;
using System.Collections.ObjectModel;
using Avalonia.Input;
using Avalonia.Metadata;
using Avalonia.Data.Converters;
using System.ComponentModel;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using Typedown.Core.Utilities;
using Typedown.Core.ViewModels;
using Avalonia;
using Avalonia.Interactivity;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Media;

namespace Typedown.Core.Controls
{
[DoNotNotify]
        public sealed partial class MainContent : UserControl, INotifyPropertyChanged
    {
        public static readonly StyledProperty<bool> IsLeftPaneLoadProperty = AvaloniaProperty.Register<MainContent, bool>(nameof(IsLeftPaneLoad), false);
        private bool IsLeftPaneLoad { get => GetValue(IsLeftPaneLoadProperty); set => SetValue(IsLeftPaneLoadProperty, value); }

        public static readonly StyledProperty<double> LeftPaneMaxWidthProperty = AvaloniaProperty.Register<MainContent, double>(nameof(LeftPaneMaxWidth), 0d);
        private double LeftPaneMaxWidth { get => GetValue(LeftPaneMaxWidthProperty); set => SetValue(LeftPaneMaxWidthProperty, value); }

        public AppViewModel ViewModel => DataContext as AppViewModel;

        public SettingsViewModel Settings => ViewModel?.SettingsViewModel;

        private readonly CompositeDisposable disposables = new();

        public MainContent()
        {
            InitializeComponent();
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            disposables.Add(Settings.WhenPropertyChanged(nameof(Settings.SidePaneOpen)).Cast<bool>().Subscribe(x => UpdateSidePaneState(x, true)));
            // TODO: Requires SettingsViewModel.UseEditorMicaEffect (not yet available)
            // disposables.Add(Settings.WhenPropertyChanged(nameof(Settings.UseEditorMicaEffect)).Cast<bool>().StartWith(Settings.UseEditorMicaEffect).Subscribe(x => UpdateBackground(x)));
            UpdateSidePaneState(Settings.SidePaneOpen, false);
        }

        private void UpdateSidePaneState(bool sidePaneOpen, bool useTransitions = true)
        {
            // TODO: VisualStateManager.GoToState not available in Avalonia
            // VisualStateManager.GoToState(this, sidePaneOpen ? "SidePaneExpand" : "SidePaneCollapse", useTransitions && Settings.AnimationEnable);
        }

        private void UpdateBackground(bool useMica)
        {
            var mainContentGrid = this.FindControl<Grid>("MainContentGrid");
            if (mainContentGrid != null) mainContentGrid.Background = Resources[useMica ? "MicaContentBackgroundBrush" : "SolidContentBackgroundBrush"] as Brush;
        }

        [SuppressPropertyChangedWarnings]
        private void OnSizeChanged(object sender, SizeChangedEventArgs e)
        {
            LeftPaneMaxWidth = Bounds.Width - 40;
        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            disposables.Clear();
        }

        public static double GetColumnWidthNegative(GridLength length)
        {
            return -length.Value;
        }
    }
}
