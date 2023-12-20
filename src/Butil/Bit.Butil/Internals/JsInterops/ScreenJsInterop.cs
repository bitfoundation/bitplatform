using System;
using System.Threading.Tasks;
using Microsoft.JSInterop;

namespace Bit.Butil;

internal static class ScreenJsInterop
{
    internal static async Task<float> ScreenGetAvailableHeight(this IJSRuntime js) 
        => await js.InvokeAsync<float>("BitButil.screen.availHeight");

    internal static async Task<float> ScreenGetAvailableWidth(this IJSRuntime js)
        => await js.InvokeAsync<float>("BitButil.screen.availWidth");

    internal static async Task<byte> ScreenGetColorDepth(this IJSRuntime js)
        => await js.InvokeAsync<byte>("BitButil.screen.colorDepth");

    internal static async Task<float> ScreenGetHeight(this IJSRuntime js)
        => await js.InvokeAsync<float>("BitButil.screen.height");

    internal static async Task<bool> ScreenIsExtended(this IJSRuntime js)
        => await js.InvokeAsync<bool>("BitButil.screen.isExtended");

    internal static async Task<byte> ScreenGetPixelDepth(this IJSRuntime js)
        => await js.InvokeAsync<byte>("BitButil.screen.pixelDepth");

    internal static async Task<float> ScreenGetWidth(this IJSRuntime js)
        => await js.InvokeAsync<float>("BitButil.screen.width");

    internal static async Task ScreenAddChange(this IJSRuntime js, string methodName, Guid listenerId)
        => await js.InvokeVoidAsync("BitButil.screen.addChange", methodName, listenerId);

    internal static async Task ScreenRemoveChange(this IJSRuntime js, Guid[] ids)
        => await js.InvokeVoidAsync("BitButil.screen.removeChange", ids);
}
