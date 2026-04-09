using Avalonia.Layout;
using PropertyChanged;
﻿using Avalonia;
using Avalonia.Interactivity;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Markup.Xaml;

namespace Typedown.Core.Controls
{
    [DoNotNotify]
        public sealed partial class NormalSettingItem : UserControl
    {
        public static readonly StyledProperty<object> TitleProperty = AvaloniaProperty.Register<NormalSettingItem, object>(nameof(Title), null);
        public object Title { get => GetValue(TitleProperty); set => SetValue(TitleProperty, value); }

        public static readonly StyledProperty<object> DescriptionProperty = AvaloniaProperty.Register<NormalSettingItem, object>(nameof(Description), null);
        public object Description { get => GetValue(DescriptionProperty); set => SetValue(DescriptionProperty, value); }

        public static readonly StyledProperty<object> ActionProperty = AvaloniaProperty.Register<NormalSettingItem, object>(nameof(Action), null);
        public object Action { get => GetValue(ActionProperty); set => SetValue(ActionProperty, value); }

        // TODO: IconElement doesn't exist in Avalonia - use object or IImage
        public static readonly StyledProperty<object> IconProperty = AvaloniaProperty.Register<NormalSettingItem, object>(nameof(Icon), null);
        public object Icon { get => GetValue(IconProperty); set => SetValue(IconProperty, value); }

        public static readonly StyledProperty<HorizontalAlignment> HorizontalActionAlignmentProperty = AvaloniaProperty.Register<NormalSettingItem, HorizontalAlignment>(nameof(HorizontalActionAlignment), HorizontalAlignment.Right);
        public HorizontalAlignment HorizontalActionAlignment { get => GetValue(HorizontalActionAlignmentProperty); set => SetValue(HorizontalActionAlignmentProperty, value); }

        public NormalSettingItem()
        {
            InitializeComponent();
        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
        }
    }
}
