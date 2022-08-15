using System.Collections.Generic;
using System.Threading.Tasks;
using Bit.BlazorUI;
using Microsoft.AspNetCore.Components;

namespace Microsoft.JSInterop;

internal static class BitColorPickerJsRuntimeExtension
{
    internal static async Task<string> RegisterOnWindowMouseUpEvent(this IJSRuntime jsRuntime, BitComponentBase component, string callbackName)
    {
        return await jsRuntime.InvokeAsync<string>("BitColorPicker.registerOnWindowMouseUpEvent", DotNetObjectReference.Create(component), callbackName);
    }

    internal static async Task<string> RegisterOnWindowMouseMoveEvent(this IJSRuntime jsRuntime, BitComponentBase component, string callbackName)
    {
        return await jsRuntime.InvokeAsync<string>("BitColorPicker.registerOnWindowMouseMoveEvent", DotNetObjectReference.Create(component), callbackName);
    }

    internal static async Task AbortProcedure(this IJSRuntime jSRuntime, string abortControllerId)
    {
        await jSRuntime.InvokeVoidAsync("BitColorPicker.abortProcedure", abortControllerId);
    }
}
