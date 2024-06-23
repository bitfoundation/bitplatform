
namespace Bit.BlazorUI.Demo.Client.Core.Services;

/// <summary>
/// For more information <see cref="IPrerenderStateService"/> docs.
/// </summary>
public class PrerenderStateService : IPrerenderStateService, IDisposable
{
    private PersistingComponentStateSubscription? subscription;
    private readonly PersistentComponentState? persistentComponentState;
    private readonly ConcurrentDictionary<string, object?> values = new();

    public PrerenderStateService(PersistentComponentState? persistentComponentState = null)
    {
        subscription = persistentComponentState?.RegisterOnPersisting(PersistAsJson, AppRenderMode.Current);
        this.persistentComponentState = persistentComponentState;
    }

    public async Task<T?> GetValue<T>(string key, Func<Task<T?>> factory)
    {
        if (AppRenderMode.PrerenderEnabled is false)
            return await factory();

        if (persistentComponentState!.TryTakeFromJson(key, out T? value)) return value;

        var result = await factory();
        Persist(key, result);
        return result;
    }

    void Persist<T>(string key, T value)
    {
        if (AppRenderMode.PrerenderEnabled is false)
            return;

        values.TryRemove(key, out object? _);
        values.TryAdd(key, value);
    }

    private Task PersistAsJson()
    {
        foreach (var item in values)
        {
            persistentComponentState!.PersistAsJson(item.Key, item.Value);
        }

        return Task.CompletedTask;
    }

    public void Dispose()
    {
        if (AppRenderMode.PrerenderEnabled is false) return;

        subscription?.Dispose();
    }
}
