namespace Bit.BlazorUI;

/// <summary>
/// The names (or selectors) of the members a <see cref="BitStepper{TItem}"/> reads the steps of a custom item class from.
/// Each one defaults to the name of the matching member of <see cref="BitStepperItem"/>.
/// </summary>
public class BitStepperNameSelectors<TItem>
{
    /// <summary>
    /// The AriaLabel field name and selector of the custom input class.
    /// </summary>
    public BitNameSelectorPair<TItem, string?> AriaLabel { get; set; } = new(nameof(BitStepperItem.AriaLabel));

    /// <summary>
    /// The CSS Class field name and selector of the custom input class.
    /// </summary>
    public BitNameSelectorPair<TItem, string?> Class { get; set; } = new(nameof(BitStepperItem.Class));

    /// <summary>
    /// The Description field name and selector of the custom input class.
    /// </summary>
    public BitNameSelectorPair<TItem, string?> Description { get; set; } = new(nameof(BitStepperItem.Description));

    /// <summary>
    /// The Icon field name and selector of the custom input class.
    /// </summary>
    public BitNameSelectorPair<TItem, BitIconInfo?> Icon { get; set; } = new(nameof(BitStepperItem.Icon));

    /// <summary>
    /// The IconName field name and selector of the custom input class.
    /// </summary>
    public BitNameSelectorPair<TItem, string?> IconName { get; set; } = new(nameof(BitStepperItem.IconName));

    /// <summary>
    /// The IsDisabled field name and selector of the custom input class.
    /// </summary>
    public BitNameSelectorPair<TItem, bool> IsDisabled { get; set; } = new(nameof(BitStepperItem.IsDisabled));

    /// <summary>
    /// The IsError field name and selector of the custom input class.
    /// </summary>
    public BitNameSelectorPair<TItem, bool> IsError { get; set; } = new(nameof(BitStepperItem.IsError));

    /// <summary>
    /// The Key field name and selector of the custom input class.
    /// </summary>
    public BitNameSelectorPair<TItem, string?> Key { get; set; } = new(nameof(BitStepperItem.Key));

    /// <summary>
    /// The OnClick field name and selector of the custom input class.
    /// </summary>
    public BitNameSelectorPair<TItem, Action<TItem>?> OnClick { get; set; } = new(nameof(BitStepperItem.OnClick));

    /// <summary>
    /// The CSS Style field name and selector of the custom input class.
    /// </summary>
    public BitNameSelectorPair<TItem, string?> Style { get; set; } = new(nameof(BitStepperItem.Style));

    /// <summary>
    /// The Text field name and selector of the custom input class.
    /// </summary>
    public BitNameSelectorPair<TItem, string?> Text { get; set; } = new(nameof(BitStepperItem.Text));

    /// <summary>
    /// The Title field name and selector of the custom input class.
    /// </summary>
    public BitNameSelectorPair<TItem, string?> Title { get; set; } = new(nameof(BitStepperItem.Title));
}
