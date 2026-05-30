namespace Bit.BlazorUI;

/// <summary>
/// ArcGIS Maps SDK for JavaScript 5.0 provider for <see cref="BitMap{TMapProvider}"/>.
/// The <c>"osm"</c> basemap works without an API key; Esri-hosted basemaps
/// (<c>"streets-vector"</c>, <c>"satellite"</c>, …) require an ArcGIS API key.
/// </summary>
public sealed class BitArcGisMapProvider : BitMapProviderBase
{
    /// <summary>Basemap identifier such as <c>osm</c>, <c>streets-vector</c>, <c>satellite</c>, <c>hybrid</c>.</summary>
    public string BasemapId { get; set; } = "osm";

    /// <summary>ArcGIS Developer API key (required for Esri-hosted basemaps).</summary>
    public string? ApiKey { get; set; }

    /// <summary>Show a scale bar control.</summary>
    public bool ShowScaleControl { get; set; }

    /// <inheritdoc />
    public override string Key => "arcgis";

    /// <inheritdoc />
    public override string JsObjectName => "BitMapArcGis";

    /// <inheritdoc />
    public override IReadOnlyList<string> Scripts => ["https://js.arcgis.com/5.0/"];

    /// <inheritdoc />
    public override IReadOnlyList<string> Stylesheets => ["https://js.arcgis.com/5.0/esri/themes/light/main.css"];

    /// <inheritdoc />
    public override bool ScriptsAreModules => true;

    /// <inheritdoc />
    public override object BuildOptionsPayload()
    {
        // Trim inputs so values like " osm " are treated as "osm" for both
        // validation (Equals against "osm") and the payload sent to JS.
        var basemapId = BasemapId?.Trim();
        var apiKey = ApiKey?.Trim();

        if (string.IsNullOrWhiteSpace(basemapId))
        {
            throw new InvalidOperationException(
                "BitArcGisMapProvider: A BasemapId is required. " +
                "Use 'osm' for the no-key default, or an Esri-hosted basemap id such as 'streets-vector' or 'satellite' along with an ApiKey.");
        }

        if (!string.Equals(basemapId, "osm", StringComparison.OrdinalIgnoreCase) && string.IsNullOrWhiteSpace(apiKey))
        {
            throw new InvalidOperationException(
                $"BitArcGisMapProvider: An ApiKey is required for the '{basemapId}' basemap. " +
                "Only the 'osm' basemap works without an API key.");
        }

        var common = GetCommonOptions();
        common["basemapId"] = basemapId;
        common["apiKey"] = apiKey;
        common["showScaleControl"] = ShowScaleControl;
        return common;
    }
}
