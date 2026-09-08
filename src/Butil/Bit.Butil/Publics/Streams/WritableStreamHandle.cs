using System;
using System.Threading.Tasks;
using Microsoft.JSInterop;

namespace Bit.Butil;

/// <summary>
/// A writable stream - the destination end of a pipe, whose chunks arrive at the C# callback given
/// to <see cref="Streams.CreateWritable"/>.
/// </summary>
/// <remarks>
/// Two ways to fill it: hand it to <see cref="ReadableStreamHandle.PipeTo"/> and let the pipe do it,
/// or call <see cref="Write"/> yourself. Both honour back-pressure - a write waits until the sink is
/// ready for it rather than queueing without limit.
/// </remarks>
public sealed class WritableStreamHandle : IAsyncDisposable
{
    private readonly IJSRuntime _js;
    private readonly Action _onReleased;
    private bool _released;

    internal WritableStreamHandle(IJSRuntime js, Guid id, Action onReleased)
    {
        _js = js;
        Id = id;
        _onReleased = onReleased;
    }

    /// <summary>The internal stream id.</summary>
    public Guid Id { get; }

    /// <summary>
    /// Writes a chunk, waiting until the sink is ready for it.
    /// </summary>
    /// <returns>False when the stream has been closed, aborted or released.</returns>
    /// <remarks>
    /// The wait is the point: a caller writing in a loop is slowed to the speed of the consumer
    /// rather than filling a queue that has no limit. A stream being piped into cannot also be
    /// written to by hand - the pipe holds it.
    /// </remarks>
    public ValueTask<bool> Write(byte[] data)
    {
        ArgumentNullException.ThrowIfNull(data);
        return _js.Invoke<bool>("BitButil.streams.write", Id, data);
    }

    /// <summary>
    /// Ends the stream cleanly once everything already written has been handled. The C# callback's
    /// <c>onFinished</c> runs with a null reason.
    /// </summary>
    /// <returns>False when the stream was already closed or aborted.</returns>
    public ValueTask<bool> Close() => _js.Invoke<bool>("BitButil.streams.closeWritable", Id);

    /// <summary>
    /// Ends the stream now, discarding anything still queued. The C# callback's <c>onFinished</c>
    /// runs with <paramref name="reason"/>.
    /// </summary>
    /// <returns>False when the stream was already closed or aborted.</returns>
    public ValueTask<bool> Abort(string? reason = null) => _js.Invoke<bool>("BitButil.streams.abortWritable", Id, reason);

    /// <summary>
    /// Whether a writer or a pipe holds this stream. A locked stream cannot be written to by hand.
    /// </summary>
    public ValueTask<bool> GetLocked() => _js.Invoke<bool>("BitButil.streams.locked", Id);

    /// <summary>
    /// Aborts the stream and releases it. Idempotent, and safe during teardown.
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        if (_released) return;
        _released = true;

        // The sink's callbacks stay registered across the abort, so onFinished still hears about the
        // stream ending; releasing them first would make disposal the one ending nobody is told of.
        try { await _js.Invoke<bool>("BitButil.streams.abortWritable", Id, "disposed"); }
        catch (Exception ex) when (ex.IsIgnorableDisposalException()) { } // teardown: circuit gone, cancelled, or already disposed
        finally { _onReleased(); }
    }
}
