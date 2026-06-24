namespace Bit.Butil;

/// <summary>
/// Mirrors <see href="https://developer.mozilla.org/en-US/docs/Web/API/GeolocationCoordinates">GeolocationCoordinates</see>.
/// </summary>
public class GeolocationCoordinates
{
    /// <summary>Decimal latitude in degrees.</summary>
    public double Latitude { get; set; }

    /// <summary>Decimal longitude in degrees.</summary>
    public double Longitude { get; set; }

    /// <summary>Accuracy in meters of the latitude/longitude (1-sigma).</summary>
    public double Accuracy { get; set; }

    /// <summary>Altitude in meters relative to the WGS84 reference ellipsoid, or null if unavailable.</summary>
    public double? Altitude { get; set; }

    /// <summary>Accuracy of the altitude in meters, or null if unavailable.</summary>
    public double? AltitudeAccuracy { get; set; }

    /// <summary>Heading in degrees clockwise from true north, or null if unknown / not moving.</summary>
    public double? Heading { get; set; }

    /// <summary>Speed in meters per second, or null if unknown.</summary>
    public double? Speed { get; set; }
}
