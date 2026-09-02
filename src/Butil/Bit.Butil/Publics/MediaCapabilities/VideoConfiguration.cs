namespace Bit.Butil;

/// <summary>
/// The video half of a <see cref="MediaCapabilities"/> query, mirroring
/// <see href="https://developer.mozilla.org/en-US/docs/Web/API/VideoConfiguration">VideoConfiguration</see>.
/// </summary>
/// <remarks>
/// <see cref="ContentType"/>, <see cref="Width"/>, <see cref="Height"/>, <see cref="Bitrate"/> and
/// <see cref="Framerate"/> are required by the specification - a query missing any of them is
/// rejected, which <see cref="MediaCapabilities.DecodingInfo"/> surfaces as <c>null</c>. Everything
/// else is optional and is only sent when set, so an engine that does not know a member is never
/// handed one.
/// </remarks>
public class VideoConfiguration
{
    /// <summary>The MIME type with its codecs, e.g. <c>"video/mp4;codecs=avc1.42E01E"</c>. A type without codecs is rejected.</summary>
    public string ContentType { get; set; } = string.Empty;

    /// <summary>Frame width in pixels.</summary>
    public int Width { get; set; }

    /// <summary>Frame height in pixels.</summary>
    public int Height { get; set; }

    /// <summary>Average bitrate in bits per second.</summary>
    public long Bitrate { get; set; }

    /// <summary>Frames per second.</summary>
    public double Framerate { get; set; }

    /// <summary>True when the stream carries an alpha channel.</summary>
    public bool? HasAlphaChannel { get; set; }

    /// <summary>HDR metadata type: <c>"smpteSt2086"</c>, <c>"smpteSt2094-10"</c> or <c>"smpteSt2094-40"</c>.</summary>
    public string? HdrMetadataType { get; set; }

    /// <summary>Color gamut the display is asked to reproduce: <c>"srgb"</c>, <c>"p3"</c> or <c>"rec2020"</c>.</summary>
    public string? ColorGamut { get; set; }

    /// <summary>Transfer function: <c>"srgb"</c>, <c>"pq"</c> or <c>"hlg"</c>.</summary>
    public string? TransferFunction { get; set; }

    /// <summary>Scalable-video-coding mode for a WebRTC encoding query, e.g. <c>"L1T2"</c>.</summary>
    public string? ScalabilityMode { get; set; }

    /// <summary>True when a WebRTC encoding query wants spatial scalability.</summary>
    public bool? SpatialScalability { get; set; }
}
