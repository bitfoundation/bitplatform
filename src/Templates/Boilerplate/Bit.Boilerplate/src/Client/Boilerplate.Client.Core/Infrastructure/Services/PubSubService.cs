namespace Boilerplate.Client.Core.Infrastructure.Services;

/// <summary>
/// Service for Publish/Subscribe pattern.
/// You could publish messages within blazor components, pages outside blazor components (Like MAUI Xaml pages), JavaScript
/// codes using window.postMessage, or - when SignalR is enabled - from the server side (<c>SESSION_REVOKED</c> as example).
/// </summary>
public partial class PubSubService
{
    // Handlers are stored per message as a list of weak subscriptions.
    private readonly ConcurrentDictionary<string, List<WeakHandler>> handlers = [];

    /// <summary>
    /// Messages that were published before any handler was subscribed.
    /// </summary>
    private readonly ConcurrentBag<(string message, object? payload)> persistentMessages = [];

    [AutoInject] private readonly IServiceProvider serviceProvider = default!;

    /// <summary>
    /// Serializes "queue a persistent message" against "register a handler and drain the queue". Without it the
    /// two can interleave so that Publish finds no handler, Subscribe drains an empty queue, and Publish then
    /// enqueues a message the handler that just arrived will never be given.
    /// It is also the only lock guarding the inner <see cref="List{T}"/> of <see cref="handlers"/>: the dictionary
    /// is concurrent, the lists inside it are not, and publishers run off the renderer's thread (SignalR callbacks,
    /// and the faulted-task continuation below) while unsubscribes run on it.
    /// </summary>
    private readonly Lock handlersLock = new();

    public void Publish(string message, object? payload = null, bool persistent = false)
    {
        if (TryDeliver(message, payload) || persistent is false) return;

        lock (handlersLock)
        {
            // Re-attempt under the lock: a Subscribe that completed while we were deciding has a live handler
            // now. Nothing was delivered above, so this cannot deliver twice.
            if (TryDeliver(message, payload)) return;

            persistentMessages.Add((message, payload));
        }
    }

    private bool TryDeliver(string message, object? payload)
    {
        var delivered = false;

        if (handlers.TryGetValue(message, out var weakHandlers) is false) return delivered;

        WeakHandler[] snapshot;

        lock (handlersLock)
        {
            snapshot = [.. weakHandlers];
        }

        // Dispatched outside the lock on purpose: a handler that blocks must not stall every other Publish and
        // Subscribe on this instance.
        foreach (var weakHandler in snapshot)
        {
            if (weakHandler.Invoke(payload) is Task handlerTask)
            {
                delivered = true;
                handlerTask.ContinueWith(HandleException, TaskContinuationOptions.OnlyOnFaulted);
            }
        }

        return delivered;
    }

    public Action Subscribe(string message, Func<object?, Task> handler)
    {
        var weakHandler = new WeakHandler(handler);
        var weakHandlers = handlers.GetOrAdd(message, _ => []);

        lock (handlersLock)
        {
            weakHandlers.Add(weakHandler);

            // If persistent messages exist for this message, publish them immediately.
            // A ConcurrentBag can't remove a specific item (TryTake removes an arbitrary one), so we drain it
            // once: matching messages are dispatched and dropped, the rest are put back.
            if (persistentMessages.IsEmpty is false)
            {
                var retained = new List<(string message, object? payload)>();
                while (persistentMessages.TryTake(out var pending))
                {
                    if (pending.message == message)
                    {
                        weakHandler.Invoke(pending.payload)?.ContinueWith(HandleException, TaskContinuationOptions.OnlyOnFaulted);
                    }
                    else
                    {
                        retained.Add(pending);
                    }
                }
                foreach (var pending in retained)
                {
                    persistentMessages.Add(pending);
                }
            }
        }

        return () =>
        {
            lock (handlersLock)
            {
                weakHandlers.RemoveAll(wh => wh.Matches(handler));
            }
        };
    }

    private void HandleException(Task t)
    {
        // The AggregateException a faulted Task carries is not what the handler threw, and every type test in
        // ClientExceptionHandlerBase (KnownException, TransientException, UnauthorizedException) is written against
        // the real exception. Handing over the wrapper turns an expected KnownException into a Critical log and a
        // blocking modal showing "unknown error".
        var exception = t.Exception is { InnerExceptions.Count: 1 } aggregateException
            ? aggregateException.InnerExceptions[0]
            : (Exception)t.Exception!;

        try
        {
            serviceProvider.GetRequiredService<ClientExceptionHandlerBase>().Handle(exception);
        }
        catch (Exception)
        {
            // This is the terminal observer of the handler's exception: nothing awaits this continuation, so letting
            // it fault would destroy the exception it was called to report. Reaching here means the scope that owns
            // the exception handler is already gone (a torn-down Blazor Server circuit), and nothing scoped survives
            // to report either exception.
        }
    }

    private class WeakHandler
    {
        private string targetInfo;
        private readonly bool isStatic;
        private readonly MethodInfo method;
        private readonly WeakReference? target;

        public WeakHandler(Func<object?, Task> handler)
        {
            isStatic = handler.Target is null;
            if (isStatic is false)
            {
                target = new WeakReference(handler.Target);
            }
            method = handler.Method;
            targetInfo = $"{(handler.Target?.GetType().FullName ?? "static")}'s {method.Name}";
        }

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
            else if (this.target?.IsAlive is true && this.target.Target is object target)
            {
                return (Task?)method.Invoke(target, [payload]);
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
