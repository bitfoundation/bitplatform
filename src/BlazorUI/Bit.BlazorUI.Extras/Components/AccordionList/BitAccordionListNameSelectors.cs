namespace Bit.BlazorUI;

/// <summary>
/// The names and selectors of the custom input type properties for the <see cref="BitAccordionList{TItem}"/>.
/// </summary>
public class BitAccordionListNameSelectors<TItem>
{
    /// <summary>
    /// The CSS Class field name and selector of the custom input class.
    /// </summary>
    public BitNameSelectorPair<TItem, string?> Class { get; set; } = new(nameof(BitAccordionListItem.Class));

    /// <summary>
    /// Description field name and selector of the custom input class.
    /// </summary>
    public BitNameSelectorPair<TItem, string?> Description { get; set; } = new(nameof(BitAccordionListItem.Description));

    /// <summary>
    /// ExpanderIcon field name and selector of the custom input class.
    /// </summary>
    public BitNameSelectorPair<TItem, BitIconInfo?> ExpanderIcon { get; set; } = new(nameof(BitAccordionListItem.ExpanderIcon));

    /// <summary>
    /// ExpanderIconName field name and selector of the custom input class.
    /// </summary>
    public BitNameSelectorPair<TItem, string?> ExpanderIconName { get; set; } = new(nameof(BitAccordionListItem.ExpanderIconName));

    /// <summary>
    /// Body field name and selector of the custom input class.
    /// </summary>
    public BitNameSelectorPair<TItem, RenderFragment<TItem>?> Body { get; set; } = new(nameof(BitAccordionListItem.Body));

    /// <summary>
    /// HeaderTemplate field name and selector of the custom input class.
    /// </summary>
    public BitNameSelectorPair<TItem, RenderFragment<TItem>?> HeaderTemplate { get; set; } = new(nameof(BitAccordionListItem.HeaderTemplate));

    /// <summary>
    /// IsEnabled field name and selector of the custom input class.
    /// </summary>
    public BitNameSelectorPair<TItem, bool> IsEnabled { get; set; } = new(nameof(BitAccordionListItem.IsEnabled));

    /// <summary>
    /// IsExpanded field name and selector of the custom input class.
    /// </summary>
    public BitNameSelectorPair<TItem, bool> IsExpanded { get; set; } = new(nameof(BitAccordionListItem.IsExpanded));

    /// <summary>
    /// Key field name and selector of the custom input class.
    /// </summary>
    public BitNameSelectorPair<TItem, string?> Key { get; set; } = new(nameof(BitAccordionListItem.Key));

    /// <summary>
    /// OnClick field name and selector of the custom input class.
    /// </summary>
    public BitNameSelectorPair<TItem, Action<TItem>?> OnClick { get; set; } = new(nameof(BitAccordionListItem.OnClick));

    /// <summary>
    /// The CSS Style field name and selector of the custom input class.
    /// </summary>
    public BitNameSelectorPair<TItem, string?> Style { get; set; } = new(nameof(BitAccordionListItem.Style));

    /// <summary>
    /// Title field name and selector of the custom input class.
    /// </summary>
    public BitNameSelectorPair<TItem, string?> Title { get; set; } = new(nameof(BitAccordionListItem.Title));
}
