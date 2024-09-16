namespace Bit.BlazorUI;

internal static class UtilsJsRuntimeExtensions
{
    internal static ValueTask Log(this IJSRuntime jsRuntime, object value)
    {
        return jsRuntime.InvokeVoid("console.log", value);
    }

    internal static ValueTask SetProperty(this IJSRuntime jsRuntime, ElementReference element, string property, object value)
    {
        return jsRuntime.InvokeVoid("BitBlazorUI.Utils.setProperty", element, property, value);
    }

    internal static ValueTask<string> GetProperty(this IJSRuntime jsRuntime, ElementReference element, string property)
    {
        return jsRuntime.Invoke<string>("BitBlazorUI.Utils.getProperty", element, property);
    }

    internal static ValueTask<int> GetClientHeight(this IJSRuntime jsRuntime, ElementReference element)
    {
        return jsRuntime.Invoke<int>("BitBlazorUI.Utils.getClientHeight", element);
    }

    internal static ValueTask<BoundingClientRect> GetBoundingClientRect(this IJSRuntime jsRuntime, ElementReference element)
    {
        return jsRuntime.Invoke<BoundingClientRect>("BitBlazorUI.Utils.getBoundingClientRect", element);
    }

    internal static ValueTask ScrollElementIntoView(this IJSRuntime jsRuntime, string targetElementId)
    {
        return jsRuntime.InvokeVoid("BitBlazorUI.Utils.scrollElementIntoView", targetElementId);
    }

    internal static ValueTask SelectText(this IJSRuntime jsRuntime, ElementReference element)
    {
        return jsRuntime.InvokeVoid("BitBlazorUI.Utils.selectText", element);
    }

    internal static ValueTask SetStyle(this IJSRuntime jsRuntime, ElementReference element, string key, string value)
    {
        return jsRuntime.InvokeVoid("BitBlazorUI.Utils.setStyle", element, key, value);
    }

    internal static ValueTask PreventDefault(this IJSRuntime jsRuntime, ElementReference element, string @event)
    {
        return jsRuntime.InvokeVoid("BitBlazorUI.Utils.preventDefault", element, @event);
    }

    internal static ValueTask<TransformMatrix> GetComputedTransform(this IJSRuntime jsRuntime, ElementReference element)
    {
        return jsRuntime.Invoke<TransformMatrix>("BitBlazorUI.Utils.getComputedTransform", element);
    }

    internal static ValueTask<int> ToggleOverflow(this IJSRuntime jsRuntime, string scrollerSelector, bool isHidden)
    {
        return jsRuntime.Invoke<int>("BitBlazorUI.Utils.toggleOverflow", scrollerSelector, isHidden);
    }
}
