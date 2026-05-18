namespace Bit.BlazorUI;

/// <summary>
/// Declarative marker definition used by <see cref="BitMap"/>.
/// </summary>
public sealed class BitMapMarker
{
    /// <summary>Unique identifier of the marker within the map.</summary>
    public required string Id { get; init; }

    /// <summary>Geographic coordinate of the marker.</summary>
    public required BitMapLatLng Position { get; init; }

    /// <summary>HTML content of the click popup.</summary>
    public string? PopupHtml { get; init; }

    /// <summary>Tooltip on hover (separate from <see cref="PopupHtml"/> which opens on click).</summary>
    public string? TooltipHtml { get; init; }

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
