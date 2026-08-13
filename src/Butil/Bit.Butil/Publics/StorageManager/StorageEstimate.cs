namespace Bit.Butil;

/// <summary>
/// Mirrors <see href="https://developer.mozilla.org/en-US/docs/Web/API/StorageManager/estimate">StorageManager.estimate()</see>.
/// All values are in bytes; null when the runtime can't report them.
/// </summary>
public class StorageEstimate
{
    public long? Quota { get; set; }

    public long? Usage { get; set; }

    /// <summary>
    /// Per-API breakdown of <see cref="Usage"/> - which storage API is holding the bytes. Empty
    /// where the browser doesn't report it (everything except Chromium), so treat an empty list as
    /// "unknown", not "nothing stored".
    /// </summary>
    public StorageUsageDetail[] UsageDetails { get; set; } = [];
}

/// <summary>One entry of <see cref="StorageEstimate.UsageDetails"/>.</summary>
public class StorageUsageDetail
{
    /// <summary>The storage API holding the bytes - e.g. <c>indexedDB</c>, <c>caches</c>, <c>serviceWorkerRegistrations</c>.</summary>
    public string Api { get; set; } = string.Empty;

    /// <summary>Bytes attributed to that API.</summary>
    public long Bytes { get; set; }
}
