namespace Bit.BlazorUI;

/// <summary>
/// OpenLayers provider for <see cref="BitMap{TMapProvider}"/>. Loads OpenLayers from esm.sh
/// (see <see cref="OpenLayersVersion"/>). No API key required; defaults to OpenStreetMap raster tiles.
/// </summary>
public sealed class BitOpenLayersMapProvider : BitMapProviderBase
{
    /// <summary>
    /// OpenLayers version used by this provider. Keep in sync with <c>OL_VER</c> in
    /// <c>wwwroot/openlayers/bit-map-ol-loader.js</c>; both reference the same release.
    /// </summary>
    public const string OpenLayersVersion = "10.5.0";

    /// <summary>Tile URL template (XYZ).</summary>
    public string TileUrl { get; set; } = "https://tile.openstreetmap.org/{z}/{x}/{y}.png";

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

    /// <summary>Show a metric/imperial scale line control.</summary>
    public bool ShowScaleControl { get; set; }

    /// <summary>When <see cref="ShowScaleControl"/> is true, show imperial alongside metric.</summary>
    public bool ScaleControlImperial { get; set; }

    /// <inheritdoc />
    public override string Key => "openlayers";

    /// <inheritdoc />
    public override string JsObjectName => "BitMapOpenLayers";

    /// <inheritdoc />
    public override IReadOnlyList<string> Scripts => ["_content/Bit.BlazorUI.Extras/openlayers/bit-map-ol-loader.js"];

    /// <inheritdoc />
    public override IReadOnlyList<string> Stylesheets => [$"https://cdn.jsdelivr.net/npm/ol@{OpenLayersVersion}/ol.css"];

    /// <inheritdoc />
    public override bool ScriptsAreModules => true;

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
