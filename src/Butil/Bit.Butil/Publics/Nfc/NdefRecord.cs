namespace Bit.Butil;

/// <summary>
/// Mirrors <see href="https://developer.mozilla.org/en-US/docs/Web/API/NDEFRecord">NDEFRecord</see>.
/// </summary>
public class NdefRecord
{
    /// <summary>One of <c>"text"</c>, <c>"url"</c>, <c>"absolute-url"</c>, <c>"mime"</c>, <c>"empty"</c>, …</summary>
    public string RecordType { get; set; } = string.Empty;

    /// <summary>MIME type when <see cref="RecordType"/> is <c>"mime"</c>.</summary>
    public string? MediaType { get; set; }

    /// <summary>Application-defined record id.</summary>
    public string? Id { get; set; }

    /// <summary>BCP-47 language tag for text records.</summary>
    public string? Lang { get; set; }

    /// <summary>Encoding for text records, e.g. <c>"utf-8"</c>.</summary>
    public string? Encoding { get; set; }

    /// <summary>Decoded text payload when applicable.</summary>
    public string? Text { get; set; }

    /// <summary>Raw bytes when applicable (mime/etc.).</summary>
    public byte[]? Data { get; set; }
}

/// <summary>One scanned NDEF message.</summary>
public class NdefMessage
{
    public string SerialNumber { get; set; } = string.Empty;
    public NdefRecord[] Records { get; set; } = [];
}
