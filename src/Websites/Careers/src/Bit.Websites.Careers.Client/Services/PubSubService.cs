namespace Bit.Websites.Careers.Client.Services;

/// <summary>
/// For more information <see cref="IPubSubService"/> docs.
/// </summary>
public partial class PubSubService : IPubSubService
{
    [AutoInject] private IServiceProvider serviceProvider = default!;

    private readonly ConcurrentDictionary<string, List<Func<object?, Task>>> handlers = new();

    public void Publish(string message, object? payload)
    {
        if (handlers.TryGetValue(message, out var messageHandlers))
        {
            foreach (var handler in messageHandlers)
            {
                handler(payload)
                    .ContinueWith(t => serviceProvider.GetRequiredService<IExceptionHandler>().Handle(t.Exception!), TaskContinuationOptions.OnlyOnFaulted);
            }
        }
    }

    public Action Subscribe(string message, Func<object?, Task> handler)
    {
        var handlers = this.handlers.ContainsKey(message)
                            ? this.handlers[message]
                            : this.handlers[message] = [];

        handlers.Add(handler);

        return () => handlers.Remove(handler);
    }
}
