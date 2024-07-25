﻿namespace Bit.BlazorUI;

public class BitBreadcrumbNameSelectors<TItem> where TItem : class
{
    /// <summary>
    /// The Key field name and selector of the custom input class.
    /// </summary>
    public BitNameSelectorPair<TItem, string?> Key { get; set; } = new(nameof(BitBreadcrumbItem.Key));

    /// <summary>
    /// The Text field name and selector of the custom input class.
    /// </summary>
    public BitNameSelectorPair<TItem, string?> Text { get; set; } = new(nameof(BitBreadcrumbItem.Text));

    /// <summary>
    /// The Href field name and selector of the custom input class.
    /// </summary>
    public BitNameSelectorPair<TItem, string?> Href { get; set; } = new(nameof(BitBreadcrumbItem.Href));

    /// <summary>
    /// The CSS Class field name and selector of the custom input class.
    /// </summary>
    public BitNameSelectorPair<TItem, string?> Class { get; set; } = new(nameof(BitBreadcrumbItem.Class));

    /// <summary>
    /// The CSS Style field name and selector of the custom input class.
    /// </summary>
    public BitNameSelectorPair<TItem, string?> Style { get; set; } = new(nameof(BitBreadcrumbItem.Style));

    /// <summary>
    /// The IsSelected field name and selector of the custom input class.
    /// </summary>
    public BitNameSelectorPair<TItem, bool> IsSelected { get; set; } = new(nameof(BitBreadcrumbItem.IsSelected));

    /// <summary>
    /// The IsEnabled field name and selector of the custom input class.
    /// </summary>
    public BitNameSelectorPair<TItem, bool> IsEnabled { get; set; } = new(nameof(BitBreadcrumbItem.IsEnabled));

    /// <summary>
    /// Click event handler of the item.
    /// </summary>
    public Action<TItem>? OnClick { get; set; }
}
