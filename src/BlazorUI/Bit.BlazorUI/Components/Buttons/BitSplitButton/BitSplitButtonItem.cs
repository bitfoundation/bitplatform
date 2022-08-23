
namespace Bit.BlazorUI;

public class BitSplitButtonItem
{
    /// <summary>
    /// Name of an icon to render next to the item text
    /// </summary>
    public BitIconName? IconName { get; set; }

    /// <summary>
    /// Text to render in the item
    /// </summary>
    public string? Text { get; set; }

    /// <summary>
    /// A unique value to use as a key of the item
    /// </summary>
    public string? key { get; set; }

    /// <summary>
    /// Whether or not the item is enabled.
    /// </summary>
    public bool IsEnabled { get; set; } = true;
}
