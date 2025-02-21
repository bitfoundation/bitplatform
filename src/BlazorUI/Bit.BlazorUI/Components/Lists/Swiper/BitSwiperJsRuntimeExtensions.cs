using System.Diagnostics.CodeAnalysis;

namespace Bit.BlazorUI;

[UnconditionalSuppressMessage("Trimming", "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code", Justification = "<Pending>")]
internal static class BitSwiperJsRuntimeExtensions
{
    internal static ValueTask<SwiperDimensions> BitSwiperGetDimensions(this IJSRuntime jsRuntime, ElementReference root, ElementReference swiper)
    {
        return jsRuntime.FastInvoke<SwiperDimensions>("BitBlazorUI.Swiper.getDimensions", root, swiper);
    }

    internal static ValueTask BitSwiperRegisterPointerLeave(this IJSRuntime jsRuntime, ElementReference root, DotNetObjectReference<BitSwiper> dotnetObj)
    {
        return jsRuntime.FastInvokeVoid("BitBlazorUI.Swiper.registerPointerLeave", root, dotnetObj);
    }
}
