namespace Bit.Butil;

/// <summary>
/// Optional knobs that match
/// <see href="https://developer.mozilla.org/en-US/docs/Web/API/PositionOptions">PositionOptions</see>.
/// </summary>
public class GeolocationOptions
{
    /// <summary>Request the most accurate result possible (may be slower / use more power).</summary>
    public bool EnableHighAccuracy { get; set; }

    /// <summary>Maximum acceptable age of a cached position in milliseconds. 0 means never use a cache.</summary>
    public long MaximumAge { get; set; }

    /// <summary>How long the device may take to return a position before <see cref="GeolocationException"/> is raised.</summary>
    public long Timeout { get; set; } = long.MaxValue;
}
