namespace Bit.BlazorUI;

/// <summary>
/// The names and selectors of the custom input type properties for the <see cref="BitAccordionList{TItem}"/>.
/// </summary>
public class BitAccordionListNameSelectors<TItem>
{
    /// <summary>
    /// Actions field name and selector of the custom input class.
    /// </summary>
    public BitNameSelectorPair<TItem, RenderFragment<TItem>?> Actions { get; set; } = new(nameof(BitAccordionListItem.Actions));

    /// <summary>
    /// The CSS Class field name and selector of the custom input class.
    /// </summary>
    public BitNameSelectorPair<TItem, string?> Class { get; set; } = new(nameof(BitAccordionListItem.Class));

    /// <summary>
    /// Description field name and selector of the custom input class.
    /// </summary>
    public BitNameSelectorPair<TItem, string?> Description { get; set; } = new(nameof(BitAccordionListItem.Description));

    /// <summary>
    /// ExpandedExpanderIcon field name and selector of the custom input class.
    /// </summary>
    public BitNameSelectorPair<TItem, BitIconInfo?> ExpandedExpanderIcon { get; set; } = new(nameof(BitAccordionListItem.ExpandedExpanderIcon));

    /// <summary>
    /// ExpandedExpanderIconName field name and selector of the custom input class.
    /// </summary>
    public BitNameSelectorPair<TItem, string?> ExpandedExpanderIconName { get; set; } = new(nameof(BitAccordionListItem.ExpandedExpanderIconName));

    /// <summary>
    /// ExpanderIcon field name and selector of the custom input class.
    /// </summary>
    public BitNameSelectorPair<TItem, BitIconInfo?> ExpanderIcon { get; set; } = new(nameof(BitAccordionListItem.ExpanderIcon));

    /// <summary>
    /// ExpanderIconName field name and selector of the custom input class.
    /// </summary>
    public BitNameSelectorPair<TItem, string?> ExpanderIconName { get; set; } = new(nameof(BitAccordionListItem.ExpanderIconName));

    /// <summary>
    /// ExpanderTemplate field name and selector of the custom input class.
    /// </summary>
    public BitNameSelectorPair<TItem, RenderFragment<TItem>?> ExpanderTemplate { get; set; } = new(nameof(BitAccordionListItem.ExpanderTemplate));

    /// <summary>
    /// Body field name and selector of the custom input class.
    /// </summary>
    public BitNameSelectorPair<TItem, RenderFragment<TItem>?> Body { get; set; } = new(nameof(BitAccordionListItem.Body));

    /// <summary>
    /// HeaderAriaLabel field name and selector of the custom input class.
    /// </summary>
    public BitNameSelectorPair<TItem, string?> HeaderAriaLabel { get; set; } = new(nameof(BitAccordionListItem.HeaderAriaLabel));

    /// <summary>
    /// HeaderTemplate field name and selector of the custom input class.
    /// </summary>
    public BitNameSelectorPair<TItem, RenderFragment<TItem>?> HeaderTemplate { get; set; } = new(nameof(BitAccordionListItem.HeaderTemplate));

    /// <summary>
    /// HideExpanderIcon field name and selector of the custom input class.
    /// </summary>
    public BitNameSelectorPair<TItem, bool?> HideExpanderIcon { get; set; } = new(nameof(BitAccordionListItem.HideExpanderIcon));

    /// <summary>
    /// Icon field name and selector of the custom input class.
    /// </summary>
    public BitNameSelectorPair<TItem, BitIconInfo?> Icon { get; set; } = new(nameof(BitAccordionListItem.Icon));

    /// <summary>
    /// IconName field name and selector of the custom input class.
    /// </summary>
    public BitNameSelectorPair<TItem, string?> IconName { get; set; } = new(nameof(BitAccordionListItem.IconName));

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
    /// ReadOnly field name and selector of the custom input class.
    /// </summary>
    public BitNameSelectorPair<TItem, bool?> ReadOnly { get; set; } = new(nameof(BitAccordionListItem.ReadOnly));

    /// <summary>
    /// The CSS Style field name and selector of the custom input class.
    /// </summary>
    public BitNameSelectorPair<TItem, string?> Style { get; set; } = new(nameof(BitAccordionListItem.Style));

    /// <summary>
    /// Title field name and selector of the custom input class.
    /// </summary>
    public BitNameSelectorPair<TItem, string?> Title { get; set; } = new(nameof(BitAccordionListItem.Title));

    /// <summary>
    /// TitleTemplate field name and selector of the custom input class.
    /// </summary>
    public BitNameSelectorPair<TItem, RenderFragment<TItem>?> TitleTemplate { get; set; } = new(nameof(BitAccordionListItem.TitleTemplate));
}
