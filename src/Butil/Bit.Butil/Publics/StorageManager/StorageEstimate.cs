namespace Bit.Butil;

/// <summary>
/// Mirrors <see href="https://developer.mozilla.org/en-US/docs/Web/API/StorageManager/estimate">StorageManager.estimate()</see>.
/// All values are in bytes; null when the runtime can't report them.
/// </summary>
public class StorageEstimate
{
    /// <summary>How many bytes the origin may use. A conservative figure, deliberately fuzzed.</summary>
    public long? Quota { get; set; }

    /// <summary>How many bytes the origin is using, likewise fuzzed and rounded.</summary>
    public long? Usage { get; set; }

    /// <summary>
    /// Per-API breakdown of <see cref="Usage"/> - which storage API is holding the bytes. Empty
    /// where the browser doesn't report it (everything except Chromium), so treat an empty list as
    /// "unknown", not "nothing stored".
    /// </summary>
    public StorageUsageDetail[] UsageDetails { get; set; } = [];
}
