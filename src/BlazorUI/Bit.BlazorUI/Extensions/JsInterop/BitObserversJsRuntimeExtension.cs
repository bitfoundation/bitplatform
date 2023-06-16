using System.Diagnostics.CodeAnalysis;
using Bit.BlazorUI;

namespace Microsoft.JSInterop;

internal static class BitObserversJsRuntimeExtension
{
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(ContentRect))]
    internal static async Task<string> RegisterResizeObserver<T>(this IJSRuntime jsRuntime, ElementReference element, DotNetObjectReference<T> obj, string method) where T : class
    {
        return await jsRuntime.InvokeAsync<string>("BitObservers.observeResize", element, obj, method);
    }

    internal static async Task UnregisterResizeObserver<T>(this IJSRuntime jsRuntime, ElementReference element, string id, DotNetObjectReference<T> obj) where T : class
    {
        await jsRuntime.InvokeVoidAsync("BitObservers.unobserveResize", element, id, obj);
    }
}
