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

    /// <summary>
    /// For a write the storage itself refused for lack of space - the browser's <c>QuotaExceededError</c> -
    /// rather than one the engine held back on its own estimate. <paramref name="innerException"/> is what
    /// the storage reported.
    /// </summary>
    public BlazeDbStorageQuotaExceededException(Storage.BlazeDbStorageQuota quota, long requiredBytes, Exception innerException)
        : base($"The storage refused to write {requiredBytes} bytes because the storage quota is exhausted " +
               $"({quota.UsageBytes} of {quota.QuotaBytes} bytes used, as last estimated). " +
               "The database is intact in memory; free space or reduce the data set and flush again.", innerException)
    {
        Quota = quota;
        RequiredBytes = requiredBytes;
    }

    public Storage.BlazeDbStorageQuota Quota { get; }

    public long RequiredBytes { get; }
}
