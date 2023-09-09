namespace Bit.BlazorUI;

public class BitDropdownItem<TValue>
{
    /// <summary>
    /// The aria label attribute for the dropdown item.
    /// </summary>
    public string? AriaLabel { get; set; }

    /// <summary>
    /// The id for the dropdown item.
    /// </summary>
    public string? Id { get; set; }

    /// <summary>
    /// The custom data for the dropdown item to provide state for the item template.
    /// </summary>
    public object? Data { get; set; }

    /// <summary>
    /// Determines if the dropdown item is enabled.
    /// </summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// Determines if the dropdown item is hidden.
    /// </summary>
    public bool IsHidden { get; set; }

    /// <summary>
    /// Determines if the dropdown item is selected.
    /// </summary>
    public bool IsSelected { get; internal set; }

    /// <summary>
    /// The type of the dropdown item.
    /// </summary>
    public BitDropdownItemType ItemType { get; set; } = BitDropdownItemType.Normal;

    /// <summary>
    /// The text to render for the dropdown item.
    /// </summary>
    public string? Text { get; set; }

    /// <summary>
    /// The title attribute for the dropdown item.
    /// </summary>
    public string? Title { get; set; }

    /// <summary>
    /// The value of the dropdown item.
    /// </summary>
    public TValue? Value { get; set; }
}
