using System.Diagnostics.CodeAnalysis;

namespace Bit.BlazorUI;

internal static class ObserversJsRuntimeExtensions
{
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(ContentRect))]
    internal static ValueTask<string> BitObserversRegisterResize<T>(this IJSRuntime jsRuntime, ElementReference element, DotNetObjectReference<T> obj, string method) where T : class
    {
        return jsRuntime.InvokeAsync<string>("BitBlazorUI.Observers.registerResize", element, obj, method);
    }

    internal static ValueTask BitObserversUnregisterResize<T>(this IJSRuntime jsRuntime, ElementReference element, string id, DotNetObjectReference<T> obj) where T : class
    {
        return jsRuntime.InvokeVoidAsync("BitBlazorUI.Observers.unregisterResize", element, id, obj);
    }
}
