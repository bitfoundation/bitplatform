using System.Diagnostics.CodeAnalysis;

namespace Bit.BlazorUI;

internal static class ObserversJsRuntimeExtension
{
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(ContentRect))]
    internal static async Task<string> BitObserversRegisterResize<T>(this IJSRuntime jsRuntime, ElementReference element, DotNetObjectReference<T> obj, string method) where T : class
    {
        return await jsRuntime.InvokeAsync<string>("BitBlazorUI.Observers.registerResize", element, obj, method);
    }

    internal static async Task BitObserversUnregisterResize<T>(this IJSRuntime jsRuntime, ElementReference element, string id, DotNetObjectReference<T> obj) where T : class
    {
        await jsRuntime.InvokeVoidAsync("BitBlazorUI.Observers.unregisterResize", element, id, obj);
    }
}
