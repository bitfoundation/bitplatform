namespace BlazorWeb.Client.Services;

/// <summary>
/// For more information <see cref="IPubSubService"/> docs.
/// </summary>
public class PubSubService : IPubSubService
{
    private readonly ConcurrentDictionary<string, List<Action<object?>>> _handlers = new();

    public void Publish(string message, object? payload)
    {
        if (_handlers.TryGetValue(message, out var handlers))
        {
            handlers.ForEach(handler => handler(payload));
        }
    }

    public Action Subscribe(string message, Action<object?> handler)
    {
        var handlers = _handlers.ContainsKey(message)
                            ? _handlers[message]
                            : _handlers[message] = [];

        handlers.Add(handler);

        return () => handlers.Remove(handler);
    }
}
