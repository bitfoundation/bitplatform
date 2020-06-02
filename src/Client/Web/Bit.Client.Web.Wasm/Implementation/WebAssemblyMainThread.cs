using System;
using System.Threading;
using System.Threading.Tasks;
using Xamarin.Essentials.Interfaces;

namespace Bit.Client.Web.Wasm.Implementation
{
    public class WebAssemblyMainThread : IMainThread
    {
        public virtual bool IsMainThread => true;

        public virtual void BeginInvokeOnMainThread(Action action)
        {
            action();
        }

        public virtual Task<SynchronizationContext> GetMainThreadSynchronizationContextAsync()
        {
            return Task.FromResult(SynchronizationContext.Current);
        }

        public virtual Task InvokeOnMainThreadAsync(Action action)
        {
            action();

            return Task.CompletedTask;
        }

        public virtual Task<T> InvokeOnMainThreadAsync<T>(Func<T> func)
        {
            return Task.FromResult(func());
        }

        public virtual Task InvokeOnMainThreadAsync(Func<Task> funcTask)
        {
            return funcTask();
        }

        public virtual Task<T> InvokeOnMainThreadAsync<T>(Func<Task<T>> funcTask)
        {
            return funcTask();
        }
    }
}
