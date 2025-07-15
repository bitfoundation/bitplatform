using System.Diagnostics.CodeAnalysis;

namespace Bit.BlazorUI;

internal static class SwipesJsRuntimeExtensions
{
    internal static ValueTask BitSwipesSetup<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] T>(this IJSRuntime js,
             string id,
             decimal trigger,
             BitPanelPosition position,
             bool isRtl,
             BitSwipeOrientation orientationLock,
             DotNetObjectReference<T>? dotnetObj,
             bool isResponsive = true,
             string scrollContainerId = "") where T : class
    {
        return js.InvokeVoid("BitBlazorUI.Swipes.setup", id, trigger, position, isRtl, orientationLock, dotnetObj, isResponsive, scrollContainerId);
    }

    internal static ValueTask BitSwipesDispose(this IJSRuntime jsRuntime, string id)
    {
        return jsRuntime.InvokeVoid("BitBlazorUI.Swipes.dispose", id);
    }
}
