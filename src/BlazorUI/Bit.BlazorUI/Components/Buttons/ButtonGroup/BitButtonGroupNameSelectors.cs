namespace Bit.BlazorUI;

public class BitButtonGroupNameSelectors<TItem>
{
    /// <summary>
    /// The CSS Class field name and selector of the custom input class.
    /// </summary>
    public BitNameSelectorPair<TItem, string?> Class { get; set; } = new(nameof(BitButtonGroupItem.Class));

    /// <summary>
    /// IconName field name and selector of the custom input class.
    /// </summary>
    public BitNameSelectorPair<TItem, string?> IconName { get; set; } = new(nameof(BitButtonGroupItem.IconName));

    /// <summary>
    /// IsEnabled field name and selector of the custom input class.
    /// </summary>
    public BitNameSelectorPair<TItem, bool> IsEnabled { get; set; } = new(nameof(BitButtonGroupItem.IsEnabled));

    /// <summary>
    /// Key field name and selector of the custom input class.
    /// </summary>
    public BitNameSelectorPair<TItem, string?> Key { get; set; } = new(nameof(BitButtonGroupItem.Key));

    /// <summary>
    /// OffIconName field name and selector of the custom input class.
    /// </summary>
    public BitNameSelectorPair<TItem, string?> OffIconName { get; set; } = new(nameof(BitButtonGroupItem.OffIconName));

    /// <summary>
    /// OffText field name and selector of the custom input class.
    /// </summary>
    public BitNameSelectorPair<TItem, string?> OffText { get; set; } = new(nameof(BitButtonGroupItem.OffText));

    /// <summary>
    /// OffTitle field name and selector of the custom input class.
    /// </summary>
    public BitNameSelectorPair<TItem, string?> OffTitle { get; set; } = new(nameof(BitButtonGroupItem.OffTitle));

    /// <summary>
    /// OnIconName field name and selector of the custom input class.
    /// </summary>
    public BitNameSelectorPair<TItem, string?> OnIconName { get; set; } = new(nameof(BitButtonGroupItem.OnIconName));

    /// <summary>
    /// OnText field name and selector of the custom input class.
    /// </summary>
    public BitNameSelectorPair<TItem, string?> OnText { get; set; } = new(nameof(BitButtonGroupItem.OnText));

    /// <summary>
    /// OnTitle field name and selector of the custom input class.
    /// </summary>
    public BitNameSelectorPair<TItem, string?> OnTitle { get; set; } = new(nameof(BitButtonGroupItem.OnTitle));

    /// <summary>
    /// OnClick field name and selector of the custom input class.
    /// </summary>
    public BitNameSelectorPair<TItem, Action<TItem>?> OnClick { get; set; } = new(nameof(BitButtonGroupItem.OnClick));

    /// <summary>
    /// ReversedIcon field name and selector of the custom input class.
    /// </summary>
    public BitNameSelectorPair<TItem, bool> ReversedIcon { get; set; } = new(nameof(BitButtonGroupItem.ReversedIcon));

    /// <summary>
    /// The CSS Style field name and selector of the custom input class.
    /// </summary>
    public BitNameSelectorPair<TItem, string?> Style { get; set; } = new(nameof(BitButtonGroupItem.Style));

    /// <summary>
    /// Template field name and selector of the custom input class.
    /// </summary>
    public BitNameSelectorPair<TItem, RenderFragment<TItem>?> Template { get; set; } = new(nameof(BitButtonGroupItem.Template));

    /// <summary>
    /// Text field name and selector of the custom input class.
    /// </summary>
    public BitNameSelectorPair<TItem, string?> Text { get; set; } = new(nameof(BitButtonGroupItem.Text));

    /// <summary>
    /// Title field name and selector of the custom input class.
    /// </summary>
    public BitNameSelectorPair<TItem, string?> Title { get; set; } = new(nameof(BitButtonGroupItem.Title));
}
