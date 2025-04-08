namespace Bit.BlazorUI;

internal static class BitSwiperJsRuntimeExtensions
{
    internal static ValueTask BitSwiperRegisterSetup(this IJSRuntime jsRuntime, string id, ElementReference element, DotNetObjectReference<BitSwiper> dotnetObj)
    {
        return jsRuntime.InvokeVoid("BitBlazorUI.Swiper.setup", id, element, dotnetObj);
    }

    internal static ValueTask<SwiperDimensions> BitSwiperGetDimensions(this IJSRuntime jsRuntime, ElementReference root, ElementReference container)
    {
        return jsRuntime.Invoke<SwiperDimensions>("BitBlazorUI.Swiper.getDimensions", root, container);
    }

    internal static ValueTask BitSwiperDispose(this IJSRuntime jsRuntime, string id)
    {
        return jsRuntime.InvokeVoid("BitBlazorUI.Swiper.dispose", id);
    }
}
