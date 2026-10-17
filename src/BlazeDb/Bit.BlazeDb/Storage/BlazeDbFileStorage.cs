namespace Bit.BlazeDb.Storage;

/// <summary>
/// File-system storage backend for desktop hosts and integration tests. Atomic writes go
/// through a temp file + replace; appends fsync before returning.
/// <para>
/// Opening a directory for writing takes an exclusive lock on it, which is the same single-writer
/// guarantee the browser backends get from the Web Locks API: two processes appending to one log
/// would interleave their records and leave it unreadable. The lock is held until the instance is
/// disposed, so a database that is finished with has to dispose its storage before the directory can
/// be opened again. Use <see cref="OpenReadOnly"/> for a replica that only follows along.
/// </para>
/// </summary>
public sealed class BlazeDbFileStorage : IBlazeDbEnumerableStorage, IDisposable
{
    /// <summary>Holds the writer lock. Not database content, so it is never listed or wiped.</summary>
    private const string LockFileName = "writer.lock";

    private readonly string _directory;
    private readonly FileStream? _writerLock;
    private readonly bool _readOnly;
    private bool _disposed;

    /// <summary>
    /// Opens <paramref name="directory"/> for writing, creating it when missing, and takes its
    /// writer lock.
    /// </summary>
    /// <exception cref="BlazeDbDatabaseLockedException">Another instance or process holds the lock.</exception>
    public BlazeDbFileStorage(string directory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(directory);
        _directory = directory;
        Directory.CreateDirectory(directory);
        try
        {
            _writerLock = new FileStream(
                Path.Combine(directory, LockFileName), FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
        }
        catch (IOException ex)
        {
            throw new BlazeDbDatabaseLockedException(
                $"The database in '{directory}' is already open for writing. BlazeDb allows a single " +
                "writer per database: dispose the storage that holds it, or open this one read-only.", ex);
        }
    }

    private BlazeDbFileStorage(string directory, bool readOnly)
    {
        _directory = directory;
        _readOnly = readOnly;
    }

    /// <summary>
    /// Opens <paramref name="directory"/> without taking the writer lock, for a replica that only
    /// reads. Every write on the returned instance throws, so a replica cannot corrupt the writer's
    /// files by being opened without <see cref="BlazeDbDatabaseOptions.ReadOnly"/>. Returns null when
    /// the directory does not exist yet, so a replica can wait for the writer instead of creating an
    /// empty database of its own.
    /// </summary>
    public static BlazeDbFileStorage? OpenReadOnly(string directory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(directory);
        return Directory.Exists(directory) ? new BlazeDbFileStorage(directory, readOnly: true) : null;
    }

    private string PathOf(string name) => Path.Combine(_directory, name);

    public async ValueTask<byte[]?> ReadAsync(string name, CancellationToken cancellationToken = default)
    {
        var path = PathOf(name);
        if (!File.Exists(path))
        {
            return null;
        }
        try
        {
            // Sharing write and delete access is what lets a replica read while the writer has the
            // log open for an append or is swapping a new manifest in with File.Replace; the default
            // FileShare.Read of File.ReadAllBytes would make those moments a transient IOException.
            await using var stream = new FileStream(
                path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete, bufferSize: 1,
                FileOptions.Asynchronous | FileOptions.SequentialScan);
            var bytes = new byte[stream.Length];
            await stream.ReadExactlyAsync(bytes, cancellationToken).ConfigureAwait(false);
            return bytes;
        }
        catch (FileNotFoundException)
        {
            return null; // Deleted between the existence check and the open.
        }
    }

    public async ValueTask WriteAtomicAsync(string name, ReadOnlyMemory<byte> data, CancellationToken cancellationToken = default)
    {
        EnsureWritable();
        var path = PathOf(name);
        var tempPath = path + ".tmp";
        await using (var stream = new FileStream(tempPath, FileMode.Create, FileAccess.Write, FileShare.None))
        {
            await stream.WriteAsync(data, cancellationToken).ConfigureAwait(false);
            stream.Flush(flushToDisk: true);
        }

        // File.Replace maps to an atomic replace on both Windows (ReplaceFile) and Unix (rename), so
        // a crash leaves the old contents or the new, never a mix. File.Move(overwrite: true) is not
        // that on Windows, which matters most for the manifest: half of one would make the database
        // unopenable even though the snapshot and log beside it are intact.
        if (File.Exists(path))
        {
            File.Replace(tempPath, path, destinationBackupFileName: null);
        }
        else
        {
            File.Move(tempPath, path);
        }
    }

    public async ValueTask AppendAsync(string name, ReadOnlyMemory<byte> data, CancellationToken cancellationToken = default)
    {
        EnsureWritable();
        await using var stream = new FileStream(PathOf(name), FileMode.Append, FileAccess.Write, FileShare.Read);
        await stream.WriteAsync(data, cancellationToken).ConfigureAwait(false);
        stream.Flush(flushToDisk: true);
    }

    public ValueTask DeleteAsync(string name, CancellationToken cancellationToken = default)
    {
        EnsureWritable();
        var path = PathOf(name);
        if (File.Exists(path))
        {
            File.Delete(path);
        }
        return default;
    }

    public ValueTask<IReadOnlyCollection<string>> ListAsync(CancellationToken cancellationToken = default)
    {
        if (!Directory.Exists(_directory))
        {
            return ValueTask.FromResult<IReadOnlyCollection<string>>([]);
        }
        var names = Directory.EnumerateFiles(_directory)
            .Select(Path.GetFileName)
            .Where(name => name is not null && name != LockFileName)
            .ToArray();
        return ValueTask.FromResult<IReadOnlyCollection<string>>(names!);
    }

    private void EnsureWritable()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (_readOnly)
        {
            throw new InvalidOperationException(
                $"The storage for '{_directory}' was opened read-only, so it refuses writes. The writer " +
                "is the instance holding the directory's lock.");
        }
    }

    /// <summary>Releases the directory's writer lock.</summary>
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }
        _disposed = true;
        _writerLock?.Dispose();
    }
}
