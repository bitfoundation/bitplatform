namespace Bit.Butil;

/// <summary>
/// How hard the browser tries not to lose a bucket's most recent writes, requested when the bucket
/// is created. See <see href="https://developer.mozilla.org/en-US/docs/Web/API/StorageBucket/durability">StorageBucket.durability</see>.
/// </summary>
public enum StorageBucketDurability
{
    /// <summary>Leave it to the browser, which today means <see cref="Relaxed"/> everywhere.</summary>
    Default,

    /// <summary>
    /// Writes may sit in an OS buffer, so a power loss can lose the last few seconds. The default,
    /// and the faster of the two.
    /// </summary>
    Relaxed,

    /// <summary>
    /// Writes are flushed to disk before they are reported as done - slower, and what a bucket
    /// holding something you can't re-fetch wants.
    /// </summary>
    Strict
}
