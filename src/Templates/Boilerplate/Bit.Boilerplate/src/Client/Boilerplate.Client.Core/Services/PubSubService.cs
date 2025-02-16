using System.Reflection;

namespace Boilerplate.Client.Core.Services;

/// <summary>
/// Service for Publish/Subscribe pattern.
/// You could publish messages within blazor components, pages outside blazor components (Like MAUI Xaml pages), JavaScript
/// codes using window.postMessage or even from server side using SignalR (<see cref="SharedPubSubMessages.SESSION_REVOKED"/> as example.
/// </summary>
public partial class PubSubService
{
    // Handlers are stored per message as a list of weak subscriptions.
    private readonly ConcurrentDictionary<string, List<WeakSubscription>> handlers = [];

    /// <summary>
    /// Messages that were published before any handler was subscribed.
    /// </summary>
    private readonly ConcurrentBag<(string message, object? payload)> persistentMessages = [];

    [AutoInject] private readonly IServiceProvider serviceProvider = default!;

    public void Publish(string message, object? payload = null, bool persistent = false)
    {
        if (handlers.TryGetValue(message, out var weakHandlers))
        {
            foreach (var weakHandler in weakHandlers.ToArray())
            {
                if (weakHandler.IsAlive)
                {
                    weakHandler.Invoke(payload)?.ContinueWith(HandleException, TaskContinuationOptions.OnlyOnFaulted);
                }
            }
        }
        else if (persistent)
        {
            persistentMessages.Add((message, payload));
        }
    }

    public Action Subscribe(string message, Func<object?, Task> handler)
    {
        var weakHandler = new WeakSubscription(handler);
        var weakHandlers = handlers.GetOrAdd(message, _ => []);
        weakHandlers.Add(weakHandler);

        // If persistent messages exist for this message, publish them immediately.
        foreach (var (notHandledMessage, payload) in persistentMessages)
        {
            if (notHandledMessage == message)
            {
                weakHandler.Invoke(payload)?.ContinueWith(HandleException, TaskContinuationOptions.OnlyOnFaulted);
                persistentMessages.TryTake(out _);
            }
        }

        return () =>
        {
            var removedHandlersCount = weakHandlers.RemoveAll(wh => wh.Matches(handler));
        };
    }

    private void HandleException(Task t)
    {
        serviceProvider.GetRequiredService<IExceptionHandler>().Handle(t.Exception!);
    }

    private class WeakSubscription
    {
        private string targetInfo;
        private readonly bool isStatic;
        private readonly MethodInfo method;
        private readonly WeakReference? target;

        public WeakSubscription(Func<object?, Task> handler)
        {
            isStatic = handler.Target is null;
            if (isStatic is false)
            {
                target = new WeakReference(handler.Target);
            }
            method = handler.Method;
            targetInfo = $"{(handler.Target?.GetType().FullName ?? "static")}'s {method.Name}";
        }

        public bool IsAlive => isStatic || target?.IsAlive is true;

        /// <summary>
        /// Invokes the stored handler if it is still alive.
        /// Returns the Task from the handler or null if the target is no longer available.
        /// </summary>
        public Task? Invoke(object? payload)
        {
            if (isStatic)
            {
                return (Task?)method.Invoke(null, [payload]);
            }
            else
            {
                if (this.target?.Target is object target)
                {
                    return (Task?)method.Invoke(target, [payload]);
                }
            }
            return null;
        }

        /// <summary>
        /// Checks if the given handler matches this weak subscription.
        /// </summary>
        public bool Matches(Func<object?, Task> handler)
        {
            if (isStatic)
            {
                return handler.Target is null && handler.Method.Equals(method);
            }
            else
            {
                return handler.Target is not null &&
                       ReferenceEquals(handler.Target, target?.Target) &&
                       handler.Method.Equals(method);
            }
        }

        public override string ToString()
        {
            return targetInfo;
        }
    }
}
