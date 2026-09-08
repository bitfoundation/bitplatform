namespace Bit.Butil;

/// <summary>
/// A decoding query for <see cref="MediaCapabilities.DecodingInfo"/>, mirroring
/// <see href="https://developer.mozilla.org/en-US/docs/Web/API/MediaCapabilities/decodingInfo">MediaDecodingConfiguration</see>.
/// </summary>
/// <remarks>
/// At least one of <see cref="Video"/> and <see cref="Audio"/> has to be set, and each one that is
/// set has to carry a full content type including its codecs. A configuration the specification
/// rejects comes back as <c>null</c> rather than as an unsupported result, so a null answer means
/// "the question was malformed", not "the browser said no".
/// </remarks>
public class MediaDecodingConfiguration
{
    /// <summary>Whether the media arrives as a file, through Media Source Extensions, or over WebRTC.</summary>
    public MediaDecodingType Type { get; set; } = MediaDecodingType.File;

    /// <summary>The video track to ask about. Leave null for an audio-only query.</summary>
    public VideoConfiguration? Video { get; set; }

    /// <summary>The audio track to ask about. Leave null for a video-only query.</summary>
    public AudioConfiguration? Audio { get; set; }

    /// <summary>The DRM the content is protected with, when the question is about protected playback.</summary>
    public MediaCapabilitiesKeySystemConfiguration? KeySystemConfiguration { get; set; }

    internal MediaCapabilitiesJsConfiguration ToJsObject() => new()
    {
        Type = Type switch
        {
            MediaDecodingType.MediaSource => "media-source",
            MediaDecodingType.WebRtc => "webrtc",
            _ => "file"
        },
        Video = Video,
        Audio = Audio,
        KeySystemConfiguration = KeySystemConfiguration?.ToJsObject()
    };
}
