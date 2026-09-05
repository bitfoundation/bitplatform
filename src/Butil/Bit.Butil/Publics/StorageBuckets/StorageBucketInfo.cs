namespace Bit.Butil;

/// <summary>
/// The state of one <see href="https://developer.mozilla.org/en-US/docs/Web/API/StorageBucket">storage bucket</see>,
/// gathered from its several individual promises into one payload.
/// </summary>
public class StorageBucketInfo
{
    /// <summary>The bucket's name, as passed to <see cref="StorageBuckets.Open"/>.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// True when the bucket is exempt from eviction. Asking for a persisted bucket does not make
    /// one - the browser decides, so read this rather than assuming what you requested.
    /// </summary>
    public bool Persisted { get; set; }

    /// <summary>
    /// <c>"relaxed"</c> or <c>"strict"</c>, or null where the browser doesn't report it. Fixed when
    /// the bucket is created and not changeable afterwards.
    /// </summary>
    public string? Durability { get; set; }

    /// <summary>
    /// When the browser may delete the bucket, in milliseconds since the Unix epoch, or null for no
    /// expiry. See <see cref="StorageBuckets.GetExpires"/>.
    /// </summary>
    public long? Expires { get; set; }

    /// <summary>How many bytes this bucket may use, or null when the browser doesn't report it.</summary>
    public long? Quota { get; set; }

    /// <summary>How many bytes this bucket is using, or null when the browser doesn't report it.</summary>
    public long? Usage { get; set; }
}
