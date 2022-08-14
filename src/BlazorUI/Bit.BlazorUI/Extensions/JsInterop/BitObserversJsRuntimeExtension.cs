using Bit.BlazorUI;

namespace Microsoft.JSInterop;

internal static class BitObserversJsRuntimeExtension
{
    internal static async Task<string> RegisterResizeObserver<T>(this IJSRuntime jsRuntime, ElementReference element, DotNetObjectReference<T> obj, string method) where T : class
    {
        return await jsRuntime.InvokeAsync<string>("BitObservers.resize", element, obj, method);
    }

    internal static async Task UnregisterResizeObserver(this IJSRuntime jsRuntime, ElementReference element, string id)
    {
        await jsRuntime.InvokeVoidAsync("BitObservers.unresize", element, id);
    }
}
