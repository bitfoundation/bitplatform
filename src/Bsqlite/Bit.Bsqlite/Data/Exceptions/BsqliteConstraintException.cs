namespace Bit.Bsqlite.Data;

/// <summary>
/// A constraint was violated (<c>SQLITE_CONSTRAINT</c>): unique, primary key, foreign key, check,
/// or not-null.
/// </summary>
/// <remarks>
/// Raised as its own type because a duplicate key is the failure applications most often handle
/// deliberately - offering a retry, showing a validation message - and matching it by result code
/// in every catch block is the thing this type saves callers from.
/// </remarks>
public sealed class BsqliteConstraintException : BsqliteException
{
    public BsqliteConstraintException(string message, int? sqliteErrorCode = 19)
        : base(message, sqliteErrorCode)
    {
    }
}
