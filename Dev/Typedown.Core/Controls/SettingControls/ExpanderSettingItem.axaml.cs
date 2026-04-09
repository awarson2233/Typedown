using PropertyChanged;
﻿using Avalonia.Controls;
using Avalonia;
using Avalonia.Interactivity;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Presenters;
using Avalonia.Markup.Xaml;
using Avalonia.Media;

namespace Typedown.Core.Controls
{
    [DoNotNotify]
        public sealed partial class ExpanderSettingItem : UserControl
    {
        public static readonly StyledProperty<object> TitleProperty = AvaloniaProperty.Register<ExpanderSettingItem, object>(nameof(Title), null);
        public object Title { get => GetValue(TitleProperty); set => SetValue(TitleProperty, value); }

        public static readonly StyledProperty<object> DescriptionProperty = AvaloniaProperty.Register<ExpanderSettingItem, object>(nameof(Description), null);
        public object Description { get => GetValue(DescriptionProperty); set => SetValue(DescriptionProperty, value); }

        public static readonly StyledProperty<object> StateProperty = AvaloniaProperty.Register<ExpanderSettingItem, object>(nameof(State), null);
        public object State { get => GetValue(StateProperty); set => SetValue(StateProperty, value); }

        public static readonly StyledProperty<object> ActionProperty = AvaloniaProperty.Register<ExpanderSettingItem, object>(nameof(Action), null);
        public object Action { get => GetValue(ActionProperty); set => SetValue(ActionProperty, value); }

        // TODO: IconElement doesn't exist in Avalonia - use object or IImage
        public static readonly StyledProperty<object> IconProperty = AvaloniaProperty.Register<ExpanderSettingItem, object>(nameof(Icon), null);
        public object Icon { get => GetValue(IconProperty); set => SetValue(IconProperty, value); }

        public ExpanderSettingItem()
        {
            InitializeComponent();
        }

        private void OnSizeChanged(object sender, SizeChangedEventArgs e)
        {
            // TODO: Re-implement for Avalonia - ActualWidth/ActualOffset/FrameworkElement don't exist
            // Original code adjusted ContentPresenter_Expander margin/width based on parent offset
            var contentPresenter = this.FindControl<ContentPresenter>("ContentPresenter_Expander");
            if (contentPresenter != null && sender is Expander expander)
            {
                contentPresenter.Width = expander.Bounds.Width;
            }
        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
        }
    }
}
