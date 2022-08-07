using System.Collections.Generic;
using System.Threading.Tasks;
using Bit.BlazorUI;
using Microsoft.AspNetCore.Components;

namespace Microsoft.JSInterop;

internal static class BitSwiperJsRuntimeExtension
{
    internal static async Task<SwiperDimensions> GetSwiperDimensions(this IJSRuntime jSRuntime, ElementReference swiper)
    {
        return await jSRuntime.InvokeAsync<SwiperDimensions>("BitSwiper.getSwiperDimensions", swiper);
    }

    internal static async Task<SwiperDimensions> RegisterPointerLeave(this IJSRuntime jSRuntime, ElementReference root, DotNetObjectReference<BitSwiper> dotnetObj)
    {
        return await jSRuntime.InvokeAsync<SwiperDimensions>("BitSwiper.registerPointerLeave", root, dotnetObj);
    }
}
