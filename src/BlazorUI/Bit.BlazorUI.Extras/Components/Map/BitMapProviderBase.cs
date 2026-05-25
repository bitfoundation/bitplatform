namespace Bit.BlazorUI;

/// <summary>
/// Common base class for every <see cref="IBitMapProvider"/> implementation.
/// Holds the shared options that are meaningful across all providers
/// (center, zoom, basic interaction toggles).
/// Provider-specific options live on the concrete subclasses.
/// </summary>
public abstract class BitMapProviderBase : IBitMapProvider
{
    /// <summary>Initial geographic center.</summary>
    public BitMapLatLng Center { get; set; } = new(51.505, -0.09);

    /// <summary>Initial zoom level.</summary>
    public double Zoom { get; set; } = 13;

    /// <summary>Minimum allowed zoom level.</summary>
    public int? MinZoom { get; set; }

    /// <summary>Maximum allowed zoom level.</summary>
    public int? MaxZoom { get; set; }

    /// <summary>Show the +/- zoom control buttons.</summary>
    public bool ZoomControl { get; set; } = true;

    /// <summary>Show the attribution control.</summary>
    public bool AttributionControl { get; set; } = true;

    /// <summary>Enable mouse-wheel zoom.</summary>
    public bool ScrollWheelZoom { get; set; } = true;

    /// <summary>Enable double-click zoom.</summary>
    public bool DoubleClickZoom { get; set; } = true;

    /// <summary>Enable shift-drag box zoom (Leaflet/OpenLayers/MapLibre/Mapbox only).</summary>
    public bool BoxZoom { get; set; } = true;

    /// <summary>Enable mouse/touch dragging of the map.</summary>
    public bool Dragging { get; set; } = true;

    /// <summary>Enable +/- and arrow key navigation when the map container is focused.</summary>
    public bool KeyboardNavigation { get; set; } = true;

    /// <summary>Restrict panning to this geographic rectangle (Leaflet/MapLibre/Mapbox only).</summary>
    public BitMapLatLngBounds? MaxBounds { get; set; }

    /// <inheritdoc />
    public abstract string Key { get; }

    /// <inheritdoc />
    public abstract string JsObjectName { get; }

    /// <inheritdoc />
    public virtual IReadOnlyList<string> Scripts => [];

    /// <inheritdoc />
    public virtual IReadOnlyList<string> Stylesheets => [];

    /// <inheritdoc />
    public virtual bool ScriptsAreModules => false;

    /// <summary>
    /// Provider-specific extra fields. Override in a derived class and merge with
    /// <see cref="GetCommonOptions"/> when building the payload.
    /// </summary>
    public abstract object BuildOptionsPayload();

    /// <summary>
    /// Shared options dictionary that every provider's payload should include.
    /// Returns a dictionary so concrete providers can spread/extend it before sending.
    /// </summary>
    protected Dictionary<string, object?> GetCommonOptions()
    {
        if (MinZoom.HasValue && MaxZoom.HasValue && MinZoom.Value > MaxZoom.Value)
        {
            throw new ArgumentException($"MinZoom ({MinZoom.Value}) cannot be greater than MaxZoom ({MaxZoom.Value}).");
        }

        if (MinZoom.HasValue && Zoom < MinZoom.Value)
        {
            throw new ArgumentOutOfRangeException(
                nameof(Zoom),
                Zoom,
                $"Zoom ({Zoom}) must be greater than or equal to MinZoom ({MinZoom.Value}).");
        }

        if (MaxZoom.HasValue && Zoom > MaxZoom.Value)
        {
            throw new ArgumentOutOfRangeException(
                nameof(Zoom),
                Zoom,
                $"Zoom ({Zoom}) must be less than or equal to MaxZoom ({MaxZoom.Value}).");
        }

        return new Dictionary<string, object?>
        {
            ["center"] = new { lat = Center.Latitude, lng = Center.Longitude },
            ["zoom"] = Zoom,
            ["minZoom"] = MinZoom,
            ["maxZoom"] = MaxZoom,
            ["zoomControl"] = ZoomControl,
            ["attributionControl"] = AttributionControl,
            ["scrollWheelZoom"] = ScrollWheelZoom,
            ["doubleClickZoom"] = DoubleClickZoom,
            ["boxZoom"] = BoxZoom,
            ["dragging"] = Dragging,
            ["dragPan"] = Dragging,
            ["keyboardNavigation"] = KeyboardNavigation,
            ["maxBounds"] = MaxBounds is { } b
                ? new
                {
                    southWest = new { lat = b.SouthWest.Latitude, lng = b.SouthWest.Longitude },
                    northEast = new { lat = b.NorthEast.Latitude, lng = b.NorthEast.Longitude },
                }
                : null,
        };
    }

    /// <summary>
    /// Validates an XYZ tile URL template. The value must be non-empty and contain the
    /// <c>{z}</c>, <c>{x}</c> and <c>{y}</c> placeholders that every supported tile
    /// backend expects. Throws <see cref="ArgumentException"/> on failure so configuration
    /// mistakes surface in .NET rather than as opaque JS errors.
    /// </summary>
    protected static void ValidateTileUrl(string? tileUrl, string propertyName)
    {
        if (string.IsNullOrWhiteSpace(tileUrl))
        {
            throw new ArgumentException($"{propertyName} must be a non-empty XYZ tile URL template.", propertyName);
        }

        if (tileUrl.Contains("{z}", StringComparison.Ordinal) is false
            || tileUrl.Contains("{x}", StringComparison.Ordinal) is false
            || tileUrl.Contains("{y}", StringComparison.Ordinal) is false)
        {
            throw new ArgumentException(
                $"{propertyName} ('{tileUrl}') must contain the {{z}}, {{x}} and {{y}} placeholders.",
                propertyName);
        }
    }

    /// <summary>
    /// Validates a tile max-zoom value is within the broadly supported XYZ range (0–30).
    /// </summary>
    protected static void ValidateTileMaxZoom(int tileMaxZoom, string propertyName)
    {
        if (tileMaxZoom is < 0 or > 30)
        {
            throw new ArgumentOutOfRangeException(
                propertyName,
                tileMaxZoom,
                $"{propertyName} must be between 0 and 30.");
        }
    }
}
