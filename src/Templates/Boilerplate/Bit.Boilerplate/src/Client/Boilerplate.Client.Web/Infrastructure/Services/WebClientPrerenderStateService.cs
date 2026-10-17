//-:cnd:noEmit

using System.Runtime.CompilerServices;
using Microsoft.AspNetCore.Components;

namespace Boilerplate.Client.Web.Infrastructure.Services;

/// <summary>
/// This implementation retrieves values persisted by the WebServerPrerenderStateService in Boilerplate.Server.Web.Services.
/// <inheritdoc cref="IPrerenderStateService"/>
/// </summary>
public partial class WebClientPrerenderStateService(IAuthTokenProvider authTokenProvider,
    PersistentComponentState? persistentComponentState = null) : IPrerenderStateService
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
        if (persistentComponentState!.TryTakeFromJson(await KeyForCaller(authTokenProvider, key), out T? value)) return value;

        var result = await factory();

        return result;
    }

    /// <summary>
    /// A page pre-rendered for an anonymous visitor is kept by the CDN edge and the browser under its url alone, and handed,
    /// with the values persisted into it, to whoever opens that url next, signed in or not. The server may answer a signed-in
    /// caller differently (by its roles or tenant, for instance), so a value is keyed by the user it was fetched for, and any
    /// other caller asks the server itself. WebServerPrerenderStateService persists under the same key.
    /// </summary>
    public static async Task<string> KeyForCaller(IAuthTokenProvider authTokenProvider, string key)
    {
        var user = IAuthTokenProvider.ParseAccessToken(await authTokenProvider.GetAccessToken(), validateExpiry: false);

        return user.IsAuthenticated() ? FormattableString.Invariant($"{user.GetUserId()} {key}") : key;
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}
