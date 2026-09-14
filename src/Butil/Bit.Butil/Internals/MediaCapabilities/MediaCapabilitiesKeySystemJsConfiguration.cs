namespace Bit.Butil;

/// <summary>
/// The JS-shaped DRM half of a <see cref="MediaCapabilities"/> query: the same members as
/// <see cref="MediaCapabilitiesKeySystemConfiguration"/>, except that the enum-typed ones are
/// already the strings the browser expects. The robustness members stay strings on both sides -
/// their values are defined by each key system, not by the specification.
/// </summary>
internal class MediaCapabilitiesKeySystemJsConfiguration
{
    public string KeySystem { get; set; } = string.Empty;

    public string? InitDataType { get; set; }

    public string? DistinctiveIdentifier { get; set; }

    public string? PersistentState { get; set; }

    public string[]? SessionTypes { get; set; }

    public string? AudioRobustness { get; set; }

    public string? VideoRobustness { get; set; }

    public string? AudioEncryptionScheme { get; set; }

    public string? VideoEncryptionScheme { get; set; }
}
