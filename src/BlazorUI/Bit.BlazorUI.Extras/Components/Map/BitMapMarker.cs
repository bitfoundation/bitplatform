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
    /// Never pass unsanitized user input. The caller is responsible for escaping or sanitizing any
    /// user-provided content before assigning it here. Prefer <see cref="PopupText"/> for plain-text content.
    /// </para>
    /// </summary>
    public string? PopupHtml { get; init; }

    /// <summary>
    /// Plain-text content rendered inside the click popup. The text is safely escaped by the provider
    /// (using <c>setText</c> / <c>textContent</c>) so it is safe to pass user-supplied strings.
    /// When both <see cref="PopupHtml"/> and <see cref="PopupText"/> are set, <see cref="PopupHtml"/> takes precedence.
    /// </summary>
    public string? PopupText { get; init; }

    /// <summary>
    /// Raw HTML content rendered as a tooltip on hover (separate from <see cref="PopupHtml"/> which opens on click).
    /// <para>
    /// <b>Security:</b> This value is injected as raw HTML into the map tooltip.
    /// Never pass unsanitized user input. The caller is responsible for escaping or sanitizing any
    /// user-provided content before assigning it here. Prefer <see cref="TooltipText"/> for plain-text content.
    /// </para>
    /// </summary>
    public string? TooltipHtml { get; init; }

    /// <summary>
    /// Plain-text content rendered as a tooltip on hover. The text is safely escaped by the provider
    /// (using <c>setText</c> / <c>textContent</c>) so it is safe to pass user-supplied strings.
    /// When both <see cref="TooltipHtml"/> and <see cref="TooltipText"/> are set, <see cref="TooltipHtml"/> takes precedence.
    /// </summary>
    public string? TooltipText { get; init; }

    /// <summary>When true, the tooltip stays visible (use sparingly).</summary>
    public bool TooltipPermanent { get; init; }

    /// <summary>Tooltip placement direction: <c>top</c>, <c>bottom</c>, <c>right</c>, <c>left</c>, <c>center</c>, or <c>auto</c>.</summary>
    public string? TooltipDirection { get; init; }

    /// <summary>Native browser tooltip rendered as the <c>title</c> attribute.</summary>
    public string? Title { get; init; }

    /// <summary>When true, the marker can be moved by the user.</summary>
    public bool Draggable { get; init; }

    /// <summary>Optional URL to a custom marker icon image.</summary>
    public string? IconUrl { get; init; }

    /// <summary>Width in pixels of the custom marker icon.</summary>
    public int? IconWidth { get; init; }

    /// <summary>Height in pixels of the custom marker icon.</summary>
    public int? IconHeight { get; init; }

    /// <summary>Stack order offset for overlapping markers.</summary>
    public int ZIndexOffset { get; init; }
}
