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

    /// <summary>The CesiumJS release this provider is written against.</summary>
    public const string CesiumVersion = "1.124";

    /// <summary>Default <see cref="BaseUrl"/>: the official CesiumJS CDN build.</summary>
    public const string DefaultBaseUrl = $"https://cesium.com/downloads/cesiumjs/releases/{CesiumVersion}/Build/Cesium";

    /// <summary>
    /// Base URL of the CesiumJS build to load (the <c>Build/Cesium</c> folder), without a trailing
    /// slash. Defaults to <see cref="DefaultBaseUrl"/>.
    /// <para>
    /// Point this at a copy on your own origin when the page is served cross-origin isolated with
    /// <c>Cross-Origin-Embedder-Policy: require-corp</c>: cesium.com sends neither a
    /// <c>Cross-Origin-Resource-Policy</c> nor an <c>Access-Control-Allow-Origin</c> header, so the
    /// browser blocks the script in both no-cors and CORS mode. Note that CesiumJS resolves its own
    /// workers, <c>.wasm</c> decoders and assets relative to the URL it was loaded from, so the whole
    /// <c>Build/Cesium</c> folder has to be reachable under this base - pointing it at a lone copy of
    /// <c>Cesium.js</c> is not enough.
    /// </para>
    /// </summary>
    public string BaseUrl { get; set; } = DefaultBaseUrl;

    /// <inheritdoc />
    public override IReadOnlyList<string> Scripts => [$"{NormalizedBaseUrl}/Cesium.js"];

    /// <inheritdoc />
    public override IReadOnlyList<string> Stylesheets => [$"{NormalizedBaseUrl}/Widgets/widgets.css"];

    /// <summary>
    /// <see cref="BaseUrl"/> without a trailing slash, falling back to <see cref="DefaultBaseUrl"/>
    /// when it is blank, so the composed URLs never end up with an empty or doubled separator.
    /// </summary>
    private string NormalizedBaseUrl => string.IsNullOrWhiteSpace(BaseUrl)
        ? DefaultBaseUrl
        : BaseUrl.Trim().TrimEnd('/');

    /// <inheritdoc />
    public override object BuildOptionsPayload()
    {
        // Trim once and reuse so leading/trailing whitespace in IonAccessToken
        // doesn't break presence checks or downstream auth headers.
        var trimmedToken = string.IsNullOrWhiteSpace(IonAccessToken) ? null : IonAccessToken.Trim();
        var hasToken = trimmedToken is not null;

        // Validate and normalize ImageryStyle / SceneMode at the .NET layer so invalid
        // values are rejected with a clear message instead of being forwarded to the
        // JS provider where they silently fall back to defaults.
        var imageryStyle = NormalizeImageryStyle(ImageryStyle);
        var sceneMode = NormalizeSceneMode(SceneMode);

        var terrainEnabled = TerrainEnabled && hasToken;
        var isBing = imageryStyle is "bing_aerial" or "bing_labels";
        var effectiveImagery = hasToken || !isBing ? imageryStyle : "osm";

        // Guard against NaN/±Infinity: System.Text.Json (used by the JS interop layer)
        // rejects non-finite numbers by default and would throw mid-serialization.
        if (Altitude.HasValue && double.IsFinite(Altitude.Value) is false)
        {
            throw new ArgumentOutOfRangeException(
                nameof(Altitude),
                Altitude.Value,
                $"{nameof(Altitude)} ({Altitude.Value}) must be a finite number.");
        }

        var common = GetCommonOptions();
        common["altitude"] = Altitude;
        common["imageryStyle"] = effectiveImagery;
        common["ionAccessToken"] = trimmedToken;
        common["sceneMode"] = sceneMode;
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

    private static readonly string[] _allowedImageryStyles = ["osm", "bing_aerial", "bing_labels", "none"];
    private static readonly string[] _allowedSceneModes = ["scene3d", "scene2d", "columbus"];

    private static string NormalizeImageryStyle(string value)
    {
        var trimmed = (value ?? string.Empty).Trim();
        foreach (var allowed in _allowedImageryStyles)
        {
            if (string.Equals(trimmed, allowed, StringComparison.OrdinalIgnoreCase))
            {
                return allowed;
            }
        }
        throw new ArgumentException(
            $"{nameof(ImageryStyle)} must be one of: {string.Join(", ", _allowedImageryStyles)}.",
            nameof(ImageryStyle));
    }

    private static string NormalizeSceneMode(string value)
    {
        var trimmed = (value ?? string.Empty).Trim();
        foreach (var allowed in _allowedSceneModes)
        {
            if (string.Equals(trimmed, allowed, StringComparison.OrdinalIgnoreCase))
            {
                return allowed;
            }
        }
        throw new ArgumentException(
            $"{nameof(SceneMode)} must be one of: {string.Join(", ", _allowedSceneModes)}.",
            nameof(SceneMode));
    }
}
