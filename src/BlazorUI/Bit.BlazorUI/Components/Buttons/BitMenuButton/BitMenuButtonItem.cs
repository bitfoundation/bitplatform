namespace Bit.BlazorUI;

public class BitMenuButtonItem
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
    /// Determines the selection state of the item.
    /// </summary>
    public bool IsSelected { get; set; }

    /// <summary>
    /// A unique value to use as a key of the item.
    /// </summary>
    public string? Key { get; set; }

    /// <summary>
    /// Click event handler of the item.
    /// </summary>
    public Action<BitMenuButtonItem>? OnClick { get; set; }

    /// <summary>
    /// The custom value for the style attribute of the item.
    /// </summary>
    public string? Style { get; set; }

    /// <summary>
    /// The custom template for the item.
    /// </summary>
    public RenderFragment<BitMenuButtonItem>? Template { get; set; }

    /// <summary>
    /// Text to render in the item.
    /// </summary>
    public string? Text { get; set; }
}
