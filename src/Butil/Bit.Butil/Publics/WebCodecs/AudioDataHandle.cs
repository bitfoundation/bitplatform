using System;
using System.Threading.Tasks;
using Microsoft.JSInterop;

namespace Bit.Butil;

/// <summary>
/// Live handle to an <see href="https://developer.mozilla.org/en-US/docs/Web/API/AudioData">AudioData</see>:
/// one block of uncompressed samples, either built from PCM bytes or produced by an
/// <see cref="AudioDecoderHandle"/>.
/// </summary>
/// <remarks>
/// Like a video frame, this holds memory the garbage collector cannot reclaim - dispose it as soon
/// as the samples have been read or handed to an encoder.
/// </remarks>
public sealed class AudioDataHandle : IAsyncDisposable
{
    private readonly IJSRuntime _js;
    private readonly Guid _id;
    private bool _disposed;

    internal AudioDataHandle(IJSRuntime js, Guid id, long timestamp, long? duration, int sampleRate, int numberOfFrames, int numberOfChannels, string format)
    {
        _js = js;
        _id = id;
        Timestamp = timestamp;
        Duration = duration;
        SampleRate = sampleRate;
        NumberOfFrames = numberOfFrames;
        NumberOfChannels = numberOfChannels;
        Format = format;
    }

    /// <summary>The internal audio-data id.</summary>
    public Guid Id => _id;

    /// <summary>Presentation timestamp in microseconds.</summary>
    public long Timestamp { get; }

    /// <summary>How long these samples last, in microseconds, when it is known.</summary>
    public long? Duration { get; }

    /// <summary>Sample rate in samples per second.</summary>
    public int SampleRate { get; }

    /// <summary>Sample count per channel.</summary>
    public int NumberOfFrames { get; }

    /// <summary>Channel count.</summary>
    public int NumberOfChannels { get; }

    /// <summary>
    /// The sample format: <c>"f32"</c>, <c>"s16"</c>, <c>"u8"</c> and their <c>-planar</c> variants.
    /// The suffix is what decides whether <see cref="CopyToBytes"/> has one plane or one per channel.
    /// </summary>
    public string Format { get; }

    /// <summary>
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/AudioData/copyTo">AudioData.copyTo()</see>:
    /// the raw samples of one plane.
    /// </summary>
    /// <param name="planeIndex">
    /// Which plane to copy. An interleaved format (<c>"f32"</c>) has only plane 0 and holds every
    /// channel in it; a planar format (<c>"f32-planar"</c>) has one plane per channel.
    /// </param>
    /// <returns>The sample bytes, or <c>null</c> when the plane doesn't exist or the data is closed.</returns>
    /// <remarks>
    /// During prerender/SSR (no JS runtime) this returns <c>default</c> (e.g. <c>false</c>/<c>0</c>)
    /// rather than throwing, so the result can't be distinguished from a genuine value. If you
    /// branch on it, defer the read to <c>OnAfterRenderAsync</c>.
    /// </remarks>
    public ValueTask<byte[]?> CopyToBytes(int planeIndex = 0)
        => _js.Invoke<byte[]?>("BitButil.webCodecs.copyAudio", _id, planeIndex);

    /// <summary>
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/AudioData/close">AudioData.close()</see>:
    /// releases the samples. Calling it again does nothing.
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;
        _disposed = true;
        try { await _js.InvokeVoid("BitButil.webCodecs.closeAudio", _id); }
        catch (Exception ex) when (ex.IsIgnorableDisposalException()) { } // teardown: circuit gone, cancelled, or already disposed
    }
}
