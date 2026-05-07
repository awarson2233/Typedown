using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Reactive.Disposables;
using System.Threading.Tasks;

namespace Typedown.Core.Services
{
    public class RemoteInvoke : IDisposable
    {
        private record Handler(object Source, Func<JToken?, Task<object?>> Func);

        private readonly Dictionary<string, Handler> handlerDic = new();

        public IDisposable Handle(string name, Action handler)
        {
            handlerDic[name] = new(handler, _ =>
            {
                handler();
                return Task.FromResult<object?>(null);
            });
            return Disposable.Create(() => RemoveHandle(name, handler));
        }

        public IDisposable Handle<T>(string name, Action<T> handler)
        {
            handlerDic[name] = new(handler, x =>
            {
                handler(ReadArgument<T>(x, name));
                return Task.FromResult<object?>(null);
            });
            return Disposable.Create(() => RemoveHandle(name, handler));
        }

        public IDisposable Handle<T, TResult>(string name, Func<T, TResult> handler)
        {
            handlerDic[name] = new(handler, x => Task.FromResult<object?>(handler(ReadArgument<T>(x, name))));
            return Disposable.Create(() => RemoveHandle(name, handler));
        }

        public IDisposable Handle<T, TResult>(string name, Func<T, Task<TResult>> handler)
        {
            handlerDic[name] = new(handler, async x => await handler(ReadArgument<T>(x, name)));
            return Disposable.Create(() => RemoveHandle(name, handler));
        }

        public IDisposable Handle<TResult>(string name, Func<TResult> handler)
        {
            handlerDic[name] = new(handler, _ => Task.FromResult<object?>(handler()));
            return Disposable.Create(() => RemoveHandle(name, handler));
        }

        public IDisposable Handle<TResult>(string name, Func<Task<TResult>> handler)
        {
            handlerDic[name] = new(handler, async _ => await handler());
            return Disposable.Create(() => RemoveHandle(name, handler));
        }

        public void RemoveHandle(string name, object handler)
        {
            if (handlerDic.TryGetValue(name, out var val) && val.Source == handler)
                handlerDic.Remove(name);
        }

        public async Task<object?> Invoke(string name, JToken? args)
        {
            if (handlerDic.TryGetValue(name, out var handler))
                return await handler.Func(args);
            throw new Exception($"function [{name}] does not exist");
        }

        private static T ReadArgument<T>(JToken? args, string name)
        {
            if (args == null)
                throw new InvalidOperationException($"function [{name}] requires a valid argument payload");

            var value = args.ToObject<T>();
            if (value == null && default(T) is null)
                throw new InvalidOperationException($"function [{name}] requires a valid argument payload");
            return value!;
        }

        public void Dispose()
        {
            handlerDic.Clear();
        }
    }
}
