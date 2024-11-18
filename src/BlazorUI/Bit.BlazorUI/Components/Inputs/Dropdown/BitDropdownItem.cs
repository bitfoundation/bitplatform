namespace Bit.BlazorUI;

public class BitDropdownItem<TValue>
{
    /// <summary>
    /// The aria label attribute for the dropdown item.
    /// </summary>
    public string? AriaLabel { get; set; }

    /// <summary>
    /// Custom CSS class for the dropdown item.
    /// </summary>
    public string? Class { get; set; }

    /// <summary>
    /// The id for the dropdown item.
    /// </summary>
    public string? Id { get; set; }

    /// <summary>
    /// The custom data for the dropdown item to provide extra state for the template.
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
    /// The type of the dropdown item.
    /// </summary>
    public BitDropdownItemType ItemType { get; set; } = BitDropdownItemType.Normal;

    /// <summary>
    /// Custom CSS style for the dropdown item.
    /// </summary>
    public string? Style { get; set; }

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



    /// <summary>
    /// Determines if the item is selected. This property's value is assigned by the component.
    /// </summary>
    public bool IsSelected { get; internal set; }
}
