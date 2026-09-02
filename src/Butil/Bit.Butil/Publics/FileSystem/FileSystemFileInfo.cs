namespace Bit.Butil;

/// <summary>
/// A file's metadata, read from the underlying
/// <see href="https://developer.mozilla.org/en-US/docs/Web/API/File">File</see> without loading its
/// contents.
/// </summary>
public class FileSystemFileInfo
{
    /// <summary>The file name, without any path: the browser never reveals where on disk it came from.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Size in bytes.</summary>
    public long Size { get; set; }

    /// <summary>The MIME type the browser inferred, usually from the extension. Empty when unknown.</summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>Last modified, as milliseconds since the Unix epoch. See <see cref="LastModifiedUtc"/>.</summary>
    public long LastModified { get; set; }

    /// <summary><see cref="LastModified"/> as a <see cref="System.DateTimeOffset"/>.</summary>
    public System.DateTimeOffset LastModifiedUtc => System.DateTimeOffset.FromUnixTimeMilliseconds(LastModified);
}
