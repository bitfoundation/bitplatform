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
    /// Watches the usable width of the element with the given id and reports it - immediately, then on
    /// every change - to the named method. The iconography grid needs it to know how many icons fit on
    /// a row, which is what lets it virtualize by row.
    /// </summary>
    public static async Task ObserveElementWidth<T>(this IJSRuntime jsRuntime, string id, DotNetObjectReference<T> dotnetObj, string methodName) where T : class
    {
        await jsRuntime.InvokeVoid("observeElementWidth", id, dotnetObj, methodName);
    }

    public static async Task UnobserveElementWidth(this IJSRuntime jsRuntime, string id)
    {
        await jsRuntime.InvokeVoid("unobserveElementWidth", id);
    }

    /// <summary>
    /// Calls the named method once the element with the given id has come within reach of the viewport,
    /// and then stops watching it. This is what lets a demo page hold back the parts of itself the
    /// reader has not reached yet - an example's live preview, the API tables - instead of building the
    /// whole page on every navigation.
    /// <para>
    /// The registration is filed under <paramref name="key"/>, not under the element id, and that is
    /// what <see cref="UnobserveVisibility"/> takes back. The ids on these pages are not unique over
    /// time - every component page has an "example1" and one "api-tables" - and a page tears its
    /// registrations down asynchronously, after the page that replaced it has already put its own in.
    /// A key belonging to the component instance is what stops one page's teardown from unwatching
    /// another page's elements, which leaves them unmounted for good.
    /// </para>
    /// </summary>
    public static async Task ObserveVisibility<T>(this IJSRuntime jsRuntime, string key, string id, DotNetObjectReference<T> dotnetObj, string methodName) where T : class
    {
        await jsRuntime.InvokeVoid("observeVisibility", key, id, dotnetObj, methodName);
    }

    public static async Task UnobserveVisibility(this IJSRuntime jsRuntime, string key)
    {
        await jsRuntime.InvokeVoid("unobserveVisibility", key);
    }

    /// <summary>
    /// The height the element with the given id takes in the document right now, asked for
    /// synchronously - or 0 when it cannot be asked at all.
    /// <para>
    /// Synchronously is the whole point: a demo page needs this while it is deciding, in OnInit,
    /// whether to hold a preview back, and the only moment that answer means anything is the one
    /// before the render batch replaces the prerendered markup it is measuring. Only the WebAssembly
    /// runtime can answer in that window - under Blazor Server, and during prerendering itself,
    /// there is no in-process runtime and this returns 0, which every caller reads as "do not hold
    /// anything back".
    /// </para>
    /// </summary>
    public static double TryGetElementHeight(this IJSRuntime jsRuntime, string id)
    {
        return jsRuntime is IJSInProcessRuntime inProcessRuntime
                ? inProcessRuntime.Invoke<double>("getElementHeight", id)
                : 0;
    }

    /// <summary>
    /// Calls the named method once, the next time the browser is idle. A demo page uses it to fill in
    /// the parts of itself the reader has not reached, one at a time, so that holding them back never
    /// leaves the page permanently incomplete.
    /// <para>
    /// As with <see cref="ObserveVisibility"/>, <paramref name="key"/> has to belong to the component
    /// instance: a key shared by every demo page means the page being navigated away from cancels the
    /// idle queue of the page that replaced it, and that page then never fills anything in - the chain
    /// that would have rescheduled it is exactly what was cancelled.
    /// </para>
    /// </summary>
    public static async Task RequestIdleWork<T>(this IJSRuntime jsRuntime, string key, DotNetObjectReference<T> dotnetObj, string methodName) where T : class
    {
        await jsRuntime.InvokeVoid("requestIdleWork", key, dotnetObj, methodName);
    }

    public static async Task CancelIdleWork(this IJSRuntime jsRuntime, string key)
    {
        await jsRuntime.InvokeVoid("cancelIdleWork", key);
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
