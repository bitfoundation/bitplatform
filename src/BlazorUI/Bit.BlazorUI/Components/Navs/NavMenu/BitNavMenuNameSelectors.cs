namespace Bit.BlazorUI;

public class BitNavMenuNameSelectors<TItem>
{
    /// <summary>
    /// The Class field name and selector of the custom input class.
    /// </summary>
    public BitNameSelectorPair<TItem, string?> Class { get; set; } = new(nameof(BitNavMenuItem.Class));

    /// <summary>
    /// The Data field name and selector of the custom input class.
    /// </summary>
    public BitNameSelectorPair<TItem, object?> Data { get; set; } = new(nameof(BitNavMenuItem.Data));

    /// <summary>
    /// The IconName field name and selector of the custom input class.
    /// </summary>
    public BitNameSelectorPair<TItem, string?> IconName { get; set; } = new(nameof(BitNavMenuItem.IconName));

    /// <summary>
    /// The IsEnabled field name and selector of the custom input class.
    /// </summary>
    public BitNameSelectorPair<TItem, bool?> IsEnabled { get; set; } = new(nameof(BitNavMenuItem.IsEnabled));

    /// <summary>
    /// The Key field name and selector of the custom input class.
    /// </summary>
    public BitNameSelectorPair<TItem, string?> Key { get; set; } = new(nameof(BitNavMenuItem.Key));

    /// <summary>
    /// The Style field name and selector of the custom input class.
    /// </summary>
    public BitNameSelectorPair<TItem, string?> Style { get; set; } = new(nameof(BitNavMenuItem.Style));

    /// <summary>
    /// The Target field name and selector of the custom input class.
    /// </summary>
    public BitNameSelectorPair<TItem, string?> Target { get; set; } = new(nameof(BitNavMenuItem.Target));

    /// <summary>
    /// The Template field name and selector of the custom input class.
    /// </summary>
    public BitNameSelectorPair<TItem, RenderFragment<TItem>?> Template { get; set; } = new(nameof(BitNavMenuItem.Template));

    /// <summary>
    /// The Text field name and selector of the custom input class.
    /// </summary>
    public BitNameSelectorPair<TItem, string?> Text { get; set; } = new(nameof(BitNavMenuItem.Text));

    /// <summary>
    /// The Title field name and selector of the custom input class.
    /// </summary>
    public BitNameSelectorPair<TItem, string?> Title { get; set; } = new(nameof(BitNavMenuItem.Title));

    /// <summary>
    /// The Url field name and selector of the custom input class.
    /// </summary>
    public BitNameSelectorPair<TItem, string?> Url { get; set; } = new(nameof(BitNavMenuItem.Url));

    /// <summary>
    /// The AdditionalUrls field name and selector of the custom input class.
    /// </summary>
    public BitNameSelectorPair<TItem, IEnumerable<string>?> AdditionalUrls { get; set; } = new(nameof(BitNavMenuItem.AdditionalUrls));
}
