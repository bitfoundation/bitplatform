//-:cnd:noEmit

using System.Runtime.CompilerServices;

namespace Boilerplate.Client.Core.Services;

/// <summary>
/// For more information <see cref="IPrerenderStateService"/> docs.
/// </summary>
public partial class PrerenderStateService : IPrerenderStateService, IAsyncDisposable
{
    private PersistingComponentStateSubscription? subscription;
    private readonly ClientAppSettings clientAppSettings = default!;
    private readonly PersistentComponentState? persistentComponentState;
    private readonly ConcurrentDictionary<string, object?> values = new();

    private bool NoPersistant => clientAppSettings.WebAppRender.RenderMode is null /*Ssr*/ ||
                                       clientAppSettings.WebAppRender.PrerenderEnabled is false ||
                                       AppPlatform.IsBlazorHybrid;

    public PrerenderStateService(ClientAppSettings webAppRenderMode, PersistentComponentState? persistentComponentState = null)
    {
        this.clientAppSettings = webAppRenderMode;
        this.persistentComponentState = persistentComponentState;
        if (NoPersistant) return;
        subscription = persistentComponentState?.RegisterOnPersisting(PersistAsJson, webAppRenderMode.WebAppRender.RenderMode);
    }

    public async Task<T?> GetValue<T>(Func<Task<T?>> factory,
        [CallerLineNumber] int lineNumber = 0,
        [CallerMemberName] string memberName = "",
        [CallerFilePath] string filePath = "")
    {
        if (NoPersistant) return await factory();

        string key = $"{filePath.Split('\\').LastOrDefault()} {memberName} {lineNumber}";

        return await GetValue(key, factory);
    }

    public async Task<T?> GetValue<T>(string key, Func<Task<T?>> factory)
    {
        if (NoPersistant) return await factory();

        if (persistentComponentState!.TryTakeFromJson(key, out T? value)) return value;

        var result = await factory();
        Persist(key, result);
        return result;
    }

    void Persist<T>(string key, T value)
    {
        if (NoPersistant || AppPlatform.IsBlazorHybridOrBrowser) return;

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
        if (NoPersistant) return;

        subscription?.Dispose();
    }
}
