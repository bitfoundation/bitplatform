using System.Runtime.InteropServices.JavaScript;
using System.Runtime.Versioning;
using Bit.BlazeDb.Storage;

namespace Bit.BlazeDb.Browser;

/// <summary>
/// OPFS (Origin Private File System) storage backend for Blazor WebAssembly. Uses the async
/// OPFS API from the main thread - good enough for BlazeDb's background group-commit flushes -
/// and the Web Locks API to guarantee a single writer tab per database.
/// </summary>
[SupportedOSPlatform("browser")]
public sealed class BlazeDbOpfsStorage : IBlazeDbQuotaAwareStorage, IDisposable
{
    private readonly JSObject _directory;
    private readonly string? _lockName;
    private bool _disposed;

    private BlazeDbOpfsStorage(JSObject directory, string? lockName)
    {
        _directory = directory;
        _lockName = lockName;
    }

    /// <summary>True for an instance from <see cref="CreateReadOnlyAsync"/>, which refuses every write.</summary>
    public bool IsReadOnly => _lockName is null;

    /// <summary>
    /// Opens (or creates) the OPFS directory for <paramref name="databaseName"/> and acquires
    /// its cross-tab write lock. Throws <see cref="BlazeDbDatabaseLockedException"/> when another tab
    /// already owns the database. <paramref name="moduleUrl"/> overrides where the JS module is
    /// loaded from; by default it is resolved relative to the document base.
    /// <paramref name="allowWithoutWebLocks"/> lets the open go ahead on a browser without the Web
    /// Locks API, where no election is possible; the application then has to guarantee a single tab.
    /// </summary>
    public static async ValueTask<BlazeDbOpfsStorage> CreateAsync(
        string databaseName, string? moduleUrl = null, bool allowWithoutWebLocks = false)
    {
        await BlazeDbOpfsInterop.EnsureModuleAsync(moduleUrl).ConfigureAwait(false);

        var lockName = await BlazeDbWriterLock.AcquireAsync(databaseName, allowWithoutWebLocks).ConfigureAwait(false);
        try
        {
            var directory = await BlazeDbOpfsInterop.OpenDatabaseDirectory(databaseName).ConfigureAwait(false);
            return new BlazeDbOpfsStorage(directory, lockName);
        }
        catch
        {
            BlazeDbOpfsInterop.ReleaseLock(lockName);
            throw;
        }
    }

    /// <summary>
    /// Opens the same OPFS directory without taking the writer lock, for a replica tab that only
    /// reads. Every write on the returned instance throws, so a replica opened without
    /// <see cref="BlazeDbDatabaseOptions.ReadOnly"/> cannot end up as a second, unelected writer
    /// on the same files. Returns null when the database does not exist yet, so a replica can wait
    /// for the writer instead of creating an empty one.
    /// </summary>
    public static async ValueTask<BlazeDbOpfsStorage?> CreateReadOnlyAsync(string databaseName, string? moduleUrl = null)
    {
        await BlazeDbOpfsInterop.EnsureModuleAsync(moduleUrl).ConfigureAwait(false);
        var directory = await BlazeDbOpfsInterop.OpenExistingDatabaseDirectory(databaseName).ConfigureAwait(false);
        return directory is null ? null : new BlazeDbOpfsStorage(directory, lockName: null);
    }

    /// <summary>OPFS appends by staging the whole file in a swap copy; see <see cref="IBlazeDbQuotaAwareStorage.AppendRewritesWholeFile"/>.</summary>
    public bool AppendRewritesWholeFile => true;

    private void EnsureWritable()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (_lockName is null)
        {
            throw new InvalidOperationException(
                "This storage was opened with CreateReadOnlyAsync and refuses writes. Open the database " +
                "with BlazeDbDatabaseOptions.ReadOnly = true, or use CreateAsync to become the writer tab.");
        }
        BlazeDbWriterLock.EnsureHeld(_lockName);
    }

    public async ValueTask<byte[]?> ReadAsync(string name, CancellationToken cancellationToken = default)
    {
        using var bytes = await BlazeDbOpfsInterop.ReadFile(_directory, name).ConfigureAwait(false);
        if (bytes is null)
        {
            return null;
        }
        var length = bytes.GetPropertyAsInt32("length");
        var result = new byte[length];
        BlazeDbOpfsInterop.CopyBytes(bytes, result);
        return result;
    }

    public async ValueTask WriteAtomicAsync(string name, ReadOnlyMemory<byte> data, CancellationToken cancellationToken = default)
    {
        EnsureWritable();
        await BlazeDbOpfsInterop.WriteAtomic(_directory, name, data.ToArray()).ConfigureAwait(false);
    }

    public async ValueTask AppendAsync(string name, ReadOnlyMemory<byte> data, CancellationToken cancellationToken = default)
    {
        EnsureWritable();
        await BlazeDbOpfsInterop.AppendFile(_directory, name, data.ToArray()).ConfigureAwait(false);
    }

    public async ValueTask DeleteAsync(string name, CancellationToken cancellationToken = default)
    {
        EnsureWritable();
        await BlazeDbOpfsInterop.DeleteFile(_directory, name).ConfigureAwait(false);
    }

    /// <summary>
    /// Usage and allowance for the whole origin, from <c>navigator.storage.estimate()</c>.
    /// Browsers deliberately blur these numbers to limit fingerprinting, so treat them as a
    /// guide rather than an exact byte count.
    /// </summary>
    public async ValueTask<BlazeDbStorageQuota?> GetQuotaAsync(CancellationToken cancellationToken = default)
    {
        using var estimate = await BlazeDbOpfsInterop.StorageEstimate().ConfigureAwait(false);
        return ReadEstimate(estimate);
    }

    /// <summary>Shared with the IndexedDB backend: the estimate covers the origin, not one backend.</summary>
    internal static BlazeDbStorageQuota? ReadEstimate(JSObject? estimate) =>
        estimate is null
            ? null
            : new BlazeDbStorageQuota(
                (long)estimate.GetPropertyAsDouble("usage"),
                (long)estimate.GetPropertyAsDouble("quota"));

    /// <summary>
    /// Asks the browser to mark this origin's storage as persistent, exempting the database from
    /// eviction when the device runs low on space. Returns whether persistence is in effect.
    /// Without it, OPFS data is "best-effort" and the browser may clear it.
    /// </summary>
    public static async ValueTask<bool> RequestPersistenceAsync(string? moduleUrl = null)
    {
        await BlazeDbOpfsInterop.EnsureModuleAsync(moduleUrl).ConfigureAwait(false);
        return await BlazeDbOpfsInterop.RequestPersistence().ConfigureAwait(false);
    }

    /// <summary>Releases the cross-tab write lock and the directory handle.</summary>
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }
        _disposed = true;
        if (_lockName is not null)
        {
            BlazeDbOpfsInterop.ReleaseLock(_lockName);
        }
        _directory.Dispose();
    }
}
