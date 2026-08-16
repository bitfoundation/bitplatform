using System.Diagnostics.CodeAnalysis;

namespace Bit.BlazorUI;

internal static class BitSwiperJsRuntimeExtensions
{
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(BitSwiperState))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(BitSwiperOptions))]
    internal static ValueTask BitSwiperSetup(this IJSRuntime jsRuntime,
                                             string id,
                                             ElementReference root,
                                             ElementReference container,
                                             DotNetObjectReference<BitSwiper> dotnetObj,
                                             BitSwiperOptions options)
    {
        return jsRuntime.InvokeVoid("BitBlazorUI.Swiper.setup", id, root, container, dotnetObj, options);
    }

    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(BitSwiperOptions))]
    internal static ValueTask BitSwiperUpdate(this IJSRuntime jsRuntime, string id, BitSwiperOptions options)
    {
        return jsRuntime.InvokeVoid("BitBlazorUI.Swiper.update", id, options);
    }

    internal static ValueTask BitSwiperRefresh(this IJSRuntime jsRuntime, string id)
    {
        return jsRuntime.InvokeVoid("BitBlazorUI.Swiper.refresh", id);
    }

    internal static ValueTask BitSwiperGo(this IJSRuntime jsRuntime, string id, bool forward, int count)
    {
        return jsRuntime.InvokeVoid("BitBlazorUI.Swiper.go", id, forward, count);
    }

    internal static ValueTask BitSwiperGoToItem(this IJSRuntime jsRuntime, string id, int index)
    {
        return jsRuntime.InvokeVoid("BitBlazorUI.Swiper.goToItem", id, index);
    }

    internal static ValueTask BitSwiperGoToPage(this IJSRuntime jsRuntime, string id, int page)
    {
        return jsRuntime.InvokeVoid("BitBlazorUI.Swiper.goToPage", id, page);
    }

    internal static ValueTask BitSwiperGoToEdge(this IJSRuntime jsRuntime, string id, bool end)
    {
        return jsRuntime.InvokeVoid("BitBlazorUI.Swiper.goToEdge", id, end);
    }

    internal static ValueTask BitSwiperDispose(this IJSRuntime jsRuntime, string id)
    {
        return jsRuntime.InvokeVoid("BitBlazorUI.Swiper.dispose", id);
    }
}
