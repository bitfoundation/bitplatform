//-:cnd:noEmit

using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using Microsoft.AspNetCore.Components;
using Boilerplate.Client.Web.Infrastructure.Services;

namespace Boilerplate.Server.Web.Infrastructure.Services;

/// <summary>
/// This class persists values to be accessed by the web client after hydration using <see cref="WebClientPrerenderStateService"/>.
/// <inheritdoc cref="IPrerenderStateService"/>
/// </summary>
public partial class WebServerPrerenderStateService : IPrerenderStateService, IAsyncDisposable
{
    private PersistingComponentStateSubscription? subscription;
    private readonly ServerWebSettings serverWebSettings = default!;
    private readonly PersistentComponentState? persistentComponentState;
    private readonly ConcurrentDictionary<string, object?> values = new();

    private bool NoPersistent => serverWebSettings.WebAppRender.RenderMode is null /*Ssr*/ ||
                                       serverWebSettings.WebAppRender.PrerenderEnabled is false;

    public WebServerPrerenderStateService(ServerWebSettings clientWebSettings, PersistentComponentState? persistentComponentState = null)
    {
        this.serverWebSettings = clientWebSettings;
        this.persistentComponentState = persistentComponentState;
        if (NoPersistent) return;
        subscription = persistentComponentState?.RegisterOnPersisting(PersistAsJson, clientWebSettings.WebAppRender.RenderMode);
    }

    public async Task<T?> GetValue<T>(Func<Task<T?>> factory,
        [CallerLineNumber] int lineNumber = 0,
        [CallerMemberName] string memberName = "",
        [CallerFilePath] string filePath = "")
    {
        if (NoPersistent) return await factory();

        string key = $"{filePath.Split('\\').LastOrDefault()} {memberName} {lineNumber}";

        return await GetValue(key, factory);
    }

    public async Task<T?> GetValue<T>(string key, Func<Task<T?>> factory)
    {
        if (NoPersistent) return await factory();

        if (persistentComponentState!.TryTakeFromJson(key, out T? value)) return value;

        var result = await factory();
        Persist(key, result);
        return result;
    }

    void Persist<T>(string key, T value)
    {
        if (NoPersistent || AppPlatform.IsBrowser) return;

        values.AddOrUpdate(key, value, (_, _) => value);
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
        if (NoPersistent) return;

        subscription?.Dispose();
    }
}
