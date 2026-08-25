using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Microsoft.JSInterop;

namespace Bit.Butil;

/// <summary>
/// The Navigator interface represents the state and the identity of the user agent. It allows scripts to query it and to register themselves to carry on some activities.
/// <br/>
/// More info: <see href="https://developer.mozilla.org/en-US/docs/Web/API/Navigator">https://developer.mozilla.org/en-US/docs/Web/API/Navigator</see>
/// </summary>
[ButilService(typeof(Navigator))]
public class Navigator(IJSRuntime js)
{
    /// <summary>
    /// Returns the amount of device memory in gigabytes. 
    /// This value is an approximation given by rounding to the nearest power of 2 and dividing that number by 1024.
    /// <br/>
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/Navigator/deviceMemory">https://developer.mozilla.org/en-US/docs/Web/API/Navigator/deviceMemory</see>
    /// </summary>
    /// <remarks>
    /// During prerender/SSR (no JS runtime) this returns <c>default</c> (e.g. <c>false</c>/<c>0</c>)
    /// rather than throwing, so the result can't be distinguished from a genuine value. If you
    /// branch on it, defer the read to <c>OnAfterRenderAsync</c>.
    /// </remarks>
    public async Task<float> GetDeviceMemory()
        => await js.Invoke<float>("BitButil.navigator.deviceMemory");

    /// <summary>
    /// Returns the number of logical processor cores available.
    /// <br/>
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/Navigator/hardwareConcurrency">https://developer.mozilla.org/en-US/docs/Web/API/Navigator/hardwareConcurrency</see>
    /// </summary>
    /// <remarks>
    /// During prerender/SSR (no JS runtime) this returns <c>default</c> (e.g. <c>false</c>/<c>0</c>)
    /// rather than throwing, so the result can't be distinguished from a genuine value. If you
    /// branch on it, defer the read to <c>OnAfterRenderAsync</c>.
    /// </remarks>
    public async Task<float> GetHardwareConcurrency()
        => await js.Invoke<ushort>("BitButil.navigator.hardwareConcurrency");

    /// <summary>
    /// Returns a string representing the preferred language of the user, usually the language of the browser UI. 
    /// The null value is returned when this is unknown.
    /// <br/>
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/Navigator/language">https://developer.mozilla.org/en-US/docs/Web/API/Navigator/language</see>
    /// </summary>
    public async Task<string> GetLanguage()
        => await js.Invoke<string>("BitButil.navigator.language");

    /// <summary>
    /// Returns an array of strings representing the languages known to the user, by order of preference.
    /// <br/>
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/Navigator/languages">https://developer.mozilla.org/en-US/docs/Web/API/Navigator/languages</see>
    /// </summary>
    public async Task<string[]> GetLanguages()
        => await js.Invoke<string[]>("BitButil.navigator.languages");

    /// <summary>
    /// Returns the maximum number of simultaneous touch contact points are supported by the current device.
    /// <br/>
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/Navigator/maxTouchPoints">https://developer.mozilla.org/en-US/docs/Web/API/Navigator/maxTouchPoints</see>
    /// </summary>
    /// <remarks>
    /// During prerender/SSR (no JS runtime) this returns <c>default</c> (e.g. <c>false</c>/<c>0</c>)
    /// rather than throwing, so the result can't be distinguished from a genuine value. If you
    /// branch on it, defer the read to <c>OnAfterRenderAsync</c>.
    /// </remarks>
    public async Task<byte> GetMaxTouchPoints()
        => await js.Invoke<byte>("BitButil.navigator.maxTouchPoints");

    /// <summary>
    /// Returns a boolean value indicating whether the browser is working online.
    /// <br/>
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/Navigator/onLine">https://developer.mozilla.org/en-US/docs/Web/API/Navigator/onLine</see>
    /// </summary>
    /// <remarks>
    /// During prerender/SSR (no JS runtime) this returns <c>default</c> (e.g. <c>false</c>/<c>0</c>)
    /// rather than throwing, so the result can't be distinguished from a genuine value. If you
    /// branch on it, defer the read to <c>OnAfterRenderAsync</c>.
    /// </remarks>
    public async Task<bool> IsOnLine()
        => await js.Invoke<bool>("BitButil.navigator.onLine");

