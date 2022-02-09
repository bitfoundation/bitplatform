using System.Runtime.InteropServices;

namespace TodoTemplate.App.Services.Implementations;

#if BlazorWebAssembly || BlazorServer
public class StateService : IAsyncDisposable
{
    private PersistingComponentStateSubscription? _subscription;
    private readonly PersistentComponentState _applicationState;
    private readonly ConcurrentDictionary<string, object?> _values = new ConcurrentDictionary<string, object?>();

    public StateService(PersistentComponentState applicationState)
    {
        _applicationState = applicationState;

        if (RuntimeInformation.ProcessArchitecture != Architecture.Wasm)
            _subscription = applicationState.RegisterOnPersisting(PersistAsJson);
    }

    public async Task<T?> GetValue<T>(string key, Func<Task<T?>> factory)
    {
        if (_applicationState.TryTakeFromJson<T>(key, out T? value))
            return value;
        var result = await factory();
        Persist(key, result);
        return result;
    }

    void Persist<T>(string key, T value)
    {
        if (RuntimeInformation.ProcessArchitecture == Architecture.Wasm)
            return;
        _values.TryRemove(key, out object? _);
        _values.TryAdd(key, value);
    }

    async Task PersistAsJson()
    {
        if (RuntimeInformation.ProcessArchitecture == Architecture.Wasm)
            return;
        foreach (var item in _values)
            _applicationState.PersistAsJson(item.Key, item.Value);
    }

    public async ValueTask DisposeAsync()
    {
        _subscription?.Dispose();
    }
}
#else
public class StateService
{
    public Task<T?> GetValue<T>(string key, Func<Task<T?>> factory)
    {
        return factory();
    }
}
#endif
