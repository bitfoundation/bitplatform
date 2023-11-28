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

    public static async Task SetCookie(this IJSRuntime jsRuntime, string key, string value, long expiresIn, bool rememberMe)
    {
        bool secure = true;
#if DEBUG
        secure = false;
#endif

        await jsRuntime.InvokeVoidAsync("App.setCookie", key, value, expiresIn, rememberMe, secure);
    }

    public static async Task RemoveCookie(this IJSRuntime jsRuntime, string key)
    {
        await jsRuntime.InvokeVoidAsync("App.removeCookie", key);
    }
}
