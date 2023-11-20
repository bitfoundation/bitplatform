//-:cnd:noEmit

namespace Boilerplate.Client.Core.Services;

/// <summary>
/// For more information <see cref="IPrerenderStateService"/> docs.
/// </summary>
#if (BlazorWebAssembly || BlazorServer) && (SpaPrerendered || PwaPrerendered)
public class PrerenderStateService : IPrerenderStateService, IAsyncDisposable
{
    private PersistingComponentStateSubscription? _subscription;
    private readonly PersistentComponentState _applicationState;
    private readonly ConcurrentDictionary<string, object?> _values = new();

    public PrerenderStateService(PersistentComponentState applicationState)
    {
        _applicationState = applicationState;

        if (OperatingSystem.IsBrowser() is false)
        {
            _subscription = applicationState.RegisterOnPersisting(PersistAsJson);
        }
    }

    public async Task<T?> GetValue<T>(string key, Func<Task<T?>> factory)
    {
        if (_applicationState.TryTakeFromJson(key, out T? value)) return value;

        var result = await factory();
        Persist(key, result);
        return result;
    }

    void Persist<T>(string key, T value)
    {
        _values.TryRemove(key, out object? _);
        _values.TryAdd(key, value);
    }

    async Task PersistAsJson()
    {
        if (OperatingSystem.IsBrowser()) return;

        foreach (var item in _values)
        {
            _applicationState.PersistAsJson(item.Key, item.Value);
        }
    }

    public async ValueTask DisposeAsync()
    {
        _subscription?.Dispose();
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
