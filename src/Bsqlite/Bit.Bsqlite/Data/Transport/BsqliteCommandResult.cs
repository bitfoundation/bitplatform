namespace Bit.Bsqlite.Data;

/// <summary>The outcome of one <see cref="BsqliteCommandRequest"/>.</summary>
public sealed record BsqliteCommandResult
{
    /// <summary>Column names, present when the request asked for a reader.</summary>
    public IReadOnlyList<string> ColumnNames { get; init; } = [];

    /// <summary>
    /// SQLite type per column, as reported by <see cref="BsqliteDataReader.GetDataTypeName"/>.
    /// </summary>
    /// <remarks>
    /// Transports differ in what they can report, and the difference is not hidden because it is not
    /// fixable: the vendored engine is built with <c>SQLITE_OMIT_DECLTYPE</c>, so declared types do not
    /// exist in it at all and the worker reports the storage class of the first row instead. The
    /// in-process transport reports true declared types, so it is the more generous of the two here.
    /// EF Core is unaffected - it materializes from the model, not from this - but a caller reading
    /// <c>GetDataTypeName</c> directly should expect <c>TEXT</c> where a server would say
    /// <c>decimal(18,2)</c>.
    /// </remarks>
    public IReadOnlyList<string?> ColumnTypes { get; init; } = [];

    /// <summary>Rows, each of <see cref="ColumnNames"/> length.</summary>
    public IReadOnlyList<object?[]> Rows { get; init; } = [];

    /// <summary>
    /// Rows changed by the statements that write, summed. Read-only statements do not contribute,
    /// so an <c>INSERT</c> followed by a <c>SELECT</c> reports the insert alone.
    /// </summary>
    public int RecordsAffected { get; init; }

    /// <summary>The rowid of the most recent successful <c>INSERT</c> on the connection.</summary>
    public long LastInsertRowId { get; init; }
}
