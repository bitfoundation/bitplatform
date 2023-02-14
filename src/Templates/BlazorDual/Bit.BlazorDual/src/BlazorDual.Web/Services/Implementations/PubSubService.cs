namespace BlazorDual.Web.Services.Implementations;

public class PubSubService : IPubSubService
{
    private readonly ConcurrentDictionary<string, List<Action<object?>>> _handlers = new();

    public void Pub(string message, object? payload)
    {
        if (_handlers.TryGetValue(message, out var handlers))
        {
            handlers.ForEach(handler => handler(payload));
        }
    }

    public Action Sub(string message, Action<object?> handler)
    {
        var handlers = _handlers.ContainsKey(message)
                            ? _handlers[message]
                            : _handlers[message] = new List<Action<object?>>();

        handlers.Add(handler);

        return () => handlers.Remove(handler);
    }
}
