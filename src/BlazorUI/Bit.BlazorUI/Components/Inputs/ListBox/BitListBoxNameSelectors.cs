namespace Bit.BlazorUI;

/// <summary>
/// The names (or selectors) of the members a <see cref="BitListBox{TItem, TValue}"/> reads the items of a custom class
/// from. Each one defaults to the name of the matching member of <see cref="BitListBoxItem{TValue}"/>.
/// </summary>
public class BitListBoxNameSelectors<TItem, TValue>
{
    /// <summary>
    /// The AriaLabel field name and selector of the custom input class.
    /// </summary>
    public BitNameSelectorPair<TItem, string?> AriaLabel { get; set; } = new(nameof(BitListBoxItem<TValue>.AriaLabel));

    /// <summary>
    /// The CSS Class field name and selector of the custom input class.
    /// </summary>
    public BitNameSelectorPair<TItem, string?> Class { get; set; } = new(nameof(BitListBoxItem<TValue>.Class));

    /// <summary>
    /// The Description field name and selector of the custom input class.
    /// </summary>
    public BitNameSelectorPair<TItem, string?> Description { get; set; } = new(nameof(BitListBoxItem<TValue>.Description));

    /// <summary>
    /// The Icon field name and selector of the custom input class.
    /// </summary>
    public BitNameSelectorPair<TItem, BitIconInfo?> Icon { get; set; } = new(nameof(BitListBoxItem<TValue>.Icon));

    /// <summary>
    /// The IconName field name and selector of the custom input class.
    /// </summary>
    public BitNameSelectorPair<TItem, string?> IconName { get; set; } = new(nameof(BitListBoxItem<TValue>.IconName));

    /// <summary>
    /// The IsDisabled field name and selector of the custom input class.
    /// </summary>
    public BitNameSelectorPair<TItem, bool> IsDisabled { get; set; } = new(nameof(BitListBoxItem<TValue>.IsDisabled));

    /// <summary>
    /// The Key field name and selector of the custom input class.
    /// </summary>
    public BitNameSelectorPair<TItem, string?> Key { get; set; } = new(nameof(BitListBoxItem<TValue>.Key));

    /// <summary>
    /// The CSS Style field name and selector of the custom input class.
    /// </summary>
    public BitNameSelectorPair<TItem, string?> Style { get; set; } = new(nameof(BitListBoxItem<TValue>.Style));

    /// <summary>
    /// The Text field name and selector of the custom input class.
    /// </summary>
    public BitNameSelectorPair<TItem, string?> Text { get; set; } = new(nameof(BitListBoxItem<TValue>.Text));

    /// <summary>
    /// The Title field name and selector of the custom input class.
    /// </summary>
    public BitNameSelectorPair<TItem, string?> Title { get; set; } = new(nameof(BitListBoxItem<TValue>.Title));

    /// <summary>
    /// The Value field name and selector of the custom input class.
    /// </summary>
    public BitNameSelectorPair<TItem, TValue?> Value { get; set; } = new(nameof(BitListBoxItem<TValue>.Value));
}
