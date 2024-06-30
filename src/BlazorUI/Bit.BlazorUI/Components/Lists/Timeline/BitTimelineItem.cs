namespace Bit.BlazorUI;

public class BitTimelineItem
{
    /// <summary>
    /// The custom CSS classes of the item.
    /// </summary>
    public string? Class { get; set; }

    /// <summary>
    /// The color of the item.
    /// </summary>
    public BitColor? Color { get; set; }

    /// <summary>
    /// The custom template for the item's dot.
    /// </summary>
    public RenderFragment<BitTimelineItem>? DotTemplate { get; set; }

    /// <summary>
    /// Hides the item's dot.
    /// </summary>
    public bool HideDot { get; set; }

    /// <summary>
    /// Name of an icon to render in the item.
    /// </summary>
    public string? IconName { get; set; }

    /// <summary>
    /// Whether or not the item is enabled.
    /// </summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// A unique value to use as a key of the item.
    /// </summary>
    public string? Key { get; set; }

    /// <summary>
    /// Click event handler of the item.
    /// </summary>
    public Action<BitTimelineItem>? OnClick { get; set; }

    /// <summary>
    /// The primary content of the item.
    /// </summary>
    public RenderFragment<BitTimelineItem>? PrimaryContent { get; set; }

    /// <summary>
    /// The primary text of the item.
    /// </summary>
    public string? PrimaryText { get; set; }

    /// <summary>
    /// Reverses the item direction.
    /// </summary>
    public bool Reversed { get; set; }

    /// <summary>
    /// The secondary content of the item.
    /// </summary>
    public RenderFragment<BitTimelineItem>? SecondaryContent { get; set; }

    /// <summary>
    /// The secondary text of the item.
    /// </summary>
    public string? SecondaryText { get; set; }

    /// <summary>
    /// The size of the item.
    /// </summary>
    public BitSize? Size { get; set; }

    /// <summary>
    /// The custom value for the style attribute of the item.
    /// </summary>
    public string? Style { get; set; }

    /// <summary>
    /// The custom template for the item.
    /// </summary>
    public RenderFragment<BitTimelineItem>? Template { get; set; }
}
