namespace Boilerplate.Client.Core.Services;

/// <summary>
/// Service for Publish/Subscribe pattern.
/// You could publish messages within blazor components, pages outside blazor components (Like MAUI Xaml pages), JavaScript
/// codes using window.postMessage or even from server side using SignalR (<see cref="SharedPubSubMessages.SESSION_REVOKED"/> as example.
/// </summary>
public partial class PubSubService
{
    private readonly ConcurrentDictionary<string, List<Func<object?, Task>>> handlers = [];

    /// <summary>
    /// Messages that were published before any handler was subscribed.
    /// </summary>
    private readonly ConcurrentBag<(string message, object? payload)> persistentMessages = [];

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
            persistentMessages.Add((message, payload));
        }
    }

    public Action Subscribe(string message, Func<object?, Task> handler)
    {
        var messageHandlers = handlers.TryGetValue(message, out var value) ? value : handlers[message] = [];

        messageHandlers.Add(handler);

        foreach (var (notHandledMessage, payload) in persistentMessages)
        {
            if (notHandledMessage == message)
            {
                handler(payload).ContinueWith(handleException, TaskContinuationOptions.OnlyOnFaulted);
                persistentMessages.TryTake(out _);
            }
        }

        return () => messageHandlers.Remove(handler);
    }

    private void handleException(Task t)
    {
        serviceProvider.GetRequiredService<IExceptionHandler>().Handle(t.Exception!);
    }
}
