
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
    /// Callback invoked with key value when a selected item is clicked.
    /// </summary>
    public Action<string?>? OnClick { get; set; }

    /// <summary>
    /// The content inside the item can be customized.
    /// </summary>
    public RenderFragment<BitSplitButtonItem>? ItemTemplate { get; set; }

    /// <summary>
    /// If true, the item is selected in the first rendering.
    /// </summary>
    public bool DefaultIsSelected { get; set; }

    /// <summary>
    /// Whether or not the item is enabled.
    /// </summary>
    public bool IsEnabled { get; set; } = true;
}
