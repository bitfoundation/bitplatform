namespace Bit.BlazorUI;

internal static class UtilsJsRuntimeExtension
{
    internal static async Task Log(this IJSRuntime jsRuntime, object value)
    {
        await jsRuntime.InvokeVoidAsync("console.log", value);
    }

    internal static async Task SetProperty(this IJSRuntime jsRuntime, ElementReference element, string property, object value)
    {
        await jsRuntime.InvokeVoidAsync("BitBlazorUI.Utils.setProperty", element, property, value);
    }

    internal static async Task<string> GetProperty(this IJSRuntime jsRuntime, ElementReference element, string property)
    {
        return await jsRuntime.InvokeAsync<string>("BitBlazorUI.Utils.getProperty", element, property);
    }

    internal static async Task<int> GetClientHeight(this IJSRuntime jsRuntime, ElementReference element)
    {
        return await jsRuntime.InvokeAsync<int>("BitBlazorUI.Utils.getClientHeight", element);
    }

    internal static async Task<BoundingClientRect> GetBoundingClientRect(this IJSRuntime jsRuntime, ElementReference element)
    {
        return await jsRuntime.InvokeAsync<BoundingClientRect>("BitBlazorUI.Utils.getBoundingClientRect", element);
    }

    internal static async Task ScrollElementIntoView(this IJSRuntime jsRuntime, string targetElementId)
    {
        await jsRuntime.InvokeVoidAsync("BitBlazorUI.Utils.scrollElementIntoView", targetElementId);
    }

    internal static async Task SelectText(this IJSRuntime jsRuntime, ElementReference element)
    {
        await jsRuntime.InvokeVoidAsync("BitBlazorUI.Utils.selectText", element);
    }

    internal static async Task SetStyle(this IJSRuntime jsRuntime, ElementReference element, string key, string value)
    {
        await jsRuntime.InvokeVoidAsync("BitBlazorUI.Utils.setStyle", element, key, value);
    }

    internal static async Task PreventDefault(this IJSRuntime jsRuntime, ElementReference element, string @event)
    {
        await jsRuntime.InvokeVoidAsync("BitBlazorUI.Utils.preventDefault", element, @event);
    }

    internal static async Task<TransformMatrix> GetComputedTransform(this IJSRuntime jsRuntime, ElementReference element)
    {
        return await jsRuntime.InvokeAsync<TransformMatrix>("BitBlazorUI.Utils.getComputedTransform", element);
    }

    internal static async Task<int> ToggleOverflow(this IJSRuntime jsRuntime, string scrollerSelector, bool isHidden)
    {
        return await jsRuntime.InvokeAsync<int>("BitBlazorUI.Utils.toggleOverflow", scrollerSelector, isHidden);
    }
}
