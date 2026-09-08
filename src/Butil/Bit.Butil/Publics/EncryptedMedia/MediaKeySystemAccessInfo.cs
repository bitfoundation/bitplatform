namespace Bit.Butil;

/// <summary>
/// What a key system agreed to, from
/// <see href="https://developer.mozilla.org/en-US/docs/Web/API/MediaKeySystemAccess">MediaKeySystemAccess</see>.
/// </summary>
/// <remarks>
/// The resolved <see cref="Configuration"/> is the interesting half: it holds only the capabilities
/// the browser could satisfy, with the optional members filled in, so it says which robustness level
/// and which session types are actually available - not merely that the key system exists.
/// </remarks>
public class MediaKeySystemAccessInfo
{
    /// <summary>The key system that answered, e.g. <c>"com.widevine.alpha"</c>.</summary>
    public string KeySystem { get; set; } = string.Empty;

    /// <summary>The configuration the browser resolved out of the ones that were offered.</summary>
    public MediaKeySystemConfiguration Configuration { get; set; } = new();
}
