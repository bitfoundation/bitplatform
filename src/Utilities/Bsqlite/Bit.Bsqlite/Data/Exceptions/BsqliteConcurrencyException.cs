namespace Bit.Bsqlite.Data;

/// <summary>The database is locked or busy (<c>SQLITE_BUSY</c> / <c>SQLITE_LOCKED</c>).</summary>
public sealed class BsqliteConcurrencyException : BsqliteException
{
    public BsqliteConcurrencyException(string message, int? sqliteErrorCode = 5)
        : base(message, sqliteErrorCode)
    {
    }

    /// <param name="message">The engine's message.</param>
    /// <param name="sqliteErrorCode">The SQLite result code.</param>
    /// <param name="innerException">The failure this one stands for, such as Microsoft.Data.Sqlite's on a device.</param>
    public BsqliteConcurrencyException(string message, int? sqliteErrorCode, Exception? innerException)
        : base(message, sqliteErrorCode, innerException)
    {
    }
}
