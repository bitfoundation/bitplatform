namespace Boilerplate.Client.Core.Services;

/// <summary>
/// Service for Publish/Subscribe pattern.
/// </summary>
public partial class PubSubService
{
    private readonly ConcurrentDictionary<string, List<Func<object?, Task>>> handlers = [];

    /// <summary>
    /// Messages that were published before any handler was subscribed.
    /// </summary>
    private readonly ConcurrentBag<(string message, object? payload)> notHandledMessages = [];

    [AutoInject] private readonly IServiceProvider serviceProvider = default!;

    public void Publish(string message, object? payload = null, bool persistent = false)
    {
        if (handlers.TryGetValue(message, out var messageHandlers))
        {
            foreach (var handler in messageHandlers.ToArray())
            {
                handler(payload).ContinueWith(handleException, TaskContinuationOptions.OnlyOnFaulted);
            }
        }
        else if (persistent)
        {
            notHandledMessages.Add((message, payload));
        }

        void handleException(Task t)
        {
            serviceProvider.GetRequiredService<IExceptionHandler>().Handle(t.Exception!);
        }
    }

    public Action Subscribe(string message, Func<object?, Task> handler)
    {
        var messageHandlers = handlers.TryGetValue(message, out var value) ? value : handlers[message] = [];

        messageHandlers.Add(handler);

        foreach (var (notHandledMessage, payload) in notHandledMessages)
        {
            if (notHandledMessage == message)
            {
                Publish(message, payload);
            }
        }

        return () => messageHandlers.Remove(handler);
    }
}
