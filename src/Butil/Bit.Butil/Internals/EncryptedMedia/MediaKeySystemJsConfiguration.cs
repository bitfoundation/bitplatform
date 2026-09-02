namespace Bit.Butil;

/// <summary>
/// The JS-shaped key-system configuration, used in both directions: sent as the offer, and received
/// back as the configuration the browser resolved. Same members as
/// <see cref="MediaKeySystemConfiguration"/>, except that the enumerated ones are already the
/// hyphenated strings the specification uses.
/// </summary>
internal class MediaKeySystemJsConfiguration
{
    public string? Label { get; set; }

    public string[]? InitDataTypes { get; set; }

    public MediaKeySystemMediaCapability[]? AudioCapabilities { get; set; }

    public MediaKeySystemMediaCapability[]? VideoCapabilities { get; set; }

    public string? DistinctiveIdentifier { get; set; }

    public string? PersistentState { get; set; }

    public string[]? SessionTypes { get; set; }
}
