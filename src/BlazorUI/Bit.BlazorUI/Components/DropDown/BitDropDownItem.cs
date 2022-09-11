namespace Bit.BlazorUI;

public class BitDropDownItem
{
    /// <summary>
    /// Text to render for this item
    /// </summary>
    public string Text { get; set; } = string.Empty;

    /// <summary>
    /// Value of this item
    /// </summary>
    public string Value { get; set; } = string.Empty;

    /// <summary>
    /// Whether or not this item is enabled
    /// </summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// Whether or not this item is selected
    /// </summary>
    public bool IsSelected { get; internal set; }

    /// <summary>
    /// The type of this item, Refers to the dropdown separator
    /// </summary>
    public BitDropDownItemType ItemType { get; set; } = BitDropDownItemType.Normal;

    /// <summary>
    /// Title attribute (built in tooltip) for a given item
    /// </summary>
    public string? Title { get; set; }

    /// <summary>
    /// Defines whether the item is hidden or not
    /// </summary>
    public bool IsHidden { get; set; }

    /// <summary>
    /// The aria label for the dropdown item. If not present, the `text` will be used
    /// </summary>
    public string? AriaLabel { get; set; }

    /// <summary>
    /// Data available to custom templates
    /// </summary>
    public object? Data { get; set; }
}
