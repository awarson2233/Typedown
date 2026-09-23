using System;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Reactive;
using System.Reactive.Linq;

namespace Typedown.Core.Utilities
{
    public static class ReactiveExtensions
    {
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

        /// <summary>属性 <paramref name="propertyName"/> 每变化一次发一个信号，不取值。</summary>
        public static IObservable<Unit> WhenPropertyChanged<T>(this T source, string propertyName)
            where T : INotifyPropertyChanged
        {
            return source.GetPropertyObservable()
                .Where(x => x.EventArgs.PropertyName == propertyName)
                .Select(_ => Unit.Default);
        }

        /// <summary>
        /// 属性 <paramref name="propertyName"/> 每变化一次，发出 <paramref name="getValue"/> 读到的新值。
        /// 取值由调用方给出，不经反射，所以在裁剪与 Native AOT 下同样可用。
        /// </summary>
        public static IObservable<TValue> WhenPropertyChanged<T, TValue>(this T source, string propertyName, Func<T, TValue> getValue)
            where T : INotifyPropertyChanged
        {
            return source.GetPropertyObservable()
                .Where(x => x.EventArgs.PropertyName == propertyName)
                .Select(_ => getValue(source));
        }
    }
}
