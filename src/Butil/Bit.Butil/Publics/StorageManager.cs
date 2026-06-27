using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Microsoft.JSInterop;

namespace Bit.Butil;

/// <summary>
/// Wraps <see href="https://developer.mozilla.org/en-US/docs/Web/API/StorageManager">navigator.storage</see>.
/// </summary>
public class StorageManager(IJSRuntime js)
{
    /// <summary>True when the runtime exposes <c>navigator.storage</c>.</summary>
    /// <remarks>
    /// During prerender/SSR (no JS runtime) this returns <c>default</c> (e.g. <c>false</c>/<c>0</c>)
    /// rather than throwing, so the result can't be distinguished from a genuine value. If you
    /// branch on it, defer the read to <c>OnAfterRenderAsync</c>.
    /// </remarks>
    public ValueTask<bool> IsSupported() => js.Invoke<bool>("BitButil.storageManager.isSupported");

    /// <summary>
    /// Reports an estimate of the storage quota and current usage for the origin.
    /// </summary>
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(StorageEstimate))]
    public ValueTask<StorageEstimate> Estimate() => js.Invoke<StorageEstimate>("BitButil.storageManager.estimate");

    /// <summary>True when the origin's storage is persistent (won't be evicted under pressure).</summary>
    /// <remarks>
    /// During prerender/SSR (no JS runtime) this returns <c>default</c> (e.g. <c>false</c>/<c>0</c>)
    /// rather than throwing, so the result can't be distinguished from a genuine value. If you
    /// branch on it, defer the read to <c>OnAfterRenderAsync</c>.
    /// </remarks>
    public ValueTask<bool> Persisted() => js.Invoke<bool>("BitButil.storageManager.persisted");

    /// <summary>
    /// Asks the browser to make storage persistent. The user agent decides whether to grant.
    /// </summary>
    /// <remarks>
    /// During prerender/SSR (no JS runtime) this returns <c>default</c> (e.g. <c>false</c>/<c>0</c>)
    /// rather than throwing, so the result can't be distinguished from a genuine value. If you
    /// branch on it, defer the read to <c>OnAfterRenderAsync</c>.
    /// </remarks>
    public ValueTask<bool> Persist() => js.Invoke<bool>("BitButil.storageManager.persist");
}
