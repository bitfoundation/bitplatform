using Boilerplate.Shared.Dtos.Identity;

namespace Microsoft.JSInterop;

// In Blazor Hybrid, we employ Preferences along with an in-memory dictionary to emulate the functionality of cookies, localStorage, and sessionStorage.
public static class IJSRuntimeHybridExtensions
{
#if BlazorHybrid
    private static readonly Dictionary<string, string?> _sessionStorage = [];

    public static async Task SetCookie(this IJSRuntime jsRuntime, string key, string value, long expiresIn, bool rememberMe)
    {
        if (rememberMe)
        {
            Preferences.Set(key, value);
        }
        else
        {
            _sessionStorage[key] = value;
        }
    }

    public static async Task RemoveCookie(this IJSRuntime jsRuntime, string key)
    {
        _sessionStorage.Remove(key);
        Preferences.Remove(key);
    }

    public static async Task<string?> GetCookie(this IJSRuntime jsRuntime, string key)
    {
        return _sessionStorage.TryGetValue(key, out string? value) ? value : Preferences.Get(key, null);
    }

    public static async Task SetLocalStorage(this IJSRuntime jsRuntime, string key, string value, bool rememberMe)
    {
        await jsRuntime.SetCookie(key, value, default, rememberMe);
    }

    public static async Task RemoveLocalStorage(this IJSRuntime jsRuntime, string key)
    {
        await jsRuntime.RemoveCookie(key);
    }

    public static async Task<string?> GetLocalStorage(this IJSRuntime jsRuntime, string key)
    {
        return await jsRuntime.GetCookie(key);
    }

    public static async Task StoreToken(this IJSRuntime jsRuntime, TokenResponseDto tokenResponse, bool? rememberMe = null)
    {
        if (rememberMe is null)
        {
            rememberMe = Preferences.Get("refresh_token", null) is null;
        }

        await jsRuntime.SetCookie("access_token", tokenResponse.AccessToken!, tokenResponse.ExpiresIn, rememberMe is true);
        await jsRuntime.SetLocalStorage("refresh_token", tokenResponse.RefreshToken!, rememberMe is true);
    }
#endif
}
