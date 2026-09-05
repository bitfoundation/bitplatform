using System;
using System.Threading.Tasks;
using Microsoft.JSInterop;

namespace Bit.Butil;

/// <summary>
/// Live handle to a <see href="https://developer.mozilla.org/en-US/docs/Web/API/VideoDecoder">VideoDecoder</see>
/// created by <see cref="WebCodecs.CreateVideoDecoder"/>: takes <see cref="EncodedVideoChunk"/>
/// instances and emits <see cref="VideoFrameHandle"/> ones on its output callback.
/// </summary>
/// <remarks>
/// This is frame-level access without a <c>&lt;video&gt;</c> element in the way: the frames can be
/// drawn to a canvas, read as pixels, or thrown away - and nothing about playback, timing or audio
/// sync happens unless the app makes it happen.
/// <br/>
/// Every frame handed to the callback must be disposed, and a decoder whose frames pile up
/// undisposed stalls.
/// </remarks>
public sealed class VideoDecoderHandle : WebCodecsHandle
{
    internal const string FrameMethodName = nameof(InvokeVideoDecoderFrame);
    internal const string ErrorMethodName = nameof(InvokeVideoDecoderError);

    private readonly Action<VideoFrameHandle> _onFrame;
    private readonly Action<string>? _onError;

    internal VideoDecoderHandle(IJSRuntime js, Guid id, Action<VideoFrameHandle> onFrame, Action<string>? onError)
        : base(js, id)
    {
        _onFrame = onFrame;
        _onError = onError;
        CallbackRef = DotNetObjectReference.Create(this);
        TrackCallbackRef(CallbackRef);
    }

    internal DotNetObjectReference<VideoDecoderHandle> CallbackRef { get; }

    /// <summary>
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/VideoDecoder/decode">VideoDecoder.decode()</see>:
    /// queues one compressed frame.
    /// </summary>
    /// <param name="chunk">The chunk to decode. The first one after configuring, or after a reset, has to be a key frame.</param>
    /// <returns>False when the decoder is unconfigured or closed, or the chunk was rejected.</returns>
    /// <remarks>
    /// Decoded frames arrive on the output callback, and a codec that reorders frames (B-frames) will
    /// emit them in presentation order some way behind the chunks that were submitted - which is what
    /// <see cref="WebCodecsHandle.Flush"/> exists to settle.
    /// </remarks>
    public ValueTask<bool> Decode(EncodedVideoChunk chunk)
    {
        ArgumentNullException.ThrowIfNull(chunk);

        return Js.Invoke<bool>("BitButil.webCodecs.decode", HandleId,
                               chunk.Type == EncodedChunkType.Delta ? "delta" : "key",
                               chunk.Timestamp, chunk.Duration, chunk.Data);
    }

    /// <summary>
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/VideoDecoder/configure">VideoDecoder.configure()</see>:
    /// changes the configuration of a running decoder.
    /// </summary>
    /// <param name="config">The new configuration.</param>
    /// <returns>False when the configuration is unsupported, or the decoder is closed.</returns>
    /// <remarks>
    /// The decoder is already configured when the handle is created; this is for a stream that
    /// switches codec or parameter sets mid-play. The next chunk after it has to be a key frame.
    /// </remarks>
    public ValueTask<bool> Configure(VideoDecoderConfig config)
    {
        ArgumentNullException.ThrowIfNull(config);

        return Js.Invoke<bool>("BitButil.webCodecs.configure", HandleId, config);
    }

    /// <summary>
    /// Invoked from JS for each decoded frame. Public + <see cref="JSInvokableAttribute"/> so it can
    /// be dispatched through the per-instance <see cref="DotNetObjectReference{T}"/>.
    /// </summary>
    [JSInvokable(FrameMethodName)]
    public async Task InvokeVideoDecoderFrame(Guid id, Guid frameId, long timestamp, long? duration, int width, int height, string format)
    {
        if (id != HandleId) return;

        var frame = new VideoFrameHandle(Js, frameId, timestamp, duration, width, height, format);
        try
        {
            _onFrame.Invoke(frame);
        }
        catch
        {
            // A callback that threw never took ownership of the frame, and an undisposed frame is
            // what stalls the decoder - so it is released here rather than left to the GC.
            await frame.DisposeAsync();
            throw;
        }
    }

    /// <summary>
    /// Invoked from JS when the decoder raises an error. Public + <see cref="JSInvokableAttribute"/>
    /// so it can be dispatched through the per-instance <see cref="DotNetObjectReference{T}"/>.
    /// </summary>
    [JSInvokable(ErrorMethodName)]
    public void InvokeVideoDecoderError(Guid id, string message)
    {
        if (id != HandleId) return;

        _onError?.Invoke(message);
    }
}
