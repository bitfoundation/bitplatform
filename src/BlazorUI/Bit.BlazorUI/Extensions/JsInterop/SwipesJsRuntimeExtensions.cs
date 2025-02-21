using System.Diagnostics.CodeAnalysis;

namespace Bit.BlazorUI;

[UnconditionalSuppressMessage("Trimming", "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code", Justification = "<Pending>")]
internal static class SwipesJsRuntimeExtensions
{
    internal static ValueTask BitSwipesSetup<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] T>(
        this IJSRuntime jsRuntime,
             string id,
             decimal trigger,
             BitPanelPosition position,
             bool isRtl,
             BitSwipeOrientation orientationLock,
             DotNetObjectReference<T>? dotnetObj,
             bool isResponsive = true) where T : class
    {
        return jsRuntime.FastInvokeVoid("BitBlazorUI.Swipes.setup", id, trigger, position, isRtl, orientationLock, dotnetObj, isResponsive);
    }

    internal static ValueTask BitSwipesDispose(this IJSRuntime jsRuntime, string id)
    {
        return jsRuntime.FastInvokeVoid("BitBlazorUI.Swipes.dispose", id);
    }
}
