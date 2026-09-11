namespace Bit.BlazorUI;

/// <summary>
/// Declarative marker definition used by <see cref="BitMap{TMapProvider}"/>.
/// <para>
/// A <c>record</c> rather than a class so two definitions compare by value. That is what lets
/// <see cref="BitMap{TMapProvider}.Markers"/> work out which markers actually changed between
/// renders and touch only those, instead of tearing down and rebuilding the whole set.
/// </para>
/// </summary>
public sealed record BitMapMarker
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
    /// <b>Provider support:</b> Leaflet, MapLibre, Mapbox, OpenLayers and Azure Maps render tooltips;
    /// they also open on keyboard focus, since the marker is a tab stop. ArcGIS and Cesium ignore
    /// <see cref="TooltipHtml"/>, <see cref="TooltipText"/> and <see cref="TooltipPermanent"/> - use
    /// <see cref="Title"/> there instead, noting that its rendering varies by provider.
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
    /// <b>Provider support:</b> see <see cref="TooltipHtml"/>.
    /// </para>
    /// </summary>
    public string? TooltipText { get; init; }

    /// <summary>
    /// When true, the tooltip stays visible instead of appearing on hover. Use sparingly: a map of
    /// permanently labelled markers is quickly unreadable, and the labels collide as you zoom out.
    /// <para><b>Provider support:</b> Leaflet, MapLibre, Mapbox and Azure Maps. OpenLayers renders
    /// tooltips on hover only.</para>
    /// </summary>
    public bool TooltipPermanent { get; init; }

    /// <summary>
    /// Tooltip placement direction.
    /// <para><b>Provider support:</b> Leaflet honours every direction; MapLibre and Mapbox honour
    /// all but <see cref="BitMapTooltipDirection.Auto"/>, which they resolve themselves. OpenLayers
    /// and Azure Maps always place the tooltip above the marker.</para>
    /// </summary>
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

    /// <summary>
    /// Accessible name of the marker, written to the marker element's <c>alt</c> /
    /// <c>aria-label</c>. Falls back to <see cref="Title"/> when not set.
    /// <para>
    /// Worth setting on every marker: without one a screen reader announces a row of
    /// indistinguishable "marker" entries, which is the most common accessibility failure in
    /// map UIs. Describe the place, not the pin - "Kyiv office", not "map marker 3".
    /// </para>
    /// <para><b>Provider support:</b> Leaflet, MapLibre and Mapbox (DOM markers). The
    /// canvas-rendered backends (OpenLayers, ArcGIS, Azure Maps, Cesium) have no element to put
    /// it on and ignore it.</para>
    /// </summary>
    public string? Alt { get; init; }

    /// <summary>
    /// Marker opacity (0–1). Non-finite (NaN/±Infinity) inputs default to 1; out-of-range values
    /// are clamped. Useful for dimming markers that are filtered out rather than removing them.
    /// <para><b>Provider support:</b> Leaflet, MapLibre and Mapbox. Ignored elsewhere.</para>
    /// </summary>
    public double Opacity
    {
        get => _opacity;
        init => _opacity = double.IsFinite(value) ? Math.Clamp(value, 0, 1) : 1;
    }
    private readonly double _opacity = 1;

    /// <summary>
    /// Bring the marker to the front of its pane while the pointer is over it, so a pin in a
    /// dense cluster can be picked out without zooming in.
    /// <para><b>Provider support:</b> Leaflet only.</para>
    /// </summary>
    public bool RiseOnHover { get; init; }

    /// <summary>
    /// Whether the marker is a keyboard tab stop that can be activated with Enter or Space,
    /// exactly like a button.
    /// <para>
    /// On by default, because a marker that can only be reached with a mouse is invisible to
    /// keyboard and screen-reader users. Turn it off for markers that are purely decorative, or
    /// when there are so many that tabbing through them all would be worse than not reaching them
    /// - past a few dozen, cluster them instead.
    /// </para>
    /// <para><b>Provider support:</b> Leaflet, MapLibre and Mapbox (DOM markers). The
    /// canvas-rendered backends have no element to focus and ignore it.</para>
    /// </summary>
    public bool Focusable { get; init; } = true;

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
    /// Horizontal offset, in pixels from the icon image's left edge, of the point that sits on the
    /// coordinate. Defaults to the horizontal centre.
    /// <para>
    /// Together with <see cref="IconAnchorY"/> this is what decides whether an icon is a pin
    /// (whose tip marks the place) or a dot (whose centre does). Leave both unset for the pin
    /// behaviour every mapping library defaults to; set them to half the icon's size for a dot.
    /// </para>
    /// <para><b>Provider support:</b> Leaflet, MapLibre and Mapbox. The canvas-rendered backends
    /// centre their symbols and ignore it.</para>
    /// </summary>
    public int? IconAnchorX { get; init; }

    /// <summary>
    /// Vertical offset, in pixels from the icon image's top edge, of the point that sits on the
    /// coordinate. Defaults to the icon's bottom edge, which is where a pin's tip is.
    /// <para><b>Provider support:</b> see <see cref="IconAnchorX"/>.</para>
    /// </summary>
    public int? IconAnchorY { get; init; }

    /// <summary>
    /// Stack order offset for overlapping markers.
    /// <para>
    /// <b>Provider support:</b> Leaflet only. Other providers (MapLibre, Mapbox, OpenLayers, ArcGIS, Azure Maps, Cesium)
    /// do not expose an equivalent per-marker stacking offset and ignore this value.
    /// </para>
    /// </summary>
    public int ZIndexOffset { get; init; }
}
