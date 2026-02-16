namespace Bit.BlazorUI;

public class BitTimelineItem
{
    /// <summary>
    /// The custom CSS classes of the timeline item.
    /// </summary>
    public string? Class { get; set; }

    /// <summary>
    /// The general color of the timeline item.
    /// </summary>
    public BitColor? Color { get; set; }

    /// <summary>
    /// The custom template for the timeline item's dot.
    /// </summary>
    public RenderFragment<BitTimelineItem>? DotTemplate { get; set; }

    /// <summary>
    /// Hides the timeline item's dot.
    /// </summary>
    public bool HideDot { get; set; }

    /// <summary>
    /// Gets or sets the icon to display using custom CSS classes for external icon libraries.
    /// Takes precedence over <see cref="IconName"/> when both are set.
    /// </summary>
    /// <remarks>
    /// Use this property to render icons from external libraries like FontAwesome, Material Icons, or Bootstrap Icons.
    /// For built-in Fluent UI icons, use <see cref="IconName"/> instead.
    /// </remarks>
    /// <example>
    /// Bootstrap: Icon="BitIconInfo.Bi("gear-fill")"
    /// FontAwesome: Icon = BitIconInfo.Fa("solid house")
    /// Custom CSS: Icon = BitIconInfo.Css("my-icon-class")
    /// </example>
    public BitIconInfo? Icon { get; set; }

    /// <summary>
    /// Name of an icon to render in the timeline item.
    /// </summary>
    public string? IconName { get; set; }

    /// <summary>
    /// Whether or not the timeline item is enabled.
    /// </summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// A unique value to use as a key of the timeline item.
    /// </summary>
    public string? Key { get; set; }

    /// <summary>
    /// Click event handler of the timeline item.
    /// </summary>
    public Action<BitTimelineItem>? OnClick { get; set; }

    /// <summary>
    /// The primary content of the timeline item.
    /// </summary>
    public RenderFragment<BitTimelineItem>? PrimaryContent { get; set; }

    /// <summary>
    /// The primary text of the timeline item.
    /// </summary>
    public string? PrimaryText { get; set; }

    /// <summary>
    /// Reverses the timeline item direction.
    /// </summary>
    public bool Reversed { get; set; }

    /// <summary>
    /// The secondary content of the timeline item.
    /// </summary>
    public RenderFragment<BitTimelineItem>? SecondaryContent { get; set; }

    /// <summary>
    /// The secondary text of the timeline item.
    /// </summary>
    public string? SecondaryText { get; set; }

    /// <summary>
    /// The size of the timeline item.
    /// </summary>
    public BitSize? Size { get; set; }

    /// <summary>
    /// The custom value for the style attribute of the timeline item.
    /// </summary>
    public string? Style { get; set; }

    /// <summary>
    /// The custom template for the timeline item.
    /// </summary>
    public RenderFragment<BitTimelineItem>? Template { get; set; }
}
