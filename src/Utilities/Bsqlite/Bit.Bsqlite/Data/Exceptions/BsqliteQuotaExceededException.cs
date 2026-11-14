namespace Bit.Bsqlite.Data;

/// <summary>The origin is out of storage quota, or SQLite reported <c>SQLITE_FULL</c>.</summary>
public sealed class BsqliteQuotaExceededException : BsqliteException
{
    public BsqliteQuotaExceededException(string message, int? sqliteErrorCode = 13)
        : base(message, sqliteErrorCode)
    {
    }

    /// <param name="message">The engine's message.</param>
    /// <param name="sqliteErrorCode">The SQLite result code.</param>
    /// <param name="innerException">The failure this one stands for, such as Microsoft.Data.Sqlite's on a device.</param>
    public BsqliteQuotaExceededException(string message, int? sqliteErrorCode, Exception? innerException)
        : base(message, sqliteErrorCode, innerException)
    {
    }
}
