namespace Bit.BlazorUI;

/// <summary>
/// Declarative marker definition used by <see cref="BitMap{TMapProvider}"/>.
/// </summary>
public sealed class BitMapMarker
{
    /// <summary>Unique identifier of the marker within the map.</summary>
    public required string Id { get; init; }

    /// <summary>Geographic coordinate of the marker.</summary>
    public required BitMapLatLng Position { get; init; }

    /// <summary>
    /// Raw HTML content rendered inside the click popup.
    /// <para>
    /// <b>Security:</b> This value is injected as raw HTML into the map popup (via <c>setHTML</c> / <c>innerHTML</c>).
    /// It is typed as <see cref="MarkupString"/> so the call site is loud about the intent - never construct one
    /// from unsanitized user input. Prefer <see cref="PopupText"/> for plain-text content.
    /// </para>
    /// </summary>
    public MarkupString? PopupHtml { get; init; }

    /// <summary>
    /// Plain-text content rendered inside the click popup. The text is safely escaped by the provider
    /// (using <c>setText</c> / <c>textContent</c>) so it is safe to pass user-supplied strings.
    /// When both <see cref="PopupHtml"/> and <see cref="PopupText"/> are set, <see cref="PopupHtml"/> takes precedence.
    /// </summary>
    public string? PopupText { get; init; }

    /// <summary>
    /// Raw HTML content rendered as a tooltip on hover (separate from <see cref="PopupHtml"/> which opens on click).
    /// <para>
    /// <b>Provider support:</b> tooltips are currently rendered by the Leaflet provider only. Other providers
    /// (MapLibre, Mapbox, OpenLayers, ArcGIS, Azure Maps, Cesium) ignore <see cref="TooltipHtml"/>,
    /// <see cref="TooltipText"/>, <see cref="TooltipPermanent"/>, and <see cref="TooltipDirection"/>.
    /// Use <see cref="Title"/> for a hover label, but note that its rendering varies by provider
    /// (see <see cref="Title"/> for details).
    /// </para>
    /// <para>
    /// <b>Security:</b> This value is injected as raw HTML into the map tooltip and is typed as
    /// <see cref="MarkupString"/> so the call site is loud about the intent - never construct one
    /// from unsanitized user input. Prefer <see cref="TooltipText"/> for plain-text content.
    /// </para>
    /// </summary>
    public MarkupString? TooltipHtml { get; init; }

    /// <summary>
    /// Plain-text content rendered as a tooltip on hover. The text is safely escaped by the provider
    /// (using <c>setText</c> / <c>textContent</c>) so it is safe to pass user-supplied strings.
    /// When both <see cref="TooltipHtml"/> and <see cref="TooltipText"/> are set, <see cref="TooltipHtml"/> takes precedence.
    /// <para>
    /// <b>Provider support:</b> Leaflet only. See <see cref="TooltipHtml"/> for details.
    /// </para>
    /// </summary>
    public string? TooltipText { get; init; }

    /// <summary>When true, the tooltip stays visible (use sparingly). Leaflet only.</summary>
    public bool TooltipPermanent { get; init; }

    /// <summary>Tooltip placement direction. Leaflet only.</summary>
    public BitMapTooltipDirection TooltipDirection { get; init; } = BitMapTooltipDirection.Auto;

    /// <summary>
    /// Hover label for the marker. Rendering varies by provider:
    /// <list type="bullet">
    /// <item><description><b>Leaflet, Mapbox, MapLibre:</b> applied as the DOM <c>title</c> attribute on the marker element (native browser tooltip on hover).</description></item>
    /// <item><description><b>Cesium:</b> rendered as a billboard <c>label</c> drawn next to the marker (always visible, not a hover tooltip).</description></item>
    /// <item><description><b>ArcGIS:</b> used as the title of the popup that opens when the marker is clicked (not a hover tooltip).</description></item>
    /// <item><description><b>OpenLayers, Azure Maps:</b> stored on the marker but not surfaced as a tooltip; effectively ignored.</description></item>
    /// </list>
    /// </summary>
    public string? Title { get; init; }

    /// <summary>When true, the marker can be moved by the user.</summary>
    public bool Draggable { get; init; }

    /// <summary>Optional URL to a custom marker icon image.</summary>
    public string? IconUrl { get; init; }

    /// <summary>Width in pixels of the custom marker icon. Values below 1 are clamped to 1.</summary>
    public int? IconWidth
    {
        get => _iconWidth;
        init => _iconWidth = value is null ? null : Math.Max(1, value.Value);
    }
    private readonly int? _iconWidth;

    /// <summary>Height in pixels of the custom marker icon. Values below 1 are clamped to 1.</summary>
    public int? IconHeight
    {
        get => _iconHeight;
        init => _iconHeight = value is null ? null : Math.Max(1, value.Value);
    }
    private readonly int? _iconHeight;

    /// <summary>
    /// Stack order offset for overlapping markers.
    /// <para>
    /// <b>Provider support:</b> Leaflet only. Other providers (MapLibre, Mapbox, OpenLayers, ArcGIS, Azure Maps, Cesium)
    /// do not expose an equivalent per-marker stacking offset and ignore this value.
    /// </para>
    /// </summary>
    public int ZIndexOffset { get; init; }
}
