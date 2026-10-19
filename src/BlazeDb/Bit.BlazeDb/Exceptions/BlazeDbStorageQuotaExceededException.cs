namespace Bit.BlazeDb;

/// <summary>
/// Thrown when persisting would exceed the origin's storage allowance. The in-memory state is
/// unaffected and the un-written log records stay buffered, so the application keeps working and
/// can retry once space has been freed.
/// </summary>
public sealed class BlazeDbStorageQuotaExceededException : BlazeDbException
{
    public BlazeDbStorageQuotaExceededException(Storage.BlazeDbStorageQuota quota, long requiredBytes)
        : base($"Writing {requiredBytes} bytes would exceed the storage quota " +
               $"({quota.UsageBytes} of {quota.QuotaBytes} bytes used). " +
               "The database is intact in memory; free space or reduce the data set and flush again.")
    {
        Quota = quota;
        RequiredBytes = requiredBytes;
    }

    public Storage.BlazeDbStorageQuota Quota { get; }

    public long RequiredBytes { get; }
}
