//-:cnd:noEmit

namespace Boilerplate.Client.Core.Services;

/// <summary>
/// For more information <see cref="IPrerenderStateService"/> docs.
/// </summary>
#if (BlazorWebAssembly || BlazorServer) && (SpaPrerendered || PwaPrerendered)
public class PrerenderStateService : IPrerenderStateService, IAsyncDisposable
{
    private PersistingComponentStateSubscription? subscription;
    private readonly PersistentComponentState applicationState;
    private readonly ConcurrentDictionary<string, object?> values = new();

    public PrerenderStateService(PersistentComponentState applicationState)
    {
        applicationState = applicationState;
        subscription = applicationState.RegisterOnPersisting(PersistAsJson);
    }

    public async Task<T?> GetValue<T>(string key, Func<Task<T?>> factory)
    {
        if (applicationState.TryTakeFromJson(key, out T? value)) return value;

        var result = await factory();
        Persist(key, result);
        return result;
    }

    void Persist<T>(string key, T value)
    {
        values.TryRemove(key, out object? _);
        values.TryAdd(key, value);
    }

    async Task PersistAsJson()
    {
        foreach (var item in values)
        {
            applicationState.PersistAsJson(item.Key, item.Value);
        }
    }

    public async ValueTask DisposeAsync()
    {
        subscription?.Dispose();
    }
}
#else
public class PrerenderStateService : IPrerenderStateService
{
    public Task<T?> GetValue<T>(string key, Func<Task<T?>> factory)
    {
        return factory();
    }
}
#endif
