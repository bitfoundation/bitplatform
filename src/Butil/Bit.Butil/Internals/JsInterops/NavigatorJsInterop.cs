using System.Threading.Tasks;
using Microsoft.JSInterop;

namespace Bit.Butil;

internal static class NavigatorJsInterop
{
    internal static async Task<float> NavigatorDeviceMemory(this IJSRuntime js) => await js.InvokeAsync<float>("BitButil.navigator.deviceMemory");

    internal static async Task<ushort> NavigatorHardwareConcurrency(this IJSRuntime js) => await js.InvokeAsync<ushort>("BitButil.navigator.hardwareConcurrency");

    internal static async Task<string> NavigatorLanguage(this IJSRuntime js) => await js.InvokeAsync<string>("BitButil.navigator.language");

    internal static async Task<string[]> NavigatorLanguages(this IJSRuntime js) => await js.InvokeAsync<string[]>("BitButil.navigator.languages");

    internal static async Task<byte> NavigatorMaxTouchPoints(this IJSRuntime js) => await js.InvokeAsync<byte>("BitButil.navigator.maxTouchPoints");

    internal static async Task<bool> NavigatorOnLine(this IJSRuntime js) => await js.InvokeAsync<bool>("BitButil.navigator.onLine");

    internal static async Task<bool> NavigatorPdfViewerEnabled(this IJSRuntime js) => await js.InvokeAsync<bool>("BitButil.navigator.pdfViewerEnabled");

    internal static async Task<string> NavigatorUserAgent(this IJSRuntime js) => await js.InvokeAsync<string>("BitButil.navigator.userAgent");

    internal static async Task<bool> NavigatorWebDriver(this IJSRuntime js) => await js.InvokeAsync<bool>("BitButil.navigator.webdriver");

    internal static async Task<bool> NavigatorCanShare(this IJSRuntime js) => await js.InvokeAsync<bool>("BitButil.navigator.canShare");

    internal static async Task NavigatorClearAppBadge(this IJSRuntime js) => await js.InvokeVoidAsync("BitButil.navigator.clearAppBadge");

    internal static async Task<bool> NavigatorSendBeacon(this IJSRuntime js) => await js.InvokeAsync<bool>("BitButil.navigator.sendBeacon");

    internal static async Task NavigatorSetAppBadge(this IJSRuntime js) => await js.InvokeVoidAsync("BitButil.navigator.setAppBadge");

    internal static async Task NavigatorShare(this IJSRuntime js, ShareData data) => await js.InvokeVoidAsync("BitButil.navigator.share", data);

    internal static async Task<bool> NavigatorVibrate(this IJSRuntime js, int[] pattern) => await js.InvokeAsync<bool>("BitButil.navigator.vibrate", pattern);
}
