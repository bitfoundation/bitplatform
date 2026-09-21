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

    // Takes the observer off by id alone. It used to be handed the element and the .NET reference as well,
    // the element to unobserve and the reference to release - but an element that is already gone with its
    // parent made the unobserve miss, and releasing the reference here took down a component that was still
    // on the page and still using it (see Observers.unregisterResize). The reference is the component's to
    // dispose, and disconnecting needs nothing but the id.
    internal static ValueTask BitObserversUnregisterResize(this IJSRuntime jsRuntime, string id)
    {
        return jsRuntime.InvokeVoid("BitBlazorUI.Observers.unregisterResize", id);
    }


    internal static ValueTask BitObserversRegisterIntersection<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] T>(
        this IJSRuntime jsRuntime,
        string id,
        ElementReference element,
        DotNetObjectReference<T> obj,
        string rootMargin) where T : class
    {
        return jsRuntime.InvokeVoid("BitBlazorUI.Observers.registerIntersection", id, element, obj, rootMargin);
    }

    internal static ValueTask BitObserversUnregisterIntersection(this IJSRuntime jsRuntime, string id)
    {
        return jsRuntime.InvokeVoid("BitBlazorUI.Observers.unregisterIntersection", id);
    }
}
