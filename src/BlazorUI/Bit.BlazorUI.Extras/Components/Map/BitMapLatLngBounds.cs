namespace Bit.BlazorUI;

/// <summary>
/// Bounding box defined by a south-west and a north-east corner.
/// <para>
/// The constructor enforces <c>SouthWest.Latitude &lt;= NorthEast.Latitude</c>.
/// Longitudes may be inverted (i.e. <c>SouthWest.Longitude &gt; NorthEast.Longitude</c>)
/// to express bounding boxes that cross the antimeridian.
/// </para>
/// <para>
/// Because the type is a <c>record struct</c>, <c>with</c> expressions bypass
/// constructor validation. Prefer constructing a fresh instance over <c>with</c>
/// when you need the latitude invariant to be re-checked.
/// </para>
/// </summary>
public readonly record struct BitMapLatLngBounds
{
    /// <summary>Creates a new bounding box from the given corners.</summary>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="southWest"/>'s latitude is greater than
    /// <paramref name="northEast"/>'s latitude.
    /// </exception>
    public BitMapLatLngBounds(BitMapLatLng southWest, BitMapLatLng northEast)
    {
        if (southWest.Latitude > northEast.Latitude)
        {
            throw new ArgumentException(
                $"{nameof(BitMapLatLngBounds)}: SouthWest latitude ({southWest.Latitude}) must be less than or equal to NorthEast latitude ({northEast.Latitude}).",
                nameof(southWest));
        }

        SouthWest = southWest;
        NorthEast = northEast;
    }

    /// <summary>South-west corner of the bounding box.</summary>
    public BitMapLatLng SouthWest { get; init; }

    /// <summary>North-east corner of the bounding box.</summary>
    public BitMapLatLng NorthEast { get; init; }

    /// <summary>Deconstructs into south-west and north-east corners.</summary>
    public void Deconstruct(out BitMapLatLng southWest, out BitMapLatLng northEast)
    {
        southWest = SouthWest;
        northEast = NorthEast;
    }
}
