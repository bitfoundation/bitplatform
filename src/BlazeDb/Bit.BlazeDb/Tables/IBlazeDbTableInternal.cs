using Bit.BlazeDb.Serialization;

namespace Bit.BlazeDb;

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

    /// <summary>Drops every row and index entry, for reloading a replica from a fresh snapshot.</summary>
    void Clear();
}
