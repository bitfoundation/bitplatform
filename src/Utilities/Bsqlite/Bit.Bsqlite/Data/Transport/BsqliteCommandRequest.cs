namespace Bit.Bsqlite.Data;

/// <summary>A single statement plus its parameters.</summary>
public sealed record BsqliteCommandRequest
{
    /// <summary>The SQL to execute. May contain multiple statements.</summary>
    public required string CommandText { get; init; }

    public IReadOnlyList<BsqliteParameterValue> Parameters { get; init; } = [];

    public BsqliteResultKind ResultKind { get; init; } = BsqliteResultKind.NonQuery;

    /// <summary>
    /// The most rows to return, or <see langword="null"/> for all of them. Zero returns the column
    /// list without stepping the statement, which is what <c>CommandBehavior.SchemaOnly</c> asks for.
    /// </summary>
    public int? RowLimit { get; init; }
}
