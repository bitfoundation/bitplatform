using System;
using System.Threading.Tasks;
using Microsoft.JSInterop;

namespace Bit.Butil;

/// <summary>
/// Live handle to a decoded <see href="https://developer.mozilla.org/en-US/docs/Web/API/AudioBuffer">AudioBuffer</see>
/// produced by <see cref="WebAudio.DecodeAudioData"/> - the samples, in memory, ready to be played
/// as many times as you like.
/// </summary>
/// <remarks>
/// Decoding is the expensive part, so decode once and keep the handle: every
/// <see cref="WebAudio.CreateBufferSource"/> over the same buffer is cheap, which is what makes this
/// the right shape for sound effects and loops.
/// <br/>
/// The samples are uncompressed and can be large - roughly ten megabytes per stereo minute at 44.1
/// kHz - so dispose a buffer that is no longer needed.
/// </remarks>
public sealed class AudioBufferHandle : IAsyncDisposable
{
    private readonly IJSRuntime _js;
    private readonly Guid _id;
    private bool _disposed;

    internal AudioBufferHandle(IJSRuntime js, Guid id, double duration, int sampleRate, int numberOfChannels, int length)
    {
        _js = js;
        _id = id;
        Duration = duration;
        SampleRate = sampleRate;
        NumberOfChannels = numberOfChannels;
        Length = length;
    }

    /// <summary>The internal buffer id.</summary>
    public Guid Id => _id;

    /// <summary>How long the buffer plays for, in seconds.</summary>
    public double Duration { get; }

    /// <summary>Sample rate in samples per second - the context's rate, whatever the source file used.</summary>
    public int SampleRate { get; }

    /// <summary>Channel count.</summary>
    public int NumberOfChannels { get; }

    /// <summary>Sample count per channel.</summary>
    public int Length { get; }

    /// <summary>Releases the decoded samples. Calling it again does nothing.</summary>
    /// <remarks>
    /// Sources already playing this buffer are unaffected - they hold their own reference to it - so
    /// disposing merely means no new source can be built over it.
    /// </remarks>
    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;
        _disposed = true;
        try { await _js.InvokeVoid("BitButil.webAudio.releaseBuffer", _id); }
        catch (Exception ex) when (ex.IsIgnorableDisposalException()) { } // teardown: circuit gone, cancelled, or already disposed
    }
}
