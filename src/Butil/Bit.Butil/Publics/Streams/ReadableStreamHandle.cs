using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Microsoft.JSInterop;

namespace Bit.Butil;

/// <summary>
/// A readable stream - a body arriving in pieces.
/// </summary>
/// <remarks>
/// A stream has one consumer. <see cref="Read"/> locks it, and a locked stream can no longer be
/// <see cref="Tee"/>d or piped: decide what you are doing with it before you start reading. That is
/// the specification's rule rather than this wrapper's, and it is what stops two consumers silently
/// stealing each other's chunks.
/// <br/>
/// <see cref="Tee"/>, <see cref="PipeThrough"/> and <see cref="PipeTo"/> consume this handle: the
/// stream belongs to the result afterwards, and this one is spent.
/// </remarks>
public sealed class ReadableStreamHandle : IAsyncDisposable
{
    private readonly IJSRuntime _js;
    private bool _released;

    internal ReadableStreamHandle(IJSRuntime js, Guid id)
    {
        _js = js;
        Id = id;
    }

    /// <summary>The internal stream id.</summary>
    public Guid Id { get; }

    /// <summary>
    /// Reads the next chunk, waiting for it to arrive.
    /// </summary>
    /// <returns>
    /// A chunk, or <see cref="StreamChunk.Done"/> when the stream has ended. The final read carries
    /// no data, so a read loop ends on <c>Done</c> and never has to check for an empty array.
    /// </returns>
    /// <remarks>
    /// The first read locks the stream. Chunk sizes are the browser's choice and vary within one
    /// stream - they are not a protocol, and a consumer that needs fixed-size records has to
    /// reassemble them.
    /// </remarks>
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(StreamChunk))]
    public ValueTask<StreamChunk> Read() => _js.Invoke<StreamChunk>("BitButil.streams.read", Id);

    /// <summary>
    /// Splits the stream into two, each of which gets every chunk.
    /// </summary>
    /// <returns>Two handles, or null when this stream has already been read from or piped.</returns>
    /// <remarks>
    /// This handle is spent afterwards - the stream has been split, not copied. Both branches are
    /// fed from the one underlying source, so a branch nobody reads makes the browser buffer for it
    /// indefinitely: read both, or cancel the one you do not want.
    /// </remarks>
    public async ValueTask<(ReadableStreamHandle First, ReadableStreamHandle Second)?> Tee()
    {
        var firstId = Guid.NewGuid();
        var secondId = Guid.NewGuid();

        var teed = await _js.Invoke<bool>("BitButil.streams.tee", Id, firstId, secondId);
        if (teed is false) return null;

        _released = true;   // the original stream is gone; there is nothing left here to cancel
        return (new ReadableStreamHandle(_js, firstId), new ReadableStreamHandle(_js, secondId));
    }

    /// <summary>
    /// Runs the stream through a transform and hands back what comes out the other side.
    /// </summary>
    /// <param name="transform">A transform from <see cref="Streams.CreateCompression"/> or <see cref="Streams.CreateDecompression"/>.</param>
    /// <returns>The transform's output as a stream, or null when this stream is locked or the transform has been released.</returns>
    /// <remarks>
    /// This handle is spent afterwards. Nothing is read until something reads the result - a
    /// pipeline is a description until it is pulled on.
    /// </remarks>
    public async ValueTask<ReadableStreamHandle?> PipeThrough(TransformStreamHandle transform)
    {
        ArgumentNullException.ThrowIfNull(transform);

        var resultId = Guid.NewGuid();
        var piped = await _js.Invoke<bool>("BitButil.streams.pipeThrough", Id, transform.Id, resultId);
        if (piped is false) return null;

        _released = true;
        return new ReadableStreamHandle(_js, resultId);
    }

    /// <summary>
    /// Pumps everything into a destination and completes when the stream has ended.
    /// </summary>
    /// <param name="destination">Where the chunks go.</param>
    /// <param name="preventClose">
    /// True to leave the destination open when this stream ends - for feeding several streams into
    /// one sink in turn. False (the default) closes it, which is what a single pipe should do.
    /// </param>
    /// <returns>Null on success, or the reason the pipe failed.</returns>
    /// <remarks>
    /// This is the whole pipe in one call: back-pressure, closing the destination, and propagating
    /// an error from either end all happen inside it. This handle is spent afterwards.
    /// </remarks>
    public async ValueTask<string?> PipeTo(WritableStreamHandle destination, bool preventClose = false)
    {
        ArgumentNullException.ThrowIfNull(destination);

        var error = await _js.Invoke<string?>("BitButil.streams.pipeTo", Id, destination.Id, preventClose);
        if (error is null) _released = true;
        return error;
    }

    /// <summary>
    /// Whether something already holds this stream - a reader from <see cref="Read"/>, or a pipe.
    /// A locked stream cannot be teed or piped.
    /// </summary>
    public ValueTask<bool> GetLocked() => _js.Invoke<bool>("BitButil.streams.locked", Id);

    /// <summary>
    /// Stops the stream and tells the source to give up - which, for a fetch body, cancels the
    /// download rather than letting it finish into nothing.
    /// </summary>
    /// <param name="reason">Why, for anything watching.</param>
    public ValueTask Cancel(string? reason = null) => _js.InvokeVoid("BitButil.streams.cancel", Id, reason);

    /// <summary>
    /// Cancels the stream and releases it. Idempotent, and safe during teardown.
    /// </summary>
    /// <remarks>
    /// A handle spent by <see cref="Tee"/>, <see cref="PipeThrough"/> or a successful
    /// <see cref="PipeTo"/> no longer owns anything, so disposing it cancels nothing - the stream is
    /// the result's to manage.
    /// </remarks>
    public async ValueTask DisposeAsync()
    {
        if (_released) return;
        _released = true;

        try { await _js.InvokeVoid("BitButil.streams.cancel", Id, "disposed"); }
        catch (Exception ex) when (ex.IsIgnorableDisposalException()) { } // teardown: circuit gone, cancelled, or already disposed
    }
}