    /// <summary>
    /// Returns true if the browser can display PDF files inline when navigating to them, and false otherwise.
    /// <br/>
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/Navigator/pdfViewerEnabled">https://developer.mozilla.org/en-US/docs/Web/API/Navigator/pdfViewerEnabled</see>
    /// </summary>
    /// <remarks>
    /// During prerender/SSR (no JS runtime) this returns <c>default</c> (e.g. <c>false</c>/<c>0</c>)
    /// rather than throwing, so the result can't be distinguished from a genuine value. If you
    /// branch on it, defer the read to <c>OnAfterRenderAsync</c>.
    /// </remarks>
    public async Task<bool> IsPdfViewerEnabled()
        => await js.Invoke<bool>("BitButil.navigator.pdfViewerEnabled");

    /// <summary>
    /// Returns the user agent string for the current browser.
    /// <br/>
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/Navigator/userAgent">https://developer.mozilla.org/en-US/docs/Web/API/Navigator/userAgent</see>
    /// </summary>
    public async Task<string> GetUserAgent()
        => await js.Invoke<string>("BitButil.navigator.userAgent");

    /// <summary>
    /// Indicates whether the user agent is controlled by automation.
    /// <br/>
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/Navigator/webdriver">https://developer.mozilla.org/en-US/docs/Web/API/Navigator/webdriver</see>
    /// </summary>
    /// <remarks>
    /// During prerender/SSR (no JS runtime) this returns <c>default</c> (e.g. <c>false</c>/<c>0</c>)
    /// rather than throwing, so the result can't be distinguished from a genuine value. If you
    /// branch on it, defer the read to <c>OnAfterRenderAsync</c>.
    /// </remarks>
    public async Task<bool> IsWebDriver()
        => await js.Invoke<bool>("BitButil.navigator.webdriver");

    /// <summary>
    /// Whether the browser will accept cookies at all.
    /// <br />
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/Navigator/cookieEnabled">https://developer.mozilla.org/en-US/docs/Web/API/Navigator/cookieEnabled</see>
    /// </summary>
    /// <remarks>
    /// Reports the global setting, not whether <em>this</em> document can set one - a third-party
    /// iframe with partitioned or blocked cookies still reads true here. Use
    /// <see cref="StorageAccess"/> for that question.
    /// <br/>
    /// During prerender/SSR (no JS runtime) this returns <c>default</c> (e.g. <c>false</c>/<c>0</c>)
    /// rather than throwing, so the result can't be distinguished from a genuine value. If you
    /// branch on it, defer the read to <c>OnAfterRenderAsync</c>.
    /// </remarks>
    public async Task<bool> IsCookieEnabled()
        => await js.Invoke<bool>("BitButil.navigator.cookieEnabled");

    /// <summary>
    /// The user's Do Not Track preference: <c>"1"</c>, <c>"0"</c>, or null when unset.
    /// <br />
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/Navigator/doNotTrack">https://developer.mozilla.org/en-US/docs/Web/API/Navigator/doNotTrack</see>
    /// </summary>
    /// <remarks>
    /// Being removed from browsers - Chrome and Firefox no longer expose it - so treat a null as
    /// "no signal" rather than "the user consents".
    /// </remarks>
    public async Task<string?> GetDoNotTrack()
        => await js.Invoke<string?>("BitButil.navigator.doNotTrack");

