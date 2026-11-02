using Bit.BlazeDb.Storage;

namespace Bit.BlazeDb.Demo.Client.Data;

/// <summary>
/// In-memory storage with an allowance, reported and enforced the way a browser backend's origin quota
/// is - so the storage page can run the engine into its quota in a second instead of after hundreds of
/// megabytes. A write that would not fit throws, as the browser's QuotaExceededError does; raising
/// <see cref="QuotaBytes"/> plays the user freeing space.
/// </summary>
public sealed class QuotaLimitedStorage(long quotaBytes) : IBlazeDbQuotaAwareStorage
{
    private readonly BlazeDbInMemoryStorage _inner = new();

    public long QuotaBytes { get; set; } = quotaBytes;

    public long UsageBytes => _inner.FileNames.Sum(SizeOf);

    public IReadOnlyCollection<string> FileNames => _inner.FileNames;

    public ValueTask<BlazeDbStorageQuota?> GetQuotaAsync(CancellationToken cancellationToken = default) =>
        ValueTask.FromResult<BlazeDbStorageQuota?>(new BlazeDbStorageQuota(UsageBytes, QuotaBytes));

    public ValueTask<byte[]?> ReadAsync(string name, CancellationToken cancellationToken = default) =>
        _inner.ReadAsync(name, cancellationToken);

    public ValueTask WriteAtomicAsync(string name, ReadOnlyMemory<byte> data, CancellationToken cancellationToken = default)
    {
        EnsureRoom(data.Length - SizeOf(name));
        return _inner.WriteAtomicAsync(name, data, cancellationToken);
    }

    public ValueTask AppendAsync(string name, ReadOnlyMemory<byte> data, CancellationToken cancellationToken = default)
    {
        EnsureRoom(data.Length);
        return _inner.AppendAsync(name, data, cancellationToken);
    }

    public ValueTask DeleteAsync(string name, CancellationToken cancellationToken = default) =>
        _inner.DeleteAsync(name, cancellationToken);

    private long SizeOf(string name) => _inner.GetFile(name)?.Length ?? 0;

    private void EnsureRoom(long growth)
    {
        if (UsageBytes + growth > QuotaBytes)
        {
            throw new IOException($"QuotaExceededError: {growth:N0} more bytes do not fit in the {QuotaBytes:N0}-byte allowance.");
        }
    }
}
