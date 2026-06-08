using System;
using System.Collections.Generic;
using Microsoft.Windows.AppLifecycle;

namespace Typedown.WinUI.Services
{
    internal sealed class WinUIActivationBroker : IDisposable
    {
        private readonly AppInstance appInstance;
        private readonly object gate = new();
        private readonly Queue<AppActivationArguments> pendingActivations = new();
        private EventHandler<AppActivationArguments>? activationReceived;
        private bool disposed;

        public WinUIActivationBroker(AppInstance appInstance)
        {
            this.appInstance = appInstance ?? throw new ArgumentNullException(nameof(appInstance));
            this.appInstance.Activated += OnAppInstanceActivated;
        }

        public event EventHandler<AppActivationArguments> ActivationReceived
        {
            add
            {
                lock (gate)
                {
                    ObjectDisposedException.ThrowIf(disposed, this);
                    activationReceived += value;
                }
            }
            remove
            {
                lock (gate)
                {
                    activationReceived -= value;
                }
            }
        }

        public void FlushPendingActivations()
        {
            while (true)
            {
                AppActivationArguments args;
                EventHandler<AppActivationArguments>? handler;

                lock (gate)
                {
                    if (disposed || activationReceived is null || pendingActivations.Count == 0)
                    {
                        return;
                    }

                    args = pendingActivations.Dequeue();
                    handler = activationReceived;
                }

                handler.Invoke(this, args);
            }
        }

        public void Dispose()
        {
            lock (gate)
            {
                if (disposed)
                {
                    return;
                }

                disposed = true;
                activationReceived = null;
                pendingActivations.Clear();
            }

            appInstance.Activated -= OnAppInstanceActivated;
        }

        private void OnAppInstanceActivated(object? sender, AppActivationArguments args)
        {
            EventHandler<AppActivationArguments>? handler;

            lock (gate)
            {
                if (disposed)
                {
                    return;
                }

                handler = activationReceived;
                if (handler is null)
                {
                    pendingActivations.Enqueue(args);
                    return;
                }
            }

            handler.Invoke(this, args);
        }
    }
}
