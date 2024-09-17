//-:cnd:noEmit

using System.Runtime.CompilerServices;

namespace Boilerplate.Client.Core.Services;

/// <summary>
/// For more information <see cref="IPrerenderStateService"/> docs.
/// </summary>
public partial class PrerenderStateService : IPrerenderStateService, IAsyncDisposable
{
    [AutoInject] private IJSRuntime jsRuntime = default!;

    private PersistingComponentStateSubscription? subscription;
    private readonly PersistentComponentState? persistentComponentState;
    private readonly ConcurrentDictionary<string, object?> values = new();

    private static bool noPersistant = AppRenderMode.Current == AppRenderMode.StaticSsr ||
                                       AppRenderMode.PrerenderEnabled is false ||
                                       AppPlatform.IsBlazorHybrid;

    public PrerenderStateService(PersistentComponentState? persistentComponentState = null)
    {
        if (noPersistant) return;
        subscription = persistentComponentState?.RegisterOnPersisting(PersistAsJson, AppRenderMode.Current);
        this.persistentComponentState = persistentComponentState;
    }

    public async Task<T?> GetValue<T>(Func<Task<T?>> factory,
        [CallerLineNumber] int lineNumber = 0,
        [CallerMemberName] string memberName = "",
        [CallerFilePath] string filePath = "")
    {
        if (noPersistant) return await factory();

        string key = $"{filePath.Split('\\').LastOrDefault()} {memberName} {lineNumber}";

        return await GetValue(key, factory);
    }

    public async Task<T?> GetValue<T>(string key, Func<Task<T?>> factory)
    {
        if (noPersistant) return await factory();

        if (persistentComponentState!.TryTakeFromJson(key, out T? value)) return value;

        var result = await factory();
        Persist(key, result);
        return result;
    }

    void Persist<T>(string key, T value)
    {
        if (noPersistant || jsRuntime.IsInPrerenderSession() is false) return;

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
        if (noPersistant) return;

        subscription?.Dispose();
    }
}
