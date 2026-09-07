namespace Bit.Butil;

/// <summary>
/// How an <see cref="AudioDecoderHandle"/> should decode, mirroring
/// <see href="https://developer.mozilla.org/en-US/docs/Web/API/AudioDecoder/configure">AudioDecoder.configure()</see>.
/// </summary>
public class AudioDecoderConfig
{
    /// <summary>The codec string, e.g. <c>"opus"</c> or <c>"mp4a.40.2"</c>.</summary>
    public string Codec { get; set; } = string.Empty;

    /// <summary>Sample rate in samples per second the chunks were encoded at.</summary>
    public int SampleRate { get; set; }

    /// <summary>Channel count the chunks were encoded with.</summary>
    public int NumberOfChannels { get; set; }

    /// <summary>
    /// The codec's out-of-band header, where it has one - an Opus identification header, an AAC
    /// AudioSpecificConfig. The first <see cref="EncodedAudioChunk"/> produced by an
    /// <see cref="AudioEncoderHandle"/> carries these bytes in
    /// <see cref="EncodedAudioChunk.DecoderDescription"/>.
    /// </summary>
    public byte[]? Description { get; set; }
}
