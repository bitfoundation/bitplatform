using Prism.Mvvm;
using Prism.Navigation;
using System;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Bit.ViewModel
{
    public struct NoContextYieldAwaitable
    {
        public NoContextYieldAwaiter GetAwaiter()
        {
            return new NoContextYieldAwaiter();
        }

        public struct NoContextYieldAwaiter : INotifyCompletion
        {
            public bool IsCompleted { get { return false; } }

            public void OnCompleted(Action continuation)
            {
                TaskScheduler scheduler = TaskScheduler.Current;
                if (scheduler == TaskScheduler.Default)
                {
                    ThreadPool.QueueUserWorkItem(RunAction, continuation);
                }
                else
                {
                    Task.Factory.StartNew(continuation, CancellationToken.None, TaskCreationOptions.PreferFairness, scheduler);
                }
            }

            public void GetResult()
            {
            }

            private static void RunAction(object state)
            {
                ((Action)state)();
            }
        }
    }

    public class BitViewModelBase : BindableBase, INavigatedAware, INavigatingAware, INavigationAware, IDestructible
    {
        public async void Destroy()
        {
            try
            {
                await DestroyAsync().ConfigureAwait(false);
                await new NoContextYieldAwaitable();
            }
            catch (Exception exp)
            {
                BitExceptionHandler.Current.OnExceptionReceived(exp);
            }
        }

        public virtual Task DestroyAsync()
        {
            return Task.CompletedTask;
        }

        public async void OnNavigatedFrom(INavigationParameters parameters)
        {
            try
            {
                await OnNavigatedFromAsync(parameters).ConfigureAwait(false);
                await new NoContextYieldAwaitable();
            }
            catch (Exception exp)
            {
                BitExceptionHandler.Current.OnExceptionReceived(exp);
            }
        }

        public virtual Task OnNavigatedFromAsync(INavigationParameters parameters)
        {
            return Task.CompletedTask;
        }

        public async void OnNavigatedTo(INavigationParameters parameters)
        {
            try
            {
                await new NoContextYieldAwaitable();
                await OnNavigatedToAsync(parameters).ConfigureAwait(false);
            }
            catch (Exception exp)
            {
                BitExceptionHandler.Current.OnExceptionReceived(exp);
            }
        }

        public virtual Task OnNavigatedToAsync(INavigationParameters parameters)
        {
            return Task.CompletedTask;
        }

        public async void OnNavigatingTo(INavigationParameters parameters)
        {
            try
            {
                await new NoContextYieldAwaitable();
                await OnNavigatingToAsync(parameters).ConfigureAwait(false);
            }
            catch (Exception exp)
            {
                BitExceptionHandler.Current.OnExceptionReceived(exp);
            }
        }

        public virtual Task OnNavigatingToAsync(INavigationParameters parameters)
        {
            return Task.CompletedTask;
        }
    }
}
