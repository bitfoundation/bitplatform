namespace Bit.Butil;

/// <summary>
/// One codec-and-robustness combination a <see cref="MediaKeySystemConfiguration"/> is willing to
/// accept, mirroring
/// <see href="https://developer.mozilla.org/en-US/docs/Web/API/Navigator/requestMediaKeySystemAccess#audiocapabilities">MediaKeySystemMediaCapability</see>.
/// </summary>
/// <remarks>
/// A configuration lists these in preference order and the browser keeps the ones it can satisfy, so
/// the resolved configuration that comes back tells you which robustness the key system actually
/// agreed to - the difference between software decryption and a hardware-secured pipeline, and
/// therefore between SD and HD entitlement on most services.
/// </remarks>
public class MediaKeySystemMediaCapability
{
    /// <summary>The container with its codecs, e.g. <c>video/mp4;codecs="avc1.42E01E"</c>. Required.</summary>
    public string ContentType { get; set; } = string.Empty;

    /// <summary>
    /// The robustness level, in the key system's own vocabulary - Widevine's <c>SW_SECURE_CRYPTO</c>
    /// through <c>HW_SECURE_ALL</c>, PlayReady's <c>3000</c>. An empty string asks for the key
    /// system's default, which some engines refuse for video.
    /// </summary>
    public string? Robustness { get; set; }

    /// <summary>The encryption scheme the content uses: <c>"cenc"</c>, <c>"cbcs"</c> or <c>"cbcs-1-9"</c>.</summary>
    public string? EncryptionScheme { get; set; }
}
