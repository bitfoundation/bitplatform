namespace Bit.BlazorUI;

/// <summary>
/// CesiumJS 3D globe provider for <see cref="BitMap{TMapProvider}"/>.
/// OSM tiles + smooth-ellipsoid terrain work without a token; a Cesium ion access token
/// unlocks Cesium World Terrain and Bing imagery.
/// </summary>
public sealed class BitCesiumMapProvider : BitMapProviderBase
{
    /// <summary>Camera altitude in meters above the surface (alternative to zoom level).</summary>
    public double? Altitude { get; set; }

    /// <summary>Imagery style: <c>osm</c>, <c>bing_aerial</c>, <c>bing_labels</c>, <c>none</c>.</summary>
    public string ImageryStyle { get; set; } = "osm";

    /// <summary>Cesium ion access token. Optional; required for Cesium World Terrain and Bing imagery.</summary>
    public string? IonAccessToken { get; set; }

    /// <summary>Scene mode: <c>scene3d</c>, <c>scene2d</c>, <c>columbus</c>.</summary>
    public string SceneMode { get; set; } = "scene3d";

    /// <summary>Enable real-world terrain. Requires <see cref="IonAccessToken"/>.</summary>
    public bool TerrainEnabled { get; set; }

    /// <summary>Enable shadow rendering.</summary>
    public bool ShadowsEnabled { get; set; }

    /// <summary>Show the animation timeline widget.</summary>
    public bool AnimationWidget { get; set; }

    /// <summary>Show the timeline widget.</summary>
    public bool TimelineWidget { get; set; }

    /// <summary>Show the base layer picker.</summary>
    public bool BaseLayerPicker { get; set; }

    /// <summary>Show the help button.</summary>
    public bool NavigationHelpButton { get; set; }

    /// <summary>Show the home button.</summary>
    public bool HomeButton { get; set; }

    /// <summary>Show the fullscreen button.</summary>
    public bool FullscreenButton { get; set; }

    /// <summary>Show the geocoder search box.</summary>
    public bool Geocoder { get; set; }

    /// <summary>Show the info-box panel when an entity is clicked.</summary>
    public bool InfoBox { get; set; } = true;

    /// <inheritdoc />
    public override string Key => "cesium";

    /// <inheritdoc />
    public override string JsObjectName => "BitMapCesium";

    /// <inheritdoc />
    public override IReadOnlyList<string> Scripts => ["https://cesium.com/downloads/cesiumjs/releases/1.124/Build/Cesium/Cesium.js"];

    /// <inheritdoc />
    public override IReadOnlyList<string> Stylesheets => ["https://cesium.com/downloads/cesiumjs/releases/1.124/Build/Cesium/Widgets/widgets.css"];

    /// <inheritdoc />
    public override object BuildOptionsPayload()
    {
        // Trim once and reuse so leading/trailing whitespace in IonAccessToken
        // doesn't break presence checks or downstream auth headers.
        var trimmedToken = string.IsNullOrWhiteSpace(IonAccessToken) ? null : IonAccessToken.Trim();
        var hasToken = trimmedToken is not null;
        var terrainEnabled = TerrainEnabled && hasToken;
        var isBing = ImageryStyle?.Equals("bing_aerial", StringComparison.OrdinalIgnoreCase) == true ||
                     ImageryStyle?.Equals("bing_labels", StringComparison.OrdinalIgnoreCase) == true;
        var imageryStyle = hasToken || !isBing ? ImageryStyle : "osm";

        var common = GetCommonOptions();
        common["altitude"] = Altitude;
        common["imageryStyle"] = imageryStyle;
        common["ionAccessToken"] = trimmedToken;
        common["sceneMode"] = SceneMode;
        common["terrainEnabled"] = terrainEnabled;
        common["shadowsEnabled"] = ShadowsEnabled;
        common["animationWidget"] = AnimationWidget;
        common["timelineWidget"] = TimelineWidget;
        common["baseLayerPicker"] = BaseLayerPicker;
        common["navigationHelpButton"] = NavigationHelpButton;
        common["homeButton"] = HomeButton;
        common["fullscreenButton"] = FullscreenButton;
        common["geocoder"] = Geocoder;
        common["infoBox"] = InfoBox;
        return common;
    }
}
