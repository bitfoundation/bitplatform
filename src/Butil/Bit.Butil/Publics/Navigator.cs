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
    /// Returns the amount of device memory in gigabytes. This value is an approximation given by rounding to the nearest power of 2 and dividing that number by 1024.
    /// </summary>
    public async Task<float> GetDeviceMemory() => await js.NavigatorDeviceMemory();

    /// <summary>
    /// Returns the number of logical processor cores available.
    /// </summary>
    public async Task<float> GetHardwareConcurrency() => await js.NavigatorHardwareConcurrency();

    /// <summary>
    /// Returns a string representing the preferred language of the user, usually the language of the browser UI. The null value is returned when this is unknown.
    /// </summary>
    public async Task<string> GetLanguage() => await js.NavigatorLanguage();

    /// <summary>
    /// Returns an array of strings representing the languages known to the user, by order of preference.
    /// </summary>
    public async Task<string[]> GetLanguages() => await js.NavigatorLanguages();

    /// <summary>
    /// Returns the maximum number of simultaneous touch contact points are supported by the current device.
    /// </summary>
    public async Task<byte> GetMaxTouchPoints() => await js.NavigatorMaxTouchPoints();

    /// <summary>
    /// Returns a boolean value indicating whether the browser is working online.
    /// </summary>
    public async Task<bool> IsOnLine() => await js.NavigatorOnLine();

    /// <summary>
    /// Returns true if the browser can display PDF files inline when navigating to them, and false otherwise.
    /// </summary>
    public async Task<bool> IsPdfViewerEnabled() => await js.NavigatorPdfViewerEnabled();

    /// <summary>
    /// Returns the user agent string for the current browser.
    /// </summary>
    public async Task<string> GetUserAgent() => await js.NavigatorUserAgent();

    /// <summary>
    /// Indicates whether the user agent is controlled by automation.
    /// </summary>
    public async Task<bool> IsWebDriver() => await js.NavigatorWebDriver();

    /// <summary>
    /// Returns true if a call to Navigator.share() would succeed.
    /// </summary>
    public async Task<bool> CanShare() => await js.NavigatorCanShare();

    /// <summary>
    /// Clears a badge on the current app's icon and returns a Promise that resolves with undefined.
    /// </summary>
    public async Task ClearAppBadge() => await js.NavigatorClearAppBadge();

    /// <summary>
    /// Used to asynchronously transfer a small amount of data using HTTP from the User Agent to a web server.
    /// </summary>
    public async Task<bool> SendBeacon() => await js.NavigatorSendBeacon();

    /// <summary>
    /// Sets a badge on the icon associated with this app and returns a Promise that resolves with undefined.
    /// </summary>
    public async Task SetAppBadge() => await js.NavigatorSetAppBadge();

    /// <summary>
    /// Invokes the native sharing mechanism of the current platform.
    /// </summary>
    public async Task Share(ShareData data) => await js.NavigatorShare(data);

    /// <summary>
    /// Causes vibration on devices with support for it. Does nothing if vibration support isn't available.
    /// </summary>
    public async Task<bool> Vibrate(int[] pattern) => await js.NavigatorVibrate(pattern);
}
