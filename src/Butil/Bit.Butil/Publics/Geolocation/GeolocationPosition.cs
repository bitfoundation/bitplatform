namespace Bit.Butil;

/// <summary>
/// Mirrors <see href="https://developer.mozilla.org/en-US/docs/Web/API/GeolocationPosition">GeolocationPosition</see>.
/// </summary>
public class GeolocationPosition
{
    /// <summary>The geographic coordinates.</summary>
    public GeolocationCoordinates Coords { get; set; } = new();

    /// <summary>Time at which the position was acquired, as Unix epoch in milliseconds.</summary>
    public long Timestamp { get; set; }
}
