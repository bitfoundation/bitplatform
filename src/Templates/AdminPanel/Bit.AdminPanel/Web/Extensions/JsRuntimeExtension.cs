namespace Microsoft.JSInterop;

public static class JsRuntimeExtension
{
    public static async Task SetToggleBodyOverflow(this IJSRuntime jsRuntime, bool isOverflowHidden)
    {
        await jsRuntime.InvokeVoidAsync("toggleBodyOverflow", isOverflowHidden);
    }
}
