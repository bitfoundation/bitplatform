namespace Bit.Butil;

/// <summary>
/// One child of a directory in the
/// <see href="https://developer.mozilla.org/en-US/docs/Web/API/File_System_API/Origin_private_file_system">origin private file system</see>,
/// as returned by <see cref="OriginPrivateFileSystem.List"/>.
/// </summary>
public class OpfsEntry
{
    /// <summary>The entry's own name, without any directory in front of it.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// The path to pass back to the other <see cref="OriginPrivateFileSystem"/> members: the listed
    /// path and <see cref="Name"/> joined with <c>/</c>.
    /// </summary>
    public string Path { get; set; } = string.Empty;

    /// <summary>
    /// True for a directory, which is what makes this entry something to <see cref="OriginPrivateFileSystem.List"/>
    /// rather than to read.
    /// </summary>
    public bool IsDirectory { get; set; }
}
