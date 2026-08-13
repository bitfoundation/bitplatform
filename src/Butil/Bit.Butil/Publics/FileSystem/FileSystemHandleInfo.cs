namespace Bit.Butil;

/// <summary>
/// A reference to a file or directory the user granted access to - the C# side of a
/// <see href="https://developer.mozilla.org/en-US/docs/Web/API/FileSystemHandle">FileSystemHandle</see>.
/// </summary>
/// <remarks>
/// The handle itself is a live browser object with no serializable form, so what crosses into C# is
/// an id that identifies it. Pass this object back to <see cref="FileSystem"/> to act on the file;
/// call <see cref="FileSystem.Release"/> when you're done with it.
/// </remarks>
public class FileSystemHandleInfo
{
    /// <summary>The id the JS side tracks the underlying handle by.</summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>The entry's name, without any directory part.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary><c>"file"</c> or <c>"directory"</c>.</summary>
    public string Kind { get; set; } = string.Empty;

    /// <summary>True when this handle is a directory, i.e. valid for <see cref="FileSystem.ListDirectory"/>.</summary>
    public bool IsDirectory => Kind == "directory";
}
