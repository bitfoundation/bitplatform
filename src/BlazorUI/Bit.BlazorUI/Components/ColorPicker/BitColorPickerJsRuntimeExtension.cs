using System.Collections.Generic;
using System.Threading.Tasks;
using Bit.BlazorUI;
using Microsoft.AspNetCore.Components;

namespace Microsoft.JSInterop;

internal static class BitColorPickerJsRuntimeExtension
{
    internal static async Task<string> RegisterOnWindowPointerUpEvent(this IJSRuntime jsRuntime, BitComponentBase component, string callbackName)
    {
        return await jsRuntime.InvokeAsync<string>("BitColorPicker.registerOnWindowPointerUpEvent", DotNetObjectReference.Create(component), callbackName);
    }

    internal static async Task<string> RegisterOnWindowPointerMoveEvent(this IJSRuntime jsRuntime, BitComponentBase component, string callbackName)
    {
        return await jsRuntime.InvokeAsync<string>("BitColorPicker.registerOnWindowPointerMoveEvent", DotNetObjectReference.Create(component), callbackName);
    }

    internal static async Task AbortProcedure(this IJSRuntime jSRuntime, string? abortControllerId)
    {
        await jSRuntime.InvokeVoidAsync("BitColorPicker.abortProcedure", abortControllerId);
    }
}
