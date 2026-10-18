namespace Bit.Bsqlite.Data;

/// <summary>The file image is not a usable SQLite database.</summary>
public sealed class BsqliteCorruptDatabaseException : BsqliteException
{
    public BsqliteCorruptDatabaseException(string message, int? sqliteErrorCode = 11)
        : base(message, sqliteErrorCode)
    {
    }
}
