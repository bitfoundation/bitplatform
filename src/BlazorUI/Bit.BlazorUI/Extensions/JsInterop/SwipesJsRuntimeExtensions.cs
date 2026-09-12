using System.Diagnostics.CodeAnalysis;

namespace Bit.BlazorUI;

internal static class SwipesJsRuntimeExtensions
{
    internal static ValueTask BitSwipesSetup<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] T>(this IJSRuntime js,
             string id,
             decimal trigger,
             BitPlacement position,
             bool isRtl,
             BitSwipeOrientation orientationLock,
             DotNetObjectReference<T>? dotnetObj,
             bool isResponsive = true,
             string scrollContainerId = "") where T : class
    {
        // Handed over by name rather than as its ordinal, so the order of BitPlacement is no contract with Swipes.ts.
        // A caller resolves its placement to one of the four edges a swipe can be set up for first (ToPanelSide).
        var edge = position switch
        {
            BitPlacement.Top => "top",
            BitPlacement.Bottom => "bottom",
            BitPlacement.Start => "start",
            _ => "end"
        };

        return js.InvokeVoid("BitBlazorUI.Swipes.setup", id, trigger, edge, isRtl, orientationLock, dotnetObj, isResponsive, scrollContainerId);
    }

    internal static ValueTask BitSwipesDispose(this IJSRuntime jsRuntime, string id)
    {
        return jsRuntime.InvokeVoid("BitBlazorUI.Swipes.dispose", id);
    }
}
