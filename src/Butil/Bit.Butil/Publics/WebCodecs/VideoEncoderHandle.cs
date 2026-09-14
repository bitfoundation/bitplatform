using System;
using System.Threading.Tasks;
using Microsoft.JSInterop;

namespace Bit.Butil;

/// <summary>
/// Live handle to a <see href="https://developer.mozilla.org/en-US/docs/Web/API/VideoEncoder">VideoEncoder</see>
/// created by <see cref="WebCodecs.CreateVideoEncoder"/>: takes <see cref="VideoFrameHandle"/>
/// instances and emits <see cref="EncodedVideoChunk"/> ones on its output callback.
/// </summary>
/// <remarks>
/// Nothing here writes a container: the chunks are raw compressed frames, and turning them into an
/// MP4 or WebM (or sending them over a transport) is the app's job. That is the point of WebCodecs -
/// <see cref="MediaRecorder"/> is the API that hands you a finished file instead.
/// </remarks>
public sealed class VideoEncoderHandle : WebCodecsHandle
{
    internal const string ChunkMethodName = nameof(InvokeVideoEncoderChunk);
    internal const string ErrorMethodName = nameof(InvokeVideoEncoderError);

    private readonly Action<EncodedVideoChunk> _onChunk;
    private readonly Action<string>? _onError;

    internal VideoEncoderHandle(IJSRuntime js, Guid id, Action<EncodedVideoChunk> onChunk, Action<string>? onError)
        : base(js, id)
    {
        _onChunk = onChunk;
        _onError = onError;
        CallbackRef = DotNetObjectReference.Create(this);
        TrackCallbackRef(CallbackRef);
    }

    internal DotNetObjectReference<VideoEncoderHandle> CallbackRef { get; }

    /// <summary>
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/VideoEncoder/encode">VideoEncoder.encode()</see>:
    /// queues one frame.
    /// </summary>
    /// <param name="frame">The frame to encode. It is not disposed here - dispose it yourself once the call returns.</param>
    /// <param name="keyFrame">True to force this frame to be encoded as a key frame.</param>
    /// <returns>False when the encoder is unconfigured or closed, or the frame is already disposed.</returns>
    /// <remarks>
    /// The encoded result arrives on the output callback, not from this call. Watch
    /// <see cref="WebCodecsHandle.GetQueueSize"/> and stop feeding when it climbs: every queued frame
    /// holds its pixels.
    /// </remarks>
    public ValueTask<bool> Encode(VideoFrameHandle frame, bool keyFrame = false)
    {
        ArgumentNullException.ThrowIfNull(frame);

        return Js.Invoke<bool>("BitButil.webCodecs.encodeFrame", HandleId, frame.Id, keyFrame);
    }

    /// <summary>
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/VideoEncoder/configure">VideoEncoder.configure()</see>:
    /// changes the configuration of a running encoder.
    /// </summary>
    /// <param name="config">The new configuration.</param>
    /// <returns>False when the configuration is unsupported, or the encoder is closed.</returns>
    /// <remarks>
    /// The encoder is already configured when the handle is created; this is for changing bitrate or
    /// resolution mid-stream, which makes the next emitted chunk a key frame.
    /// </remarks>
    public ValueTask<bool> Configure(VideoEncoderConfig config)
    {
        ArgumentNullException.ThrowIfNull(config);

        return Js.Invoke<bool>("BitButil.webCodecs.configure", HandleId, config);
    }

    /// <summary>
    /// Invoked from JS for each encoded chunk. Public + <see cref="JSInvokableAttribute"/> so it can
    /// be dispatched through the per-instance <see cref="DotNetObjectReference{T}"/>.
    /// </summary>
    [JSInvokable(ChunkMethodName)]
    public void InvokeVideoEncoderChunk(Guid id, string type, long timestamp, long? duration, byte[] data, byte[]? description)
    {
        if (id != HandleId) return;

        _onChunk.Invoke(new EncodedVideoChunk(ToChunkType(type), timestamp, duration, data, description));
    }

    /// <summary>
    /// Invoked from JS when the encoder raises an error. Public + <see cref="JSInvokableAttribute"/>
    /// so it can be dispatched through the per-instance <see cref="DotNetObjectReference{T}"/>.
    /// </summary>
    [JSInvokable(ErrorMethodName)]
    public void InvokeVideoEncoderError(Guid id, string message)
    {
        if (id != HandleId) return;

        _onError?.Invoke(message);
    }
}
