using System.Diagnostics.CodeAnalysis;

namespace Bit.BlazorUI;

internal static class SwipesJsRuntimeExtensions
{
    internal static ValueTask SwipesSetup<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] T>(this IJSRuntime js,
                                               string id,
                                               decimal trigger,
                                               SwipesPosition position,
                                               bool isRtl,
                                               DotNetObjectReference<T>? dotnetObj) where T : class
    {
        return js.InvokeVoid("BitBlazorUI.Swipes.setup", id, trigger, position, isRtl, dotnetObj);
    }

    internal static ValueTask SwipesDispose(this IJSRuntime jsRuntime, string id)
    {
        return jsRuntime.InvokeVoid("BitBlazorUI.Swipes.dispose", id);
    }
}
