namespace Bit.Bsqlite.Data;

/// <summary>The file image is not a usable SQLite database.</summary>
public sealed class BsqliteCorruptDatabaseException : BsqliteException
{
    public BsqliteCorruptDatabaseException(string message, int? sqliteErrorCode = 11)
        : base(message, sqliteErrorCode)
    {
    }

    /// <param name="message">The engine's message.</param>
    /// <param name="sqliteErrorCode">The SQLite result code.</param>
    /// <param name="innerException">The failure this one stands for, such as Microsoft.Data.Sqlite's on a device.</param>
    public BsqliteCorruptDatabaseException(string message, int? sqliteErrorCode, Exception? innerException)
        : base(message, sqliteErrorCode, innerException)
    {
    }
}
