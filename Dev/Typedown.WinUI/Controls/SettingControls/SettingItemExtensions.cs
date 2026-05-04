using System;
using System.Reactive.Linq;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;
using Typedown.Presentation.ViewModels;
using Windows.Storage.Pickers;
using WinRT.Interop;

namespace Typedown.WinUI.Controls
{
    internal static class SettingItemExtensions
    {
        public static T GetService<T>(this FrameworkElement element)
            where T : notnull
        {
            return element.FindAppViewModel().ServiceProvider.GetRequiredService<T>();
        }

        public static T? GetAncestor<T>(this FrameworkElement element, string? name = null)
            where T : FrameworkElement
        {
            var parent = element.Parent as FrameworkElement;
            while (parent != null)
            {
                if (parent is T matched && (name == null || matched.Name == name))
                {
                    return matched;
                }

                parent = parent.Parent as FrameworkElement;
            }

            return null;
        }

        public static IObservable<object?> Binding<T>(this T source, PropertyPath path)
            where T : DependencyObject
        {
            var bindingSource = new SettingBindingSource();
            BindingOperations.SetBinding(
                bindingSource,
                SettingBindingSource.ValueProperty,
                new Binding { Source = source, Path = path });

            return Observable
                .FromEventPattern<DependencyPropertyChangedEventHandler, DependencyPropertyChangedEventArgs>(
                    handler => bindingSource.ValueChanged += handler,
                    handler => bindingSource.ValueChanged -= handler)
                .Select(args => args.EventArgs.NewValue);
        }

        public static void SetOwnerWindow(this FileOpenPicker picker, nint hwnd)
        {
            InitializeWithWindow.Initialize(picker, hwnd);
        }

        public static void SetOwnerWindow(this FileSavePicker picker, nint hwnd)
        {
            InitializeWithWindow.Initialize(picker, hwnd);
        }

        public static void SetOwnerWindow(this FolderPicker picker, nint hwnd)
        {
            InitializeWithWindow.Initialize(picker, hwnd);
        }

        private static AppViewModel FindAppViewModel(this FrameworkElement element)
        {
            var current = element;
            while (current != null)
            {
                if (current.DataContext is AppViewModel appViewModel)
                {
                    return appViewModel;
                }

                current = current.Parent as FrameworkElement;
            }

            throw new InvalidOperationException("Settings controls require an AppViewModel DataContext.");
        }

        private sealed class SettingBindingSource : DependencyObject
        {
            public static readonly DependencyProperty ValueProperty =
                DependencyProperty.Register(nameof(Value), typeof(object), typeof(SettingBindingSource), new PropertyMetadata(null, OnValueChanged));

            public event DependencyPropertyChangedEventHandler? ValueChanged;

            public object? Value
            {
                get => GetValue(ValueProperty);
                set => SetValue(ValueProperty, value);
            }

            private static void OnValueChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs args)
            {
                ((SettingBindingSource)dependencyObject).ValueChanged?.Invoke(dependencyObject, args);
            }
        }
    }
}
