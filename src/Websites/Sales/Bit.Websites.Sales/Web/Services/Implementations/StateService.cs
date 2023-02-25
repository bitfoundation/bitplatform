using System.Runtime.InteropServices;

namespace Bit.Websites.Sales.Web.Services.Implementations;

// Using this class, persisting the application state on Pre-Rendering mode (explained here: https://docs.microsoft.com/en-us/aspnet/core/blazor/components/prerendering-and-integration#persist-prerendered-state) will be very simple in this multi-mode Template project.

#if (BlazorWebAssembly || BlazorServer) && SSR
public class StateService : IStateService, IDisposable
{
    private PersistingComponentStateSubscription? _subscription;
    private readonly PersistentComponentState _applicationState;
    private readonly ConcurrentDictionary<string, object?> _values = new();

    public StateService(PersistentComponentState applicationState)
    {
        _applicationState = applicationState;

        if (RuntimeInformation.ProcessArchitecture != Architecture.Wasm)
        {
            _subscription = applicationState.RegisterOnPersisting(PersistAsJsonAsync);
        }
    }

    public async Task<T?> GetValue<T>(string key, Func<Task<T?>> factory)
    {
        if (_applicationState.TryTakeFromJson(key, out T? value)) return value;

        var result = await factory();
        Persist(key, result);

        return result;
    }

    private void Persist<T>(string key, T value)
    {
        if (RuntimeInformation.ProcessArchitecture == Architecture.Wasm) return;

        _values.TryRemove(key, out object? _);
        _values.TryAdd(key, value);
    }

    private async Task PersistAsJsonAsync()
    {
        if (RuntimeInformation.ProcessArchitecture == Architecture.Wasm) return;

        foreach (var item in _values)
        {
            _applicationState.PersistAsJson(item.Key, item.Value);
        }

        await Task.CompletedTask;
    }

    public void Dispose() => _subscription?.Dispose();
}
#else
public class StateService : IStateService
{
    public Task<T?> GetValue<T>(string key, Func<Task<T?>> factory)
    {
        return factory();
    }
}
#endif
