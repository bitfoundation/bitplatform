namespace Bit.Butil;

/// <summary>
/// Lightweight description of a Blob/File usable across interop. Actual binary contents are
/// fetched through <see cref="FileReader"/>.
/// </summary>
public class BlobInfo
{
    /// <summary>For File objects: the original name. Empty for plain Blobs.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>MIME type (e.g. <c>image/png</c>). Empty when unknown.</summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>Size in bytes.</summary>
    public long Size { get; set; }

    /// <summary>Last-modified time (Unix epoch milliseconds). 0 for plain Blobs.</summary>
    public long LastModified { get; set; }
}
