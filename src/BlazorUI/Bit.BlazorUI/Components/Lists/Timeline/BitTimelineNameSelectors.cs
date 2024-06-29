namespace Bit.BlazorUI;

public class BitTimelineNameSelectors<TItem>
{
    /// <summary>
    /// The CSS Class field name and selector of the custom input class.
    /// </summary>
    public BitNameSelectorPair<TItem, string?> Class { get; set; } = new(nameof(BitTimelineItem.Class));

    /// <summary>
    /// The Color field name and selector of the custom input class.
    /// </summary>
    public BitNameSelectorPair<TItem, BitColor?> Color { get; set; } = new(nameof(BitTimelineItem.Color));

    /// <summary>
    /// DotTemplate field name and selector of the custom input class.
    /// </summary>
    public BitNameSelectorPair<TItem, RenderFragment<TItem>?> DotTemplate { get; set; } = new(nameof(BitTimelineItem.DotTemplate));

    /// <summary>
    /// The HideDot field name and selector of the custom input class.
    /// </summary>
    public BitNameSelectorPair<TItem, bool> HideDot { get; set; } = new(nameof(BitTimelineItem.HideDot));

    /// <summary>
    /// IconName field name and selector of the custom input class.
    /// </summary>
    public BitNameSelectorPair<TItem, string?> IconName { get; set; } = new(nameof(BitTimelineItem.IconName));

    /// <summary>
    /// IsEnabled field name and selector of the custom input class.
    /// </summary>
    public BitNameSelectorPair<TItem, bool> IsEnabled { get; set; } = new(nameof(BitTimelineItem.IsEnabled));

    /// <summary>
    /// Key field name and selector of the custom input class.
    /// </summary>
    public BitNameSelectorPair<TItem, string?> Key { get; set; } = new(nameof(BitTimelineItem.Key));

    /// <summary>
    /// OnClick field name and selector of the custom input class.
    /// </summary>
    public BitNameSelectorPair<TItem, Action<TItem>?> OnClick { get; set; } = new(nameof(BitTimelineItem.OnClick));

    /// <summary>
    /// PrimaryContent field name and selector of the custom input class.
    /// </summary>
    public BitNameSelectorPair<TItem, RenderFragment<TItem>?> PrimaryContent { get; set; } = new(nameof(BitTimelineItem.PrimaryContent));

    /// <summary>
    /// PrimaryText field name and selector of the custom input class.
    /// </summary>
    public BitNameSelectorPair<TItem, string?> PrimaryText { get; set; } = new(nameof(BitTimelineItem.PrimaryText));

    /// <summary>
    /// Reversed field name and selector of the custom input class.
    /// </summary>
    public BitNameSelectorPair<TItem, bool> Reversed { get; set; } = new(nameof(BitTimelineItem.Reversed));

    /// <summary>
    /// SecondaryContent field name and selector of the custom input class.
    /// </summary>
    public BitNameSelectorPair<TItem, RenderFragment<TItem>?> SecondaryContent { get; set; } = new(nameof(BitTimelineItem.SecondaryContent));

    /// <summary>
    /// SecondaryText field name and selector of the custom input class.
    /// </summary>
    public BitNameSelectorPair<TItem, string?> SecondaryText { get; set; } = new(nameof(BitTimelineItem.SecondaryText));

    /// <summary>
    /// The CSS Style field name and selector of the custom input class.
    /// </summary>
    public BitNameSelectorPair<TItem, string?> Style { get; set; } = new(nameof(BitTimelineItem.Style));

    /// <summary>
    /// The Size field name and selector of the custom input class.
    /// </summary>
    public BitNameSelectorPair<TItem, BitSize?> Size { get; set; } = new(nameof(BitTimelineItem.Size));

    /// <summary>
    /// Template field name and selector of the custom input class.
    /// </summary>
    public BitNameSelectorPair<TItem, RenderFragment<TItem>?> Template { get; set; } = new(nameof(BitTimelineItem.Template));
}
