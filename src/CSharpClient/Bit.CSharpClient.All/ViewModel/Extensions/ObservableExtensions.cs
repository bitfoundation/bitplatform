using Bit.ViewModel;
using System.Threading;
using System.Threading.Tasks;

namespace System
{
    public static class ObservableExtensions
    {
        public static IDisposable SubscribeAsync<T>(this IObservable<T> source, Func<T, Task> onNext)
        {
            return source.Subscribe(onNext: async (arg) =>
            {
                try
                {
                    await onNext(arg);
                }
                catch (Exception exp)
                {
                    BitExceptionHandler.Current.OnExceptionReceived(exp);
                }
            });
        }

        public static IDisposable SubscribeAsync<T>(this IObservable<T> source, Func<T, Task> onNext, Func<Exception, Task> onError)
        {
            return source.Subscribe(onNext: async (arg) =>
            {
                try
                {
                    await onNext(arg);
                }
                catch (Exception exp)
                {
                    BitExceptionHandler.Current.OnExceptionReceived(exp);
                }
            }, onError: async (originalExp) =>
            {
                try
                {
                    await onError(originalExp);
                }
                catch (Exception exp)
                {
                    BitExceptionHandler.Current.OnExceptionReceived(new AggregateException(exp, originalExp));
                }
            });
        }

        public static IDisposable SubscribeAsync<T>(this IObservable<T> source, Func<T, Task> onNext, Func<Task> onCompleted)
        {
            return source.Subscribe(onNext: async (arg) =>
            {
                try
                {
                    await onNext(arg);
                }
                catch (Exception exp)
                {
                    BitExceptionHandler.Current.OnExceptionReceived(exp);
                }
            }, onCompleted: async () =>
            {
                try
                {
                    await onCompleted();
                }
                catch (Exception exp)
                {
                    BitExceptionHandler.Current.OnExceptionReceived(exp);
                }
            });
        }

        public static IDisposable SubscribeAsync<T>(this IObservable<T> source, Func<T, Task> onNext, Func<Exception, Task> onError, Func<Task> onCompleted)
        {
            return source.Subscribe(onNext: async (arg) =>
            {
                try
                {
                    await onNext(arg);
                }
                catch (Exception exp)
                {
                    BitExceptionHandler.Current.OnExceptionReceived(exp);
                }
            }, onError: async (originalExp) =>
            {
                try
                {
                    await onError(originalExp);
                }
                catch (Exception exp)
                {
                    BitExceptionHandler.Current.OnExceptionReceived(new AggregateException(exp, originalExp));
                }
            }, onCompleted: async () =>
            {
                try
                {
                    await onCompleted();
                }
                catch (Exception exp)
                {
                    BitExceptionHandler.Current.OnExceptionReceived(exp);
                }
            });
        }

        public static void SubscribeAsync<T>(this IObservable<T> source, Func<T, Task> onNext, CancellationToken token)
        {
            source.Subscribe(onNext: async (arg) =>
            {
                try
                {
                    await onNext(arg);
                }
                catch (Exception exp)
                {
                    BitExceptionHandler.Current.OnExceptionReceived(exp);
                }
            }, token: token);
        }

        public static void SubscribeAsync<T>(this IObservable<T> source, Func<T, Task> onNext, Func<Exception, Task> onError, CancellationToken token)
        {
            source.Subscribe(onNext: async (arg) =>
            {
                try
                {
                    await onNext(arg);
                }
                catch (Exception exp)
                {
                    BitExceptionHandler.Current.OnExceptionReceived(exp);
                }
            }, onError: async (originalExp) =>
            {
                try
                {
                    await onError(originalExp);
                }
                catch (Exception exp)
                {
                    BitExceptionHandler.Current.OnExceptionReceived(new AggregateException(exp, originalExp));
                }
            }, token: token);
        }

        public static void SubscribeAsync<T>(this IObservable<T> source, Func<T, Task> onNext, Func<Task> onCompleted, CancellationToken token)
        {
            source.Subscribe(onNext: async (arg) =>
            {
                try
                {
                    await onNext(arg);
                }
                catch (Exception exp)
                {
                    BitExceptionHandler.Current.OnExceptionReceived(exp);
                }
            }, onCompleted: async () =>
            {
                try
                {
                    await onCompleted();
                }
                catch (Exception exp)
                {
                    BitExceptionHandler.Current.OnExceptionReceived(exp);
                }
            }, token: token);
        }

        public static void SubscribeAsync<T>(this IObservable<T> source, Func<T, Task> onNext, Func<Exception, Task> onError, Func<Task> onCompleted, CancellationToken token)
        {
            source.Subscribe(onNext: async (arg) =>
            {
                try
                {
                    await onNext(arg);
                }
                catch (Exception exp)
                {
                    BitExceptionHandler.Current.OnExceptionReceived(exp);
                }
            }, onError: async (originalExp) =>
            {
                try
                {
                    await onError(originalExp);
                }
                catch (Exception exp)
                {
                    BitExceptionHandler.Current.OnExceptionReceived(new AggregateException(exp, originalExp));
                }
            }, onCompleted: async () =>
            {
                try
                {
                    await onCompleted();
                }
                catch (Exception exp)
                {
                    BitExceptionHandler.Current.OnExceptionReceived(exp);
                }
            }, token: token);
        }
    }
}
