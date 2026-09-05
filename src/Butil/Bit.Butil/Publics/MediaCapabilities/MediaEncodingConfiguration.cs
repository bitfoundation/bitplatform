namespace Bit.Butil;

/// <summary>
/// An encoding query for <see cref="MediaCapabilities.EncodingInfo"/>, mirroring
/// <see href="https://developer.mozilla.org/en-US/docs/Web/API/MediaCapabilities/encodingInfo">MediaEncodingConfiguration</see>.
/// </summary>
/// <remarks>
/// The same rules as <see cref="MediaDecodingConfiguration"/> apply: at least one track, each with
/// a content type that names its codecs. DRM plays no part in encoding, so there is no key-system
/// member here.
/// </remarks>
public class MediaEncodingConfiguration
{
    /// <summary>Whether the media is being encoded to a file or for a WebRTC stream.</summary>
    public MediaEncodingType Type { get; set; } = MediaEncodingType.Record;

    /// <summary>The video track to ask about. Leave null for an audio-only query.</summary>
    public VideoConfiguration? Video { get; set; }

    /// <summary>The audio track to ask about. Leave null for a video-only query.</summary>
    public AudioConfiguration? Audio { get; set; }

    internal MediaCapabilitiesJsConfiguration ToJsObject() => new()
    {
        Type = Type == MediaEncodingType.WebRtc ? "webrtc" : "record",
        Video = Video,
        Audio = Audio
    };
}
