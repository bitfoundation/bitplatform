//-:cnd:noEmit

using System.Runtime.CompilerServices;

namespace Boilerplate.Client.Core.Services;

/// <summary>
/// For more information <see cref="IPrerenderStateService"/> docs.
/// </summary>
public class PrerenderStateService : IPrerenderStateService, IAsyncDisposable
{
    private PersistingComponentStateSubscription? subscription;
    private readonly PersistentComponentState? persistentComponentState;
    private readonly ConcurrentDictionary<string, object?> values = new();

    public PrerenderStateService(PersistentComponentState? persistentComponentState = null)
    {
        subscription = persistentComponentState?.RegisterOnPersisting(PersistAsJson, AppRenderMode.Current);
        this.persistentComponentState = persistentComponentState;
    }

    public async Task<T?> GetValue<T>(Func<Task<T?>> factory,
        [CallerLineNumber] int lineNumber = 0,
        [CallerMemberName] string memberName = "",
        [CallerFilePath] string filePath = "")
    {
        if (AppRenderMode.PrerenderEnabled is false || AppRenderMode.IsBlazorHybrid)
            return await factory();

        string key = $"{filePath.Split('\\').LastOrDefault()} {memberName} {lineNumber}";

        return await GetValue(key, factory);
    }

    public async Task<T?> GetValue<T>(string key, Func<Task<T?>> factory)
    {
        if (AppRenderMode.PrerenderEnabled is false || AppRenderMode.IsBlazorHybrid)
            return await factory();

        if (persistentComponentState!.TryTakeFromJson(key, out T? value)) return value;

        var result = await factory();
        Persist(key, result);
        return result;
    }

    void Persist<T>(string key, T value)
    {
        if (AppRenderMode.PrerenderEnabled is false || AppRenderMode.IsBlazorHybrid)
            return;

        values.TryRemove(key, out object? _);
        values.TryAdd(key, value);
    }

    async Task PersistAsJson()
    {
        foreach (var item in values)
        {
            persistentComponentState!.PersistAsJson(item.Key, item.Value);
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (AppRenderMode.PrerenderEnabled is false || AppRenderMode.IsBlazorHybrid)
            return;

        subscription?.Dispose();
    }
}
