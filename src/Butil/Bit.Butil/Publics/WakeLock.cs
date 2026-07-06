using System;
using System.Threading.Tasks;
using Microsoft.JSInterop;

namespace Bit.Butil;

/// <summary>
/// Wraps the <see href="https://developer.mozilla.org/en-US/docs/Web/API/Screen_Wake_Lock_API">Screen Wake Lock API</see>.
/// </summary>
/// <remarks>
/// The browser will automatically release the wake lock when the page is hidden.
/// Re-acquire it on <c>visibilitychange</c> when the page becomes visible again.
/// </remarks>
public class WakeLock(IJSRuntime js) : IAsyncDisposable
{
    private bool _heldByUs;

    /// <summary>True when the runtime exposes <c>navigator.wakeLock</c>.</summary>
    /// <remarks>
    /// During prerender/SSR (no JS runtime) this returns <c>default</c> (e.g. <c>false</c>/<c>0</c>)
    /// rather than throwing, so the result can't be distinguished from a genuine value. If you
    /// branch on it, defer the read to <c>OnAfterRenderAsync</c>.
    /// </remarks>
    public ValueTask<bool> IsSupported() => js.Invoke<bool>("BitButil.wakeLock.isSupported");

    /// <summary>
    /// Requests a screen wake lock. The lock is released either explicitly
    /// (<see cref="Release"/> / <see cref="DisposeAsync"/>) or when the user-agent decides
    /// (typically when the page is hidden).
    /// </summary>
    /// <returns>True when the lock was acquired.</returns>
    /// <remarks>
    /// During prerender/SSR (no JS runtime) this returns <c>default</c> (e.g. <c>false</c>/<c>0</c>)
    /// rather than throwing, so the result can't be distinguished from a genuine value. If you
    /// branch on it, defer the read to <c>OnAfterRenderAsync</c>.
    /// </remarks>
    public async ValueTask<bool> Request()
    {
        var ok = await js.Invoke<bool>("BitButil.wakeLock.request");
        if (ok) _heldByUs = true;
        return ok;
    }

    /// <summary>Releases the most recently acquired lock if it is still active.</summary>
    public async ValueTask Release()
    {
        if (_heldByUs is false) return;
        _heldByUs = false;
        await js.InvokeVoid("BitButil.wakeLock.release");
    }

    /// <summary>
    /// Acquires a wake lock and keeps it alive across the page-visibility cycle by re-acquiring
    /// it whenever the page becomes visible again. Browsers always release the lock when the
    /// page is hidden - this helper restores it on resume.
    /// </summary>
    /// <returns>An <see cref="IAsyncDisposable"/> that stops the auto-reacquire and releases the lock.</returns>
    public async ValueTask<IAsyncDisposable> RequestPersistent()
    {
        var token = Guid.NewGuid().ToString("N");
        await js.InvokeVoid("BitButil.wakeLock.persist", token);
        return new PersistentLockHandle(js, token);
    }

    public async ValueTask DisposeAsync()
    {
        try
        {
            await Release();
        }
        catch (Exception ex) when (ex.IsIgnorableDisposalException()) { } // teardown: circuit gone, cancelled, or already disposed
        GC.SuppressFinalize(this);
    }

    private sealed class PersistentLockHandle(IJSRuntime js, string token) : IAsyncDisposable
    {
        private bool _disposed;

        public async ValueTask DisposeAsync()
        {
            if (_disposed) return;
            _disposed = true;
            try { await js.InvokeVoid("BitButil.wakeLock.unpersist", token); }
            catch (Exception ex) when (ex.IsIgnorableDisposalException()) { } // teardown: circuit gone, cancelled, or already disposed
        }
    }
}
