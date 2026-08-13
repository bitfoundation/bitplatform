using System;
using System.Threading.Tasks;
using Microsoft.JSInterop;

namespace Bit.Butil;

/// <summary>
/// An open server-sent event stream, returned by <see cref="EventSource.Open"/>. Dispose it to
/// close the connection and stop the browser reconnecting.
/// </summary>
public sealed class EventSourceHandle : IAsyncDisposable
{
    private readonly IJSRuntime _js;
    private readonly Action _onClosed;
    private bool _closed;

    internal EventSourceHandle(IJSRuntime js, Guid id, Action onClosed)
    {
        _js = js;
        Id = id;
        _onClosed = onClosed;
    }

    /// <summary>The internal stream id.</summary>
    public Guid Id { get; }

    /// <summary>
    /// The connection's current state.
    /// </summary>
    /// <remarks>
    /// <see cref="EventSourceState.Connecting"/> after a dropped connection is normal - the browser
    /// retries on its own. <see cref="EventSourceState.Closed"/> is also what a disposed handle
    /// reports.
    /// </remarks>
    public async ValueTask<EventSourceState> GetState()
        => (EventSourceState)await _js.Invoke<int>("BitButil.eventSource.readyState", Id);

    /// <summary>
    /// Closes the connection. Idempotent, and safe during teardown.
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        if (_closed) return;
        _closed = true;
        _onClosed();
        try { await _js.InvokeVoid("BitButil.eventSource.close", Id); }
        catch (Exception ex) when (ex.IsIgnorableDisposalException()) { } // teardown: circuit gone, cancelled, or already disposed
    }
}
