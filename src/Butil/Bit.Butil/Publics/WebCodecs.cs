using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace Bit.Butil;

/// <summary>
/// Wraps the <see href="https://developer.mozilla.org/en-US/docs/Web/API/WebCodecs_API">WebCodecs API</see>:
/// the browser's own video and audio codecs, addressed frame by frame, with no media element and no
/// container in the way.
/// </summary>
/// <remarks>
/// Everything else in the media stack hands you a finished thing - a playing element, a recorded
/// file. WebCodecs hands you frames and compressed chunks, which is what makes a custom container,
/// a low-latency transport, a frame-accurate editor or a thumbnail extractor possible at all.
/// <br/>
/// The trade for that is ownership: a <see cref="VideoFrameHandle"/> and an
/// <see cref="AudioDataHandle"/> hold memory (often a GPU surface) that only their disposal
/// releases, and a codec whose outputs are never disposed stalls and then exhausts the tab. Dispose
/// each one as soon as it has been drawn, copied or encoded.
/// <br/>
/// Codecs work asynchronously: submitted work is queued and results arrive on the callback the
/// handle was created with. Use <see cref="WebCodecsHandle.GetQueueSize"/> as the backpressure
/// signal, and <see cref="WebCodecsHandle.Flush"/> to know that everything has come out.
/// </remarks>
[ButilService(typeof(WebCodecs))]
public class WebCodecs(IJSRuntime js) : IAsyncDisposable
{
    /// <summary>True when the runtime exposes the WebCodecs constructors.</summary>
    /// <remarks>
    /// During prerender/SSR (no JS runtime) this returns <c>default</c> (e.g. <c>false</c>/<c>0</c>)
    /// rather than throwing, so the result can't be distinguished from a genuine value. If you
    /// branch on it, defer the read to <c>OnAfterRenderAsync</c>.
    /// </remarks>
    public ValueTask<bool> IsSupported() => js.Invoke<bool>("BitButil.webCodecs.isSupported");

