using System.Linq;

namespace Bit.Butil;

/// <summary>
/// The optional DRM half of a decoding query, mirroring the <c>keySystemConfiguration</c> member of
/// <see href="https://developer.mozilla.org/en-US/docs/Web/API/MediaCapabilities/decodingInfo">MediaCapabilities.decodingInfo()</see>.
/// Asking for it is how you learn whether a codec plays smoothly under DRM, which is not the same
/// question as whether it plays smoothly in the clear.
/// </summary>
/// <remarks>
/// When the browser can satisfy it, <see cref="MediaCapabilitiesInfo.KeySystemAccessible"/> comes
/// back true and the same key system and robustness can be handed to
/// <see cref="EncryptedMedia.CreateMediaKeys"/>.
/// </remarks>
public class MediaCapabilitiesKeySystemConfiguration
{
    /// <summary>The key system id, e.g. <c>"com.widevine.alpha"</c>.</summary>
    public string KeySystem { get; set; } = string.Empty;

    /// <summary>The initialization-data type the content uses, e.g. <c>"cenc"</c>.</summary>
    public string? InitDataType { get; set; }

    /// <summary>Whether a distinctive identifier is required, the same values
    /// <see cref="MediaKeySystemConfiguration.DistinctiveIdentifier"/> takes.</summary>
    public MediaKeysRequirement? DistinctiveIdentifier { get; set; }

    /// <summary>Whether persistent state is required, the same values
    /// <see cref="MediaKeySystemConfiguration.PersistentState"/> takes.</summary>
    public MediaKeysRequirement? PersistentState { get; set; }

    /// <summary>The session types the content needs, e.g. a single <see cref="MediaKeySessionType.Temporary"/>.</summary>
    public MediaKeySessionType[]? SessionTypes { get; set; }

    /// <summary>The robustness level asked of the audio track, e.g. <c>"SW_SECURE_CRYPTO"</c>. Key-system specific, so a string.</summary>
    public string? AudioRobustness { get; set; }

    /// <summary>The robustness level asked of the video track, e.g. <c>"HW_SECURE_ALL"</c>. Key-system specific, so a string.</summary>
    public string? VideoRobustness { get; set; }

    internal MediaCapabilitiesKeySystemJsConfiguration ToJsObject() => new()
    {
        KeySystem = KeySystem,
        InitDataType = InitDataType,
        DistinctiveIdentifier = MediaKeySystemConfiguration.ToName(DistinctiveIdentifier),
        PersistentState = MediaKeySystemConfiguration.ToName(PersistentState),
        SessionTypes = SessionTypes?.Select(MediaKeySystemConfiguration.ToName).ToArray(),
        AudioRobustness = AudioRobustness,
        VideoRobustness = VideoRobustness
    };
}
