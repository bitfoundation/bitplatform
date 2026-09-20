namespace Bit.Butil;

/// <summary>One entry of <see cref="StorageEstimate.UsageDetails"/>.</summary>
public class StorageUsageDetail
{
    /// <summary>The storage API holding the bytes - e.g. <c>indexedDB</c>, <c>caches</c>, <c>serviceWorkerRegistrations</c>.</summary>
    public string Api { get; set; } = string.Empty;

    /// <summary>Bytes attributed to that API.</summary>
    public long Bytes { get; set; }
}
