namespace Bit.BlazorUI;

public class BitMenuButtonNameSelectors<TItem>
{
    /// <summary>
    /// AriaLabel field name and selector of the custom input class.
    /// </summary>
    public BitNameSelectorPair<TItem, string?> AriaLabel { get; set; } = new(nameof(BitMenuButtonItem.AriaLabel));

    /// <summary>
    /// Checkable field name and selector of the custom input class.
    /// </summary>
    public BitNameSelectorPair<TItem, bool> Checkable { get; set; } = new(nameof(BitMenuButtonItem.Checkable));

    /// <summary>
    /// The CSS Class field name and selector of the custom input class.
    /// </summary>
    public BitNameSelectorPair<TItem, string?> Class { get; set; } = new(nameof(BitMenuButtonItem.Class));

    /// <summary>
    /// Href field name and selector of the custom input class.
    /// </summary>
    public BitNameSelectorPair<TItem, string?> Href { get; set; } = new(nameof(BitMenuButtonItem.Href));

    /// <summary>
    /// Icon field name and selector of the custom input class.
    /// </summary>
    public BitNameSelectorPair<TItem, BitIconInfo?> Icon { get; set; } = new(nameof(BitMenuButtonItem.Icon));

    /// <summary>
    /// IconName field name and selector of the custom input class.
    /// </summary>
    public BitNameSelectorPair<TItem, string?> IconName { get; set; } = new(nameof(BitMenuButtonItem.IconName));

    /// <summary>
    /// IsChecked field name and selector of the custom input class. The menu button writes the new state back
    /// to the named property as a check item is clicked, so a selector alone leaves the toggling to the page.
    /// </summary>
    public BitNameSelectorPair<TItem, bool> IsChecked { get; set; } = new(nameof(BitMenuButtonItem.IsChecked));

    /// <summary>
    /// IsEnabled field name and selector of the custom input class.
    /// </summary>
    public BitNameSelectorPair<TItem, bool> IsEnabled { get; set; } = new(nameof(BitMenuButtonItem.IsEnabled));

    /// <summary>
    /// IsHeader field name and selector of the custom input class.
    /// </summary>
    public BitNameSelectorPair<TItem, bool> IsHeader { get; set; } = new(nameof(BitMenuButtonItem.IsHeader));

    /// <summary>
    /// IsSelected field name and selector of the custom input class.
    /// </summary>
    public BitNameSelectorPair<TItem, bool> IsSelected { get; set; } = new(nameof(BitMenuButtonItem.IsSelected));

    /// <summary>
    /// IsSeparator field name and selector of the custom input class.
    /// </summary>
    public BitNameSelectorPair<TItem, bool> IsSeparator { get; set; } = new(nameof(BitMenuButtonItem.IsSeparator));

    /// <summary>
    /// Key field name and selector of the custom input class.
    /// </summary>
    public BitNameSelectorPair<TItem, string?> Key { get; set; } = new(nameof(BitMenuButtonItem.Key));

    /// <summary>
    /// OnClick field name and selector of the custom input class.
    /// </summary>
    public BitNameSelectorPair<TItem, Action<TItem>?> OnClick { get; set; } = new(nameof(BitMenuButtonItem.OnClick));

    /// <summary>
    /// SecondaryText field name and selector of the custom input class.
    /// </summary>
    public BitNameSelectorPair<TItem, string?> SecondaryText { get; set; } = new(nameof(BitMenuButtonItem.SecondaryText));

    /// <summary>
    /// The CSS Style field name and selector of the custom input class.
    /// </summary>
    public BitNameSelectorPair<TItem, string?> Style { get; set; } = new(nameof(BitMenuButtonItem.Style));

    /// <summary>
    /// Target field name and selector of the custom input class.
    /// </summary>
    public BitNameSelectorPair<TItem, string?> Target { get; set; } = new(nameof(BitMenuButtonItem.Target));

    /// <summary>
    /// Template field name and selector of the custom input class.
    /// </summary>
    public BitNameSelectorPair<TItem, RenderFragment<TItem>?> Template { get; set; } = new(nameof(BitMenuButtonItem.Template));

    /// <summary>
    /// Text field name and selector of the custom input class.
    /// </summary>
    public BitNameSelectorPair<TItem, string?> Text { get; set; } = new(nameof(BitMenuButtonItem.Text));

    /// <summary>
    /// Title field name and selector of the custom input class.
    /// </summary>
    public BitNameSelectorPair<TItem, string?> Title { get; set; } = new(nameof(BitMenuButtonItem.Title));
}
