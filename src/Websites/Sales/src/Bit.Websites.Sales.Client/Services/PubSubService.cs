namespace Bit.Websites.Sales.Client.Services;

/// <summary>
/// For more information <see cref="IPubSubService"/> docs.
/// </summary>
public class PubSubService : IPubSubService
{
    private readonly ConcurrentDictionary<string, List<Func<object?, Task>>> _handlers = new();

    public async Task Publish(string message, object? payload)
    {
        if (_handlers.TryGetValue(message, out var handlers))
        {
            foreach (var handler in handlers)
            {
                await handler(payload);
            }
        }
    }

    public Action Subscribe(string message, Func<object?, Task> handler)
    {
        var handlers = _handlers.ContainsKey(message)
                            ? _handlers[message]
                            : _handlers[message] = [];

        handlers.Add(handler);

        return () => handlers.Remove(handler);
    }
}
