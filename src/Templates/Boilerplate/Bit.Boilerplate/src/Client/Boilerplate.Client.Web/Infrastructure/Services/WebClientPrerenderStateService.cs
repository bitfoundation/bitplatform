//-:cnd:noEmit

using System.Runtime.CompilerServices;
using Microsoft.AspNetCore.Components;

namespace Boilerplate.Client.Web.Infrastructure.Services;

/// <summary>
/// This implementation retrieves values persisted by the WebServerPrerenderStateService in Boilerplate.Server.Web.Services.
/// <inheritdoc cref="IPrerenderStateService"/>
/// </summary>
public partial class WebClientPrerenderStateService(JsonSerializerOptions jsonSerializerOptions, PersistentComponentState? persistentComponentState = null) : IPrerenderStateService
{
    public async Task<T?> GetValue<T>(Func<Task<T?>> factory,
        [CallerLineNumber] int lineNumber = 0,
        [CallerMemberName] string memberName = "",
        [CallerFilePath] string filePath = "")
    {
        string key = $"{filePath.Split('\\').LastOrDefault()} {memberName} {lineNumber}";

        return await GetValue(key, factory);
    }

    public async Task<T?> GetValue<T>(string key, Func<Task<T?>> factory)
    {
        if (persistentComponentState!.TryTakeFromJson(key, out JsonElement value)) return value.Deserialize(jsonSerializerOptions.GetTypeInfo<T>());

        var result = await factory();

        return result;
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}
