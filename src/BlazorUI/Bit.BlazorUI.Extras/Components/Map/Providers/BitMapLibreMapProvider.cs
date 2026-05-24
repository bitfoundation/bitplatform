namespace Bit.BlazorUI;

/// <summary>
/// MapLibre GL JS provider for <see cref="BitMap{TMapProvider}"/>. Loads MapLibre 4.7.1 from a CDN.
/// No token required for the default demo style.
/// </summary>
public sealed class BitMapLibreMapProvider : BitMapProviderBase
{
    /// <summary>Map style URL (JSON). Defaults to the MapLibre public demo tiles.</summary>
    public string StyleUrl { get; set; } = "https://demotiles.maplibre.org/style.json";

    /// <summary>Show the navigation (zoom/compass) control.</summary>
    public bool ShowNavigationControl { get; set; } = true;

    /// <summary>Allow rotating the map by dragging with the right mouse button or two-finger gesture.</summary>
    public bool DragRotate { get; set; } = true;

    /// <inheritdoc />
    public override string Key => "maplibre";

    /// <inheritdoc />
    public override string JsObjectName => "BitMapMapLibre";

    /// <inheritdoc />
    public override IReadOnlyList<string> Scripts => ["https://unpkg.com/maplibre-gl@4.7.1/dist/maplibre-gl.js"];

    /// <inheritdoc />
    public override IReadOnlyList<string> Stylesheets => ["https://unpkg.com/maplibre-gl@4.7.1/dist/maplibre-gl.css"];

    /// <inheritdoc />
    public override object BuildOptionsPayload()
    {
        if (string.IsNullOrWhiteSpace(StyleUrl))
        {
            throw new InvalidOperationException(
                $"{nameof(BitMapLibreMapProvider)}.{nameof(StyleUrl)} must be a non-empty URL pointing to a MapLibre style JSON.");
        }

        var common = GetCommonOptions();
        common["styleUrl"] = StyleUrl;
        common["showNavigationControl"] = ShowNavigationControl;
        common["dragRotate"] = DragRotate;
        return common;
    }
}
