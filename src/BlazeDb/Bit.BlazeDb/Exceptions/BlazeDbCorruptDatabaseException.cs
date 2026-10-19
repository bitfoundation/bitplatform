namespace Bit.BlazeDb;

/// <summary>Thrown when persisted data fails validation (corrupt snapshot, bad magic, etc.).</summary>
public sealed class BlazeDbCorruptDatabaseException : BlazeDbException
{
    public BlazeDbCorruptDatabaseException(string message) : base(message)
    {
    }

    public BlazeDbCorruptDatabaseException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
