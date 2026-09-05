using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace Bit.Butil;

/// <summary>
/// Live handle to a <see href="https://developer.mozilla.org/en-US/docs/Web/API/VideoFrame">VideoFrame</see>:
/// one uncompressed frame, either captured from an element or produced by a
/// <see cref="VideoDecoderHandle"/>.
/// </summary>
/// <remarks>
/// A frame holds real memory - frequently a GPU surface - that the garbage collector cannot reclaim,
/// and a decoder that runs out of them stalls. Dispose every frame as soon as you are done with it;
/// this is the one place in Butil where forgetting has an immediate cost.
/// </remarks>
public sealed class VideoFrameHandle : IAsyncDisposable
{
    private readonly IJSRuntime _js;
    private readonly Guid _id;
    private bool _disposed;

    internal VideoFrameHandle(IJSRuntime js, Guid id, long timestamp, long? duration, int width, int height, string format)
    {
        _js = js;
        _id = id;
        Timestamp = timestamp;
        Duration = duration;
        Width = width;
        Height = height;
        Format = format;
    }

    /// <summary>The internal frame id.</summary>
    public Guid Id => _id;

    /// <summary>Presentation timestamp in microseconds.</summary>
    public long Timestamp { get; }

    /// <summary>How long the frame is shown, in microseconds, when it is known.</summary>
    public long? Duration { get; }

    /// <summary>Display width in pixels.</summary>
    public int Width { get; }

    /// <summary>Display height in pixels.</summary>
    public int Height { get; }

    /// <summary>
    /// The pixel format, e.g. <c>"I420"</c>, <c>"NV12"</c>, <c>"RGBA"</c> or <c>"BGRA"</c>. Empty for
    /// a frame whose pixels the engine keeps opaque - which is normal for hardware-decoded frames,
    /// and is why <see cref="DrawTo"/> works on frames <see cref="CopyToBytes"/> cannot read.
    /// </summary>
    public string Format { get; }

    /// <summary>
    /// Draws the frame into a <c>&lt;canvas&gt;</c>, resizing the canvas to the frame first.
    /// </summary>
    /// <param name="canvas">The <c>&lt;canvas&gt;</c> to draw into.</param>
    /// <returns>False when the element isn't a canvas, or its 2D context is unavailable.</returns>
    /// <remarks>
    /// The cheap way to show a decoded frame: the browser can keep a hardware-decoded frame on the
    /// GPU all the way to the screen, where copying it to bytes would drag it through main memory.
    /// </remarks>
    public ValueTask<bool> DrawTo(ElementReference canvas)
        => _js.Invoke<bool>("BitButil.webCodecs.drawFrame", _id, canvas);

    /// <summary>
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/VideoFrame/copyTo">VideoFrame.copyTo()</see>:
    /// the raw pixels, in the frame's own <see cref="Format"/>.
    /// </summary>
    /// <returns>The pixel bytes, or <c>null</c> when the frame is closed or its pixels are not readable.</returns>
    /// <remarks>
    /// The planes are laid out as the format defines them - <c>I420</c> is a full-size Y plane
    /// followed by two quarter-size chroma planes, not interleaved RGB. For display, prefer
    /// <see cref="DrawTo"/>; use this when the pixels themselves are the point.
    /// <br/>
    /// During prerender/SSR (no JS runtime) this returns <c>default</c> (e.g. <c>false</c>/<c>0</c>)
    /// rather than throwing, so the result can't be distinguished from a genuine value. If you
    /// branch on it, defer the read to <c>OnAfterRenderAsync</c>.
    /// </remarks>
    public ValueTask<byte[]?> CopyToBytes() => _js.Invoke<byte[]?>("BitButil.webCodecs.copyFrame", _id);

    /// <summary>
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/VideoFrame/close">VideoFrame.close()</see>:
    /// releases the frame's memory. Calling it again does nothing.
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;
        _disposed = true;
        try { await _js.InvokeVoid("BitButil.webCodecs.closeFrame", _id); }
        catch (Exception ex) when (ex.IsIgnorableDisposalException()) { } // teardown: circuit gone, cancelled, or already disposed
    }
}
