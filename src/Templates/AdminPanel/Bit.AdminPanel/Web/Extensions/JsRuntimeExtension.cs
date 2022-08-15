namespace Microsoft.JSInterop;

public static class JsRuntimeExtension
{
    public static async Task SetToggleBodyOverflow(this IJSRuntime jsRuntime, bool isOverflowHidden)
    {
        await jsRuntime.InvokeVoidAsync("toggleBodyOverflow", isOverflowHidden);
    }

    public static async Task GoBack(this IJSRuntime jsRuntime)
    {
        await jsRuntime.InvokeVoidAsync("navigateToPrevUrl");
    }
}
