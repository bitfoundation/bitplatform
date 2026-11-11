using System.Runtime.InteropServices;

namespace Bit.BlazeDb.Storage;

/// <summary>
/// Makes changes to a directory's entries durable on Unix-like systems.
/// <para>
/// Flushing a file (<c>FileStream.Flush(flushToDisk: true)</c>) makes its contents durable, but not its
/// name: on Linux, Android and macOS a file created, renamed over another or moved into place is only
/// recorded for good once the directory holding it is flushed too. Until then a power loss can undo the
/// rename while keeping later changes - the deletion of the previous generation's files, say, which a
/// checkpoint makes right after replacing the manifest - and leave a manifest naming files that are gone.
/// SQLite flushes the directory for the same reason after creating its journal.
/// </para>
/// <para>
/// Windows records renames in the NTFS journal, and the browser has no directories to flush, so this is a
/// no-op there. Elsewhere it is best effort: a file system that cannot flush a directory - some network
/// and FUSE mounts refuse - or a platform whose C library cannot be loaded leaves the write as durable as
/// the file system makes it, the behavior before this existed, rather than failing a write that succeeded.
/// </para>
/// </summary>
internal static partial class BlazeDbDirectorySync
{
    private static volatile bool _unavailable = OperatingSystem.IsWindows() || OperatingSystem.IsBrowser() || OperatingSystem.IsWasi();

    /// <summary>Flushes <paramref name="directory"/>'s entries to stable storage, where the platform needs and allows it.</summary>
    public static void Flush(string directory)
    {
        if (_unavailable)
        {
            return;
        }
        try
        {
            var fd = Open(directory, OpenReadOnly);
            if (fd < 0)
            {
                return;
            }
            try
            {
                _ = FSync(fd);
            }
            finally
            {
                _ = Close(fd);
            }
        }
        catch (Exception ex) when (ex is DllNotFoundException or EntryPointNotFoundException or MarshalDirectiveException)
        {
            // No C library to call: stop trying rather than pay for the failure on every write.
            _unavailable = true;
        }
    }

    // O_RDONLY is 0 on every Unix-like system; a directory opens for reading without O_DIRECTORY, whose value
    // differs between them.
    private const int OpenReadOnly = 0;

    [LibraryImport("libc", EntryPoint = "open", StringMarshalling = StringMarshalling.Utf8)]
    private static partial int Open(string path, int flags);

    [LibraryImport("libc", EntryPoint = "fsync")]
    private static partial int FSync(int fd);

    [LibraryImport("libc", EntryPoint = "close")]
    private static partial int Close(int fd);
}
