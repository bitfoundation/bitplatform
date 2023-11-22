namespace Bit.Websites.Platform.Client.Services;

/// <summary>
/// For more information <see cref="IPubSubService"/> docs.
/// </summary>
public partial class PubSubService : IPubSubService
{
    [AutoInject] private IServiceProvider _serviceProvider = default!;

    private readonly ConcurrentDictionary<string, List<Func<object?, Task>>> _handlers = new();

    public void Publish(string message, object? payload)
    {
        if (_handlers.TryGetValue(message, out var handlers))
        {
            foreach (var handler in handlers)
            {
                handler(payload)
                    .ContinueWith(t => _serviceProvider.GetRequiredService<IExceptionHandler>().Handle(t.Exception!), TaskContinuationOptions.OnlyOnFaulted);
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
