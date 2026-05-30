namespace Bit.BlazorUI;

/// <summary>
/// Leaflet provider for <see cref="BitMap{TMapProvider}"/>. Uses the bundled
/// Leaflet 1.9.4 distribution under <c>_content/Bit.BlazorUI.Extras/leaflet/</c>.
/// No API key required; defaults to OpenStreetMap tiles.
/// </summary>
public sealed class BitLeafletMapProvider : BitMapProviderBase
{
    /// <summary>Tile URL template. Defaults to OpenStreetMap.</summary>
    public string TileUrl { get; set; } = "https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png";

    /// <summary>HTML attribution shown for the base tile layer.</summary>
    public string TileAttribution { get; set; } =
        "&copy; <a href=\"https://www.openstreetmap.org/copyright\">OpenStreetMap</a> contributors";

    /// <summary>Maximum zoom of the base tile source.</summary>
    public int TileMaxZoom { get; set; } = 19;

    /// <summary>Base tile layer opacity (0–1). Non-finite inputs (NaN/±Infinity) default to 0.</summary>
    public double TileOpacity
    {
        get => _tileOpacity;
        set => _tileOpacity = double.IsFinite(value) ? Math.Clamp(value, 0, 1) : 0;
    }
    private double _tileOpacity = 1;

    /// <summary>Show a metric/imperial scale bar control.</summary>
    public bool ShowScaleControl { get; set; }

    /// <summary>When <see cref="ShowScaleControl"/> is true, show imperial alongside metric.</summary>
    public bool ScaleControlImperial { get; set; }

    /// <inheritdoc />
    public override string Key => "leaflet";

    /// <inheritdoc />
    public override string JsObjectName => "BitMapLeaflet";

    /// <inheritdoc />
    public override IReadOnlyList<string> Scripts => ["_content/Bit.BlazorUI.Extras/leaflet/leaflet-1.9.4.js"];

    /// <inheritdoc />
    public override IReadOnlyList<string> Stylesheets => ["_content/Bit.BlazorUI.Extras/leaflet/leaflet-1.9.4.css"];

    /// <inheritdoc />
    public override object BuildOptionsPayload()
    {
        ValidateTileUrl(TileUrl, nameof(TileUrl));
        ValidateTileMaxZoom(TileMaxZoom, nameof(TileMaxZoom));

        var common = GetCommonOptions();
        common["tileUrl"] = TileUrl;
        common["tileAttribution"] = TileAttribution;
        common["tileMaxZoom"] = TileMaxZoom;
        common["tileOpacity"] = TileOpacity;
        common["showScaleControl"] = ShowScaleControl;
        common["scaleControlImperial"] = ScaleControlImperial;
        return common;
    }
}
