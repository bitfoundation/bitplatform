namespace Bit.Butil;

/// <summary>
/// The JSON shape of one <c>keyStatuses</c> entry: the key id already hex-encoded, and the status as
/// the hyphenated string the specification uses.
/// </summary>
internal class MediaKeyStatusJsEntry
{
    public string KeyId { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;
}
