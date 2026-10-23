//-:cnd:noEmit

using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization.Metadata;
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
    private readonly JsonSerializerOptions jsonSerializerOptions;
    private readonly PersistentComponentState? persistentComponentState;
    private readonly ConcurrentDictionary<string, (object? Value, JsonTypeInfo TypeInfo)> values = new();

    private bool NoPersistent => serverWebSettings.WebAppRender.RenderMode is null /*Ssr*/ ||
                                       serverWebSettings.WebAppRender.PrerenderEnabled is false;

    public WebServerPrerenderStateService(ServerWebSettings clientWebSettings, JsonSerializerOptions jsonSerializerOptions, PersistentComponentState? persistentComponentState = null)
    {
        this.serverWebSettings = clientWebSettings;
        this.jsonSerializerOptions = jsonSerializerOptions;
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

        if (persistentComponentState!.TryTakeFromJson(key, out JsonElement value)) return value.Deserialize(jsonSerializerOptions.GetTypeInfo<T>());

        var result = await factory();
        Persist(key, result);
        return result;
    }

    void Persist<T>(string key, T value)
    {
        if (NoPersistent || AppPlatform.IsBrowser) return;

        values[key] = (value, jsonSerializerOptions.GetTypeInfo<T>());
    }

    async Task PersistAsJson()
    {
        foreach (var item in values)
        {
            persistentComponentState!.PersistAsJson(item.Key, JsonSerializer.SerializeToElement(item.Value.Value, item.Value.TypeInfo));
        }

        values.Clear();
    }

    public async ValueTask DisposeAsync()
    {
        if (NoPersistent) return;

        subscription?.Dispose();
    }
}
