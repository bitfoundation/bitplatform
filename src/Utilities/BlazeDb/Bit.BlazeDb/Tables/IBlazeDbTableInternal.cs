using Bit.BlazeDb.Serialization;

namespace Bit.BlazeDb;

/// <summary>
/// The log's changes to one table's persisted rows, for a checkpoint rolled forward from the bytes that were committed
/// (see <c>BlazeDbWalManager.BuildCommittedSnapshotAsync</c>): the last write per key, a row or a delete. Snapshot rows
/// whose key the log did not touch are copied as they are; only keys are decoded, and only to tell equal keys apart.
/// Rows are slices of the buffers they were read from.
/// </summary>
internal abstract class BlazeDbRowMerger
{
    /// <summary>Whether any log record touched the table.</summary>
    public abstract bool HasChanges { get; }

    /// <summary>Records that <paramref name="key"/> now holds <paramref name="row"/>.</summary>
    public abstract void Set(ReadOnlyMemory<byte> key, ReadOnlyMemory<byte> row);

    /// <summary>Records that <paramref name="key"/> was deleted.</summary>
    public abstract void Delete(ReadOnlySpan<byte> key);

    /// <summary>Whether the log wrote or deleted the row a snapshot holds under <paramref name="key"/>, which supersedes it.</summary>
    public abstract bool Supersedes(ReadOnlySpan<byte> key);

    /// <summary>
    /// The key bytes of a row from a snapshot of the first format, which stored rows without keys, written into
    /// <paramref name="scratch"/>; only a registered table can derive them.
    /// </summary>
    public abstract void KeyOfRow(ReadOnlySpan<byte> row, BlazeDbBufferWriter scratch);

    /// <summary>Writes every row the log left in place - key, then row - and returns how many.</summary>
    public abstract int WriteRows(BlazeDbBufferWriter writer, BlazeDbBufferWriter scratch);
}

/// <summary>
/// Non-generic view of a table used by the database for recovery, snapshotting and replay.
/// </summary>
internal interface IBlazeDbTableInternal
{
    string Name { get; }

    int Count { get; }

    void ReplaySet(ReadOnlySpan<byte> keyBytes, ReadOnlySpan<byte> rowBytes);

    void ReplayDelete(ReadOnlySpan<byte> keyBytes);

    void WriteSnapshot(BlazeDbBufferWriter writer, BlazeDbBufferWriter scratch);

    /// <summary>
    /// Reads this table's section of a snapshot. <paramref name="formatVersion"/> is the snapshot's
    /// format (see <see cref="BlazeDbSnapshotFormat"/>): version 1 holds rows only, version 2 a key
    /// before each row.
    /// </summary>
    void LoadSnapshot(ref BlazeDbBufferReader reader, byte formatVersion);

    /// <summary>
    /// A merger for this table's persisted rows, for a checkpoint built from what storage and the log buffer hold
    /// rather than from memory (see <see cref="BlazeDbRowMerger"/>). It uses only the descriptor, never the table's
    /// rows, so it may run outside the write lock.
    /// </summary>
    BlazeDbRowMerger CreateMerger();

    /// <summary>Drops every row and index entry, for reloading a replica from a fresh snapshot.</summary>
    void Clear();

    /// <summary>Adds every disagreement between the table's rows and its indexes to <paramref name="issues"/>.</summary>
    void VerifyIntegrity(List<BlazeDbIntegrityIssue> issues);

    /// <summary>Tells the table's subscribers its rows were replaced wholesale; collects what a handler throws.</summary>
    void RaiseReset(ref List<Exception>? errors);
}
