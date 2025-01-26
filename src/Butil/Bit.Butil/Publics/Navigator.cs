using System.Threading.Tasks;
using Microsoft.JSInterop;

namespace Bit.Butil;

/// <summary>
/// The Navigator interface represents the state and the identity of the user agent. It allows scripts to query it and to register themselves to carry on some activities.
/// <br/>
/// More info: <see href="https://developer.mozilla.org/en-US/docs/Web/API/Navigator">https://developer.mozilla.org/en-US/docs/Web/API/Navigator</see>
/// </summary>
public class Navigator(IJSRuntime js)
{
    /// <summary>
    /// Returns the amount of device memory in gigabytes. 
    /// This value is an approximation given by rounding to the nearest power of 2 and dividing that number by 1024.
    /// <br/>
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/Navigator/deviceMemory">https://developer.mozilla.org/en-US/docs/Web/API/Navigator/deviceMemory</see>
    /// </summary>
    public async Task<float> GetDeviceMemory()
        => await js.FastInvokeAsync<float>("BitButil.navigator.deviceMemory");

    /// <summary>
    /// Returns the number of logical processor cores available.
    /// <br/>
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/Navigator/hardwareConcurrency">https://developer.mozilla.org/en-US/docs/Web/API/Navigator/hardwareConcurrency</see>
    /// </summary>
    public async Task<float> GetHardwareConcurrency()
        => await js.FastInvokeAsync<ushort>("BitButil.navigator.hardwareConcurrency");

    /// <summary>
    /// Returns a string representing the preferred language of the user, usually the language of the browser UI. 
    /// The null value is returned when this is unknown.
    /// <br/>
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/Navigator/language">https://developer.mozilla.org/en-US/docs/Web/API/Navigator/language</see>
    /// </summary>
    public async Task<string> GetLanguage()
        => await js.FastInvokeAsync<string>("BitButil.navigator.language");

    /// <summary>
    /// Returns an array of strings representing the languages known to the user, by order of preference.
    /// <br/>
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/Navigator/languages">https://developer.mozilla.org/en-US/docs/Web/API/Navigator/languages</see>
    /// </summary>
    public async Task<string[]> GetLanguages()
        => await js.FastInvokeAsync<string[]>("BitButil.navigator.languages");

    /// <summary>
    /// Returns the maximum number of simultaneous touch contact points are supported by the current device.
    /// <br/>
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/Navigator/maxTouchPoints">https://developer.mozilla.org/en-US/docs/Web/API/Navigator/maxTouchPoints</see>
    /// </summary>
    public async Task<byte> GetMaxTouchPoints()
        => await js.FastInvokeAsync<byte>("BitButil.navigator.maxTouchPoints");

    /// <summary>
    /// Returns a boolean value indicating whether the browser is working online.
    /// <br/>
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/Navigator/onLine">https://developer.mozilla.org/en-US/docs/Web/API/Navigator/onLine</see>
    /// </summary>
    public async Task<bool> IsOnLine()
        => await js.FastInvokeAsync<bool>("BitButil.navigator.onLine");

    /// <summary>
    /// Returns true if the browser can display PDF files inline when navigating to them, and false otherwise.
    /// <br/>
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/Navigator/pdfViewerEnabled">https://developer.mozilla.org/en-US/docs/Web/API/Navigator/pdfViewerEnabled</see>
    /// </summary>
    public async Task<bool> IsPdfViewerEnabled()
        => await js.FastInvokeAsync<bool>("BitButil.navigator.pdfViewerEnabled");

    /// <summary>
    /// Returns the user agent string for the current browser.
    /// <br/>
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/Navigator/userAgent">https://developer.mozilla.org/en-US/docs/Web/API/Navigator/userAgent</see>
    /// </summary>
    public async Task<string> GetUserAgent()
        => await js.FastInvokeAsync<string>("BitButil.navigator.userAgent");

    /// <summary>
    /// Indicates whether the user agent is controlled by automation.
    /// <br/>
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/Navigator/webdriver">https://developer.mozilla.org/en-US/docs/Web/API/Navigator/webdriver</see>
    /// </summary>
    public async Task<bool> IsWebDriver()
        => await js.FastInvokeAsync<bool>("BitButil.navigator.webdriver");

    /// <summary>
    /// Returns true if a call to Navigator.share() would succeed.
    /// <br/>
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/Navigator/canShare">https://developer.mozilla.org/en-US/docs/Web/API/Navigator/canShare</see>
    /// </summary>
    public async Task<bool> CanShare()
        => await js.FastInvokeAsync<bool>("BitButil.navigator.canShare");

    /// <summary>
    /// Clears a badge on the current app's icon and returns a Promise that resolves with undefined.
    /// <br/>
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/Navigator/clearAppBadge">https://developer.mozilla.org/en-US/docs/Web/API/Navigator/clearAppBadge</see>
    /// </summary>
    public async Task ClearAppBadge()
        => await js.FastInvokeVoidAsync("BitButil.navigator.clearAppBadge");

    /// <summary>
    /// Used to asynchronously transfer a small amount of data using HTTP from the User Agent to a web server.
    /// <br/>
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/Navigator/sendBeacon">https://developer.mozilla.org/en-US/docs/Web/API/Navigator/sendBeacon</see>
    /// </summary>
    public async Task<bool> SendBeacon()
        => await js.FastInvokeAsync<bool>("BitButil.navigator.sendBeacon");

    /// <summary>
    /// Sets a badge on the icon associated with this app and returns a Promise that resolves with undefined.
    /// <br/>
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/Navigator/setAppBadge">https://developer.mozilla.org/en-US/docs/Web/API/Navigator/setAppBadge</see>
    /// </summary>
    public async Task SetAppBadge()
        => await js.FastInvokeVoidAsync("BitButil.navigator.setAppBadge");

    /// <summary>
    /// Invokes the native sharing mechanism of the current platform.
    /// <br/>
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/Navigator/share">https://developer.mozilla.org/en-US/docs/Web/API/Navigator/share</see>
    /// </summary>
    public async Task Share(ShareData data)
        => await js.FastInvokeVoidAsync("BitButil.navigator.share", data);

    /// <summary>
    /// Causes vibration on devices with support for it. Does nothing if vibration support isn't available.
    /// <br/>
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/Navigator/vibrate">https://developer.mozilla.org/en-US/docs/Web/API/Navigator/vibrate</see>
    /// </summary>
    public async Task<bool> Vibrate(int[] pattern)
        => await js.FastInvokeAsync<bool>("BitButil.navigator.vibrate", pattern);
}
