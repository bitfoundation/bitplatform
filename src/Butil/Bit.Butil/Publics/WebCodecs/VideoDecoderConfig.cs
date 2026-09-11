namespace Bit.Butil;

/// <summary>
/// How a <see cref="VideoDecoderHandle"/> should decode, mirroring
/// <see href="https://developer.mozilla.org/en-US/docs/Web/API/VideoDecoder/configure">VideoDecoder.configure()</see>.
/// </summary>
/// <remarks>
/// Only <see cref="Codec"/> is required, but <see cref="Description"/> usually is in practice: most
/// codecs keep their parameter sets out of the bitstream, and a decoder without them produces
/// nothing. When the chunks came from a <see cref="VideoEncoderHandle"/>, the first
/// <see cref="EncodedVideoChunk"/> carries exactly those bytes in
/// <see cref="EncodedVideoChunk.DecoderDescription"/>.
/// </remarks>
public class VideoDecoderConfig
{
    /// <summary>The codec string, with its profile and level - e.g. <c>"avc1.42001E"</c> or <c>"vp09.00.10.08"</c>.</summary>
    public string Codec { get; set; } = string.Empty;

    /// <summary>
    /// The codec's out-of-band parameter sets (an <c>avcC</c> box for H.264, the equivalent for
    /// HEVC/AV1). Leave null only for a codec that carries them in the bitstream, such as VP8.
    /// </summary>
    public byte[]? Description { get; set; }

    /// <summary>Frame width in pixels as encoded, when it is known ahead of the first frame.</summary>
    public int? CodedWidth { get; set; }

    /// <summary>Frame height in pixels as encoded.</summary>
    public int? CodedHeight { get; set; }

    /// <summary>Horizontal part of the display aspect ratio, for anamorphic content.</summary>
    public int? DisplayAspectWidth { get; set; }

    /// <summary>Vertical part of the display aspect ratio.</summary>
    public int? DisplayAspectHeight { get; set; }

    /// <summary><c>"no-preference"</c> (default), <c>"prefer-hardware"</c> or <c>"prefer-software"</c>.</summary>
    public string? HardwareAcceleration { get; set; }

    /// <summary>
    /// True to tell the decoder to emit frames as early as it can rather than filling a reordering
    /// buffer first - what a live or interactive stream wants, at some cost in throughput.
    /// </summary>
    public bool? OptimizeForLatency { get; set; }
}
