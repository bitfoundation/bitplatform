namespace Bit.BlazorUI;

public class BitNavNameSelectors<TItem>
{
    /// <summary>
    /// The AriaCurrent field name and selector of the custom input class.
    /// </summary>
    public BitNameSelectorPair<TItem, BitNavAriaCurrent?> AriaCurrent { get; set; } = new(nameof(BitNavItem.AriaCurrent));

    /// <summary>
    /// The AriaLabel field name and selector of the custom input class.
    /// </summary>
    public BitNameSelectorPair<TItem, string?> AriaLabel { get; set; } = new(nameof(BitNavItem.AriaLabel));

    /// <summary>
    /// The Class field name and selector of the custom input class.
    /// </summary>
    public BitNameSelectorPair<TItem, string?> Class { get; set; } = new(nameof(BitNavItem.Class));

    /// <summary>
    /// The ChildItems field name and selector of the custom input class.
    /// </summary>
    public BitNameSelectorPair<TItem, List<TItem>?> ChildItems { get; set; } = new(nameof(BitNavItem.ChildItems));

    /// <summary>
    /// The CollapseAriaLabel field name and selector of the custom input class.
    /// </summary>
    public BitNameSelectorPair<TItem, string?> CollapseAriaLabel { get; set; } = new(nameof(BitNavItem.CollapseAriaLabel));

    /// <summary>
    /// The Data field name and selector of the custom input class.
    /// </summary>
    public BitNameSelectorPair<TItem, object?> Data { get; set; } = new(nameof(BitNavItem.Data));

    /// <summary>
    /// The Description field name and selector of the custom input class.
    /// </summary>
    public BitNameSelectorPair<TItem, string?> Description { get; set; } = new(nameof(BitNavItem.Description));

    /// <summary>
    /// The ExpandAriaLabel field name and selector of the custom input class.
    /// </summary>
    public BitNameSelectorPair<TItem, string?> ExpandAriaLabel { get; set; } = new(nameof(BitNavItem.ExpandAriaLabel));

    /// <summary>
    /// The ForceAnchor field name and selector of the custom input class.
    /// </summary>
    public BitNameSelectorPair<TItem, bool?> ForceAnchor { get; set; } = new(nameof(BitNavItem.ForceAnchor));

    /// <summary>
    /// The IconName field name and selector of the custom input class.
    /// </summary>
    public BitNameSelectorPair<TItem, string?> IconName { get; set; } = new(nameof(BitNavItem.IconName));

    /// <summary>
    /// The IsEnabled field name and selector of the custom input class.
    /// </summary>
    public BitNameSelectorPair<TItem, bool?> IsEnabled { get; set; } = new(nameof(BitNavItem.IsEnabled));

    /// <summary>
    /// The IsExpanded field name and selector of the custom input class.
    /// </summary>
    public BitNameSelectorPair<TItem, bool?> IsExpanded { get; set; } = new(nameof(BitNavItem.IsExpanded));

    /// <summary>
    /// The IsSeparator field name and selector of the custom input class.
    /// </summary>
    public BitNameSelectorPair<TItem, bool?> IsSeparator { get; set; } = new(nameof(BitNavItem.IsSeparator));

    /// <summary>
    /// The Key field name and selector of the custom input class.
    /// </summary>
    public BitNameSelectorPair<TItem, string?> Key { get; set; } = new(nameof(BitNavItem.Key));

    /// <summary>
    /// The Style field name and selector of the custom input class.
    /// </summary>
    public BitNameSelectorPair<TItem, string?> Style { get; set; } = new(nameof(BitNavItem.Style));

    /// <summary>
    /// The Target field name and selector of the custom input class.
    /// </summary>
    public BitNameSelectorPair<TItem, string?> Target { get; set; } = new(nameof(BitNavItem.Target));

    /// <summary>
    /// The Template field name and selector of the custom input class.
    /// </summary>
    public BitNameSelectorPair<TItem, RenderFragment<TItem>?> Template { get; set; } = new(nameof(BitNavItem.Template));

    /// <summary>
    /// The TemplateRenderMode field name and selector of the custom input class.
    /// </summary>
    public BitNameSelectorPair<TItem, BitNavItemTemplateRenderMode?> TemplateRenderMode { get; set; } = new(nameof(BitNavItem.TemplateRenderMode));

    /// <summary>
    /// The Text field name and selector of the custom input class.
    /// </summary>
    public BitNameSelectorPair<TItem, string?> Text { get; set; } = new(nameof(BitNavItem.Text));

    /// <summary>
    /// The Title field name and selector of the custom input class.
    /// </summary>
    public BitNameSelectorPair<TItem, string?> Title { get; set; } = new(nameof(BitNavItem.Title));

    /// <summary>
    /// The Url field name and selector of the custom input class.
    /// </summary>
    public BitNameSelectorPair<TItem, string?> Url { get; set; } = new(nameof(BitNavItem.Url));

    /// <summary>
    /// The AdditionalUrls field name and selector of the custom input class.
    /// </summary>
    public BitNameSelectorPair<TItem, IEnumerable<string>?> AdditionalUrls { get; set; } = new(nameof(BitNavItem.AdditionalUrls));
}
