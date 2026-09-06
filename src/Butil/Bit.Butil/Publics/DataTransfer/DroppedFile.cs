using System;

namespace Bit.Butil;

/// <summary>One file from a drop.</summary>
/// <param name="Id">
/// The handle to read it by. The file itself is held on the JavaScript side until
/// <see cref="Bit.Butil.DataTransfer.ReleaseFile"/> - which is what lets the bytes be read on your
/// own schedule rather than inside the drop handler.
/// </param>
/// <param name="Name">The file's name as the user's system reports it, with no path - a page never learns where a dropped file lives.</param>
/// <param name="Size">Its size in bytes.</param>
/// <param name="Type">
/// The MIME type the browser guessed, usually from the extension. Empty for a type it does not
/// recognise, and not to be trusted for anything security-relevant: it is a hint from the file name,
/// not an inspection of the contents.
/// </param>
/// <param name="LastModifiedMilliseconds">Last modified, as milliseconds since the Unix epoch. <see cref="LastModified"/> is the usable form.</param>
public record DroppedFile(Guid Id, string Name, long Size, string Type, long LastModifiedMilliseconds)
{
    /// <summary>When the file was last modified, according to the user's own clock.</summary>
    public DateTimeOffset LastModified => DateTimeOffset.FromUnixTimeMilliseconds(LastModifiedMilliseconds);
}
