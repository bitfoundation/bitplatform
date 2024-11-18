namespace Bit.BlazorUI;

public class BitNavBarNameSelectors<TItem>
{
    /// <summary>
    /// The Class field name and selector of the custom input class.
    /// </summary>
    public BitNameSelectorPair<TItem, string?> Class { get; set; } = new(nameof(BitNavBarItem.Class));

    /// <summary>
    /// The Data field name and selector of the custom input class.
    /// </summary>
    public BitNameSelectorPair<TItem, object?> Data { get; set; } = new(nameof(BitNavBarItem.Data));

    /// <summary>
    /// The IconName field name and selector of the custom input class.
    /// </summary>
    public BitNameSelectorPair<TItem, string?> IconName { get; set; } = new(nameof(BitNavBarItem.IconName));

    /// <summary>
    /// The IsEnabled field name and selector of the custom input class.
    /// </summary>
    public BitNameSelectorPair<TItem, bool?> IsEnabled { get; set; } = new(nameof(BitNavBarItem.IsEnabled));

    /// <summary>
    /// The Key field name and selector of the custom input class.
    /// </summary>
    public BitNameSelectorPair<TItem, string?> Key { get; set; } = new(nameof(BitNavBarItem.Key));

    /// <summary>
    /// The Style field name and selector of the custom input class.
    /// </summary>
    public BitNameSelectorPair<TItem, string?> Style { get; set; } = new(nameof(BitNavBarItem.Style));

    /// <summary>
    /// The Target field name and selector of the custom input class.
    /// </summary>
    public BitNameSelectorPair<TItem, string?> Target { get; set; } = new(nameof(BitNavBarItem.Target));

    /// <summary>
    /// The Template field name and selector of the custom input class.
    /// </summary>
    public BitNameSelectorPair<TItem, RenderFragment<TItem>?> Template { get; set; } = new(nameof(BitNavBarItem.Template));

    /// <summary>
    /// The Text field name and selector of the custom input class.
    /// </summary>
    public BitNameSelectorPair<TItem, string?> Text { get; set; } = new(nameof(BitNavBarItem.Text));

    /// <summary>
    /// The Title field name and selector of the custom input class.
    /// </summary>
    public BitNameSelectorPair<TItem, string?> Title { get; set; } = new(nameof(BitNavBarItem.Title));

    /// <summary>
    /// The Url field name and selector of the custom input class.
    /// </summary>
    public BitNameSelectorPair<TItem, string?> Url { get; set; } = new(nameof(BitNavBarItem.Url));

    /// <summary>
    /// The AdditionalUrls field name and selector of the custom input class.
    /// </summary>
    public BitNameSelectorPair<TItem, IEnumerable<string>?> AdditionalUrls { get; set; } = new(nameof(BitNavBarItem.AdditionalUrls));
}
