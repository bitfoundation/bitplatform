namespace Bit.BlazorUI;

internal static class BitSwiperJsRuntimeExtensions
{
    internal static ValueTask<SwiperDimensions> BitSwiperGetDimensions(this IJSRuntime jsRuntime, ElementReference root, ElementReference swiper)
    {
        return jsRuntime.InvokeAsync<SwiperDimensions>("BitBlazorUI.Swiper.getDimensions", root, swiper);
    }

    internal static ValueTask BitSwiperRegisterPointerLeave(this IJSRuntime jsRuntime, ElementReference root, DotNetObjectReference<BitSwiper> dotnetObj)
    {
        return jsRuntime.InvokeVoidAsync("BitBlazorUI.Swiper.registerPointerLeave", root, dotnetObj);
    }
}
