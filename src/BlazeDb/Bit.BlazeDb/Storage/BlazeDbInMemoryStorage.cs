using Bit.BlazeDb.Serialization;

namespace Bit.BlazeDb.Storage;

/// <summary>
/// Storage backend keeping "files" in memory. Useful for tests (including crash simulation:
/// reopen a new BlazeDbDatabase over the same storage instance) and for ephemeral databases that
/// still want the full persistence pipeline.
/// </summary>
public sealed class BlazeDbInMemoryStorage : IBlazeDbEnumerableStorage
{
    private readonly object _lock = new();

    // Each file is a growable buffer, so appending - which the WAL does on every flush - is
    // amortized O(1) rather than a copy of everything written before.
    private readonly Dictionary<string, BlazeDbBufferWriter> _files = new();

    public ValueTask<byte[]?> ReadAsync(string name, CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            return ValueTask.FromResult(_files.TryGetValue(name, out var file) ? file.ToArray() : (byte[]?)null);
        }
    }

    public ValueTask WriteAtomicAsync(string name, ReadOnlyMemory<byte> data, CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            Replace(name, data.Span);
        }
        return default;
    }

    public ValueTask AppendAsync(string name, ReadOnlyMemory<byte> data, CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            if (!_files.TryGetValue(name, out var file))
            {
                _files[name] = file = new BlazeDbBufferWriter();
            }
            file.WriteRaw(data.Span);
        }
        return default;
    }

    public ValueTask DeleteAsync(string name, CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            _files.Remove(name);
        }
        return default;
    }

    public ValueTask<IReadOnlyCollection<string>> ListAsync(CancellationToken cancellationToken = default) =>
        ValueTask.FromResult(FileNames);

    // ---- Test helpers ----

    /// <summary>
    /// A deep copy of every file as it is now. Handy for simulating a crash: keep working against
    /// the copy while the instance that owned the original is torn down, so nothing it does from
    /// then on - its shutdown flush, its background loop - can reach the bytes being reopened.
    /// </summary>
    public BlazeDbInMemoryStorage Clone()
    {
        var copy = new BlazeDbInMemoryStorage();
        lock (_lock)
        {
            foreach (var (name, file) in _files)
            {
                copy.Replace(name, file.WrittenSpan);
            }
        }
        return copy;
    }

    public IReadOnlyCollection<string> FileNames
    {
        get
        {
            lock (_lock)
            {
                return _files.Keys.ToArray();
            }
        }
    }

    public byte[]? GetFile(string name)
    {
        lock (_lock)
        {
            return _files.TryGetValue(name, out var file) ? file.ToArray() : null;
        }
    }

    public void SetFile(string name, byte[] data)
    {
        lock (_lock)
        {
            Replace(name, data);
        }
    }

    private void Replace(string name, ReadOnlySpan<byte> data)
    {
        var file = new BlazeDbBufferWriter(Math.Max(16, data.Length));
        file.WriteRaw(data);
        _files[name] = file;
    }
}
