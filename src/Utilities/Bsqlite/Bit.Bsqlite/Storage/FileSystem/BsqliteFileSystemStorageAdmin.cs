using Bit.Bsqlite.Interop;
using Microsoft.Data.Sqlite;

namespace Bit.Bsqlite.Storage.FileSystem;

/// <summary>
/// The file system backend's databases as files: what exists, deleting, and copying images in and out.
/// </summary>
/// <remarks>
/// <para>
/// A database is its file plus the journal SQLite may leave next to it, so deleting and importing deal
/// with both - a journal left behind by a crash would otherwise be rolled back into whatever file next
/// takes the name - and exporting first folds a journal or a write-ahead log left next to the file into it,
/// so the image holds every committed write. Listing reports SQLite files only, so a root shared with other
/// files of the app lists its databases and nothing else; an empty file - a database opened but never
/// written - holds nothing, cannot be told from any other empty file, and is not listed.
/// </para>
/// <para>
/// Deleting, importing and exporting need the database closed in every window of the app, as they do in
/// every tab in the browser: while a session in this process has the file open they throw rather than pull
/// the file out from under it.
/// </para>
/// </remarks>
internal sealed class BsqliteFileSystemStorageAdmin(BsqliteFileSystemStorageProvider provider) : IBsqliteStorageAdmin
{
    private const string StagingSuffix = ".import";

    private const string AsideSuffix = ".aside";

    private static readonly string[] SideFileSuffixes = ["-journal", "-wal", "-shm"];

    private static ReadOnlySpan<byte> SqliteHeader => "SQLite format 3\0"u8;

    public ValueTask<bool> ExistsAsync(string databaseName, CancellationToken cancellationToken = default)
        => ValueTask.FromResult(File.Exists(provider.GetDatabasePath(databaseName)));

