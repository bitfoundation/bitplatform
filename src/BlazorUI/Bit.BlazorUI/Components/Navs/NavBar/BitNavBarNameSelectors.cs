namespace Bit.BlazorUI;

public class BitNavBarNameSelectors<TItem>
{
    /// <summary>
    /// The AriaCurrent field name and selector of the custom input class.
    /// </summary>
    public BitNameSelectorPair<TItem, BitNavAriaCurrent?> AriaCurrent { get; set; } = new(nameof(BitNavBarItem.AriaCurrent));

    /// <summary>
    /// The AriaLabel field name and selector of the custom input class.
    /// </summary>
    public BitNameSelectorPair<TItem, string?> AriaLabel { get; set; } = new(nameof(BitNavBarItem.AriaLabel));

    /// <summary>
    /// The Badge field name and selector of the custom input class.
    /// </summary>
    public BitNameSelectorPair<TItem, string?> Badge { get; set; } = new(nameof(BitNavBarItem.Badge));

    /// <summary>
    /// The BadgeAriaLabel field name and selector of the custom input class.
    /// </summary>
    public BitNameSelectorPair<TItem, string?> BadgeAriaLabel { get; set; } = new(nameof(BitNavBarItem.BadgeAriaLabel));

    /// <summary>
    /// The Class field name and selector of the custom input class.
    /// </summary>
    public BitNameSelectorPair<TItem, string?> Class { get; set; } = new(nameof(BitNavBarItem.Class));

    /// <summary>
    /// The Data field name and selector of the custom input class.
    /// </summary>
    public BitNameSelectorPair<TItem, object?> Data { get; set; } = new(nameof(BitNavBarItem.Data));

    /// <summary>
    /// The Dot field name and selector of the custom input class.
    /// </summary>
    public BitNameSelectorPair<TItem, bool?> Dot { get; set; } = new(nameof(BitNavBarItem.Dot));

    /// <summary>
    /// The Icon field name and selector of the custom input class.
    /// Maps to <see cref="BitNavBarItem.Icon"/> for external icon libraries.
    /// </summary>
    public BitNameSelectorPair<TItem, BitIconInfo?> Icon { get; set; } = new(nameof(BitNavBarItem.Icon));

    /// <summary>
    /// The IconName field name and selector of the custom input class.
    /// Maps to <see cref="BitNavBarItem.IconName"/> for built-in Fluent UI icons.
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
    /// The Match field name and selector of the custom input class.
    /// </summary>
    public BitNameSelectorPair<TItem, BitNavMatch?> Match { get; set; } = new(nameof(BitNavBarItem.Match));

    /// <summary>
    /// The SelectedIcon field name and selector of the custom input class.
    /// Maps to <see cref="BitNavBarItem.SelectedIcon"/> for external icon libraries.
    /// </summary>
    public BitNameSelectorPair<TItem, BitIconInfo?> SelectedIcon { get; set; } = new(nameof(BitNavBarItem.SelectedIcon));

    /// <summary>
    /// The SelectedIconName field name and selector of the custom input class.
    /// Maps to <see cref="BitNavBarItem.SelectedIconName"/> for built-in Fluent UI icons.
    /// </summary>
    public BitNameSelectorPair<TItem, string?> SelectedIconName { get; set; } = new(nameof(BitNavBarItem.SelectedIconName));

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
    /// The TemplateRenderMode field name and selector of the custom input class.
    /// </summary>
    public BitNameSelectorPair<TItem, BitNavItemTemplateRenderMode?> TemplateRenderMode { get; set; } = new(nameof(BitNavBarItem.TemplateRenderMode));

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
