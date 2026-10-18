namespace Bit.Bsqlite.Data;

/// <summary>
/// A failure reported by SQLite itself, carrying the result code the engine returned.
/// </summary>
/// <remarks>
/// The code is preserved deliberately: EF Core distinguishes a unique-constraint violation from a busy
/// database by it, and callers write <c>catch</c> blocks around specific codes. Without it, every
/// failure would be an indistinguishable string.
/// </remarks>
public class BsqliteException : Exception
{
    /// <param name="message">The engine's message, passed through unaltered.</param>
    /// <param name="sqliteErrorCode">
    /// The SQLite result code, or <see langword="null"/> when the failure came from the transport rather
    /// than from the engine.
    /// </param>
    public BsqliteException(string message, int? sqliteErrorCode = null)
        : base(message)
        => SqliteErrorCode = sqliteErrorCode;

    /// <summary>The SQLite result code, when the engine supplied one.</summary>
    public int? SqliteErrorCode { get; }
}
