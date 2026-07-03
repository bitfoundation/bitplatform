using System.Diagnostics.CodeAnalysis;

namespace Bit.BlazorUI;

internal static class PivotJsRuntimeExtensions
{
    internal static ValueTask BitPivotSetup<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] T>(
        this IJSRuntime jsRuntime,
        string id,
        ElementReference header,
        ElementReference? moreButton,
        bool isMenu,
        bool isSlide,
        bool isRtl,
        bool isVertical,
        DotNetObjectReference<T> dotnetObj) where T : class
    {
        return jsRuntime.InvokeVoid("BitBlazorUI.Pivot.setup", id, header, moreButton, isMenu, isSlide, isRtl, isVertical, dotnetObj);
    }

    internal static ValueTask BitPivotRefresh(this IJSRuntime jsRuntime, string id)
    {
        return jsRuntime.InvokeVoid("BitBlazorUI.Pivot.refresh", id);
    }

    internal static ValueTask BitPivotSlide(this IJSRuntime jsRuntime, string id, bool forward)
    {
        return jsRuntime.InvokeVoid("BitBlazorUI.Pivot.slide", id, forward);
    }

    internal static ValueTask BitPivotDispose(this IJSRuntime jsRuntime, string id)
    {
        return jsRuntime.InvokeVoid("BitBlazorUI.Pivot.dispose", id);
    }
}
