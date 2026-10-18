using System.Diagnostics.CodeAnalysis;
using Bit.BlazeDb.Serialization;

namespace Bit.BlazeDb;

/// <summary>
/// A table the persisted data names but the current options do not: its rows are carried as raw
/// key and row bytes, replayed from the log and written back into every snapshot, so a database
/// opened with a smaller model - one context type of several sharing a store, or an application
/// whose table type was removed - neither fails to open nor quietly loses what the table held.
/// The engine cannot query it; a later <see cref="BlazeDbDatabase.EnsureTable"/> with a matching
/// descriptor decodes the bytes into a real table.
/// </summary>
internal sealed class BlazeDbOpaqueTable : IBlazeDbTableInternal
{
    private readonly Dictionary<byte[], byte[]> _rows = new(ByteArrayComparer.Instance);

    public BlazeDbOpaqueTable(string name) => Name = name;

    public string Name { get; }

    public int Count => _rows.Count;

    /// <summary>The raw rows, for decoding into a real table once its descriptor turns up.</summary>
    public IEnumerable<KeyValuePair<byte[], byte[]>> Rows => _rows;

    public void ReplaySet(ReadOnlySpan<byte> keyBytes, ReadOnlySpan<byte> rowBytes) =>
        _rows[keyBytes.ToArray()] = rowBytes.ToArray();

    public void ReplayDelete(ReadOnlySpan<byte> keyBytes) => _rows.Remove(keyBytes.ToArray());

    public void WriteSnapshot(BlazeDbBufferWriter writer, BlazeDbBufferWriter scratch)
    {
        writer.WriteVarUInt((ulong)_rows.Count);
        foreach (var (key, row) in _rows)
        {
            writer.WriteBytes(key);
            writer.WriteBytes(row);
        }
    }

    public void LoadSnapshot(ref BlazeDbBufferReader reader, byte formatVersion)
    {
        if (formatVersion < BlazeDbSnapshotFormat.KeyedRows)
        {
            // A version 1 snapshot stores rows without their keys, and only the descriptor knows how
            // to pull a key out of a row. There is nothing to carry forward safely.
            throw new BlazeDbException(
                $"Persisted data references table '{Name}', which is not registered in BlazeDbDatabaseOptions, " +
                "and the snapshot predates the format that lets unregistered tables be carried along. " +
                "Register the table to open this database.");
        }
        var count = BlazeDbSnapshotFormat.ReadRowCount(ref reader, Name);
        _rows.EnsureCapacity(count);
        for (var i = 0; i < count; i++)
        {
            var key = reader.ReadBytes().ToArray();
            _rows[key] = reader.ReadBytes().ToArray();
        }
    }

    public void Clear() => _rows.Clear();

    private sealed class ByteArrayComparer : IEqualityComparer<byte[]>
    {
        public static readonly ByteArrayComparer Instance = new();

        public bool Equals(byte[]? x, byte[]? y) => x.AsSpan().SequenceEqual(y);

        public int GetHashCode([DisallowNull] byte[] obj)
        {
            var hash = new HashCode();
            hash.AddBytes(obj);
            return hash.ToHashCode();
        }
    }
}

/// <summary>Snapshot format versions and the row-header checks every table shares.</summary>
internal static class BlazeDbSnapshotFormat
{
    /// <summary>Rows only; the key is recomputed from the row by the descriptor.</summary>
    public const byte RowsOnly = 1;

    /// <summary>Each row is preceded by its key bytes, so a table can be carried without its descriptor.</summary>
    public const byte KeyedRows = 2;

    public const byte Current = KeyedRows;

    public static int ReadRowCount(ref BlazeDbBufferReader reader, string tableName)
    {
        var count = checked((int)reader.ReadVarUInt());
        if (count < 0)
        {
            throw new InvalidDataException($"Snapshot of table '{tableName}' declares a negative row count.");
        }
        // Every row costs at least a length prefix, so a count larger than the bytes left cannot be
        // honest. Checking before reserving capacity keeps a corrupt count an error rather than an
        // out-of-memory kill; the loop would have caught it, but only after the allocation.
        if (count > reader.Remaining)
        {
            throw new InvalidDataException(
                $"Snapshot of table '{tableName}' declares {count} rows but only {reader.Remaining} bytes remain.");
        }
        return count;
    }
}
