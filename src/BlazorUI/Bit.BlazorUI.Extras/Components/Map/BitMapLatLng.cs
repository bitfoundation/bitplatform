namespace Bit.BlazorUI;

/// <summary>
/// Geographic coordinate in WGS84 (EPSG:4326).
/// </summary>
public readonly record struct BitMapLatLng(double Latitude, double Longitude)
{
    /// <summary>Shorthand for <see cref="Latitude"/>.</summary>
    public double Lat => Latitude;

    /// <summary>Shorthand for <see cref="Longitude"/>.</summary>
    public double Lng => Longitude;
}
