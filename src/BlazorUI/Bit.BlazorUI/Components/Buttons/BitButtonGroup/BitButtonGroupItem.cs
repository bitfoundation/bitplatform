
namespace Bit.BlazorUI;

public class BitButtonGroupItem
{
    /// <summary>
    /// The custom CSS classes of the item.
    /// </summary>
    public string? Class { get; set; }

    /// <summary>
    /// Name of an icon to render next to the item text.
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
    public Action<BitButtonGroupItem>? OnClick { get; set; }

    /// <summary>
    /// The custom value for the style attribute of the item.
    /// </summary>
    public string? Style { get; set; }

    /// <summary>
    /// The custom template for the item.
    /// </summary>
    public RenderFragment<BitButtonGroupItem>? Template { get; set; }

    /// <summary>
    /// Text to render in the item.
    /// </summary>
    public string? Text { get; set; }
}
