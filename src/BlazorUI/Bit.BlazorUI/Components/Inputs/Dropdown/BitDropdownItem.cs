namespace Bit.BlazorUI;

public class BitDropdownItem<TValue> : IBitDropdownItem
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
    /// The icon to display using custom CSS classes for external icon libraries.
    /// Takes precedence over <see cref="IconName"/> when both are set.
    /// Use this property to render icons from external libraries like FontAwesome, Material Icons, or Bootstrap Icons.
    /// For built-in Fluent UI icons, use <see cref="IconName"/> instead.
    /// </summary>
    public BitIconInfo? Icon { get; set; }

    /// <summary>
    /// The icon name from the Fluent UI icon set to display for the dropdown item.
    /// For external icon libraries, use <see cref="Icon"/> instead.
    /// </summary>
    public string? IconName { get; set; }

    /// <summary>
    /// The id for the dropdown item.
    /// </summary>
    public string? Id { get; set; }

    /// <summary>
    /// The custom data for the dropdown item to provide extra state for the template.
    /// </summary>
    public object? Data { get; set; }

    /// <summary>
    /// Determines if the dropdown item is disabled.
    /// </summary>
    public bool IsDisabled { get; set; }

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



    private BitItemValueType.ValueRead<TValue>? _valueRead;

    bool IBitDropdownItem.IsSelected
    {
        get => IsSelected;
        set => IsSelected = value;
    }

    T? IBitDropdownItem.GetValue<T>() where T : default => BitItemValueType.Read<TValue, T>(Value, ref _valueRead);

    bool IBitDropdownItem.TrySetValue<T>(T? value) where T : default
    {
        if (BitItemValueType.TryConvert(value, out TValue? itemValue) is false) return false;

        Value = itemValue;

        return true;
    }
}
