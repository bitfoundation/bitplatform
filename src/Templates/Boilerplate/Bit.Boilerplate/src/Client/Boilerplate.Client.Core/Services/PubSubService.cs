namespace Boilerplate.Client.Core.Services;

/// <summary>
/// For more information <see cref="IPubSubService"/> docs.
/// </summary>
public partial class PubSubService : IPubSubService
{
    private readonly ConcurrentDictionary<string, List<Func<object?, Task>>> handlers = new();

    [AutoInject] private readonly IServiceProvider serviceProvider = default!;



    public void Publish(string message, object? payload = null)
    {
        if (handlers.TryGetValue(message, out var messageHandlers))
        {
            foreach (var handler in messageHandlers.ToArray())
            {
                handler(payload).ContinueWith(handleException, TaskContinuationOptions.OnlyOnFaulted);
            }
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

        return () => messageHandlers.Remove(handler);
    }
}
