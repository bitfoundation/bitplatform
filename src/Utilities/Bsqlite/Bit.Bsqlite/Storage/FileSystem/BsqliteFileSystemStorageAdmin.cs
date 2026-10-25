namespace Bit.Bsqlite.Storage.FileSystem;

/// <summary>
/// The file system backend's databases as files: what exists, deleting, and copying images in and out.
/// </summary>
/// <remarks>
/// A database is its file plus the journal SQLite may leave next to it, so deleting and importing deal
/// with both - a journal left behind by a crash would otherwise be rolled back into whatever file next
/// takes the name. Listing reports SQLite files only, so a root shared with other files of the app lists
/// its databases and nothing else.
/// </remarks>
internal sealed class BsqliteFileSystemStorageAdmin(BsqliteFileSystemStorageProvider provider) : IBsqliteStorageAdmin
{
    private const string StagingSuffix = ".import";

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
                && !path.EndsWith(StagingSuffix, StringComparison.OrdinalIgnoreCase))
            .Where(IsDatabaseFile)
            .Select(path => Path.GetRelativePath(root, path).Replace(Path.DirectorySeparatorChar, '/'))
            .Order(StringComparer.Ordinal)
            .ToArray();

        return ValueTask.FromResult<IReadOnlyList<string>>(names);
    }

    public ValueTask DeleteAsync(string databaseName, CancellationToken cancellationToken = default)
    {
        var path = provider.GetDatabasePath(databaseName);

        File.Delete(path);
        DeleteSideFiles(path);

        return ValueTask.CompletedTask;
    }

    public async ValueTask<byte[]> ExportAsync(string databaseName, CancellationToken cancellationToken = default)
    {
        var path = provider.GetDatabasePath(databaseName);

        if (!File.Exists(path))
        {
            throw new FileNotFoundException(
                $"The file system backend holds no database named '{databaseName}' ({path}).",
                path);
        }

        return await File.ReadAllBytesAsync(path, cancellationToken).ConfigureAwait(false);
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

            DeleteSideFiles(path);
            File.Move(staging, path, overwrite: true);
        }
        finally
        {
            File.Delete(staging);
        }
    }

    private static void DeleteSideFiles(string path)
    {
        foreach (var suffix in SideFileSuffixes)
        {
            File.Delete(path + suffix);
        }
    }

    /// <summary>A SQLite file starts with its format string; a database created but never written is empty.</summary>
    private static bool IsDatabaseFile(string path)
    {
        try
        {
            using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
            if (stream.Length == 0)
            {
                return true;
            }

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
