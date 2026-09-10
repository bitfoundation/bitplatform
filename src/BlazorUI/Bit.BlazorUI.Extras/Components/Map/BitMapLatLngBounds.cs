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

    /// <summary>The whole world - the widest box every backend accepts.</summary>
    public static BitMapLatLngBounds World { get; } = new(new(-90, -180), new(90, 180));

    /// <summary>North-west corner of the bounding box.</summary>
    public BitMapLatLng NorthWest => new(NorthEast.Latitude, SouthWest.Longitude);

    /// <summary>South-east corner of the bounding box.</summary>
    public BitMapLatLng SouthEast => new(SouthWest.Latitude, NorthEast.Longitude);

    /// <summary>
    /// Centre of the bounding box. A box that crosses the antimeridian is measured the short way
    /// round, so its centre lands inside the box rather than on the far side of the globe.
    /// </summary>
    public BitMapLatLng Center
    {
        get
        {
            var lat = (SouthWest.Latitude + NorthEast.Latitude) / 2;
            var lng = CrossesAntimeridian
                ? BitMapLatLng.WrapLongitude((SouthWest.Longitude + NorthEast.Longitude + 360) / 2)
                : (SouthWest.Longitude + NorthEast.Longitude) / 2;
            return new BitMapLatLng(lat, lng);
        }
    }

    /// <summary>
    /// Whether the box wraps past +/-180 degrees - i.e. its west edge is east of its east edge,
    /// which is how a box spanning the Pacific is expressed.
    /// </summary>
    public bool CrossesAntimeridian => SouthWest.Longitude > NorthEast.Longitude;

    /// <summary>Height of the box in degrees of latitude.</summary>
    public double LatitudeSpan => NorthEast.Latitude - SouthWest.Latitude;

    /// <summary>
    /// Width of the box in degrees of longitude, measured the way the box is drawn - so a box
    /// that crosses the antimeridian reports the span it actually covers, not its complement.
    /// </summary>
    public double LongitudeSpan => CrossesAntimeridian
        ? (180 - SouthWest.Longitude) + (NorthEast.Longitude + 180)
        : NorthEast.Longitude - SouthWest.Longitude;

    /// <summary>Whether the box encloses a single point rather than an area.</summary>
    public bool IsPoint => LatitudeSpan == 0 && LongitudeSpan == 0;

    /// <summary>Whether a coordinate falls inside the box (edges included).</summary>
    public bool Contains(BitMapLatLng point)
    {
        if (point.Latitude < SouthWest.Latitude || point.Latitude > NorthEast.Latitude) return false;

        return CrossesAntimeridian
            // Two runs joined at the antimeridian, so the accepted longitudes are the union of
            // them rather than the span between the two edges.
            ? point.Longitude >= SouthWest.Longitude || point.Longitude <= NorthEast.Longitude
            : point.Longitude >= SouthWest.Longitude && point.Longitude <= NorthEast.Longitude;
    }

    /// <summary>Whether another box falls entirely inside this one (edges included).</summary>
    public bool Contains(BitMapLatLngBounds other)
        => Contains(other.SouthWest) && Contains(other.NorthEast)
        && (CrossesAntimeridian || other.CrossesAntimeridian is false);

    /// <summary>Whether the two boxes overlap at all (touching edges count).</summary>
    public bool Intersects(BitMapLatLngBounds other)
    {
        if (SouthWest.Latitude > other.NorthEast.Latitude || NorthEast.Latitude < other.SouthWest.Latitude)
        {
            return false;
        }

        return OverlapsLongitudesOf(other) || other.OverlapsLongitudesOf(this);
    }

    /// <summary>
    /// The smallest box containing this one and the given coordinate.
    /// <para>
    /// A box that crosses the antimeridian keeps its crossing form: it is widened on whichever
    /// side is nearer to the point, so it never silently flips into the complementary box that
    /// wraps the long way round the globe.
    /// </para>
    /// </summary>
    public BitMapLatLngBounds Extend(BitMapLatLng point)
    {
        if (Contains(point)) return this;

        var south = Math.Min(SouthWest.Latitude, point.Latitude);
        var north = Math.Max(NorthEast.Latitude, point.Latitude);

        var west = SouthWest.Longitude;
        var east = NorthEast.Longitude;

        if (CrossesAntimeridian)
        {
            if (AngularGap(point.Longitude, west) <= AngularGap(east, point.Longitude))
            {
                west = point.Longitude;
            }
            else
            {
                east = point.Longitude;
            }
        }
        else
        {
            west = Math.Min(west, point.Longitude);
            east = Math.Max(east, point.Longitude);
        }

        return new BitMapLatLngBounds(new(south, west), new(north, east));
    }

    /// <summary>The smallest box containing both this one and <paramref name="other"/>.</summary>
    public BitMapLatLngBounds Extend(BitMapLatLngBounds other)
        => Extend(other.SouthWest).Extend(other.NorthEast);

    /// <summary>
    /// The box grown - or, with a negative ratio, shrunk - by a fraction of its own size on every
    /// side. This is the usual way to leave breathing room around what a
    /// <see cref="BitMap{TMapProvider}.FitBounds"/> call is framing, in map units rather than in
    /// pixels. Latitudes are clamped to the poles, and a pad wide enough to wrap the box onto
    /// itself yields the full longitude range instead.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the ratio is not finite.</exception>
    public BitMapLatLngBounds Pad(double bufferRatio)
    {
        BitMapValidation.ValidateFinite(bufferRatio, nameof(bufferRatio));

        var latPad = LatitudeSpan * bufferRatio;
        var lngPad = LongitudeSpan * bufferRatio;
        var south = Math.Clamp(SouthWest.Latitude - latPad, -90, 90);
        var north = Math.Clamp(NorthEast.Latitude + latPad, -90, 90);

        if (LongitudeSpan + (2 * lngPad) >= 360)
        {
            return new BitMapLatLngBounds(new(south, -180), new(north, 180));
        }

        return new BitMapLatLngBounds(
            new(south, BitMapLatLng.WrapLongitude(SouthWest.Longitude - lngPad)),
            new(north, BitMapLatLng.WrapLongitude(NorthEast.Longitude + lngPad)));
    }

    /// <summary>
    /// The smallest box containing every one of the given coordinates.
    /// <para>
    /// This is what a "fit the view to my data" call is built from, and it is deliberately the
    /// naive box: longitudes are taken at face value, so a set spanning the Pacific produces the
    /// wide box through Greenwich rather than the narrow one across the antimeridian.
    /// </para>
    /// </summary>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="coordinates"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="coordinates"/> is empty.</exception>
    public static BitMapLatLngBounds FromCoordinates(IEnumerable<BitMapLatLng> coordinates)
    {
        ArgumentNullException.ThrowIfNull(coordinates);

        double south = 0, west = 0, north = 0, east = 0;
        var any = false;
        foreach (var c in coordinates)
        {
            if (any is false)
            {
                south = north = c.Latitude;
                west = east = c.Longitude;
                any = true;
                continue;
            }

            if (c.Latitude < south) south = c.Latitude;
            if (c.Latitude > north) north = c.Latitude;
            if (c.Longitude < west) west = c.Longitude;
            if (c.Longitude > east) east = c.Longitude;
        }

        if (any is false)
        {
            throw new ArgumentException(
                $"{nameof(BitMapLatLngBounds)}.{nameof(FromCoordinates)} needs at least one coordinate.",
                nameof(coordinates));
        }

        return new BitMapLatLngBounds(new(south, west), new(north, east));
    }

    /// <summary>The smallest box containing every one of the given markers.</summary>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="markers"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="markers"/> is empty.</exception>
    public static BitMapLatLngBounds FromMarkers(IEnumerable<BitMapMarker> markers)
    {
        ArgumentNullException.ThrowIfNull(markers);
        return FromCoordinates(markers.Select(m => m.Position));
    }

    /// <summary>Whether this box's longitude run covers either edge of <paramref name="other"/>.</summary>
    private bool OverlapsLongitudesOf(BitMapLatLngBounds other)
    {
        // Compared at a latitude this box actually spans, so the check is purely about longitude -
        // the caller has already established that the latitude runs overlap.
        var lat = Math.Clamp(other.SouthWest.Latitude, SouthWest.Latitude, NorthEast.Latitude);
        return Contains(new BitMapLatLng(lat, other.SouthWest.Longitude))
            || Contains(new BitMapLatLng(lat, other.NorthEast.Longitude));
    }

    /// <summary>Degrees travelled going east from <paramref name="from"/> to <paramref name="to"/>.</summary>
    private static double AngularGap(double from, double to)
    {
        var gap = to - from;
        while (gap < 0) gap += 360;
        return gap;
    }
}
