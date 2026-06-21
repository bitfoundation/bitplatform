namespace Bit.Butil;

/// <summary>
/// File payload for <see cref="Navigator.ShareFiles(string?, ShareFile[], string?, string?)"/>.
/// </summary>
public class ShareFile
{
    /// <summary>The file's display name (extension included).</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>MIME type - <c>image/png</c>, <c>application/pdf</c>, etc.</summary>
    public string MimeType { get; set; } = "application/octet-stream";

    /// <summary>Raw file bytes.</summary>
    public byte[] Data { get; set; } = [];
}
