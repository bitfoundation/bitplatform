﻿
namespace Bit.Websites.Sales.Client.Services;

// Using this class, persisting the application state on Pre-Rendering mode (explained here: https://docs.microsoft.com/en-us/aspnet/core/blazor/components/prerendering-and-integration#persist-prerendered-state) will be very simple in this multi-mode Template project.

public class PrerenderStateService : IPrerenderStateService, IAsyncDisposable
{
    private PersistingComponentStateSubscription? _subscription;
    private readonly PersistentComponentState _applicationState;
    private readonly ConcurrentDictionary<string, object?> _values = new();

    public PrerenderStateService(PersistentComponentState applicationState)
    {
        _applicationState = applicationState;
        _subscription = applicationState.RegisterOnPersisting(PersistAsJson, RenderModeProvider.Current);
    }

    public async Task<T?> GetValue<T>(string key, Func<Task<T?>> factory)
    {
        if (_applicationState.TryTakeFromJson(key, out T? value))
            return value;
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
        if (OperatingSystem.IsBrowser())
            return;
        foreach (var item in _values)
            _applicationState.PersistAsJson(item.Key, item.Value);
    }

    public async ValueTask DisposeAsync()
    {
        _subscription?.Dispose();
    }
}
