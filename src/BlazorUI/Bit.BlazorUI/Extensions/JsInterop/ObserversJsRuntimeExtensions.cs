using System.Diagnostics.CodeAnalysis;

namespace Bit.BlazorUI;

[UnconditionalSuppressMessage("Trimming", "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code", Justification = "<Pending>")]
internal static class ObserversJsRuntimeExtensions
{
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(ContentRect))]
    internal static ValueTask<string> BitObserversRegisterResize<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] T>(
        this IJSRuntime jsRuntime,
        string id,
        ElementReference element,
        DotNetObjectReference<T> obj) where T : class
    {
        return jsRuntime.FastInvoke<string>("BitBlazorUI.Observers.registerResize", id, element, obj);
    }

    internal static ValueTask BitObserversUnregisterResize<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] T>(
        this IJSRuntime jsRuntime,
        string id,
        ElementReference element,
        DotNetObjectReference<T> obj) where T : class
    {
        return jsRuntime.FastInvokeVoid("BitBlazorUI.Observers.unregisterResize", id, element, obj);
    }
}
