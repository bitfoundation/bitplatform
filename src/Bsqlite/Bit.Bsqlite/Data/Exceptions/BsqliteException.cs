using System.Data.Common;

namespace Bit.Bsqlite.Data;

/// <summary>
/// A failure reported by SQLite itself, carrying the result code the engine returned.
/// </summary>
/// <remarks>
/// <para>
/// The code is preserved deliberately: EF Core distinguishes a unique-constraint violation from a busy
/// database by it, and callers write <c>catch</c> blocks around specific codes. Without it, every
/// failure would be an indistinguishable string.
/// </para>
/// <para>
/// A <see cref="DbException"/>, as every ADO.NET provider's error is - Microsoft.Data.Sqlite's
/// included - so code shared between a server and the browser handles both with the same
/// <c>catch (DbException)</c>.
/// </para>
/// </remarks>
public class BsqliteException : DbException
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

    /// <summary>
    /// <see langword="true"/> for <c>SQLITE_BUSY</c> and <c>SQLITE_LOCKED</c> - another connection,
    /// usually another tab, held the lock - which a retry may get past.
    /// </summary>
    public override bool IsTransient => (SqliteErrorCode & 0xFF) is 5 or 6;
}
