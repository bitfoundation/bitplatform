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

    internal static async Task<string> WindowAtob(this IJSRuntime js, string data)
        => await js.InvokeAsync<string>("BitButil.window.atob", data);

    internal static async Task WindowAlert(this IJSRuntime js, string? message)
        => await js.InvokeVoidAsync("BitButil.window.alert", message);

    internal static async Task WindowBlur(this IJSRuntime js)
        => await js.InvokeVoidAsync("BitButil.window.blur");

    internal static async Task<string> WindowBtoa(this IJSRuntime js, string data)
        => await js.InvokeAsync<string>("BitButil.window.btoa", data);

    internal static async Task WindowClose(this IJSRuntime js)
        => await js.InvokeVoidAsync("BitButil.window.close");

    internal static async Task<bool> WindowConfirm(this IJSRuntime js, string? message)
        => await js.InvokeAsync<bool>("BitButil.window.confirm", message);

    internal static async Task<bool> WindowFind(this IJSRuntime js,
        string? text,
        bool? caseSensitive,
        bool? backward,
        bool? wrapAround,
        bool? wholeWord,
        bool? searchInFrame)
        => await js.InvokeAsync<bool>("BitButil.window.find", text, caseSensitive, backward, wrapAround, wholeWord, searchInFrame);

    internal static async Task WindowFocus(this IJSRuntime js)
        => await js.InvokeVoidAsync("BitButil.window.focus");

    internal static async Task<string> WindowGetSelection(this IJSRuntime js)
        => await js.InvokeAsync<string>("BitButil.window.getSelection");

    internal static async Task<MediaQueryList> WindowMatchMedia(this IJSRuntime js, string query)
        => await js.InvokeAsync<MediaQueryList>("BitButil.window.matchMedia", query);

    internal static async Task<bool> WindowOpen(this IJSRuntime js, string? url, string? target, string? windowFeatures)
        => await js.InvokeAsync<bool>("BitButil.window.open", url, target, windowFeatures);
    internal static async Task<bool> WindowOpen(this IJSRuntime js, string? url, string? target, WindowFeatures? windowFeatures)
        => await js.InvokeAsync<bool>("BitButil.window.open", url, target, windowFeatures?.ToString());

    internal static async Task WindowPrint(this IJSRuntime js)
        => await js.InvokeVoidAsync("BitButil.window.print");

    internal static async Task<string> WindowPrompt(this IJSRuntime js, string? message, string? defaultValue)
        => await js.InvokeAsync<string>("BitButil.window.prompt", message, defaultValue);

    internal static async Task WindowScroll(this IJSRuntime js, ScrollToOptions? options, float? x, float? y)
        => await js.InvokeVoidAsync("BitButil.window.scroll", options?.ToJsObject(), x, y);

    internal static async Task WindowScrollBy(this IJSRuntime js, ScrollToOptions? options, float? x, float? y)
        => await js.InvokeVoidAsync("BitButil.window.scrollBy", options?.ToJsObject(), x, y);

    internal static async Task WindowStop(this IJSRuntime js)
        => await js.InvokeVoidAsync("BitButil.window.stop");
}
