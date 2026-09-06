using System;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Microsoft.JSInterop;

namespace Bit.Butil;

/// <summary>
/// Wraps the <see href="https://developer.mozilla.org/en-US/docs/Web/API/Streams_API">Streams API</see>:
/// data that arrives in pieces and is handled in pieces, rather than waited for and then held whole
/// in memory.
/// </summary>
/// <remarks>
/// The package already used streams internally - <see cref="Fetch"/> reads a response body through a
/// reader to report progress, and <see cref="Compression"/> pipes through the browser's codec. This
/// exposes them, so a pipeline can be yours: read a response as it arrives, split it in two, run it
/// through the native gzip codec, and hand the bytes to C# as they come.
/// <br/>
/// <b>The transforms are the browser's.</b> <see cref="CreateCompression"/> and
/// <see cref="CreateDecompression"/> wrap <c>CompressionStream</c>/<c>DecompressionStream</c>, and
/// those are what <see cref="ReadableStreamHandle.PipeThrough"/> accepts. C# code in the middle of a
/// pipeline would mean an interop hop per chunk; write it as read-transform-write around the pipe
/// instead, which costs the same and reads more honestly.
/// <br/>
/// Streams have an ownership rule that surprises people coming from .NET: reading from a stream
/// <em>locks</em> it, and a locked stream can no longer be teed or piped. That is the specification's
/// rule, not this wrapper's, and it is what keeps two consumers from silently stealing each other's
/// chunks.
/// </remarks>
[ButilService(typeof(Streams))]
public class Streams(IJSRuntime js) : IAsyncDisposable
{
    internal const string ChunkMethodName = nameof(InvokeSinkChunk);
    internal const string CloseMethodName = nameof(InvokeSinkClose);

    private readonly ConcurrentDictionary<Guid, SinkHandlers> _sinks = new();

    // Per-instance callback reference (see Keyboard): sinks are isolated per circuit / WASM app and
    // released on disposal - no static state, no cross-circuit leak.
    private DotNetObjectReference<Streams>? _dotNetRef;
    private DotNetObjectReference<Streams> DotNetRef => DotNetObjectReferenceHelper.GetOrCreate(ref _dotNetRef, this);

    private sealed record SinkHandlers(Action<byte[]> OnChunk, Action<string?>? OnFinished);

    /// <summary>True when the runtime exposes <c>ReadableStream</c>.</summary>
    /// <remarks>
    /// During prerender/SSR (no JS runtime) this returns <c>default</c> (<c>false</c>) rather than
    /// throwing, so the result can't be distinguished from a genuine value. If you branch on it,
    /// defer the read to <c>OnAfterRenderAsync</c>.
    /// </remarks>
    public ValueTask<bool> IsSupported() => js.Invoke<bool>("BitButil.streams.isSupported");

    /// <summary>
    /// True when the runtime exposes <c>CompressionStream</c> - which is what
    /// <see cref="CreateCompression"/> and <see cref="CreateDecompression"/> need.
    /// </summary>
    public ValueTask<bool> IsTransformSupported() => js.Invoke<bool>("BitButil.streams.isTransformSupported");

    /// <summary>
    /// Invoked from JS for each chunk written into a sink. Public + <see cref="JSInvokableAttribute"/>
    /// so it can be dispatched through the per-instance <see cref="DotNetObjectReference{T}"/>.
    /// </summary>
    /// <remarks>
    /// The stream waits for this call to return before pulling the next chunk, which is how
    /// back-pressure reaches across the interop boundary instead of stopping at it. A slow handler
    /// slows the producer, as it should.
    /// </remarks>
    [JSInvokable(ChunkMethodName)]
    public void InvokeSinkChunk(Guid id, byte[] chunk)
    {
        if (_sinks.TryGetValue(id, out var handlers)) handlers.OnChunk(chunk);
    }

    /// <summary>
    /// Invoked from JS when a sink closes or aborts. Public + <see cref="JSInvokableAttribute"/> so
    /// it can be dispatched through the per-instance <see cref="DotNetObjectReference{T}"/>.
    /// </summary>
    [JSInvokable(CloseMethodName)]
    public void InvokeSinkClose(Guid id, string? reason)
    {
        // The sink is finished either way - drop it here rather than waiting for a Dispose the
        // caller has no reason to make once the stream has ended.
        if (_sinks.TryRemove(id, out var handlers)) handlers.OnFinished?.Invoke(reason);
    }

