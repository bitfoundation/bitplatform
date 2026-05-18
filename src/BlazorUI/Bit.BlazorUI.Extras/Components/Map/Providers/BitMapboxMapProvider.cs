namespace Bit.BlazorUI;

/// <summary>
/// Mapbox GL JS provider for <see cref="BitMap{TMapProvider}"/>. Loads Mapbox GL JS from
/// the official Mapbox CDN. Requires a Mapbox access token for <c>mapbox://</c> styles.
/// </summary>
public sealed class BitMapboxMapProvider : BitMapProviderBase
{
    /// <summary>Mapbox public access token (required for <c>mapbox://</c> styles and Mapbox-hosted tiles).</summary>
    public string AccessToken { get; set; } = "";

    /// <summary>Mapbox style URL or <c>mapbox://</c> shortcut.</summary>
    public string StyleUrl { get; set; } = "mapbox://styles/mapbox/streets-v12";

    /// <summary>Show the navigation (zoom/compass) control.</summary>
    public bool ShowNavigationControl { get; set; } = true;

    /// <summary>Allow rotating the map by dragging.</summary>
    public bool DragRotate { get; set; } = true;

    /// <inheritdoc />
    public override string Key => "mapbox";

    /// <inheritdoc />
    public override string JsObjectName => "BitMapMapbox";

    /// <inheritdoc />
    public override IReadOnlyList<string> Scripts => ["https://api.mapbox.com/mapbox-gl-js/v3.7.0/mapbox-gl.js"];

    /// <inheritdoc />
    public override IReadOnlyList<string> Stylesheets => ["https://api.mapbox.com/mapbox-gl-js/v3.7.0/mapbox-gl.css"];

    /// <inheritdoc />
    public override object BuildOptionsPayload()
    {
        var common = GetCommonOptions();
        common["accessToken"] = AccessToken;
        common["styleUrl"] = StyleUrl;
        common["showNavigationControl"] = ShowNavigationControl;
        common["dragRotate"] = DragRotate;
        return common;
    }
}
