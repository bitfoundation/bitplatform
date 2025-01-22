using System.Diagnostics.CodeAnalysis;

namespace Bit.BlazorUI;

internal static class ObserversJsRuntimeExtensions
{
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(ContentRect))]
    internal static ValueTask<string> BitObserversRegisterResize<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] T>(
        this IJSRuntime jsRuntime, 
        string id, 
        ElementReference element, 
        DotNetObjectReference<T> obj) where T : class
    {
        return jsRuntime.Invoke<string>("BitBlazorUI.Observers.registerResize", id, element, obj);
    }

    internal static ValueTask BitObserversUnregisterResize<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] T>(
        this IJSRuntime jsRuntime, 
        string id, 
        ElementReference element, 
        DotNetObjectReference<T> obj) where T : class
    {
        return jsRuntime.InvokeVoid("BitBlazorUI.Observers.unregisterResize", id, element, obj);
    }
}
