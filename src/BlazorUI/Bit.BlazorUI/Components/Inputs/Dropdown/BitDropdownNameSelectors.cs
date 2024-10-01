namespace Bit.BlazorUI;

public class BitDropdownNameSelectors<TItem, TValue>
{
    /// <summary>
    /// The AriaLabel field name and selector of the custom input class.
    /// </summary>
    public BitNameSelectorPair<TItem, string?> AriaLabel { get; set; } = new(nameof(BitDropdownItem<TValue>.AriaLabel));

    /// <summary>
    /// The CSS Class field name and selector of the custom input class.
    /// </summary>
    public BitNameSelectorPair<TItem, string?> Class { get; set; } = new(nameof(BitDropdownItem<TValue>.Class));

    /// <summary>
    /// The Id field name and selector of the custom input class.
    /// </summary>
    public BitNameSelectorPair<TItem, string?> Id { get; set; } = new(nameof(BitDropdownItem<TValue>.Id));

    /// <summary>
    /// The Data field name and selector of the custom input class.
    /// </summary>
    public BitNameSelectorPair<TItem, object?> Data { get; set; } = new(nameof(BitDropdownItem<TValue>.Data));

    /// <summary>
    /// The IsEnabled field name and selector of the custom input class.
    /// </summary>
    public BitNameSelectorPair<TItem, bool> IsEnabled { get; set; } = new(nameof(BitDropdownItem<TValue>.IsEnabled));

    /// <summary>
    /// The IsHidden field name and selector of the custom input class.
    /// </summary>
    public BitNameSelectorPair<TItem, bool> IsHidden { get; set; } = new(nameof(BitDropdownItem<TValue>.IsHidden));

    /// <summary>
    /// The ItemType field name and selector of the custom input class.
    /// </summary>
    public BitNameSelectorPair<TItem, BitDropdownItemType> ItemType { get; set; } = new(nameof(BitDropdownItem<TValue>.ItemType));

    /// <summary>
    /// The CSS Style field name and selector of the custom input class.
    /// </summary>
    public BitNameSelectorPair<TItem, string?> Style { get; set; } = new(nameof(BitDropdownItem<TValue>.Style));

    /// <summary>
    /// The Text field name and selector of the custom input class.
    /// </summary>
    public BitNameSelectorPair<TItem, string?> Text { get; set; } = new(nameof(BitDropdownItem<TValue>.Text));

    /// <summary>
    /// The Title field name and selector of the custom input class.
    /// </summary>
    public BitNameSelectorPair<TItem, string?> Title { get; set; } = new(nameof(BitDropdownItem<TValue>.Title));

    /// <summary>
    /// The Value field name and selector of the custom input class.
    /// </summary>
    public BitNameSelectorPair<TItem, TValue?> Value { get; set; } = new(nameof(BitDropdownItem<TValue>.Value));



    /// <summary>
    /// The setter function for updating Text property of custom item in Dynamic ComboBox mode upon new item addition.
    /// </summary>
    public Action<string, TItem>? TextSetter { get; set; }

    /// <summary>
    /// The setter function for updating Value property of custom item in Dynamic ComboBox mode upon new item addition.
    /// </summary>
    public Action<TItem, TValue>? ValueSetter { get; set; }



    /// <summary>
    /// The IsSelected field name of the custom input class. This property's value is assigned by the component.
    /// </summary>
    public string IsSelected { get; set; } = nameof(BitDropdownItem<TValue>.IsSelected);
}
