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

    /// <summary>Avatar URLs (object-URL form), if exposed by the platform.</summary>
    public string[] Icon { get; set; } = [];
}
