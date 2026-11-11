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
/// no-op there. Where the platform cannot flush a directory at all - a file system that does not support it
/// (some network and FUSE mounts refuse with EINVAL or ENOTSUP), a directory that cannot be opened, or a
/// platform whose C library cannot be loaded - it is best effort, and leaves the write as durable as the file
/// system makes it. A flush the file system attempted and failed (EIO, ENOSPC, ...) is different: the entries
/// may never reach the disk, so it is reported, and the storage then refuses to delete anything the entries
/// it could not flush may still depend on (see <see cref="BlazeDbFileStorage"/>).
/// </para>
/// </summary>
internal static partial class BlazeDbDirectorySync
{
    private static volatile bool _unavailable = OperatingSystem.IsWindows() || OperatingSystem.IsBrowser() || OperatingSystem.IsWasi();

    /// <summary>Flushes <paramref name="directory"/>'s entries to stable storage, where the platform needs and allows it.</summary>
    /// <exception cref="IOException">The file system attempted the flush and failed.</exception>
    public static void Flush(string directory)
    {
        if (_unavailable)
        {
            return;
        }
        int fd;
        try
        {
            fd = Open(directory, OpenReadOnly);
        }
        catch (Exception ex) when (ex is DllNotFoundException or EntryPointNotFoundException or MarshalDirectiveException)
        {
            // No C library to call: stop trying rather than pay for the failure on every write.
            _unavailable = true;
            return;
        }
        if (fd < 0)
        {
            return;
        }
        try
        {
            int result;
            int errno;
            do
            {
                result = FSync(fd);
                errno = result < 0 ? Marshal.GetLastPInvokeError() : 0;
            } while (result < 0 && errno == EINTR);

            if (result < 0 && !IsUnsupported(errno))
            {
                throw new IOException(
                    $"Flushing the directory '{directory}' to stable storage failed ({Marshal.GetPInvokeErrorMessage(errno)}, " +
                    $"errno {errno}): files created or replaced in it may not survive a power loss.");
            }
        }
        finally
        {
            _ = Close(fd);
        }
    }

    private const int EINTR = 4;
    private const int EINVAL = 22;

    /// <summary>Whether <paramref name="errno"/> says the file system cannot flush a directory, rather than that it tried and failed.</summary>
    private static bool IsUnsupported(int errno) =>
        errno == EINVAL ||
        // ENOTSUP / EOPNOTSUPP: one value on Linux and Android, two on Apple platforms and FreeBSD.
        (OperatingSystem.IsLinux() || OperatingSystem.IsAndroid()
            ? errno == 95
            : errno is 45 or 102);

    // O_RDONLY is 0 on every Unix-like system; a directory opens for reading without O_DIRECTORY, whose value
    // differs between them.
    private const int OpenReadOnly = 0;

    [LibraryImport("libc", EntryPoint = "open", StringMarshalling = StringMarshalling.Utf8)]
    private static partial int Open(string path, int flags);

    [LibraryImport("libc", EntryPoint = "fsync", SetLastError = true)]
    private static partial int FSync(int fd);

    [LibraryImport("libc", EntryPoint = "close")]
    private static partial int Close(int fd);
}
