//-:cnd:noEmit

using System.Runtime.CompilerServices;
using Microsoft.AspNetCore.Components;

namespace Boilerplate.Client.Web.Services;

/// <summary>
/// This implementation retrieves values persisted by the WebServerPrerenderStateService in Boilerplate.Server.Web.Services.
/// <inheritdoc cref="IPrerenderStateService"/>
/// </summary>
public partial class WebClientPrerenderStateService(PersistentComponentState? persistentComponentState = null) : IPrerenderStateService
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
        if (persistentComponentState!.TryTakeFromJson(key, out T? value)) return value;

        var result = await factory();

        return result;
    }
}
