namespace Bit.BlazorUI;

/// <summary>
/// Contract every <see cref="BitMap{TMapProvider}"/> backend implements.
/// Each provider is a thin description of how to load its vendor library and
/// build the options payload that gets handed to the JavaScript engine.
/// </summary>
public interface IBitMapProvider
{
    /// <summary>
    /// Stable provider key (e.g. <c>leaflet</c>, <c>maplibre</c>, <c>cesium</c>).
    /// </summary>
    string Key { get; }

    /// <summary>
    /// JavaScript object name on <c>BitBlazorUI</c> that exposes the provider methods
    /// (<c>init</c>, <c>dispose</c>, <c>addMarker</c>, …). For example <c>BitMapLeaflet</c>.
    /// </summary>
    string JsObjectName { get; }

    /// <summary>
    /// External script URLs to load before the provider can be initialized. Returns an empty list
    /// when the provider ships no external scripts (e.g. providers that bundle everything inline).
    /// </summary>
    IReadOnlyList<string> Scripts { get; }

    /// <summary>
    /// External stylesheet URLs to load before the provider can be initialized.
    /// </summary>
    IReadOnlyList<string> Stylesheets { get; }

    /// <summary>
    /// Whether the scripts must be loaded as <c>type="module"</c>. Most providers ship UMD scripts
    /// (return false); providers that load ESM entries (e.g. ArcGIS, OpenLayers) return true.
    /// </summary>
    bool ScriptsAreModules { get; }

    /// <summary>
    /// Build a JS-friendly anonymous options payload that the provider's <c>init</c> /
    /// <c>sync</c> functions consume.
    /// </summary>
    object BuildOptionsPayload();
}
