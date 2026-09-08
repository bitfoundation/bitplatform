using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Microsoft.JSInterop;

namespace Bit.Butil;

/// <summary>
/// Wraps the <see href="https://developer.mozilla.org/en-US/docs/Web/API/Content_Index_API">Content Index API</see>
/// (<c>ServiceWorkerRegistration.index</c>): tells the browser which pages your app can already show
/// offline, so it can offer them to the user itself.
/// </summary>
/// <remarks>
/// Caching a page makes it work offline; indexing it makes it <em>findable</em> offline. The entries
/// registered here appear in the browser's own UI (Chromium lists them under <em>Downloads,
/// Articles for you</em>), which is somewhere the user looks when your app is not open.
/// <br/>
/// An entry is a claim, not a copy: it stores a title, a description, icons and a URL, and nothing
/// else. Cache the page first (see <see cref="CacheStorage"/>) - an indexed URL that 404s offline is
/// worse than no entry, and Chromium removes entries whose cached response goes away.
/// <br/>
/// Requires an active service worker registration, and is Chromium-only at the time of writing.
/// </remarks>
[ButilService(typeof(ContentIndex))]
public class ContentIndex(IJSRuntime js)
{
    /// <summary>True when the active service worker registration exposes <c>index</c>.</summary>
    /// <remarks>
    /// This is false until a service worker is registered, even in a browser that implements the
    /// API - the whole surface hangs off a registration.
    /// <br/>
    /// During prerender/SSR (no JS runtime) this returns <c>default</c> (e.g. <c>false</c>/<c>0</c>)
    /// rather than throwing, so the result can't be distinguished from a genuine value. If you
    /// branch on it, defer the read to <c>OnAfterRenderAsync</c>.
    /// </remarks>
    public ValueTask<bool> IsSupported() => js.Invoke<bool>("BitButil.contentIndex.isSupported");

    /// <summary>
    /// Registers one piece of offline-available content, replacing any entry with the same
    /// <see cref="ContentIndexEntry.Id"/>.
    /// </summary>
    /// <param name="entry">The content description. Title, description, url and at least one icon are required in Chromium.</param>
    /// <returns>
    /// False when the browser refused it - the URL is outside the worker's scope, an icon could not
    /// be fetched, or a required field is empty.
    /// </returns>
    /// <remarks>
    /// During prerender/SSR (no JS runtime) this returns <c>default</c> (e.g. <c>false</c>/<c>0</c>)
    /// rather than throwing, so the result can't be distinguished from a genuine value. If you
    /// branch on it, defer the read to <c>OnAfterRenderAsync</c>.
    /// </remarks>
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(ContentIndexEntry))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(ContentIndexIcon))]
    public ValueTask<bool> Add(ContentIndexEntry entry)
    {
        ArgumentNullException.ThrowIfNull(entry);
        return js.Invoke<bool>("BitButil.contentIndex.add", entry);
    }

    /// <summary>Removes an entry. Returns false when the API is unavailable; removing an id that isn't there is not an error.</summary>
    /// <param name="id">The <see cref="ContentIndexEntry.Id"/> the entry was added with.</param>
    /// <remarks>
    /// The browser also removes entries on its own - when the user deletes one from its UI, or when
    /// the cached response behind the URL goes away - so <see cref="GetAll"/> is the only honest
    /// answer to what is currently indexed.
    /// <br/>
    /// During prerender/SSR (no JS runtime) this returns <c>default</c> (e.g. <c>false</c>/<c>0</c>)
    /// rather than throwing, so the result can't be distinguished from a genuine value. If you
    /// branch on it, defer the read to <c>OnAfterRenderAsync</c>.
    /// </remarks>
    public ValueTask<bool> Delete(string id) => js.Invoke<bool>("BitButil.contentIndex.delete", id);

    /// <summary>Everything currently indexed for this service worker registration.</summary>
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(ContentIndexEntry))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(ContentIndexIcon))]
    public ValueTask<ContentIndexEntry[]> GetAll() => js.Invoke<ContentIndexEntry[]>("BitButil.contentIndex.getAll");
}
