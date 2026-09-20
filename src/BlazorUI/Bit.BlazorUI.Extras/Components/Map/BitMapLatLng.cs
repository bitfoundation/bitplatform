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

    /// <summary>
    /// Mean radius of the earth in meters (IUGG), used by <see cref="DistanceTo"/> and
    /// <see cref="Offset"/>.
    /// </summary>
    public const double EarthRadiusMeters = 6371008.8;

    /// <summary>
    /// Great-circle distance to another coordinate, in meters.
    /// <para>
    /// Computed with the haversine formula on a spherical earth, which is what every mapping
    /// library uses for this and is accurate to roughly 0.5% - well inside what a map is drawn
    /// at. Reach for a geodesic (WGS84 ellipsoid) library only when you are measuring rather
    /// than displaying.
    /// </para>
    /// </summary>
    public double DistanceTo(BitMapLatLng other)
    {
        var lat1 = _latitude * Math.PI / 180;
        var lat2 = other._latitude * Math.PI / 180;
        var dLat = lat2 - lat1;
        var dLng = (other._longitude - _longitude) * Math.PI / 180;

        var a = (Math.Sin(dLat / 2) * Math.Sin(dLat / 2))
              + (Math.Cos(lat1) * Math.Cos(lat2) * Math.Sin(dLng / 2) * Math.Sin(dLng / 2));
        return 2 * EarthRadiusMeters * Math.Asin(Math.Min(1, Math.Sqrt(a)));
    }

    /// <summary>
    /// The coordinate reached by travelling <paramref name="distanceMeters"/> along
    /// <paramref name="bearingDegrees"/> (0 = north, 90 = east), on the same spherical earth
    /// <see cref="DistanceTo"/> measures on.
    /// <para>
    /// This is what a "within 5 km of here" circle is built from, and what places a label a fixed
    /// distance from a pin regardless of latitude.
    /// </para>
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when either argument is not finite.</exception>
    public BitMapLatLng Offset(double distanceMeters, double bearingDegrees)
    {
        BitMapValidation.ValidateFinite(distanceMeters, nameof(distanceMeters));
        BitMapValidation.ValidateFinite(bearingDegrees, nameof(bearingDegrees));

        var angular = distanceMeters / EarthRadiusMeters;
        var bearing = bearingDegrees * Math.PI / 180;
        var lat1 = _latitude * Math.PI / 180;
        var lng1 = _longitude * Math.PI / 180;

        var lat2 = Math.Asin((Math.Sin(lat1) * Math.Cos(angular))
                           + (Math.Cos(lat1) * Math.Sin(angular) * Math.Cos(bearing)));
        var lng2 = lng1 + Math.Atan2(Math.Sin(bearing) * Math.Sin(angular) * Math.Cos(lat1),
                                     Math.Cos(angular) - (Math.Sin(lat1) * Math.Sin(lat2)));

        // Asin/Atan2 already answer inside the valid ranges, but rounding can land a hair past
        // them - and the constructor rejects that rather than correcting it.
        return new BitMapLatLng(
            Math.Clamp(lat2 * 180 / Math.PI, -90, 90),
            WrapLongitude(lng2 * 180 / Math.PI));
    }

    /// <summary>
    /// Whether another coordinate is the same place to within <paramref name="toleranceDegrees"/>.
    /// <para>
    /// A map reports its centre as a float, so an exact comparison against a value you handed it
    /// almost never holds. Compare with a tolerance instead.
    /// </para>
    /// </summary>
    public bool IsCloseTo(BitMapLatLng other, double toleranceDegrees = 1e-9)
        => Math.Abs(_latitude - other._latitude) <= toleranceDegrees
        && Math.Abs(_longitude - other._longitude) <= toleranceDegrees;

    /// <summary>
    /// A bounding box centred on this coordinate reaching <paramref name="radiusMeters"/> due
    /// north, south, east and west of it - the usual input to a "fit the view to this radius"
    /// call.
    /// <para>
    /// Built from the degree extents rather than from two diagonal offsets, so the box's centre is
    /// exactly this coordinate. Longitudes widen with latitude, which is why a 1 km box is far more
    /// degrees across in Reykjavik than in Nairobi.
    /// </para>
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the radius is negative or not finite.</exception>
    public BitMapLatLngBounds ToBounds(double radiusMeters)
    {
        BitMapValidation.ValidateRadius(radiusMeters, nameof(radiusMeters));

        var latSpan = radiusMeters / EarthRadiusMeters * 180 / Math.PI;
        // cos(lat) collapses to zero at the poles, where every longitude is the same place. Floor
        // it so the division answers "the whole width" instead of infinity.
        var cos = Math.Max(1e-9, Math.Cos(_latitude * Math.PI / 180));
        var lngSpan = Math.Min(180, latSpan / cos);

        return new BitMapLatLngBounds(
            new(Math.Clamp(_latitude - latSpan, -90, 90), WrapLongitude(_longitude - lngSpan)),
            new(Math.Clamp(_latitude + latSpan, -90, 90), WrapLongitude(_longitude + lngSpan)));
    }

    /// <summary>
    /// Normalizes a longitude in degrees into [-180, 180], so a value produced by arithmetic
    /// across the antimeridian is accepted by the constructor.
    /// </summary>
    internal static double WrapLongitude(double longitude)
    {
        if (double.IsFinite(longitude) is false) return longitude;
        if (longitude >= -180 && longitude <= 180) return longitude;

        var wrapped = ((longitude + 180) % 360) - 180;
        if (wrapped < -180) wrapped += 360;
        return wrapped;
    }

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
