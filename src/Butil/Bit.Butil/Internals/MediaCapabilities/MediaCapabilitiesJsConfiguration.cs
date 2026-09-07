namespace Bit.Butil;

/// <summary>
/// The JS-shaped configuration both <see cref="MediaCapabilities"/> queries send: the same object as
/// the public one except that the query type is already the string the browser expects.
/// </summary>
internal class MediaCapabilitiesJsConfiguration
{
    public string Type { get; set; } = string.Empty;

    public VideoConfiguration? Video { get; set; }

    public AudioConfiguration? Audio { get; set; }

    public MediaCapabilitiesKeySystemJsConfiguration? KeySystemConfiguration { get; set; }
}
