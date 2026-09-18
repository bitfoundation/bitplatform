using System;
using System.Threading.Tasks;
using Microsoft.JSInterop;

namespace Bit.Butil;

/// <summary>
/// A transform stream: a writable end that takes chunks in and a readable end that gives them back
/// changed. Returned by <see cref="Streams.CreateCompression"/> and
/// <see cref="Streams.CreateDecompression"/>.
/// </summary>
/// <remarks>
/// The usual way to use one is <see cref="ReadableStreamHandle.PipeThrough"/>, which wires both ends
/// up for you. <see cref="Writable"/> and <see cref="Readable"/> are there for the case where the
/// two halves belong to different parts of your code - write into one end here, read from the other
/// end there.
/// <br/>
/// The transform is the browser's own codec. C# in the middle of a pipeline would mean an interop
/// hop per chunk; do it as read-transform-write around the pipe instead.
/// </remarks>
public sealed class TransformStreamHandle : IAsyncDisposable
{
    private readonly IJSRuntime _js;
    private bool _released;

    internal TransformStreamHandle(IJSRuntime js, Guid id, Guid readableId, Guid writableId)
    {
        _js = js;
        Id = id;
        Readable = new ReadableStreamHandle(js, readableId);
        Writable = new WritableStreamHandle(js, writableId, static () => { });
    }

    /// <summary>The internal transform id.</summary>
    public Guid Id { get; }

    /// <summary>The end transformed chunks come out of.</summary>
    public ReadableStreamHandle Readable { get; }

    /// <summary>The end chunks go into.</summary>
    public WritableStreamHandle Writable { get; }

    /// <summary>
    /// Releases the transform and both of its ends. Idempotent, and safe during teardown.
    /// </summary>
    /// <remarks>
    /// A transform already handed to <see cref="ReadableStreamHandle.PipeThrough"/> is part of a
    /// live pipeline; disposing it here drops this registry's record of it without tearing the pipe
    /// down mid-flight.
    /// </remarks>
    public async ValueTask DisposeAsync()
    {
        if (_released) return;
        _released = true;

        try { await _js.InvokeVoid("BitButil.streams.release", Id); }
        catch (Exception ex) when (ex.IsIgnorableDisposalException()) { } // teardown: circuit gone, cancelled, or already disposed
    }
}