    /// <summary>
    /// Starts a request and hands back its body as a stream, before the body has arrived.
    /// </summary>
    /// <param name="request">
    /// The same shape <see cref="Fetch.Send"/> takes, including
    /// <see cref="FetchRequest.Signal"/> - a shared abort signal cancels the download as it would
    /// cancel any other request.
    /// </param>
    /// <returns>
    /// The response's status and headers, with <see cref="StreamedResponse.Stream"/> set when there
    /// is a body to read. A failed request or a bodyless response (a 204, a <c>HEAD</c>, an opaque
    /// <c>no-cors</c> response) comes back with <see cref="StreamedResponse.Error"/> set rather than
    /// throwing - a request that does not arrive is a normal outcome.
    /// </returns>
    /// <remarks>
    /// This is the difference between "download it, then look at it" and "look at it as it
    /// downloads": a gigabyte through <see cref="Fetch.Send"/> is a gigabyte of managed memory,
    /// while a gigabyte through here is one chunk at a time.
    /// </remarks>
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(FetchRequest))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(StreamedResponseDto))]
    public async ValueTask<StreamedResponse> FromResponse(FetchRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var id = Guid.NewGuid();
        var result = await js.Invoke<StreamedResponseDto?>("BitButil.streams.fromResponse", id, request.Url, request);

        if (result is null)
            return new StreamedResponse(null, 0, string.Empty, request.Url, null, "The runtime has no Streams API.");

        return new StreamedResponse(
            result.Ok ? new ReadableStreamHandle(js, id) : null,
            result.Status,
            result.StatusText ?? string.Empty,
            result.Url ?? request.Url,
            result.TotalBytes,
            result.Error);
    }

    /// <summary>
    /// Creates a stream that writes into C#: everything piped to it arrives at
    /// <paramref name="onChunk"/>.
    /// </summary>
    /// <param name="onChunk">
    /// Called for each chunk, in order. The producer waits for this to return before sending the
    /// next one, so a slow handler slows the download rather than queueing behind it.
    /// </param>
    /// <param name="onFinished">
    /// Called once when the stream ends: with null after a clean close, or with the reason when it
    /// was aborted.
    /// </param>
    /// <param name="highWaterMark">How many chunks may be queued before the producer is asked to wait. One is the honest default for a sink whose consumer is on the other side of an interop call.</param>
    /// <returns>A handle, or null when the runtime has no <c>WritableStream</c>.</returns>
    public async ValueTask<WritableStreamHandle?> CreateWritable(Action<byte[]> onChunk,
                                                                 Action<string?>? onFinished = null,
                                                                 int highWaterMark = 1)
    {
        ArgumentNullException.ThrowIfNull(onChunk);

        var id = Guid.NewGuid();
        _sinks[id] = new SinkHandlers(onChunk, onFinished);

        bool created;
        try
        {
            created = await js.Invoke<bool>("BitButil.streams.createWritable", DotNetRef, id, highWaterMark);
        }
        catch
        {
            // The sink was registered before the call, so a call that throws has to take it back out
            // again - nothing else will ever reach handlers for a stream that was never created.
            _sinks.TryRemove(id, out _);
            throw;
        }

        if (created is false)
        {
            _sinks.TryRemove(id, out _);
            return null;
        }

        return new WritableStreamHandle(js, id, () => _sinks.TryRemove(id, out _));
    }

    /// <summary>
    /// A transform that compresses everything passing through it, using the browser's own codec.
    /// </summary>
    /// <param name="format">Which codec. Defaults to gzip, the interoperable one.</param>
    /// <returns>A handle, or null when the runtime has no <c>CompressionStream</c>.</returns>
    /// <remarks>
    /// Unlike <see cref="Compression.Compress"/>, which takes bytes and returns bytes, this one
    /// never holds the whole payload: it is a stage in a pipeline.
    /// </remarks>
    public ValueTask<TransformStreamHandle?> CreateCompression(CompressionFormat format = CompressionFormat.Gzip)
        => CreateTransform(format, decompress: false);

    /// <summary>
    /// A transform that decompresses everything passing through it.
    /// </summary>
    /// <param name="format">The codec the data was compressed with. Defaults to gzip.</param>
    /// <returns>A handle, or null when the runtime has no <c>DecompressionStream</c>.</returns>
    /// <remarks>
    /// A format that does not match the data errors the pipe rather than this call - corrupt input
    /// is only discovered while reading it.
    /// </remarks>
    public ValueTask<TransformStreamHandle?> CreateDecompression(CompressionFormat format = CompressionFormat.Gzip)
        => CreateTransform(format, decompress: true);

    private async ValueTask<TransformStreamHandle?> CreateTransform(CompressionFormat format, bool decompress)
    {
        var id = Guid.NewGuid();
        var readableId = Guid.NewGuid();
        var writableId = Guid.NewGuid();

        var created = await js.Invoke<bool>("BitButil.streams.createCompression",
            id, readableId, writableId, ToName(format), decompress);

        return created ? new TransformStreamHandle(js, id, readableId, writableId) : null;
    }

    // The strings CompressionStream's constructor accepts - the same mapping Compression uses.
    private static string ToName(CompressionFormat format) => format switch
    {
        CompressionFormat.Deflate => "deflate",
        CompressionFormat.DeflateRaw => "deflate-raw",
        _ => "gzip",
    };

    /// <summary>
    /// On scope/circuit teardown, cancels every stream whose handle was never disposed, so an
    /// abandoned download stops rather than running to completion into nothing.
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        try
        {
            _sinks.Clear();
            await js.InvokeVoid("BitButil.streams.disposeAll");
        }
        catch (Exception ex) when (ex.IsIgnorableDisposalException()) { } // teardown: circuit gone, cancelled, or already disposed
        finally
        {
            _dotNetRef?.Dispose();
            _dotNetRef = null;
        }
        GC.SuppressFinalize(this);
    }
}
