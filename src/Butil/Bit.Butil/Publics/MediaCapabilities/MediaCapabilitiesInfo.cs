namespace Bit.Butil;

/// <summary>
/// The answer to a <see cref="MediaCapabilities"/> query, mirroring
/// <see href="https://developer.mozilla.org/en-US/docs/Web/API/MediaCapabilitiesInfo">MediaCapabilitiesInfo</see>.
/// </summary>
/// <remarks>
/// The three flags answer different questions: <see cref="Supported"/> is "can it play at all",
/// <see cref="Smooth"/> is "without dropping frames", and <see cref="PowerEfficient"/> is "on the
/// hardware decoder rather than the CPU". A ladder rung that is supported but neither smooth nor
/// power-efficient is exactly the one an adaptive player should skip.
/// </remarks>
public class MediaCapabilitiesInfo
{
    /// <summary>True when the engine can decode (or encode) this configuration at all.</summary>
    public bool Supported { get; set; }

    /// <summary>True when playback is expected to keep up without dropping frames.</summary>
    public bool Smooth { get; set; }

    /// <summary>True when the work is expected to run on dedicated hardware rather than the CPU.</summary>
    public bool PowerEfficient { get; set; }

    /// <summary>
    /// True when the query carried a <see cref="MediaDecodingConfiguration.KeySystemConfiguration"/>
    /// and the browser returned a usable key-system access for it.
    /// </summary>
    /// <remarks>
    /// The <c>MediaKeySystemAccess</c> object itself cannot cross the interop boundary; set up the
    /// actual DRM session with <see cref="EncryptedMedia"/> using the same key system.
    /// </remarks>
    public bool KeySystemAccessible { get; set; }
}
