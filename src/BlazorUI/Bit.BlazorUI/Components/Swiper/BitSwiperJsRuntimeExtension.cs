using System.Collections.Generic;
using System.Threading.Tasks;
using Bit.BlazorUI;
using Microsoft.AspNetCore.Components;

namespace Microsoft.JSInterop;

internal static class BitSwiperJsRuntimeExtension
{
    internal static async Task<SwiperDimensions> GetDimensions(this IJSRuntime jSRuntime, ElementReference root, ElementReference swiper)
    {
        return await jSRuntime.InvokeAsync<SwiperDimensions>("BitSwiper.getDimensions", root, swiper);
    }

    internal static async Task<SwiperDimensions> RegisterPointerLeave(this IJSRuntime jSRuntime, ElementReference root, DotNetObjectReference<BitSwiper> dotnetObj)
    {
        return await jSRuntime.InvokeAsync<SwiperDimensions>("BitSwiper.registerPointerLeave", root, dotnetObj);
    }
}
