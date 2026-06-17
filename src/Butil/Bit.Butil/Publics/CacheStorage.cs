using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Microsoft.JSInterop;

namespace Bit.Butil;

/// <summary>
/// Wraps the <see href="https://developer.mozilla.org/en-US/docs/Web/API/CacheStorage">CacheStorage</see>
/// and <see href="https://developer.mozilla.org/en-US/docs/Web/API/Cache">Cache</see> APIs.
/// </summary>
/// <remarks>
/// All operations target a named cache. The browser persists caches per origin and they
/// outlive the page, so this service is intentionally side-effect-only - no instance state.
/// </remarks>
public class CacheStorage(IJSRuntime js)
{
    /// <summary>True when the runtime exposes <c>caches</c>.</summary>
    /// <remarks>
    /// During prerender/SSR (no JS runtime) this returns <c>default</c> (e.g. <c>false</c>/<c>0</c>)
    /// rather than throwing, so the result can't be distinguished from a genuine value. If you
    /// branch on it, defer the read to <c>OnAfterRenderAsync</c>.
    /// </remarks>
    public ValueTask<bool> IsSupported() => js.Invoke<bool>("BitButil.cacheStorage.isSupported");

    /// <summary>Lists every cache name visible to the current origin.</summary>
    public ValueTask<string[]> Keys() => js.Invoke<string[]>("BitButil.cacheStorage.keys");

    /// <summary>Returns true when a cache with the given name exists.</summary>
    /// <remarks>
    /// During prerender/SSR (no JS runtime) this returns <c>default</c> (e.g. <c>false</c>/<c>0</c>)
    /// rather than throwing, so the result can't be distinguished from a genuine value. If you
    /// branch on it, defer the read to <c>OnAfterRenderAsync</c>.
    /// </remarks>
    public ValueTask<bool> Has(string cacheName) => js.Invoke<bool>("BitButil.cacheStorage.has", cacheName);

    /// <summary>Deletes the named cache. Returns true when something was removed.</summary>
    /// <remarks>
    /// During prerender/SSR (no JS runtime) this returns <c>default</c> (e.g. <c>false</c>/<c>0</c>)
    /// rather than throwing, so the result can't be distinguished from a genuine value. If you
    /// branch on it, defer the read to <c>OnAfterRenderAsync</c>.
    /// </remarks>
    public ValueTask<bool> Delete(string cacheName) => js.Invoke<bool>("BitButil.cacheStorage.delete", cacheName);

    /// <summary>
    /// Adds <paramref name="url"/> to the cache by issuing a fetch and storing the response.
    /// </summary>
    public ValueTask Add(string cacheName, string url) => js.InvokeVoid("BitButil.cacheStorage.add", cacheName, url);

    /// <summary>Adds many URLs to the cache atomically.</summary>
    public ValueTask AddAll(string cacheName, params string[] urls)
        => js.InvokeVoid("BitButil.cacheStorage.addAll", cacheName, urls);

    /// <summary>
    /// Stores a response built from raw bytes against <paramref name="url"/>. Use this when the
    /// payload is generated client-side and you don't want to fetch it.
    /// </summary>
    public ValueTask PutBytes(string cacheName, string url, byte[] data,
                              string contentType = "application/octet-stream",
                              int status = 200,
                              string statusText = "OK")
        => js.InvokeVoid("BitButil.cacheStorage.putBytes", cacheName, url, data, contentType, status, statusText);

    /// <summary>Stores a UTF-8 text response against <paramref name="url"/>.</summary>
    public ValueTask PutText(string cacheName, string url, string text,
                             string contentType = "text/plain;charset=utf-8",
                             int status = 200,
                             string statusText = "OK")
        => js.InvokeVoid("BitButil.cacheStorage.putText", cacheName, url, text, contentType, status, statusText);

    /// <summary>
    /// Looks up a cached response. <see cref="CachedResponse.Found"/> is false when nothing matched.
    /// </summary>
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(CachedResponse))]
    public ValueTask<CachedResponse> Match(string cacheName, string url)
        => js.Invoke<CachedResponse>("BitButil.cacheStorage.match", cacheName, url);

    /// <summary>Removes a single entry. Returns true when something was removed.</summary>
    /// <remarks>
    /// During prerender/SSR (no JS runtime) this returns <c>default</c> (e.g. <c>false</c>/<c>0</c>)
    /// rather than throwing, so the result can't be distinguished from a genuine value. If you
    /// branch on it, defer the read to <c>OnAfterRenderAsync</c>.
    /// </remarks>
    public ValueTask<bool> DeleteEntry(string cacheName, string url)
        => js.Invoke<bool>("BitButil.cacheStorage.deleteEntry", cacheName, url);

    /// <summary>Lists the URLs currently stored in the named cache.</summary>
    public ValueTask<string[]> EntryKeys(string cacheName)
        => js.Invoke<string[]>("BitButil.cacheStorage.entryKeys", cacheName);
}
