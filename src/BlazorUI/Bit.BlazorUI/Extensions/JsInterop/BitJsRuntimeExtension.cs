using System.Collections.Generic;
using System.Threading.Tasks;
using Bit.BlazorUI;
using Microsoft.AspNetCore.Components;

namespace Microsoft.JSInterop;

internal static class BitJsRuntimeExtension
{
    internal static async Task Log(this IJSRuntime jsRuntime, object value)
    {
        await jsRuntime.InvokeVoidAsync("console.log", value);
    }

    internal static async Task SetProperty(this IJSRuntime jsRuntime, ElementReference element, string property, object value)
    {
        await jsRuntime.InvokeVoidAsync("Bit.setProperty", element, property, value);
    }

    internal static async Task<string> GetProperty(this IJSRuntime jsRuntime, ElementReference element, string property)
    {
        return await jsRuntime.InvokeAsync<string>("Bit.getProperty", element, property);
    }

    internal static async Task<int> GetClientHeight(this IJSRuntime jsRuntime, ElementReference element)
    {
        return await jsRuntime.InvokeAsync<int>("Bit.getClientHeight", element);
    }

    internal static async Task<BoundingClientRect> GetBoundingClientRect(this IJSRuntime jsRuntime, ElementReference element)
    {
        return await jsRuntime.InvokeAsync<BoundingClientRect>("Bit.getBoundingClientRect", element);
    }

    internal static async Task ScrollElementIntoView(this IJSRuntime jsRuntime, string targetElementId)
    {
        await jsRuntime.InvokeVoidAsync("Bit.scrollElementIntoView", targetElementId);
    }

    internal static async Task SelectText(this IJSRuntime jsRuntime, ElementReference element)
    {
        await jsRuntime.InvokeVoidAsync("Bit.selectText", element);
    }

    internal static async Task SetStyle(this IJSRuntime jsRuntime, ElementReference element, string key, string value)
    {
        await jsRuntime.InvokeVoidAsync("Bit.setStyle", element, key, value);
    }

    internal static async Task PreventDefault(this IJSRuntime jsRuntime, ElementReference element, string @event)
    {
        await jsRuntime.InvokeVoidAsync("Bit.preventDefault", element, @event);
    }

    internal static async Task<TransformMatrix> GetComputedTransform(this IJSRuntime jsRuntime, ElementReference element)
    {
        return await jsRuntime.InvokeAsync<TransformMatrix>("Bit.getComputedTransform", element);
    }
}
