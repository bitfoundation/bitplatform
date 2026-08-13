using System.Threading.Tasks;
using Microsoft.JSInterop;

namespace Bit.Butil;

/// <summary>
/// Wraps the <see href="https://developer.mozilla.org/en-US/docs/Web/API/Storage_Access_API">Storage Access API</see>:
/// the way an app running inside a third-party iframe asks for its own unpartitioned cookies and
/// storage back.
/// </summary>
/// <remarks>
/// Browsers partition storage by the top-level site, so a Blazor app embedded in someone else's
/// page gets a fresh, empty jar per embedding site - and any auth cookie it set on its own domain
/// is invisible. This is the sanctioned way to ask for the real one.
/// <br/>
/// Only relevant inside a cross-site iframe. In a top-level page there is nothing to ask for:
/// <see cref="HasAccess"/> answers true and <see cref="Request"/> is unnecessary.
/// </remarks>
public class StorageAccess(IJSRuntime js)
{
    /// <summary>True when the runtime exposes <c>document.requestStorageAccess</c>.</summary>
    /// <remarks>
    /// During prerender/SSR (no JS runtime) this returns <c>default</c> (e.g. <c>false</c>/<c>0</c>)
    /// rather than throwing, so the result can't be distinguished from a genuine value. If you
    /// branch on it, defer the read to <c>OnAfterRenderAsync</c>.
    /// </remarks>
    public ValueTask<bool> IsSupported() => js.Invoke<bool>("BitButil.storageAccess.isSupported");

    /// <summary>
    /// Whether this document already has access to its unpartitioned storage.
    /// </summary>
    /// <returns>
    /// True on engines that don't partition storage at all, since there is nothing to be denied -
    /// so a false here genuinely means "ask".
    /// </returns>
    /// <remarks>
    /// During prerender/SSR (no JS runtime) this returns <c>default</c> (e.g. <c>false</c>/<c>0</c>)
    /// rather than throwing, so the result can't be distinguished from a genuine value. If you
    /// branch on it, defer the read to <c>OnAfterRenderAsync</c>.
    /// </remarks>
    public ValueTask<bool> HasAccess() => js.Invoke<bool>("BitButil.storageAccess.hasAccess");

    /// <summary>
    /// Asks for access to unpartitioned storage.
    /// </summary>
    /// <returns>
    /// True when granted. False covers every refusal - the user declined, there was no user
    /// gesture, or the embedder's permissions policy forbids it - because the spec deliberately
    /// doesn't say which.
    /// </returns>
    /// <remarks>
    /// Must be called from a user-gesture handler such as a click, and browsers generally only
    /// grant it for a site the user has already interacted with at the top level.
    /// <br/>
    /// A grant applies to the rest of this document's lifetime, not permanently - ask again after
    /// a reload.
    /// </remarks>
    public ValueTask<bool> Request() => js.Invoke<bool>("BitButil.storageAccess.request");

    /// <summary>True when the runtime exposes <c>document.requestStorageAccessFor</c> (Chromium only).</summary>
    /// <remarks>
    /// During prerender/SSR (no JS runtime) this returns <c>default</c> (e.g. <c>false</c>/<c>0</c>)
    /// rather than throwing, so the result can't be distinguished from a genuine value. If you
    /// branch on it, defer the read to <c>OnAfterRenderAsync</c>.
    /// </remarks>
    public ValueTask<bool> IsRequestForSupported() => js.Invoke<bool>("BitButil.storageAccess.isRequestForSupported");

    /// <summary>
    /// The top-level counterpart of <see cref="Request"/>: a first-party page asks for storage
    /// access on behalf of an origin it embeds.
    /// </summary>
    /// <param name="origin">The embedded origin to grant access to, e.g. <c>"https://embedded.example"</c>.</param>
    /// <returns>True when granted; false on refusal or where the API doesn't exist.</returns>
    /// <remarks>
    /// Call this from the <em>top-level</em> page, not from the iframe - it saves the embedded frame
    /// from needing its own user gesture. Still requires a user gesture here, and is Chromium-only.
    /// </remarks>
    public ValueTask<bool> RequestFor(string origin) => js.Invoke<bool>("BitButil.storageAccess.requestFor", origin);
}
