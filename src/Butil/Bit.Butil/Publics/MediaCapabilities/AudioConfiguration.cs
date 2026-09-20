namespace Bit.Butil;

/// <summary>
/// The audio half of a <see cref="MediaCapabilities"/> query, mirroring
/// <see href="https://developer.mozilla.org/en-US/docs/Web/API/AudioConfiguration">AudioConfiguration</see>.
/// </summary>
/// <remarks>
/// Only <see cref="ContentType"/> is required; the rest are sent only when set.
/// </remarks>
public class AudioConfiguration
{
    /// <summary>The MIME type with its codecs, e.g. <c>"audio/mp4;codecs=mp4a.40.2"</c>.</summary>
    public string ContentType { get; set; } = string.Empty;

    /// <summary>Channel count, as a string the spec understands - <c>"2"</c>, <c>"5.1"</c>.</summary>
    public string? Channels { get; set; }

    /// <summary>Average bitrate in bits per second.</summary>
    public long? Bitrate { get; set; }

    /// <summary>Sample rate in samples per second, e.g. 48000.</summary>
    public int? Samplerate { get; set; }

    /// <summary>Whether the audio is to be rendered spatially. Only meaningful for a decoding query.</summary>
    public bool? SpatialRendering { get; set; }
}
