using System;
using System.Threading.Tasks;
using Microsoft.JSInterop;

namespace Bit.Butil;

/// <summary>
/// A streaming <see href="https://developer.mozilla.org/en-US/docs/Web/API/TextDecoder">TextDecoder</see>
/// opened by <see cref="TextEncoding.CreateDecoder"/>. Feed it the chunks as they arrive, then
/// dispose it.
/// </summary>
/// <remarks>
/// The decoder exists because a multi-byte character can straddle a chunk boundary: it holds the
/// incomplete sequence until the next chunk completes it. That state is also why the last chunk has
/// to be decoded with <c>more: false</c> (or flushed through <see cref="Flush"/>) - otherwise a
/// trailing partial character is silently dropped.
/// </remarks>
public sealed class TextDecoderHandle : IAsyncDisposable
{
    private readonly IJSRuntime _js;
    private readonly Guid _id;
    private bool _disposed;

    internal TextDecoderHandle(IJSRuntime js, Guid id) { _js = js; _id = id; }

    /// <summary>The internal decoder id.</summary>
    public Guid Id => _id;

    /// <summary>
    /// Decodes one chunk.
    /// </summary>
    /// <param name="bytes">The chunk's bytes.</param>
    /// <param name="more">
    /// True while more chunks are coming - the decoder keeps a partial character back for the next
    /// call. Pass false for the last chunk so anything pending is flushed.
    /// </param>
    /// <returns>
    /// The text decoded so far from this chunk (which may be empty when the chunk only completed a
    /// character), or null when the decoder is gone or the bytes are invalid under a fatal decoder.
    /// </returns>
    public ValueTask<string?> Decode(byte[] bytes, bool more = true)
    {
        ArgumentNullException.ThrowIfNull(bytes);
        return _js.Invoke<string?>("BitButil.textEncoding.decodeChunk", _id, bytes, more);
    }

    /// <summary>
    /// Ends the stream: decodes nothing more, but emits whatever partial character was pending
    /// (as U+FFFD, or null under a fatal decoder). Equivalent to a final empty
    /// <see cref="Decode(byte[], bool)"/> with <c>more: false</c>.
    /// </summary>
    public ValueTask<string?> Flush() => _js.Invoke<string?>("BitButil.textEncoding.decodeChunk", _id, Array.Empty<byte>(), false);

    /// <summary>Drops the decoder. Calling it again does nothing.</summary>
    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;
        _disposed = true;
        try { await _js.InvokeVoid("BitButil.textEncoding.disposeDecoder", _id); }
        catch (Exception ex) when (ex.IsIgnorableDisposalException()) { } // teardown: circuit gone, cancelled, or already disposed
    }
}
