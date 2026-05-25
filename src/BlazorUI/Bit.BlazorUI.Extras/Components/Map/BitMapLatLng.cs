namespace Bit.BlazorUI;

/// <summary>
/// Geographic coordinate in WGS84 (EPSG:4326).
/// </summary>
public readonly record struct BitMapLatLng
{
    private readonly double _latitude;
    private readonly double _longitude;

    /// <summary>
    /// Creates a new <see cref="BitMapLatLng"/> with the given coordinates.
    /// </summary>
    /// <param name="latitude">Latitude in degrees. Must be within [-90, 90].</param>
    /// <param name="longitude">Longitude in degrees. Must be within [-180, 180].</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when either value is outside its valid range or NaN.</exception>
    public BitMapLatLng(double latitude, double longitude)
    {
        _latitude = ValidateLatitude(latitude);
        _longitude = ValidateLongitude(longitude);
    }

    /// <summary>
    /// Latitude in degrees. Must be within [-90, 90].
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the value is outside the valid range.</exception>
    public double Latitude
    {
        get => _latitude;
        init => _latitude = ValidateLatitude(value);
    }

    /// <summary>
    /// Longitude in degrees. Must be within [-180, 180].
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the value is outside the valid range.</exception>
    public double Longitude
    {
        get => _longitude;
        init => _longitude = ValidateLongitude(value);
    }

    /// <summary>Shorthand for <see cref="Latitude"/>.</summary>
    public double Lat => Latitude;

    /// <summary>Shorthand for <see cref="Longitude"/>.</summary>
    public double Lng => Longitude;

    private static double ValidateLatitude(double value)
    {
        if (double.IsNaN(value) || value < -90 || value > 90)
        {
            throw new ArgumentOutOfRangeException(
                nameof(Latitude),
                value,
                $"{nameof(BitMapLatLng)}.{nameof(Latitude)} must be a number between -90 and 90 (inclusive).");
        }
        return value;
    }

    private static double ValidateLongitude(double value)
    {
        if (double.IsNaN(value) || value < -180 || value > 180)
        {
            throw new ArgumentOutOfRangeException(
                nameof(Longitude),
                value,
                $"{nameof(BitMapLatLng)}.{nameof(Longitude)} must be a number between -180 and 180 (inclusive).");
        }
        return value;
    }
}
