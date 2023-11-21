using Boilerplate.Shared.Dtos.Identity;

namespace Microsoft.JSInterop;

public static class IJSRuntimeExtensions
{
    /// <summary>
    /// To disable the scrollbar of the body when showing the modal, so the modal can be always shown in the viewport without being scrolled out.
    /// </summary>
    public static async Task SetBodyOverflow(this IJSRuntime jsRuntime, bool hidden)
    {
        await jsRuntime.InvokeVoidAsync("App.setBodyOverflow", hidden);
    }

    public static async Task GoBack(this IJSRuntime jsRuntime)
    {
        await jsRuntime.InvokeVoidAsync("App.goBack");
    }

    public static async Task ApplyBodyElementClasses(this IJSRuntime jsRuntime, List<string> cssClasses, Dictionary<string, string> cssVariables)
    {
        await jsRuntime.InvokeVoidAsync("App.applyBodyElementClasses", cssClasses, cssVariables);
    }

    public static async Task RemoveToken(this IJSRuntime jsRuntime)
    {
        await jsRuntime.RemoveCookie("access_token");
        await jsRuntime.RemoveLocalStorage("refresh_token");
    }

#if !BlazorHybrid

    public static async Task SetCookie(this IJSRuntime jsRuntime, string key, string value, long expiresIn, bool rememberMe)
    {
        await jsRuntime.InvokeVoidAsync("App.setCookie", key, value, expiresIn, rememberMe);
    }

    public static async Task RemoveCookie(this IJSRuntime jsRuntime, string key)
    {
        await jsRuntime.InvokeVoidAsync("App.removeCookie", key);
    }

    public static async Task<string?> GetCookie(this IJSRuntime jsRuntime, string key)
    {
        return await jsRuntime.InvokeAsync<string?>("App.getCookie", key);
    }

    public static async Task SetLocalStorage(this IJSRuntime jsRuntime, string key, string value, bool rememberMe)
    {
        await jsRuntime.InvokeVoidAsync($"window.{(rememberMe ? "localStorage" : "sessionStorage")}.setItem", key, value);
    }

    public static async Task RemoveLocalStorage(this IJSRuntime jsRuntime, string key)
    {
        await jsRuntime.InvokeVoidAsync("window.sessionStorage.removeItem", key);
        await jsRuntime.InvokeVoidAsync("window.localStorage.removeItem", key);
    }

    public static async Task<string?> GetLocalStorage(this IJSRuntime jsRuntime, string key)
    {
        return (await jsRuntime.InvokeAsync<string?>("window.localStorage.getItem", key)) ??
            (await jsRuntime.InvokeAsync<string?>("window.sessionStorage.getItem", key));
    }

    public static async Task StoreToken(this IJSRuntime jsRuntime, TokenResponseDto tokenResponse, bool? rememberMe = null)
    {
        if (rememberMe is null)
        {
            rememberMe = string.IsNullOrEmpty(await jsRuntime.InvokeAsync<string?>("window.localStorage.getItem", "refresh_token")) is false;
        }

        await jsRuntime.SetCookie("access_token", tokenResponse.AccessToken!, tokenResponse.ExpiresIn, rememberMe is true);
        await jsRuntime.SetLocalStorage("refresh_token", tokenResponse.RefreshToken!, rememberMe is true);
    }
#endif
}
