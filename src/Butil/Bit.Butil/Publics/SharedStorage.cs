using System.Threading.Tasks;
using Microsoft.JSInterop;

namespace Bit.Butil;

/// <summary>
/// Wraps the <see href="https://developer.mozilla.org/en-US/docs/Web/API/Shared_Storage_API">Shared Storage API</see>:
/// cross-site storage that a page can write but never read.
/// </summary>
/// <remarks>
/// <b>Write-only by design.</b> Shared storage is unpartitioned - the same data is there whichever
/// site embeds you - which would be a cross-site identifier if the page could read it back. So it
/// can't: the only code that ever sees a value is a <b>worklet</b>, which runs in an isolated scope
/// with no network access, and whose single permitted output is a choice among URLs
/// (<see cref="SelectUrl"/>) rendered inside a fenced frame. <see cref="Run"/> gets no result at all.
/// <br/>
/// If you are looking for a place to keep your own app's data, this is not it - use
/// <see cref="LocalStorage"/>, <see cref="IndexedDb"/> or <see cref="StorageManager"/>. Shared
/// storage exists for the narrow Privacy Sandbox cases: frequency capping, A/B group assignment and
/// creative selection across sites.
/// <br/>
/// <b>Early.</b> Chromium only, behind a permissions policy, in a secure context, and the shape of
/// this API has changed more than once. Every method here answers false rather than throwing where
/// the runtime disagrees.
/// </remarks>
[ButilService(typeof(SharedStorage))]
public class SharedStorage(IJSRuntime js)
{
    /// <summary>True when the runtime exposes <c>window.sharedStorage</c>.</summary>
    /// <remarks>
    /// During prerender/SSR (no JS runtime) this returns <c>default</c> (e.g. <c>false</c>/<c>0</c>)
    /// rather than throwing, so the result can't be distinguished from a genuine value. If you
    /// branch on it, defer the read to <c>OnAfterRenderAsync</c>.
    /// </remarks>
    public ValueTask<bool> IsSupported() => js.Invoke<bool>("BitButil.sharedStorage.isSupported");

    /// <summary>
    /// Writes a value.
    /// </summary>
    /// <param name="key">The key. Keys and values are both capped at a few hundred characters.</param>
    /// <param name="value">The value.</param>
    /// <param name="ignoreIfPresent">Leave an existing value alone instead of replacing it.</param>
    /// <returns>False when the runtime refused - no API, blocked by permissions policy, or over the limits.</returns>
    /// <remarks>There is no matching read. See the type's remarks for why.</remarks>
    public ValueTask<bool> Set(string key, string value, bool ignoreIfPresent = false)
        => js.Invoke<bool>("BitButil.sharedStorage.set", key, value, ignoreIfPresent);

    /// <summary>Appends to an existing value, or sets it when there is none.</summary>
    /// <returns>False when the runtime refused.</returns>
    public ValueTask<bool> Append(string key, string value)
        => js.Invoke<bool>("BitButil.sharedStorage.append", key, value);

    /// <summary>Removes one key.</summary>
    /// <returns>False when the runtime refused.</returns>
    public ValueTask<bool> Delete(string key) => js.Invoke<bool>("BitButil.sharedStorage.delete", key);

    /// <summary>Removes everything this origin has written.</summary>
    /// <returns>False when the runtime refused.</returns>
    public ValueTask<bool> Clear() => js.Invoke<bool>("BitButil.sharedStorage.clear");

    /// <summary>
    /// Loads the worklet module - the only code that can read what was written.
    /// </summary>
    /// <param name="url">
    /// Same-origin URL of the module, which registers its operations with
    /// <c>register("name", class { async run(data) { … } })</c>.
    /// </param>
    /// <returns>False when the runtime refused, or the module failed to load.</returns>
    /// <remarks>A page may add a worklet module only once.</remarks>
    public ValueTask<bool> AddModule(string url) => js.Invoke<bool>("BitButil.sharedStorage.addModule", url);

    /// <summary>
    /// Runs a registered worklet operation.
    /// </summary>
    /// <param name="operation">The name the worklet registered.</param>
    /// <param name="data">Serializable data handed to the operation.</param>
    /// <param name="keepAlive">Keep the worklet alive for another operation afterwards.</param>
    /// <returns>
    /// Whether the operation was <i>started</i> - never what it computed. The result is unreadable by
    /// design.
    /// </returns>
    public ValueTask<bool> Run(string operation, object? data = null, bool keepAlive = false)
        => js.Invoke<bool>("BitButil.sharedStorage.run", operation, data, keepAlive);

    /// <summary>
    /// Runs a worklet operation that picks one of the given URLs.
    /// </summary>
    /// <param name="operation">The name the worklet registered. It returns the index of its choice.</param>
    /// <param name="urls">The candidates, up to the runtime's cap (eight in Chromium).</param>
    /// <param name="data">Serializable data handed to the operation.</param>
    /// <param name="resolveToConfig">Resolve to a fenced-frame config rather than an opaque URL.</param>
    /// <returns>Whether a choice was made. <b>Which</b> URL won is not reported to the page.</returns>
    /// <remarks>
    /// The result is only usable by handing it to a fenced frame, which Butil does not wrap - a
    /// fenced frame is markup, and its whole point is that the embedding page cannot inspect it.
    /// </remarks>
    public ValueTask<bool> SelectUrl(string operation, string[] urls, object? data = null, bool resolveToConfig = false)
        => js.Invoke<bool>("BitButil.sharedStorage.selectURL", operation, urls, data, resolveToConfig);
}
