using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.JSInterop;

namespace Bit.Butil;

/// <summary>
/// Wraps the <see href="https://developer.mozilla.org/en-US/docs/Web/API/Web_Locks_API">Web Locks API</see>
/// (<c>navigator.locks</c>).
/// </summary>
/// <remarks>
/// The native API uses a callback that holds the lock for the duration of its returned Promise.
/// We expose two ergonomic forms:
/// <list type="bullet">
/// <item>
///   <see cref="Acquire"/> hands you an <see cref="IAsyncDisposable"/>. Dispose to release the lock.
///   This matches typical .NET <c>using</c> patterns.
/// </item>
/// <item>
///   <see cref="Run"/> runs your callback while holding the lock - closer to the JS API.
/// </item>
/// </list>
/// </remarks>
public class WebLocks(IJSRuntime js)
{
    /// <summary>True when the runtime exposes <c>navigator.locks</c>.</summary>
    /// <remarks>
    /// During prerender/SSR (no JS runtime) this returns <c>default</c> (e.g. <c>false</c>/<c>0</c>)
    /// rather than throwing, so the result can't be distinguished from a genuine value. If you
    /// branch on it, defer the read to <c>OnAfterRenderAsync</c>.
    /// </remarks>
    public ValueTask<bool> IsSupported() => js.Invoke<bool>("BitButil.webLocks.isSupported");

    /// <summary>
    /// Acquires the named lock. Dispose the returned handle to release.
    /// </summary>
    /// <param name="ifAvailable">When true, returns a null handle immediately if the lock isn't available.</param>
    /// <param name="steal">When true, steals the lock from the current holder. Use with care.</param>
    public async ValueTask<IAsyncDisposable?> Acquire(string name,
        WebLockMode mode = WebLockMode.Exclusive,
        bool ifAvailable = false,
        bool steal = false,
        CancellationToken cancellationToken = default)
    {
        var releaseToken = Guid.NewGuid().ToString("N");
        var modeStr = mode == WebLockMode.Shared ? "shared" : "exclusive";

        var acquired = await js.Invoke<bool>("BitButil.webLocks.acquire",
            cancellationToken, name, modeStr, ifAvailable, steal, releaseToken);

        if (!acquired) return null;

        return new WebLockHandle(js, releaseToken);
    }

    /// <summary>
    /// Runs <paramref name="action"/> while holding the named lock. Releases automatically.
    /// </summary>
    public async ValueTask Run(string name, Func<ValueTask> action,
        WebLockMode mode = WebLockMode.Exclusive,
        bool ifAvailable = false,
        bool steal = false,
        CancellationToken cancellationToken = default)
    {
        var handle = await Acquire(name, mode, ifAvailable, steal, cancellationToken);
        if (handle is null) return; // ifAvailable was true and the lock wasn't free
        await using (handle)
        {
            await action();
        }
    }

    /// <summary>Returns the current state of the lock manager.</summary>
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(WebLockSnapshot))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(WebLockInfo))]
    public ValueTask<WebLockSnapshot> Query() => js.Invoke<WebLockSnapshot>("BitButil.webLocks.query");

    private sealed class WebLockHandle(IJSRuntime js, string token) : IAsyncDisposable
    {
        private bool _disposed;

        public async ValueTask DisposeAsync()
        {
            if (_disposed) return;
            _disposed = true;
            try { await js.InvokeVoid("BitButil.webLocks.release", token); }
            catch (Exception ex) when (ex.IsIgnorableDisposalException()) { } // teardown: circuit gone, cancelled, or already disposed
        }
    }
}