    /// <summary>
    /// Whether the user has interacted with the page, and whether that interaction is still recent
    /// enough to spend.
    /// <br />
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/UserActivation">https://developer.mozilla.org/en-US/docs/Web/API/UserActivation</see>
    /// </summary>
    /// <returns>Null on a browser without <c>navigator.userActivation</c> (Safari at the time of writing).</returns>
    /// <remarks>
    /// Check <see cref="UserActivationState.IsActive"/> before calling an API that demands a
    /// transient user gesture - opening a window, reading the clipboard, requesting storage access -
    /// to fail fast with your own message instead of an opaque browser rejection.
    /// </remarks>
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(UserActivationState))]
    public async Task<UserActivationState?> GetUserActivation()
        => await js.Invoke<UserActivationState?>("BitButil.navigator.userActivation");

    /// <summary>
    /// Returns true if a call to Navigator.share() would succeed.
    /// <br/>
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/Navigator/canShare">https://developer.mozilla.org/en-US/docs/Web/API/Navigator/canShare</see>
    /// </summary>
    /// <remarks>
    /// During prerender/SSR (no JS runtime) this returns <c>default</c> (e.g. <c>false</c>/<c>0</c>)
    /// rather than throwing, so the result can't be distinguished from a genuine value. If you
    /// branch on it, defer the read to <c>OnAfterRenderAsync</c>.
    /// </remarks>
    public async Task<bool> CanShare()
        => await js.Invoke<bool>("BitButil.navigator.canShare");

    /// <summary>
    /// Returns true if the data passed would be shareable by Navigator.share().
    /// <br/>
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/Navigator/canShare">https://developer.mozilla.org/en-US/docs/Web/API/Navigator/canShare</see>
    /// </summary>
    /// <remarks>
    /// During prerender/SSR (no JS runtime) this returns <c>default</c> (e.g. <c>false</c>/<c>0</c>)
    /// rather than throwing, so the result can't be distinguished from a genuine value. If you
    /// branch on it, defer the read to <c>OnAfterRenderAsync</c>.
    /// </remarks>
    public async Task<bool> CanShare(ShareData data)
        => await js.Invoke<bool>("BitButil.navigator.canShare", data);

    /// <summary>
    /// Clears a badge on the current app's icon and returns a Promise that resolves with undefined.
    /// <br/>
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/Navigator/clearAppBadge">https://developer.mozilla.org/en-US/docs/Web/API/Navigator/clearAppBadge</see>
    /// </summary>
    public async Task ClearAppBadge()
        => await js.InvokeVoid("BitButil.navigator.clearAppBadge");

    /// <summary>
    /// Used to asynchronously transfer a small amount of data using HTTP from the User Agent to a web server.
    /// <br/>
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/Navigator/sendBeacon">https://developer.mozilla.org/en-US/docs/Web/API/Navigator/sendBeacon</see>
    /// </summary>
    /// <param name="url">The URL that will receive the data.</param>
    /// <param name="data">An optional payload (string, Blob, BufferSource, or FormData-shaped object) to send.</param>
    /// <remarks>
    /// During prerender/SSR (no JS runtime) this returns <c>default</c> (e.g. <c>false</c>/<c>0</c>)
    /// rather than throwing, so the result can't be distinguished from a genuine value. If you
    /// branch on it, defer the read to <c>OnAfterRenderAsync</c>.
    /// </remarks>
    public async Task<bool> SendBeacon(string url, object? data = null)
        => await js.Invoke<bool>("BitButil.navigator.sendBeacon", url, data);

    /// <summary>
    /// Sets a badge on the icon associated with this app and returns a Promise that resolves with undefined.
    /// <br/>
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/Navigator/setAppBadge">https://developer.mozilla.org/en-US/docs/Web/API/Navigator/setAppBadge</see>
    /// </summary>
    /// <param name="contents">A non-negative integer to display, or null/0 to show a generic dot badge.</param>
    public async Task SetAppBadge(int? contents = null)
        => await js.InvokeVoid("BitButil.navigator.setAppBadge", contents);

    /// <summary>
    /// Invokes the native sharing mechanism of the current platform.
    /// <br/>
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/Navigator/share">https://developer.mozilla.org/en-US/docs/Web/API/Navigator/share</see>
    /// </summary>
    public async Task Share(ShareData data)
        => await js.InvokeVoid("BitButil.navigator.share", data);

    /// <summary>
    /// Web Share Level 2: shares one or more files alongside optional title/text/url.
    /// <br/>
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/Navigator/share">Navigator.share()</see>
    /// </summary>
    /// <returns>True when the share completes (or no files were rejected). False when the
    /// runtime can't share the supplied set, e.g. file shares aren't allowed.</returns>
    /// <remarks>
    /// During prerender/SSR (no JS runtime) this returns <c>default</c> (e.g. <c>false</c>/<c>0</c>)
    /// rather than throwing, so the result can't be distinguished from a genuine value. If you
    /// branch on it, defer the read to <c>OnAfterRenderAsync</c>.
    /// </remarks>
    [System.Diagnostics.CodeAnalysis.DynamicDependency(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.All, typeof(ShareFile))]
    [System.Diagnostics.CodeAnalysis.DynamicDependency(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.All, typeof(ShareData))]
    public async Task<bool> ShareFiles(string? title, ShareFile[] files, string? text = null, string? url = null)
    {
        if (files is null || files.Length == 0) return false;
        return await js.Invoke<bool>("BitButil.navigator.shareFiles", title, text, url, files);
    }

    /// <summary>
    /// Causes vibration on devices with support for it. Does nothing if vibration support isn't available.
    /// <br/>
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/Navigator/vibrate">https://developer.mozilla.org/en-US/docs/Web/API/Navigator/vibrate</see>
    /// </summary>
    /// <remarks>
    /// During prerender/SSR (no JS runtime) this returns <c>default</c> (e.g. <c>false</c>/<c>0</c>)
    /// rather than throwing, so the result can't be distinguished from a genuine value. If you
    /// branch on it, defer the read to <c>OnAfterRenderAsync</c>.
    /// </remarks>
    public async Task<bool> Vibrate(int[] pattern)
        => await js.Invoke<bool>("BitButil.navigator.vibrate", pattern);
}
