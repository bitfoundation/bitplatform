namespace Bit.BlazorUI;

public static class IJSRuntimeExtensions
{
    public static async Task ScrollToElement(this IJSRuntime jsRuntime, string targetElementId)
    {
        await jsRuntime.InvokeVoid("scrollToElement", targetElementId);
    }

    public static async ValueTask<SideRailItem[]> GetSideRailItems(this IJSRuntime jsRuntime)
    {
        return await jsRuntime.Invoke<SideRailItem[]>("getSideRailItems");
    }

    public static async Task CopyToClipboard(this IJSRuntime jsRuntime, string codeSampleContentForCopy)
    {
        await jsRuntime.InvokeVoid("copyToClipboard", codeSampleContentForCopy);
    }

    public static async Task ApplyBodyElementClasses(this IJSRuntime jsRuntime, List<string> cssClasses, Dictionary<string, string> cssVariables)
    {
        await jsRuntime.InvokeVoid("applyBodyElementClasses", cssClasses, cssVariables);
    }
}
