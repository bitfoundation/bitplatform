namespace Bit.Bsqlite.Data;

/// <summary>The origin is out of storage quota, or SQLite reported <c>SQLITE_FULL</c>.</summary>
public sealed class BsqliteQuotaExceededException : BsqliteException
{
    public BsqliteQuotaExceededException(string message, int? sqliteErrorCode = 13)
        : base(message, sqliteErrorCode)
    {
    }
}
