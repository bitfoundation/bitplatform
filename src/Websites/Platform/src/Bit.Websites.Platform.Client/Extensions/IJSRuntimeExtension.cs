namespace Microsoft.JSInterop;

public static class IJSRuntimeExtensions
{
    public static async Task ToggleBodyOverflow(this IJSRuntime jsRuntime, bool isNavOpen)
    {
        await jsRuntime.InvokeVoidAsync("toggleBodyOverflow", isNavOpen);
    }

    public static async Task ScrollElementIntoView(this IJSRuntime jsRuntime, ElementReference element)
    {
        await jsRuntime.InvokeVoidAsync("scrollElementIntoView", element);
    }
}
