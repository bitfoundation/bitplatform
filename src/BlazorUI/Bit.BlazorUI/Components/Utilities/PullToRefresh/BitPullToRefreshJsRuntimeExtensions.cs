using System.Diagnostics.CodeAnalysis;

namespace Bit.BlazorUI;

internal static class BitPullToRefreshJsRuntimeExtensions
{
    internal static ValueTask BitPullToRefreshSetup(this IJSRuntime jsRuntime,
                                                                    string id,
                                                                    ElementReference? anchor,
                                                                    ElementReference loading,
                                                                    ElementReference? scrollerElement,
                                                                    string? scrollerSelector,
                                                                    int trigger,
                                                                    decimal factor,
                                                                    int margin,
                                                                    int threshold,
                                                                    DotNetObjectReference<BitPullToRefresh>? dotnetObjectReference)
    {
        return jsRuntime.InvokeVoid("BitBlazorUI.PullToRefresh.setup", id, anchor, loading, scrollerElement, scrollerSelector, trigger, factor, margin , threshold, dotnetObjectReference);
    }

    internal static ValueTask BitPullToRefreshDispose(this IJSRuntime jsRuntime, string id)
    {
        return jsRuntime.InvokeVoid("BitBlazorUI.PullToRefresh.dispose", id);
    }
}
