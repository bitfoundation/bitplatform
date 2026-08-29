using System.Diagnostics.CodeAnalysis;

namespace Bit.BlazorUI;

internal static class BitScrollablePaneJsRuntimeExtensions
{
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(BitScrollablePaneOptions))]
    internal static ValueTask BitScrollablePaneSetup(this IJSRuntime jsRuntime,
                                                     string id,
                                                     ElementReference element,
                                                     DotNetObjectReference<BitScrollablePane> dotnetObj,
                                                     BitScrollablePaneOptions options)
    {
        return jsRuntime.InvokeVoid("BitBlazorUI.ScrollablePane.setup", id, element, dotnetObj, options);
    }

    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(BitScrollablePaneOptions))]
    internal static ValueTask BitScrollablePaneUpdate(this IJSRuntime jsRuntime, string id, BitScrollablePaneOptions options)
    {
        return jsRuntime.InvokeVoid("BitBlazorUI.ScrollablePane.update", id, options);
    }

    internal static ValueTask BitScrollablePaneRefresh(this IJSRuntime jsRuntime, string id)
    {
        return jsRuntime.InvokeVoid("BitBlazorUI.ScrollablePane.refresh", id);
    }

    internal static ValueTask BitScrollablePaneAutoScroll(this IJSRuntime jsRuntime, string id, bool force)
    {
        return jsRuntime.InvokeVoid("BitBlazorUI.ScrollablePane.autoScroll", id, force);
    }

    internal static ValueTask BitScrollablePaneDispose(this IJSRuntime jsRuntime, string id)
    {
        return jsRuntime.InvokeVoid("BitBlazorUI.ScrollablePane.dispose", id);
    }



    internal static ValueTask BitScrollablePaneScrollToEnd(this IJSRuntime jsRuntime, ElementReference element, bool smooth)
    {
        return jsRuntime.InvokeVoid("BitBlazorUI.ScrollablePane.scrollToEnd", element, smooth);
    }

    internal static ValueTask BitScrollablePaneScrollToStart(this IJSRuntime jsRuntime, ElementReference element, bool smooth)
    {
        return jsRuntime.InvokeVoid("BitBlazorUI.ScrollablePane.scrollToStart", element, smooth);
    }

    internal static ValueTask BitScrollablePaneScrollTo(this IJSRuntime jsRuntime, ElementReference element, double? left, double? top, bool smooth)
    {
        return jsRuntime.InvokeVoid("BitBlazorUI.ScrollablePane.scrollTo", element, left, top, smooth);
    }

    internal static ValueTask BitScrollablePaneScrollBy(this IJSRuntime jsRuntime, ElementReference element, double x, double y, bool smooth)
    {
        return jsRuntime.InvokeVoid("BitBlazorUI.ScrollablePane.scrollBy", element, x, y, smooth);
    }

    internal static ValueTask BitScrollablePaneScrollToElement(this IJSRuntime jsRuntime, ElementReference element, string elementId, double offset, bool smooth)
    {
        return jsRuntime.InvokeVoid("BitBlazorUI.ScrollablePane.scrollToElement", element, elementId, offset, smooth);
    }

    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(BitScrollOffset))]
    internal static ValueTask<BitScrollOffset?> BitScrollablePaneGetOffset(this IJSRuntime jsRuntime, ElementReference element)
    {
        return jsRuntime.Invoke<BitScrollOffset?>("BitBlazorUI.ScrollablePane.getOffset", element);
    }
}
