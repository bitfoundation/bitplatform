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

    /// <summary>Restrict panning to this geographic rectangle.</summary>
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
}
