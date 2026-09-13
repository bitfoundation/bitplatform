namespace Bit.Butil;

/// <summary>
/// The state of a handle's read or write permission, mirroring
/// <see href="https://developer.mozilla.org/en-US/docs/Web/API/FileSystemHandle/queryPermission">FileSystemHandle.queryPermission()</see>.
/// </summary>
public enum FileSystemPermission
{
    /// <summary>
    /// The runtime has no permission model for handles - either the File System Access API is
    /// missing entirely, or this ran during prerender/SSR.
    /// </summary>
    Unsupported,

    /// <summary>Access is available now.</summary>
    Granted,

    /// <summary>
    /// The user would be asked. This is what a handle restored from a previous session reports -
    /// call <see cref="FileSystem.RequestPermission"/> from a user gesture to get it back.
    /// </summary>
    Prompt,

    /// <summary>The user declined.</summary>
    Denied,
}
