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

    public static async Task RegisterSideRailScrollSpy<T>(this IJSRuntime jsRuntime, string id, DotNetObjectReference<T> dotnetObj, string activeItemMethodName, string sectionsChangedMethodName, string?[] sectionIds) where T : class
    {
        await jsRuntime.InvokeVoid("registerSideRailScrollSpy", id, dotnetObj, activeItemMethodName, sectionsChangedMethodName, sectionIds);
    }

    /// <summary>
    /// Brings the entry already marked active inside the visible part of the responsive panel's copy
    /// of the rail. The spy does this for itself as the reader scrolls, but the panel's list is
    /// mounted long after the last time it moved the highlight, so it needs asking.
    /// </summary>
    public static async Task ScrollSideRailToActiveItem(this IJSRuntime jsRuntime)
    {
        await jsRuntime.InvokeVoid("scrollSideRailToActiveItem");
    }

    public static async Task UnregisterSideRailScrollSpy(this IJSRuntime jsRuntime, string id)
    {
        await jsRuntime.InvokeVoid("unregisterSideRailScrollSpy", id);
    }

    public static async Task CopyToClipboard(this IJSRuntime jsRuntime, string codeSampleContentForCopy)
    {
        await jsRuntime.InvokeVoid("copyToClipboard", codeSampleContentForCopy);
    }

    public static async Task ApplyBodyElementClasses(this IJSRuntime jsRuntime, List<string> cssClasses, Dictionary<string, string> cssVariables)
    {
        await jsRuntime.InvokeVoid("applyBodyElementClasses", cssClasses, cssVariables);
    }

    public static async Task<string> GetInnerText(this IJSRuntime jsRuntime, ElementReference element)
    {
        return await jsRuntime.Invoke<string>("getInnerText", element);
    }

    public static async Task RegisterWindowResizeListener<T>(this IJSRuntime jsRuntime, string id, DotNetObjectReference<T> dotnetObj, string methodName) where T : class
    {
        await jsRuntime.InvokeVoid("registerWindowResizeListener", id, dotnetObj, methodName);
    }

    public static async Task UnregisterWindowResizeListener(this IJSRuntime jsRuntime, string id)
    {
        await jsRuntime.InvokeVoid("unregisterWindowResizeListener", id);
    }

    /// <summary>
    /// Claims Ctrl/Cmd+K (and a bare "/") for the search input inside the element with the given id.
    /// Registered from JS rather than through a Blazor key handler so the shortcut works no matter
    /// where the focus currently is - which is the whole point of a global shortcut.
    /// </summary>
    public static async Task RegisterSearchShortcut(this IJSRuntime jsRuntime, string rootElementId)
    {
        await jsRuntime.InvokeVoid("registerSearchShortcut", rootElementId);
    }
}