    public ValueTask<IReadOnlyList<string>> ListAsync(CancellationToken cancellationToken = default)
    {
        var root = provider.RootDirectory;
        if (!Directory.Exists(root))
        {
            return ValueTask.FromResult<IReadOnlyList<string>>([]);
        }

        var names = Directory
            .EnumerateFiles(root, "*", SearchOption.AllDirectories)
            .Where(path => !SideFileSuffixes.Any(suffix => path.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
                && !path.EndsWith(StagingSuffix, StringComparison.OrdinalIgnoreCase)
                && !path.EndsWith(AsideSuffix, StringComparison.OrdinalIgnoreCase))
            .Where(IsDatabaseFile)
            .Select(path => Path.GetRelativePath(root, path).Replace(Path.DirectorySeparatorChar, '/'))
            .Order(StringComparer.Ordinal)
            .ToArray();

        return ValueTask.FromResult<IReadOnlyList<string>>(names);
    }

    public async ValueTask DeleteAsync(string databaseName, CancellationToken cancellationToken = default)
    {
        var path = provider.GetDatabasePath(databaseName);

        await BsqliteNativeTransport.WhileClosedAsync(path, () =>
        {
            // Nothing to delete where the folder was never created - and File.Delete throws there on Windows.
            if (Directory.Exists(Path.GetDirectoryName(path)))
            {
                // The file and its journal leave the name together or not at all: a journal left behind
                // would be rolled back into whatever file next takes the name.
                DeleteQuietly(MoveAside([path, .. SideFilesOf(path)]));
            }

            return Task.CompletedTask;
        }, cancellationToken).ConfigureAwait(false);
    }

    public async ValueTask<byte[]> ExportAsync(string databaseName, CancellationToken cancellationToken = default)
    {
        var path = provider.GetDatabasePath(databaseName);

        var image = Array.Empty<byte>();
        await BsqliteNativeTransport.WhileClosedAsync(path, async () =>
        {
            if (!File.Exists(path))
            {
                throw new FileNotFoundException(
                    $"The file system backend holds no database named '{databaseName}' ({path}).",
                    path);
            }

            await Task.Run(() => Settle(path), cancellationToken).ConfigureAwait(false);

            // Shared for reading and writing like SQLite's own handles, so another process that has the file
            // open does not make the read fail.
            await using var stream = new FileStream(
                path,
                FileMode.Open,
                FileAccess.Read,
                FileShare.ReadWrite | FileShare.Delete,
                bufferSize: 4096,
                useAsync: true);
            image = new byte[stream.Length];
            await stream.ReadExactlyAsync(image, cancellationToken).ConfigureAwait(false);
        }, cancellationToken).ConfigureAwait(false);

        return image;
    }

    public async ValueTask ImportAsync(
        string databaseName,
        ReadOnlyMemory<byte> contents,
        CancellationToken cancellationToken = default)
    {
        var path = provider.GetDatabasePath(databaseName);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);

        // Written beside the target and moved over it, so an interrupted import never leaves half a
        // database under the name.
        var staging = $"{path}.{Guid.NewGuid():N}{StagingSuffix}";
        try
        {
            await using (var stream = new FileStream(staging, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            {
                await stream.WriteAsync(contents, cancellationToken).ConfigureAwait(false);
            }

            await BsqliteNativeTransport.WhileClosedAsync(path, () =>
            {
                // The old file's journal and log are moved aside first, and put back if the new file cannot
                // take the name: deleted up front, a failed move would leave the old file without the
                // committed writes its log still held.
                var aside = MoveAside(SideFilesOf(path));
                try
                {
                    File.Move(staging, path, overwrite: true);
                }
                catch
                {
                    Restore(aside);
                    throw;
                }

                DeleteQuietly(aside);
                return Task.CompletedTask;
            }, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            File.Delete(staging);
        }
    }

    private static IEnumerable<string> SideFilesOf(string path) => SideFileSuffixes.Select(suffix => path + suffix);

    /// <summary>
    /// Renames each of <paramref name="paths"/> that exists to a name of its own beside it - all of them, or,
    /// when one cannot be moved, none: those already moved are put back before the failure is thrown.
    /// </summary>
    private static List<(string Original, string Aside)> MoveAside(IEnumerable<string> paths)
    {
        var moved = new List<(string Original, string Aside)>();
        try
        {
            foreach (var original in paths)
            {
                if (!File.Exists(original))
                {
                    continue;
                }

                var aside = $"{original}.{Guid.NewGuid():N}{AsideSuffix}";
                File.Move(original, aside);
                moved.Add((original, aside));
            }
        }
        catch
        {
            Restore(moved);
            throw;
        }

        return moved;
    }

    private static void Restore(List<(string Original, string Aside)> moved)
    {
        for (var i = moved.Count - 1; i >= 0; i--)
        {
            File.Move(moved[i].Aside, moved[i].Original);
        }
    }

    /// <summary>
    /// Deletes what <see cref="MoveAside"/> moved. The names are free by now, so a file that cannot be deleted
    /// yet harms nothing: listing passes it over, and it holds no name SQLite reads.
    /// </summary>
    private static void DeleteQuietly(List<(string Original, string Aside)> moved)
    {
        foreach (var (_, aside) in moved)
        {
            try
            {
                File.Delete(aside);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
            }
        }
    }

    /// <summary>
    /// Folds what SQLite left next to the file into it: a write-ahead log - the file of a stock EF Core app,
    /// Bit.Besql's on a device, or one Bsqlite could not take out of WAL mode yet - or a journal left by a
    /// crash, rolled back. A file with neither is left untouched, and is exported byte for byte.
    /// </summary>
    /// <exception cref="IOException">The log could not be folded in: another process holds the file.</exception>
    private static void Settle(string path)
    {
        if (!HasContent(path + "-wal") && !HasContent(path + "-journal"))
        {
            return;
        }

        var connectionString = new SqliteConnectionStringBuilder
        {
            DataSource = path,
            Mode = SqliteOpenMode.ReadWrite,
            Pooling = false,
        }.ToString();

        try
        {
            using var connection = new SqliteConnection(connectionString);
            connection.Open();
            if (!BsqliteNativeTransport.TryLeaveWalMode(connection))
            {
                throw new IOException(
                    $"'{path}' has committed writes in its write-ahead log that cannot be folded into the file while "
                    + "another process holds it open, so exporting it now would leave them out. Close it there and retry.");
            }
        }
        catch (SqliteException ex)
        {
            throw new IOException(
                $"What SQLite left next to '{path}' could not be folded into it ({ex.Message}), so exporting the file "
                + "alone would leave committed writes out.",
                ex);
        }
    }

    private static bool HasContent(string path) => new FileInfo(path) is { Exists: true, Length: > 0 };

    /// <summary>A SQLite file starts with its format string.</summary>
    private static bool IsDatabaseFile(string path)
    {
        try
        {
            using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
            Span<byte> header = stackalloc byte[16];
            return stream.ReadAtLeast(header, header.Length, throwOnEndOfStream: false) == header.Length
                && header.SequenceEqual(SqliteHeader);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return false;
        }
    }
}
