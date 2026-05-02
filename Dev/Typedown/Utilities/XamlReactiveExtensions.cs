using System;
using System.Collections.Generic;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Runtime.CompilerServices;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Data;

namespace Typedown.Core.Utilities
{
    public static class XamlReactiveExtensions
    {
        private class ValueObject : DependencyObject
        {
            public static DependencyProperty ValueProperty = DependencyProperty.Register(
                nameof(Value), typeof(object), typeof(ValueObject),
                new(null, (d, e) => (d as ValueObject).ValueChanged?.Invoke(d, e)));

            public object Value { get => GetValue(ValueProperty); set => SetValue(ValueProperty, value); }

            public event EventHandler<DependencyPropertyChangedEventArgs> ValueChanged;

            public static ValueObject CreateBindingObject(object source, PropertyPath path)
            {
                var valueObject = new ValueObject();
                BindingOperations.SetBinding(valueObject, ValueProperty, new Binding() { Source = source, Path = path, Mode = BindingMode.TwoWay });
                return valueObject;
            }
        }

        private static readonly ConditionalWeakTable<object, HashSet<ValueObject>> valueObjectTable = new();

        public static IObservable<object> Binding<T>(this T source, PropertyPath path) where T : DependencyObject
        {
            return Observable.Create<object>(o =>
            {
                var valueObject = ValueObject.CreateBindingObject(source, path);
                void OnChanged(object sender, DependencyPropertyChangedEventArgs e) => o.OnNext(e.NewValue);
                valueObject.ValueChanged += OnChanged;
                if (valueObjectTable.TryGetValue(source, out var valueObjects))
                    valueObjects.Add(valueObject);
                else
                    valueObjectTable.Add(source, new() { valueObject });
                var sourceRef = new WeakReference<T>(source);
                var valueObjectRef = new WeakReference<ValueObject>(valueObject);
                return () =>
                {
                    if (valueObjectRef.TryGetTarget(out var v))
                    {
                        v.ValueChanged -= OnChanged;
                        if (sourceRef.TryGetTarget(out var s) && valueObjectTable.TryGetValue(s, out var vs))
                            vs.Remove(v);
                    }
                };
            });
        }

        public static void DisposeOnUnloaded(this FrameworkElement frameworkElement, Action<Action<IDisposable>> disposableActions)
        {
            var disposables = new CompositeDisposable();
            disposableActions((d) => disposables.Add(d));
            void OnUnloaded(object sender, RoutedEventArgs e)
            {
                disposables.Dispose();
                frameworkElement.Unloaded -= OnUnloaded;
            }
            frameworkElement.Unloaded += OnUnloaded;
        }
    }
}
