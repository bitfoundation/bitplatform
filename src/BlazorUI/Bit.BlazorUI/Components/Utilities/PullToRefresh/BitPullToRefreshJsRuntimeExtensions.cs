using System.Diagnostics.CodeAnalysis;

namespace Bit.BlazorUI;

internal static class BitPullToRefreshJsRuntimeExtensions
{
    internal static ValueTask BitPullToRefreshSetup(this IJSRuntime jsRuntime,
                                                                    ElementReference element,
                                                                    string anchor,
                                                                    int threshold,
                                                                    DotNetObjectReference<BitPullToRefresh>? dotnetObjectReference)
    {
        return jsRuntime.InvokeVoid("BitBlazorUI.PullToRefresh.setup", element, anchor, threshold, dotnetObjectReference);
    }
}
