using System;
using System.Threading.Tasks;
using Microsoft.JSInterop;

namespace Bit.Butil;

/// <summary>
/// An active close watcher, returned by <see cref="CloseWatcher.Create"/>. Dispose it when the thing
/// it guards is gone.
/// </summary>
/// <remarks>
/// <b>Always dispose.</b> A watcher that outlives its dialog stays in the browser's close stack and
/// eats the next Escape press or back gesture.
/// </remarks>
public sealed class CloseWatcherHandle : IAsyncDisposable
{
    private readonly IJSRuntime _js;
    private readonly Func<ValueTask> _release;
    private bool _disposed;

    internal CloseWatcherHandle(IJSRuntime js, Guid id, Func<ValueTask> release)
    {
        _js = js;
        Id = id;
        _release = release;
    }

    /// <summary>The internal watcher id.</summary>
    public Guid Id { get; }

    /// <summary>
    /// Asks to close, exactly as an Escape press would: the cancel handler runs first and may keep
    /// it open. Wire your own close button to this so both routes behave identically.
    /// </summary>
    public ValueTask RequestClose() => _js.InvokeVoid("BitButil.closeWatcher.requestClose", Id);

    /// <summary>
    /// Closes without asking - skips the cancel handler and fires the close handler.
    /// </summary>
    public ValueTask Close() => _js.InvokeVoid("BitButil.closeWatcher.close", Id);

    /// <summary>
    /// Deactivates the watcher without firing the close handler, and releases it. Calling it again
    /// does nothing.
    /// </summary>
    /// <remarks>
    /// This is what disposal does. Use it when the dialog went away by some other route - if the
    /// watcher stayed active it would swallow the next Escape.
    /// </remarks>
    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;
        _disposed = true;

        try { await _release(); }
        catch (Exception ex) when (ex.IsIgnorableDisposalException()) { } // teardown: circuit gone, cancelled, or already disposed

        GC.SuppressFinalize(this);
    }
}
