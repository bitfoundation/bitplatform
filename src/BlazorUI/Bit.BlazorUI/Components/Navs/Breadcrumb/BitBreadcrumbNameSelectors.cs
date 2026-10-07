namespace Bit.BlazorUI;

public class BitBreadcrumbNameSelectors<TItem> where TItem : class
{
    /// <summary>
    /// The AriaLabel field name and selector of the custom input class.
    /// </summary>
    public BitNameSelectorPair<TItem, string?> AriaLabel { get; set; } = new(nameof(BitBreadcrumbItem.AriaLabel));

    /// <summary>
    /// The CSS Class field name and selector of the custom input class.
    /// </summary>
    public BitNameSelectorPair<TItem, string?> Class { get; set; } = new(nameof(BitBreadcrumbItem.Class));

    /// <summary>
    /// The Href field name and selector of the custom input class.
    /// </summary>
    public BitNameSelectorPair<TItem, string?> Href { get; set; } = new(nameof(BitBreadcrumbItem.Href));

    /// <summary>
    /// The Icon field name and selector of the custom input class.
    /// </summary>
    public BitNameSelectorPair<TItem, BitIconInfo?> Icon { get; set; } = new(nameof(BitBreadcrumbItem.Icon));

    /// <summary>
    /// The IconName field name and selector of the custom input class.
    /// </summary>
    public BitNameSelectorPair<TItem, string?> IconName { get; set; } = new(nameof(BitBreadcrumbItem.IconName));

    /// <summary>
    /// The IconPlacement field name and selector of the custom input class.
    /// </summary>
    public BitNameSelectorPair<TItem, BitPlacement?> IconPlacement { get; set; } = new(nameof(BitBreadcrumbItem.IconPlacement));

    /// <summary>
    /// The IsDisabled field name and selector of the custom input class.
    /// </summary>
    public BitNameSelectorPair<TItem, bool> IsDisabled { get; set; } = new(nameof(BitBreadcrumbItem.IsDisabled));

    /// <summary>
    /// The IsSelected field name and selector of the custom input class.
    /// </summary>
    public BitNameSelectorPair<TItem, bool> IsSelected { get; set; } = new(nameof(BitBreadcrumbItem.IsSelected));

    /// <summary>
    /// The Key field name and selector of the custom input class.
    /// </summary>
    public BitNameSelectorPair<TItem, string?> Key { get; set; } = new(nameof(BitBreadcrumbItem.Key));

    /// <summary>
    /// Click event handler of the item.
    /// </summary>
    public Action<TItem>? OnClick { get; set; }

    /// <summary>
    /// The OverflowTemplate field name and selector of the custom input class.
    /// </summary>
    public BitNameSelectorPair<TItem, RenderFragment<TItem>?> OverflowTemplate { get; set; } = new(nameof(BitBreadcrumbItem.OverflowTemplate));

    /// <summary>
    /// The Rel field name and selector of the custom input class.
    /// </summary>
    public BitNameSelectorPair<TItem, BitLinkRels?> Rel { get; set; } = new(nameof(BitBreadcrumbItem.Rel));

    /// <summary>
    /// The CSS Style field name and selector of the custom input class.
    /// </summary>
    public BitNameSelectorPair<TItem, string?> Style { get; set; } = new(nameof(BitBreadcrumbItem.Style));

    /// <summary>
    /// The Target field name and selector of the custom input class.
    /// </summary>
    public BitNameSelectorPair<TItem, string?> Target { get; set; } = new(nameof(BitBreadcrumbItem.Target));

    /// <summary>
    /// The Template field name and selector of the custom input class.
    /// </summary>
    public BitNameSelectorPair<TItem, RenderFragment<TItem>?> Template { get; set; } = new(nameof(BitBreadcrumbItem.Template));

    /// <summary>
    /// The Text field name and selector of the custom input class.
    /// </summary>
    public BitNameSelectorPair<TItem, string?> Text { get; set; } = new(nameof(BitBreadcrumbItem.Text));

    /// <summary>
    /// The Title field name and selector of the custom input class.
    /// </summary>
    public BitNameSelectorPair<TItem, string?> Title { get; set; } = new(nameof(BitBreadcrumbItem.Title));
}
