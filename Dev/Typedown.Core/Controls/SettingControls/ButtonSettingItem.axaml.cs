using Avalonia.Layout;
using PropertyChanged;
﻿using System;
using System.Collections.ObjectModel;
using Avalonia.Input;
using Avalonia.Metadata;
using Avalonia.Data.Converters;
using System.Windows.Input;
using Avalonia;
using Avalonia.Interactivity;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Markup.Xaml;

namespace Typedown.Core.Controls
{
    [DoNotNotify]
        public sealed partial class ButtonSettingItem : UserControl
    {
        public static readonly StyledProperty<object> TitleProperty = AvaloniaProperty.Register<ButtonSettingItem, object>(nameof(Title), null);
        public object Title { get => GetValue(TitleProperty); set => SetValue(TitleProperty, value); }

        public static readonly StyledProperty<object> DescriptionProperty = AvaloniaProperty.Register<ButtonSettingItem, object>(nameof(Description), null);
        public object Description { get => GetValue(DescriptionProperty); set => SetValue(DescriptionProperty, value); }

        public static readonly StyledProperty<object> ActionProperty = AvaloniaProperty.Register<ButtonSettingItem, object>(nameof(Action), null);
        public object Action { get => GetValue(ActionProperty); set => SetValue(ActionProperty, value); }

        // TODO: IconElement doesn't exist in Avalonia - use object or IImage
        public static readonly StyledProperty<object> IconProperty = AvaloniaProperty.Register<ButtonSettingItem, object>(nameof(Icon), null);
        public object Icon { get => GetValue(IconProperty); set => SetValue(IconProperty, value); }

        public static readonly StyledProperty<HorizontalAlignment> HorizontalActionAlignmentProperty = AvaloniaProperty.Register<ButtonSettingItem, HorizontalAlignment>(nameof(HorizontalActionAlignment), HorizontalAlignment.Right);
        public HorizontalAlignment HorizontalActionAlignment { get => GetValue(HorizontalActionAlignmentProperty); set => SetValue(HorizontalActionAlignmentProperty, value); }

        public static readonly StyledProperty<ICommand> CommandProperty = AvaloniaProperty.Register<ButtonSettingItem, ICommand>(nameof(Command), null);
        public ICommand Command { get => GetValue(CommandProperty); set => SetValue(CommandProperty, value); }

        public static readonly StyledProperty<object> CommandParameterProperty = AvaloniaProperty.Register<ButtonSettingItem, object>(nameof(CommandParameter), null);
        public object CommandParameter { get => GetValue(CommandParameterProperty); set => SetValue(CommandParameterProperty, value); }

        public event EventHandler Click;

        public ButtonSettingItem()
        {
            InitializeComponent();
        }

        private void OnClick(object sender, RoutedEventArgs e)
        {
            Click?.Invoke(this, EventArgs.Empty);
        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
        }
    }
}
