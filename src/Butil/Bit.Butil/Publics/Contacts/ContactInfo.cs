namespace Bit.Butil;

/// <summary>
/// One result from <see cref="ContactPicker.Select"/>. All collections are arrays so the
/// shape is friendly to common UI consumption.
/// </summary>
public class ContactInfo
{
    public string[] Name { get; set; } = [];
    public string[] Email { get; set; } = [];
    public string[] Tel { get; set; } = [];

    /// <summary>Postal addresses serialized as plain strings.</summary>
    public string[] Address { get; set; } = [];

    /// <summary>
    /// Avatar images as self-contained <c>data:</c> URLs (base64), if exposed by the platform.
    /// These are inline payloads with no lifetime to manage - unlike object URLs they don't need
    /// to be revoked.
    /// </summary>
    public string[] Icon { get; set; } = [];
}
