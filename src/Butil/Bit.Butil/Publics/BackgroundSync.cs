using System.Threading.Tasks;
using Microsoft.JSInterop;

namespace Bit.Butil;

/// <summary>
/// Wraps the <see href="https://developer.mozilla.org/en-US/docs/Web/API/Background_Synchronization_API">Background Sync API</see>
/// (<c>SyncManager</c>) plus the related
/// <see href="https://developer.mozilla.org/en-US/docs/Web/API/Web_Periodic_Background_Synchronization_API">Periodic Background Sync</see>.
/// </summary>
/// <remarks>
/// Both APIs require an active service worker registration. The actual work runs inside the
/// service worker; from C# you can register/unregister tags and inspect the registered ones.
/// </remarks>
public class BackgroundSync(IJSRuntime js)
{
    /// <summary>True when the runtime exposes <c>ServiceWorkerRegistration.sync</c>.</summary>
    /// <remarks>
    /// During prerender/SSR (no JS runtime) this returns <c>default</c> (e.g. <c>false</c>/<c>0</c>)
    /// rather than throwing, so the result can't be distinguished from a genuine value. If you
    /// branch on it, defer the read to <c>OnAfterRenderAsync</c>.
    /// </remarks>
    public ValueTask<bool> IsSupported() => js.Invoke<bool>("BitButil.backgroundSync.isSupported");

    /// <summary>True when <c>ServiceWorkerRegistration.periodicSync</c> is available.</summary>
    /// <remarks>
    /// During prerender/SSR (no JS runtime) this returns <c>default</c> (e.g. <c>false</c>/<c>0</c>)
    /// rather than throwing, so the result can't be distinguished from a genuine value. If you
    /// branch on it, defer the read to <c>OnAfterRenderAsync</c>.
    /// </remarks>
    public ValueTask<bool> IsPeriodicSupported() => js.Invoke<bool>("BitButil.backgroundSync.isPeriodicSupported");

    /// <summary>
    /// Registers a one-shot sync. The service worker's <c>sync</c> event fires once the device is online.
    /// </summary>
    /// <remarks>
    /// During prerender/SSR (no JS runtime) this returns <c>default</c> (e.g. <c>false</c>/<c>0</c>)
    /// rather than throwing, so the result can't be distinguished from a genuine value. If you
    /// branch on it, defer the read to <c>OnAfterRenderAsync</c>.
    /// </remarks>
    public ValueTask<bool> Register(string tag) => js.Invoke<bool>("BitButil.backgroundSync.register", tag);

    /// <summary>Lists tags currently registered for one-shot sync.</summary>
    public ValueTask<string[]> GetTags() => js.Invoke<string[]>("BitButil.backgroundSync.getTags");

    /// <summary>
    /// Registers a periodic sync. Requires the <c>periodic-background-sync</c> permission.
    /// </summary>
    /// <param name="minInterval">Minimum interval between fires, in milliseconds. The browser may extend it.</param>
    /// <remarks>
    /// During prerender/SSR (no JS runtime) this returns <c>default</c> (e.g. <c>false</c>/<c>0</c>)
    /// rather than throwing, so the result can't be distinguished from a genuine value. If you
    /// branch on it, defer the read to <c>OnAfterRenderAsync</c>.
    /// </remarks>
    public ValueTask<bool> RegisterPeriodic(string tag, long minInterval)
        => js.Invoke<bool>("BitButil.backgroundSync.registerPeriodic", tag, minInterval);

    /// <summary>Lists tags currently registered for periodic sync.</summary>
    public ValueTask<string[]> GetPeriodicTags() => js.Invoke<string[]>("BitButil.backgroundSync.getPeriodicTags");

    /// <summary>Removes a periodic sync registration. Returns true when a matching tag was unregistered.</summary>
    /// <remarks>
    /// During prerender/SSR (no JS runtime) this returns <c>default</c> (e.g. <c>false</c>/<c>0</c>)
    /// rather than throwing, so the result can't be distinguished from a genuine value. If you
    /// branch on it, defer the read to <c>OnAfterRenderAsync</c>.
    /// </remarks>
    public ValueTask<bool> UnregisterPeriodic(string tag) => js.Invoke<bool>("BitButil.backgroundSync.unregisterPeriodic", tag);
}
