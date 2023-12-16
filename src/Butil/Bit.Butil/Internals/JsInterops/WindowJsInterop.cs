using System.Threading.Tasks;
using Microsoft.JSInterop;

namespace Bit.Butil;

internal static class WindowJsInterop
{
    internal static async Task WindowAddBeforeUnload(this IJSRuntime js)
        => await js.InvokeVoidAsync("BitButil.window.addBeforeUnload");

    internal static async Task WindowRemoveBeforeUnload(this IJSRuntime js)
        => await js.InvokeVoidAsync("BitButil.window.removeBeforeUnload");

    internal static async Task<float> WindowGetInnerHeight(this IJSRuntime js)
        => await js.InvokeAsync<float>("BitButil.window.innerHeight");

    internal static async Task<float> WindowGetInnerWidth(this IJSRuntime js)
        => await js.InvokeAsync<float>("BitButil.window.innerWidth");

    internal static async Task<bool> WindowIsSecureContext(this IJSRuntime js)
        => await js.InvokeAsync<bool>("BitButil.window.isSecureContext");

    internal static async Task<BarProp> WindowLocationBar(this IJSRuntime js)
        => await js.InvokeAsync<BarProp>("BitButil.window.locationbar");

    internal static async Task<string> WindowGetName(this IJSRuntime js)
        => await js.InvokeAsync<string>("BitButil.window.getName");
    internal static async Task WindowSetName(this IJSRuntime js, string value)
        => await js.InvokeVoidAsync("BitButil.window.setName", value);

    internal static async Task<string> WindowGetOrigin(this IJSRuntime js)
        => await js.InvokeAsync<string>("BitButil.window.origin");

    internal static async Task<float> WindowGetOuterHeight(this IJSRuntime js)
        => await js.InvokeAsync<float>("BitButil.window.outerHeight");

    internal static async Task<float> WindowGetOuterWidth(this IJSRuntime js)
        => await js.InvokeAsync<float>("BitButil.window.outerWidth");

    internal static async Task<float> WindowGetScreenX(this IJSRuntime js)
        => await js.InvokeAsync<float>("BitButil.window.screenX");

    internal static async Task<float> WindowGetScreenY(this IJSRuntime js)
        => await js.InvokeAsync<float>("BitButil.window.screenY");

    internal static async Task<float> WindowGetScrollX(this IJSRuntime js)
        => await js.InvokeAsync<float>("BitButil.window.scrollX");

    internal static async Task<float> WindowGetScrollY(this IJSRuntime js)
        => await js.InvokeAsync<float>("BitButil.window.scrollY");
}
