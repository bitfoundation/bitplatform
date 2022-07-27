using System.Collections.Generic;
using System.Threading.Tasks;
using Bit.BlazorUI;
using Microsoft.AspNetCore.Components;

namespace Microsoft.JSInterop;

internal static class JsRuntimeExtension
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

    internal static async Task<string> RegisterOnWindowMouseUpEvent(this IJSRuntime jsRuntime, BitComponentBase dontetHelper, string callbackName)
    {

        return await jsRuntime.InvokeAsync<string>("BitColorPicker.registerOnWindowMouseUpEvent", DotNetObjectReference.Create(dontetHelper),
            callbackName);
    }
    internal static async Task<string> RegisterOnWindowMouseMoveEvent(this IJSRuntime jsRuntime, BitComponentBase dontetHelper, string callbackName)
    {
        return await jsRuntime.InvokeAsync<string>("BitColorPicker.registerOnWindowMouseMoveEvent", DotNetObjectReference.Create(dontetHelper),
             callbackName);
    }

    internal static async Task AbortProcedure(this IJSRuntime jSRuntime, string abortControllerId)
    {
        await jSRuntime.InvokeVoidAsync("BitColorPicker.abortProcedure", abortControllerId);
    }

    internal static async Task BitLinkScrollToFragmentOnClickEvent(this IJSRuntime jsRuntime, string targetElementId)
    {
        await jsRuntime.InvokeVoidAsync("BitLink.scrollToFragmentOnClickEvent", targetElementId);
    }

    internal static async Task SelectText(this IJSRuntime jsRuntime, ElementReference element)
    {
        await jsRuntime.InvokeVoidAsync("Bit.selectText", element);
    }
}
