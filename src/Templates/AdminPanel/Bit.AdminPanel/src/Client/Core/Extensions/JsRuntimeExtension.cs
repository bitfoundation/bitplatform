namespace Microsoft.JSInterop;

public static class JsRuntimeExtension
{
    public static async Task SetBodyOverflow(this IJSRuntime jsRuntime, bool hidden)
    {
        await jsRuntime.InvokeVoidAsync("App.setBodyOverflow", hidden);
    }

    public static async Task GoBack(this IJSRuntime jsRuntime)
    {
        await jsRuntime.InvokeVoidAsync("App.goBack");
    }
}
