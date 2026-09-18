namespace Bit.Butil;

/// <summary>
/// How a <see cref="VideoEncoderHandle"/> should encode, mirroring
/// <see href="https://developer.mozilla.org/en-US/docs/Web/API/VideoEncoder/configure">VideoEncoder.configure()</see>.
/// </summary>
/// <remarks>
/// <see cref="Codec"/>, <see cref="Width"/> and <see cref="Height"/> are required; everything else
/// is a hint that is only sent when set. Probe a configuration with
/// <see cref="WebCodecs.IsConfigSupported(VideoEncoderConfig)"/> before building on it - support
/// varies by codec, by profile string, and by whether the machine has a hardware encoder.
/// </remarks>
public class VideoEncoderConfig
{
    /// <summary>
    /// The codec string, with its profile and level - <c>"avc1.42001E"</c>, <c>"vp8"</c>,
    /// <c>"vp09.00.10.08"</c>, <c>"av01.0.04M.08"</c>. A bare family name is usually rejected.
    /// </summary>
    public string Codec { get; set; } = string.Empty;

    /// <summary>Encoded frame width in pixels.</summary>
    public int Width { get; set; }

    /// <summary>Encoded frame height in pixels.</summary>
    public int Height { get; set; }

    /// <summary>Width the decoder should display at, when it differs from <see cref="Width"/> (anamorphic content).</summary>
    public int? DisplayWidth { get; set; }

    /// <summary>Height the decoder should display at, when it differs from <see cref="Height"/>.</summary>
    public int? DisplayHeight { get; set; }

    /// <summary>Target bitrate in bits per second.</summary>
    public long? Bitrate { get; set; }

    /// <summary>Expected frame rate, which the encoder uses to pace its rate control.</summary>
    public double? Framerate { get; set; }

    /// <summary>
    /// <c>"no-preference"</c> (default), <c>"prefer-hardware"</c> or <c>"prefer-software"</c>.
    /// Asking for hardware can make an otherwise supported configuration unsupported on a machine
    /// without the right encoder.
    /// </summary>
    public string? HardwareAcceleration { get; set; }

    /// <summary>Whether an alpha channel is <c>"keep"</c>-ed or <c>"discard"</c>-ed. Few codecs keep it.</summary>
    public string? Alpha { get; set; }

    /// <summary>Scalable-video-coding mode, e.g. <c>"L1T2"</c> or <c>"L1T3"</c> - temporal layers for adaptive delivery.</summary>
    public string? ScalabilityMode { get; set; }

    /// <summary>
    /// <c>"constant"</c>, <c>"variable"</c> or <c>"quantizer"</c>. Constant is what a live stream on
    /// a fixed budget wants; variable gives a stored file better quality per byte.
    /// </summary>
    public string? BitrateMode { get; set; }

    /// <summary>
    /// <c>"quality"</c> or <c>"realtime"</c>. Realtime tells the encoder to emit each frame as soon
    /// as it can rather than buffering to make better decisions - the difference between a usable
    /// video call and a smooth recording.
    /// </summary>
    public string? LatencyMode { get; set; }

    /// <summary>A hint about the content: <c>"text"</c>, <c>"motion"</c> or <c>"detail"</c>.</summary>
    public string? ContentHint { get; set; }
}
