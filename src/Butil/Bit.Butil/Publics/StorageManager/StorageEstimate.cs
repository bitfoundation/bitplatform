namespace Bit.Butil;

/// <summary>
/// Mirrors <see href="https://developer.mozilla.org/en-US/docs/Web/API/StorageManager/estimate">StorageManager.estimate()</see>.
/// All values are in bytes; null when the runtime can't report them.
/// </summary>
public class StorageEstimate
{
    public long? Quota { get; set; }
    public long? Usage { get; set; }
}
