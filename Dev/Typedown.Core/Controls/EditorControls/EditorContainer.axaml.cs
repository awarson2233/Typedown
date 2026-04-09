using PropertyChanged;
﻿using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.ObjectModel;
using Avalonia.Metadata;
using Avalonia.Data.Converters;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using Typedown.Core.Interfaces;
using Typedown.Core.Models;
using Typedown.Core.Utilities;
using Typedown.Core.ViewModels;
using Avalonia;
using Avalonia.Interactivity;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Presenters;
using Avalonia.Input;

namespace Typedown.Core.Controls
{
[DoNotNotify]
        public sealed partial class EditorContainer : UserControl
    {
        public static readonly StyledProperty<bool> IsFindReplaceLoadProperty = AvaloniaProperty.Register<EditorContainer, bool>(nameof(IsFindReplaceLoad), false);
        private bool IsFindReplaceLoad { get => GetValue(IsFindReplaceLoadProperty); set => SetValue(IsFindReplaceLoadProperty, value); }

        public static readonly StyledProperty<Point> FindReplaceCenterPointProperty = AvaloniaProperty.Register<EditorContainer, Point>(nameof(FindReplaceCenterPoint), default);
        private Point FindReplaceCenterPoint { get => GetValue(FindReplaceCenterPointProperty); set => SetValue(FindReplaceCenterPointProperty, value); }

        public static readonly StyledProperty<ScrollState> ScrollStateProperty = AvaloniaProperty.Register<EditorContainer, ScrollState>(nameof(ScrollState), default);
        private ScrollState ScrollState { get => GetValue(ScrollStateProperty); set => SetValue(ScrollStateProperty, value); }

        public AppViewModel ViewModel => DataContext as AppViewModel;
        public FloatViewModel Float => ViewModel?.FloatViewModel;
        public EditorViewModel Editor => ViewModel?.EditorViewModel;
        public SettingsViewModel Settings => ViewModel?.SettingsViewModel;
        public FormatViewModel Format => ViewModel?.FormatViewModel;

        private readonly CompositeDisposable disposables = new();

        public EditorContainer()
        {
            InitializeComponent();
            AddHandler(PointerMovedEvent, new EventHandler<PointerEventArgs>(OnPointerPointerMoved), Avalonia.Interactivity.RoutingStrategies.Tunnel);
            AddHandler(PointerWheelChangedEvent, new EventHandler<PointerWheelEventArgs>(OnPointerWheelChanged), Avalonia.Interactivity.RoutingStrategies.Tunnel);
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            var markdownEditorPresenter = this.FindControl<ContentPresenter>("MarkdownEditorPresenter");
            if (markdownEditorPresenter != null) markdownEditorPresenter.Content = this.GetService<IMarkdownEditor>();
            disposables.Add(Float.WhenPropertyChanged(nameof(Float.FindReplaceDialogOpen))
                .Cast<FloatViewModel.FindReplaceDialogState>()
                .Subscribe(x => UpdateFindReplaceState(x, true)));
            disposables.Add(Editor.EventCenter.GetObservable<EditorEventArgs>("OnScroll")
                .Subscribe(x => ScrollState = x.Args.ToObject<ScrollState>()));
            UpdateFindReplaceState(Float.FindReplaceDialogOpen, false);
        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            var markdownEditorPresenter = this.FindControl<ContentPresenter>("MarkdownEditorPresenter");
            if (markdownEditorPresenter != null) markdownEditorPresenter.Content = null;
            disposables.Clear();
        }

        private void UpdateFindReplaceState(FloatViewModel.FindReplaceDialogState findReplaceOpen, bool useTransitions = true)
        {
            FindReplaceCenterPoint = new(Bounds.Width / 2, findReplaceOpen switch
            {
                FloatViewModel.FindReplaceDialogState.Search => 32,
                FloatViewModel.FindReplaceDialogState.Replace => 52,
                _ => FindReplaceCenterPoint.Y
            });
            // TODO: VisualStateManager.GoToState not available in Avalonia
            // VisualStateManager.GoToState(this, findReplaceOpen != FloatViewModel.FindReplaceDialogState.None ? "FindReplaceVisible" : "FindReplaceCollapsed", useTransitions && Settings.AnimationEnable);
        }

        private void OnScroll(object sender, ScrollEventArgs e)
        {
            var hBar = this.FindControl<ScrollBar>("HorizontalScrollBar");
            var vBar = this.FindControl<ScrollBar>("VerticalScrollBar");
            ViewModel.MarkdownEditor.PostMessage("OnScroll", new { ScrollX = hBar?.Value ?? 0, ScrollY = vBar?.Value ?? 0 });
        }

        private bool IsScrollBarVisibility(double maximum)
        {
            return maximum <= 0 ? false : true;
        }

        private double GetSmallChange(double fontSize, double lineHeight)
        {
            return fontSize * lineHeight;
        }

        private void OnDragEnter(object sender, DragEventArgs e)
        {
            // TODO: Re-implement drag-enter for Avalonia DnD API
            // Original WinUI code used DataView, StorageItems, DragUIOverride
        }

        private void OnDrop(object sender, DragEventArgs e)
        {
            // TODO: Re-implement drop for Avalonia DnD API
            // Original WinUI code used DataView.GetStorageItemsAsync
        }

        private void OnPointerPointerMoved(object sender, PointerEventArgs e)
        {
            if (e.Pointer.Type == Avalonia.Input.PointerType.Touch)
            {
                // TODO: Avalonia scrollbar indicator mode
                // TODO: Avalonia scrollbar indicator mode
            }
            else
            {
                // TODO: Avalonia scrollbar indicator mode
                // TODO: Avalonia scrollbar indicator mode
            }
        }

        private void OnPointerWheelChanged(object sender, PointerWheelEventArgs e)
        {
            if (e.KeyModifiers.HasFlag(Avalonia.Input.KeyModifiers.Control))
            {
                var delta = (e as PointerWheelEventArgs)?.Delta.Y ?? 0;
                Settings.FontSize = Math.Max(8, Math.Min(48, Math.Round(Settings.FontSize * (1 + delta / 1200d), 1)));
            }
        }

        private void OnFormatItemClick(object sender, EventArgs e)
        {
            // TODO: Hide flyout in Avalonia
            // Flyout.Hide();
        }

        private void OnStyledPropertyChanged(AvaloniaPropertyChangedEventArgs e)
        {
            if (e.Property == ScrollStateProperty)
            {
                // TODO: Re-implement MoveDummyRectangle for Avalonia
                // Original code accessed MarkdownEditorPresenter.Content as IMarkdownEditor
            }
        }

        public static bool IsLoadImageMenu(bool isImageFormat, JToken selection)
        {
            return isImageFormat && (selection["selectedImage"]?.HasValues ?? false);
        }
    }
}
