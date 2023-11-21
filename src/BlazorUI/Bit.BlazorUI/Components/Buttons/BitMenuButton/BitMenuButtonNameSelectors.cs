namespace Bit.BlazorUI;

public class BitMenuButtonNameSelectors<TItem>
{
    /// <summary>
    /// The CSS Class field name and selector of the custom input class.
    /// </summary>
    public BitNameSelectorPair<TItem, string?> Class { get; set; } = new(nameof(BitMenuButtonItem.Class));

    /// <summary>
    /// IconName field name and selector of the custom input class.
    /// </summary>
    public BitNameSelectorPair<TItem, string?> IconName { get; set; } = new(nameof(BitMenuButtonItem.IconName));

    /// <summary>
    /// IsEnabled field name and selector of the custom input class.
    /// </summary>
    public BitNameSelectorPair<TItem, bool> IsEnabled { get; set; } = new(nameof(BitMenuButtonItem.IsEnabled));

    /// <summary>
    /// IsSelected field name and selector of the custom input class.
    /// </summary>
    public BitNameSelectorPair<TItem, bool> IsSelected { get; set; } = new(nameof(BitMenuButtonItem.IsSelected));

    /// <summary>
    /// Key field name and selector of the custom input class.
    /// </summary>
    public BitNameSelectorPair<TItem, string?> Key { get; set; } = new(nameof(BitMenuButtonItem.Key));

    /// <summary>
    /// OnClick field name and selector of the custom input class.
    /// </summary>
    public BitNameSelectorPair<TItem, Action<TItem>?> OnClick { get; set; } = new(nameof(BitMenuButtonItem.OnClick));

    /// <summary>
    /// The CSS Style field name and selector of the custom input class.
    /// </summary>
    public BitNameSelectorPair<TItem, string?> Style { get; set; } = new(nameof(BitMenuButtonItem.Style));

    /// <summary>
    /// Template field name and selector of the custom input class.
    /// </summary>
    public BitNameSelectorPair<TItem, RenderFragment<TItem>?> Template { get; set; } = new(nameof(BitMenuButtonItem.Template));

    /// <summary>
    /// Text field name and selector of the custom input class.
    /// </summary>
    public BitNameSelectorPair<TItem, string?> Text { get; set; } = new(nameof(BitMenuButtonItem.Text));
}
