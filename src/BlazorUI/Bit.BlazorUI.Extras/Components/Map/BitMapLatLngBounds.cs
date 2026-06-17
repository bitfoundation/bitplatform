namespace Bit.BlazorUI;

/// <summary>
/// Bounding box defined by a south-west and a north-east corner.
/// <para>
/// The constructor enforces <c>SouthWest.Latitude &lt;= NorthEast.Latitude</c>.
/// Longitudes may be inverted (i.e. <c>SouthWest.Longitude &gt; NorthEast.Longitude</c>)
/// to express bounding boxes that cross the antimeridian.
/// </para>
/// <para>
/// Because the type is a <c>record struct</c>, both <c>with</c> expressions and
/// object initializers that assign the <c>init</c> properties bypass constructor
/// validation: in either case the instance is created first (the implicit
/// parameterless struct constructor produces a <c>default</c> value), then the
/// <c>init</c> members are assigned. Only construction via the explicit
/// constructor - <c>new BitMapLatLngBounds(southWest, northEast)</c> - enforces
/// the latitude invariant. Prefer constructing a fresh instance via the
/// constructor over <c>with</c> or object initializers when you need the
/// invariant to be re-checked.
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
