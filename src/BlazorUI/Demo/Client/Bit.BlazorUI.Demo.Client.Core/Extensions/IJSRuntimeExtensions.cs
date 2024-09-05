namespace Bit.BlazorUI;

public static class IJSRuntimeExtensions
{
    public static async Task ToggleBodyOverflow(this IJSRuntime jsRuntime, bool isNavOpen)
    {
        await jsRuntime.InvokeVoidAsync("toggleBodyOverflow", isNavOpen);
    }

    public static async Task ScrollToElement(this IJSRuntime jsRuntime, string targetElementId)
    {
        await jsRuntime.InvokeVoidAsync("scrollToElement", targetElementId);
    }

    public static async ValueTask<SideRailItem[]> GetSideRailItems(this IJSRuntime jsRuntime, string targetElement)
    {
        return await jsRuntime.InvokeAsync<SideRailItem[]>("getSideRailItems", targetElement);
    }

    public static async Task CopyToClipboard(this IJSRuntime jsRuntime, string codeSampleContentForCopy)
    {
        await jsRuntime.InvokeVoidAsync("copyToClipboard", codeSampleContentForCopy);
    }

    public static async Task ApplyBodyElementClasses(this IJSRuntime jsRuntime, List<string> cssClasses, Dictionary<string, string> cssVariables)
    {
        await jsRuntime.InvokeVoidAsync("applyBodyElementClasses", cssClasses, cssVariables);
    }
}
