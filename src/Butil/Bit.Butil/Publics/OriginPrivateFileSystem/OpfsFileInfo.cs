namespace Bit.Butil;

/// <summary>
/// A file's metadata in the origin private file system, read without reading its contents -
/// see <see cref="OriginPrivateFileSystem.GetFileInfo"/>.
/// </summary>
public class OpfsFileInfo
{
    /// <summary>The file name, without any directory in front of it.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>The path this info was read from, normalized (no leading, trailing or doubled slashes).</summary>
    public string Path { get; set; } = string.Empty;

    /// <summary>The file's size in bytes.</summary>
    public long Size { get; set; }

    /// <summary>
    /// The MIME type the browser guessed from the file's name. Empty for an extension it doesn't
    /// recognise, which is the common case in a private file system nobody named for a viewer.
    /// </summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>Last modification time, in milliseconds since the Unix epoch.</summary>
    public long LastModified { get; set; }
}
