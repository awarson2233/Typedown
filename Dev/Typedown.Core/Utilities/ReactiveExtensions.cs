using System;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Reactive;
using System.Reactive.Linq;

namespace Typedown.Core.Utilities
{
    public static class ReactiveExtensions
    {
        public static IDisposable SubscribeWeak<T>(this IObservable<T> observable, Action<T> onNext)
        {
            var targetRef = new WeakReference(onNext.Target);
            var method = onNext.Method;
            var param = Expression.Parameter(typeof(T));
            IDisposable? d = null;
            d = observable.Subscribe(x =>
            {
                if (targetRef.Target is object target)
                {
                    var body = Expression.Call(Expression.Constant(target), method, param);
                    var func = Expression.Lambda<Action<T>>(body, param).Compile();
                    func(x);
                }
                else
                {
                    d?.Dispose();
                }
            });
            return d;
        }

        public static IObservable<EventPattern<NotifyCollectionChangedEventArgs>> GetCollectionObservable(this INotifyCollectionChanged collection)
        {
            return Observable.FromEventPattern<NotifyCollectionChangedEventHandler, NotifyCollectionChangedEventArgs>(
                handler => (_, args) => handler(_, args),
                handler => collection.CollectionChanged += handler,
                handler => collection.CollectionChanged -= handler);
        }

        public static IObservable<EventPattern<PropertyChangedEventArgs>> GetPropertyObservable(this INotifyPropertyChanged obj)
        {
            return Observable.FromEventPattern<PropertyChangedEventHandler, PropertyChangedEventArgs>(
                handler => (_, args) => handler(_, args),
                handler => obj.PropertyChanged += handler,
                handler => obj.PropertyChanged -= handler);
        }

        public static IObservable<object?> WhenPropertyChanged<T>(this T source, string propertyName)
            where T : INotifyPropertyChanged
        {
            var property = ResolveRuntimeProperty(source, propertyName);
            return source.GetPropertyObservable()
                .Where(x => x.EventArgs.PropertyName == propertyName)
                .Select(_ => property.GetValue(source));
        }

        [UnconditionalSuppressMessage("Trimming", "IL2075", Justification = "Runtime property lookup is required to support derived instances passed through a base or interface generic type.")]
        private static System.Reflection.PropertyInfo ResolveRuntimeProperty(object source, string propertyName)
        {
            return source.GetType().GetProperty(propertyName) ?? throw new ArgumentException($"Property '{propertyName}' was not found.", nameof(propertyName));
        }
    }
}
