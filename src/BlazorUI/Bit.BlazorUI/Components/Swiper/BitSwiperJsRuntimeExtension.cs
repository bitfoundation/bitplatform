namespace Bit.BlazorUI;

internal static class BitSwiperJsRuntimeExtension
{
    internal static async Task<SwiperDimensions> BitSwiperGetDimensions(this IJSRuntime jSRuntime, ElementReference root, ElementReference swiper)
    {
        return await jSRuntime.InvokeAsync<SwiperDimensions>("BitBlazorUI.Swiper.getDimensions", root, swiper);
    }

    internal static async Task BitSwiperRegisterPointerLeave(this IJSRuntime jSRuntime, ElementReference root, DotNetObjectReference<BitSwiper> dotnetObj)
    {
        await jSRuntime.InvokeVoidAsync("BitBlazorUI.Swiper.registerPointerLeave", root, dotnetObj);
    }
}