    /// <summary>
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/VideoEncoder/isConfigSupported_static">VideoEncoder.isConfigSupported()</see>:
    /// whether this engine can encode video with this exact configuration.
    /// </summary>
    /// <param name="config">The configuration to probe.</param>
    /// <remarks>
    /// Ask before creating an encoder rather than after: support depends on the full codec string
    /// (profile and level included), on the resolution, and on whether a hardware encoder is present
    /// when one was asked for.
    /// <br/>
    /// During prerender/SSR (no JS runtime) this returns <c>default</c> (e.g. <c>false</c>/<c>0</c>)
    /// rather than throwing, so the result can't be distinguished from a genuine value. If you
    /// branch on it, defer the read to <c>OnAfterRenderAsync</c>.
    /// </remarks>
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(VideoEncoderConfig))]
    public ValueTask<bool> IsConfigSupported(VideoEncoderConfig config)
        => js.Invoke<bool>("BitButil.webCodecs.isConfigSupported", "video-encoder", config);

    /// <summary>
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/VideoDecoder/isConfigSupported_static">VideoDecoder.isConfigSupported()</see>:
    /// whether this engine can decode video with this exact configuration.
    /// </summary>
    /// <param name="config">The configuration to probe.</param>
    /// <remarks>
    /// During prerender/SSR (no JS runtime) this returns <c>default</c> (e.g. <c>false</c>/<c>0</c>)
    /// rather than throwing, so the result can't be distinguished from a genuine value. If you
    /// branch on it, defer the read to <c>OnAfterRenderAsync</c>.
    /// </remarks>
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(VideoDecoderConfig))]
    public ValueTask<bool> IsConfigSupported(VideoDecoderConfig config)
        => js.Invoke<bool>("BitButil.webCodecs.isConfigSupported", "video-decoder", config);

    /// <summary>
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/AudioEncoder/isConfigSupported_static">AudioEncoder.isConfigSupported()</see>:
    /// whether this engine can encode audio with this exact configuration.
    /// </summary>
    /// <param name="config">The configuration to probe.</param>
    /// <remarks>
    /// During prerender/SSR (no JS runtime) this returns <c>default</c> (e.g. <c>false</c>/<c>0</c>)
    /// rather than throwing, so the result can't be distinguished from a genuine value. If you
    /// branch on it, defer the read to <c>OnAfterRenderAsync</c>.
    /// </remarks>
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(AudioEncoderConfig))]
    public ValueTask<bool> IsConfigSupported(AudioEncoderConfig config)
        => js.Invoke<bool>("BitButil.webCodecs.isConfigSupported", "audio-encoder", config);

    /// <summary>
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/AudioDecoder/isConfigSupported_static">AudioDecoder.isConfigSupported()</see>:
    /// whether this engine can decode audio with this exact configuration.
    /// </summary>
    /// <param name="config">The configuration to probe.</param>
    /// <remarks>
    /// During prerender/SSR (no JS runtime) this returns <c>default</c> (e.g. <c>false</c>/<c>0</c>)
    /// rather than throwing, so the result can't be distinguished from a genuine value. If you
    /// branch on it, defer the read to <c>OnAfterRenderAsync</c>.
    /// </remarks>
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(AudioDecoderConfig))]
    public ValueTask<bool> IsConfigSupported(AudioDecoderConfig config)
        => js.Invoke<bool>("BitButil.webCodecs.isConfigSupported", "audio-decoder", config);

    /// <summary>
    /// Creates and configures a <see href="https://developer.mozilla.org/en-US/docs/Web/API/VideoEncoder">VideoEncoder</see>.
    /// </summary>
    /// <param name="config">How to encode. Probe it with <see cref="IsConfigSupported(VideoEncoderConfig)"/> first.</param>
    /// <param name="onChunk">
    /// Called with each encoded chunk. The first one also carries the decoder description a matching
    /// <see cref="VideoDecoderConfig"/> needs - keep it.
    /// </param>
    /// <param name="onError">Called when the encoder gives up; it is <see cref="CodecState.Closed"/> from then on.</param>
    /// <returns>The handle, or <c>null</c> when WebCodecs is missing or the configuration was rejected.</returns>
    /// <remarks>
    /// The handle comes back already configured, so frames can be submitted straight away.
    /// </remarks>
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(VideoEncoderConfig))]
    public async ValueTask<VideoEncoderHandle?> CreateVideoEncoder(VideoEncoderConfig config,
                                                                   Action<EncodedVideoChunk> onChunk,
                                                                   Action<string>? onError = null)
    {
        ArgumentNullException.ThrowIfNull(config);
        ArgumentNullException.ThrowIfNull(onChunk);

        var id = Guid.NewGuid();
        var handle = new VideoEncoderHandle(js, id, onChunk, onError);
        bool created;
        try
        {
            created = await js.Invoke<bool>("BitButil.webCodecs.create", "video-encoder", id, config,
                                            handle.CallbackRef, VideoEncoderHandle.ChunkMethodName, VideoEncoderHandle.ErrorMethodName);
        }
        catch
        {
            // The handle owns a DotNetObjectReference from the moment it is constructed, so a throw
            // that never returns it to the caller has to release it here.
            await handle.DisposeAsync();
            throw;
        }

        if (created is false)
        {
            await handle.DisposeAsync();
            return null;
        }

        return handle;
    }

    /// <summary>
    /// Creates and configures a <see href="https://developer.mozilla.org/en-US/docs/Web/API/VideoDecoder">VideoDecoder</see>.
    /// </summary>
    /// <param name="config">How to decode. Most codecs need <see cref="VideoDecoderConfig.Description"/> set.</param>
    /// <param name="onFrame">
    /// Called with each decoded frame. Dispose every one of them - the decoder stalls when its
    /// frames are not released.
    /// </param>
    /// <param name="onError">Called when the decoder gives up; it is <see cref="CodecState.Closed"/> from then on.</param>
    /// <returns>The handle, or <c>null</c> when WebCodecs is missing or the configuration was rejected.</returns>
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(VideoDecoderConfig))]
    public async ValueTask<VideoDecoderHandle?> CreateVideoDecoder(VideoDecoderConfig config,
                                                                   Action<VideoFrameHandle> onFrame,
                                                                   Action<string>? onError = null)
    {
        ArgumentNullException.ThrowIfNull(config);
        ArgumentNullException.ThrowIfNull(onFrame);

        var id = Guid.NewGuid();
        var handle = new VideoDecoderHandle(js, id, onFrame, onError);
        bool created;
        try
        {
            created = await js.Invoke<bool>("BitButil.webCodecs.create", "video-decoder", id, config,
                                            handle.CallbackRef, VideoDecoderHandle.FrameMethodName, VideoDecoderHandle.ErrorMethodName);
        }
        catch
        {
            await handle.DisposeAsync();   // as in CreateVideoEncoder: the callback ref is already live
            throw;
        }

        if (created is false)
        {
            await handle.DisposeAsync();
            return null;
        }

        return handle;
    }

    /// <summary>
    /// Creates and configures an <see href="https://developer.mozilla.org/en-US/docs/Web/API/AudioEncoder">AudioEncoder</see>.
    /// </summary>
    /// <param name="config">How to encode.</param>
    /// <param name="onChunk">Called with each encoded packet; the first also carries the decoder description.</param>
    /// <param name="onError">Called when the encoder gives up.</param>
    /// <returns>The handle, or <c>null</c> when WebCodecs is missing or the configuration was rejected.</returns>
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(AudioEncoderConfig))]
    public async ValueTask<AudioEncoderHandle?> CreateAudioEncoder(AudioEncoderConfig config,
                                                                   Action<EncodedAudioChunk> onChunk,
                                                                   Action<string>? onError = null)
    {
        ArgumentNullException.ThrowIfNull(config);
        ArgumentNullException.ThrowIfNull(onChunk);

        var id = Guid.NewGuid();
        var handle = new AudioEncoderHandle(js, id, onChunk, onError);
        bool created;
        try
        {
            created = await js.Invoke<bool>("BitButil.webCodecs.create", "audio-encoder", id, config,
                                            handle.CallbackRef, AudioEncoderHandle.ChunkMethodName, AudioEncoderHandle.ErrorMethodName);
        }
        catch
        {
            await handle.DisposeAsync();   // as in CreateVideoEncoder: the callback ref is already live
            throw;
        }

        if (created is false)
        {
            await handle.DisposeAsync();
            return null;
        }

        return handle;
    }

    /// <summary>
    /// Creates and configures an <see href="https://developer.mozilla.org/en-US/docs/Web/API/AudioDecoder">AudioDecoder</see>.
    /// </summary>
    /// <param name="config">How to decode.</param>
    /// <param name="onData">Called with each decoded block of samples. Dispose every one of them.</param>
    /// <param name="onError">Called when the decoder gives up.</param>
    /// <returns>The handle, or <c>null</c> when WebCodecs is missing or the configuration was rejected.</returns>
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(AudioDecoderConfig))]
    public async ValueTask<AudioDecoderHandle?> CreateAudioDecoder(AudioDecoderConfig config,
                                                                   Action<AudioDataHandle> onData,
                                                                   Action<string>? onError = null)
    {
        ArgumentNullException.ThrowIfNull(config);
        ArgumentNullException.ThrowIfNull(onData);

        var id = Guid.NewGuid();
        var handle = new AudioDecoderHandle(js, id, onData, onError);
        bool created;
        try
        {
            created = await js.Invoke<bool>("BitButil.webCodecs.create", "audio-decoder", id, config,
                                            handle.CallbackRef, AudioDecoderHandle.DataMethodName, AudioDecoderHandle.ErrorMethodName);
        }
        catch
        {
            await handle.DisposeAsync();   // as in CreateVideoEncoder: the callback ref is already live
            throw;
        }

        if (created is false)
        {
            await handle.DisposeAsync();
            return null;
        }

        return handle;
    }

    /// <summary>
    /// Grabs the current frame of a <c>&lt;video&gt;</c>, <c>&lt;canvas&gt;</c> or <c>&lt;img&gt;</c>
    /// as a <see href="https://developer.mozilla.org/en-US/docs/Web/API/VideoFrame">VideoFrame</see>.
    /// </summary>
    /// <param name="source">The element to capture from.</param>
    /// <param name="timestamp">
    /// The presentation timestamp in microseconds to stamp the frame with. It is the app's timeline,
    /// not the element's: an encoder uses it to pace the output, so a capture loop normally counts it
    /// up by one frame interval each time.
    /// </param>
    /// <param name="duration">How long the frame should be shown, in microseconds.</param>
    /// <returns>The frame, or <c>null</c> when the element has nothing to capture yet.</returns>
    /// <remarks>
    /// Dispose the frame once it has been encoded or drawn. Capturing from a camera stream this way -
    /// a <c>&lt;video&gt;</c> playing a <see cref="MediaStreamHandle"/> - is the usual way to feed an
    /// encoder without a recorder in between.
    /// </remarks>
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(VideoFrameJsInfo))]
    public async ValueTask<VideoFrameHandle?> CaptureFrame(ElementReference source, long timestamp, long? duration = null)
    {
        var id = Guid.NewGuid();
        var info = await js.Invoke<VideoFrameJsInfo?>("BitButil.webCodecs.frameFromElement", id, source, timestamp, duration);

        return info is null ? null : new VideoFrameHandle(js, id, info.Timestamp, info.Duration, info.Width, info.Height, info.Format);
    }

    /// <summary>
    /// Builds a <see href="https://developer.mozilla.org/en-US/docs/Web/API/VideoFrame">VideoFrame</see>
    /// out of raw pixels the app produced itself.
    /// </summary>
    /// <param name="data">The pixel bytes, laid out exactly as <paramref name="format"/> requires.</param>
    /// <param name="format">The pixel format, e.g. <c>"RGBA"</c>, <c>"BGRA"</c>, <c>"I420"</c> or <c>"NV12"</c>.</param>
    /// <param name="width">Coded width in pixels.</param>
    /// <param name="height">Coded height in pixels.</param>
    /// <param name="timestamp">Presentation timestamp in microseconds.</param>
    /// <param name="duration">How long the frame should be shown, in microseconds.</param>
    /// <returns>The frame, or <c>null</c> when the format is unknown or the buffer is the wrong size for it.</returns>
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(VideoFrameJsInfo))]
    public async ValueTask<VideoFrameHandle?> CreateFrame(byte[] data, string format, int width, int height, long timestamp, long? duration = null)
    {
        ArgumentNullException.ThrowIfNull(data);

        var id = Guid.NewGuid();
        var info = await js.Invoke<VideoFrameJsInfo?>("BitButil.webCodecs.frameFromBytes", id, format, width, height, timestamp, duration, data);

        return info is null ? null : new VideoFrameHandle(js, id, info.Timestamp, info.Duration, info.Width, info.Height, info.Format);
    }

    /// <summary>
    /// Builds an <see href="https://developer.mozilla.org/en-US/docs/Web/API/AudioData">AudioData</see>
    /// out of PCM samples, ready to be encoded.
    /// </summary>
    /// <param name="data">The sample bytes, in <paramref name="format"/>'s layout.</param>
    /// <param name="format">The sample format: <c>"f32"</c>, <c>"f32-planar"</c>, <c>"s16"</c>, <c>"s16-planar"</c>, <c>"u8"</c> or <c>"u8-planar"</c>.</param>
    /// <param name="sampleRate">Sample rate in samples per second.</param>
    /// <param name="numberOfFrames">Sample count per channel.</param>
    /// <param name="numberOfChannels">Channel count.</param>
    /// <param name="timestamp">Presentation timestamp in microseconds.</param>
    /// <returns>The audio data, or <c>null</c> when the buffer doesn't match the format and counts.</returns>
    /// <remarks>
    /// The buffer has to hold exactly <c>numberOfFrames * numberOfChannels</c> samples of the given
    /// format - a mismatch is rejected rather than truncated.
    /// </remarks>
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(AudioDataJsInfo))]
    public async ValueTask<AudioDataHandle?> CreateAudioData(byte[] data, string format, int sampleRate, int numberOfFrames, int numberOfChannels, long timestamp)
    {
        ArgumentNullException.ThrowIfNull(data);

        var id = Guid.NewGuid();
        var info = await js.Invoke<AudioDataJsInfo?>("BitButil.webCodecs.createAudioData",
                                                     id, format, sampleRate, numberOfFrames, numberOfChannels, timestamp, data);

        return info is null
            ? null
            : new AudioDataHandle(js, id, info.Timestamp, info.Duration, info.SampleRate, info.NumberOfFrames, info.NumberOfChannels, info.Format);
    }

    /// <summary>
    /// On scope/circuit teardown, closes every codec and releases every frame and audio block whose
    /// handle was never disposed.
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        try { await js.InvokeVoid("BitButil.webCodecs.disposeAll"); }
        catch (Exception ex) when (ex.IsIgnorableDisposalException()) { } // teardown: circuit gone, cancelled, or already disposed
        GC.SuppressFinalize(this);
    }
}
