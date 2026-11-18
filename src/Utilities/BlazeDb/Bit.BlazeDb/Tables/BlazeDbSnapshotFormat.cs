namespace Bit.BlazeDb;

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
