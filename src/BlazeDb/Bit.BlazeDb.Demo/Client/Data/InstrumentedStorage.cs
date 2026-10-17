using System.Diagnostics;
using Bit.BlazeDb.Storage;

namespace Bit.BlazeDb.Demo.Client.Data;

public sealed record StorageOp(int Seq, string Operation, string File, int Bytes, double Ms, DateTime At);

/// <summary>
/// A decorator over any <see cref="IBlazeDbStorage"/> that records every call the engine
/// makes. This is the whole storage contract - four methods, plus the optional quota
/// estimate that is passed straight through - so writing one is
/// all it takes to put BlazeDb on a new backend.
/// </summary>
public sealed class InstrumentedStorage(IBlazeDbStorage inner, int capacity = 500) : IBlazeDbQuotaAwareStorage
{
    private readonly List<StorageOp> _ops = [];
    private int _seq;

    public IBlazeDbStorage Inner { get; } = inner;

    /// <summary>Newest first.</summary>
    public IReadOnlyList<StorageOp> Ops => _ops;

    public long BytesWritten { get; private set; }

    public long BytesRead { get; private set; }

    public int CallCount => _seq;

    public event Action? Changed;

    public void Clear()
    {
        _ops.Clear();
        _seq = 0;
        BytesWritten = 0;
        BytesRead = 0;
        Changed?.Invoke();
    }

    public async ValueTask<byte[]?> ReadAsync(string name, CancellationToken cancellationToken = default)
    {
        var sw = Stopwatch.StartNew();
        var result = await Inner.ReadAsync(name, cancellationToken);
        BytesRead += result?.Length ?? 0;
        Record("ReadAsync", name, result?.Length ?? 0, sw);
        return result;
    }

    public async ValueTask WriteAtomicAsync(string name, ReadOnlyMemory<byte> data, CancellationToken cancellationToken = default)
    {
        var sw = Stopwatch.StartNew();
        await Inner.WriteAtomicAsync(name, data, cancellationToken);
        BytesWritten += data.Length;
        Record("WriteAtomicAsync", name, data.Length, sw);
    }

    public async ValueTask AppendAsync(string name, ReadOnlyMemory<byte> data, CancellationToken cancellationToken = default)
    {
        var sw = Stopwatch.StartNew();
        await Inner.AppendAsync(name, data, cancellationToken);
        BytesWritten += data.Length;
        Record("AppendAsync", name, data.Length, sw);
    }

    public async ValueTask DeleteAsync(string name, CancellationToken cancellationToken = default)
    {
        var sw = Stopwatch.StartNew();
        await Inner.DeleteAsync(name, cancellationToken);
        Record("DeleteAsync", name, 0, sw);
    }

    /// <summary>The origin's estimate when the wrapped backend can report one; not traced, since the engine treats it as advisory.</summary>
    public ValueTask<BlazeDbStorageQuota?> GetQuotaAsync(CancellationToken cancellationToken = default) =>
        Inner is IBlazeDbQuotaAwareStorage quotaAware ? quotaAware.GetQuotaAsync(cancellationToken) : default;

    private void Record(string operation, string name, int bytes, Stopwatch sw)
    {
        _ops.Insert(0, new StorageOp(++_seq, operation, name, bytes, sw.Elapsed.TotalMilliseconds, DateTime.Now));
        if (_ops.Count > capacity)
        {
            _ops.RemoveRange(capacity, _ops.Count - capacity);
        }
        Changed?.Invoke();
    }
}
