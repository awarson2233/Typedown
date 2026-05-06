using System;
using System.Collections.Generic;
using System.Reactive.Linq;

namespace Typedown.Core.Services
{
    public class EventCenter : IDisposable
    {
        private readonly object gate = new();
        private readonly Dictionary<string, List<Action<object>>> handlersDictionary = new();

        public IObservable<TEventArgs> GetObservable<TEventArgs>(string name)
        {
            return Observable.Create<TEventArgs>(subscribe =>
            {
                void handler(object args) => subscribe.OnNext((TEventArgs)args);
                lock (gate)
                {
                    if (!handlersDictionary.TryGetValue(name, out var handlers))
                    {
                        handlers = new();
                        handlersDictionary.Add(name, handlers);
                    }

                    handlers.Add(handler);
                }

                return () =>
                {
                    lock (gate)
                    {
                        if (handlersDictionary.TryGetValue(name, out var handlers))
                        {
                            handlers.Remove(handler);
                            if (handlers.Count == 0)
                                handlersDictionary.Remove(name);
                        }
                    }
                };
            });
        }

        public void EmitEvent(string name, object args)
        {
            Action<object>[] handlers;

            lock (gate)
            {
                if (!handlersDictionary.TryGetValue(name, out var handlersList))
                    return;

                handlers = handlersList.ToArray();
            }

            foreach (var handler in handlers)
                handler(args);
        }

        public void Dispose()
        {
            lock (gate)
            {
                handlersDictionary.Clear();
            }
        }
    }
}
