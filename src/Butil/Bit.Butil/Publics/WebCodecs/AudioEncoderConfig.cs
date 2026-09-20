namespace Bit.Butil;

/// <summary>
/// How an <see cref="AudioEncoderHandle"/> should encode, mirroring
/// <see href="https://developer.mozilla.org/en-US/docs/Web/API/AudioEncoder/configure">AudioEncoder.configure()</see>.
/// </summary>
/// <remarks>
/// The sample rate and channel count have to match the <see cref="AudioDataHandle"/> instances that
/// will be fed in; an encoder does not resample.
/// </remarks>
public class AudioEncoderConfig
{
    /// <summary>The codec string, e.g. <c>"opus"</c>, <c>"mp4a.40.2"</c> (AAC-LC) or <c>"flac"</c>.</summary>
    public string Codec { get; set; } = string.Empty;

    /// <summary>Sample rate in samples per second, e.g. 48000.</summary>
    public int SampleRate { get; set; }

    /// <summary>Channel count - 1 for mono, 2 for stereo.</summary>
    public int NumberOfChannels { get; set; }

    /// <summary>Target bitrate in bits per second. Opus is happy anywhere from 6 kbps to 510 kbps.</summary>
    public long? Bitrate { get; set; }

    /// <summary><c>"constant"</c> or <c>"variable"</c>.</summary>
    public string? BitrateMode { get; set; }
}
