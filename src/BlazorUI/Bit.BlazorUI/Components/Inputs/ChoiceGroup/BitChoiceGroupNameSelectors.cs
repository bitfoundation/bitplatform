namespace Bit.BlazorUI;

public class BitChoiceGroupNameSelectors<TItem, TValue>
{
    /// <summary>
    /// The AriaLabel field name and selector of the custom input class.
    /// </summary>
    public BitNameSelectorPair<TItem, string?> AriaLabel { get; set; } = new(nameof(BitChoiceGroupItem<TValue>.AriaLabel));

    /// <summary>
    /// The CSS class field name and selector of the custom input class.
    /// </summary>
    public BitNameSelectorPair<TItem, string?> Class { get; set; } = new(nameof(BitChoiceGroupItem<TValue>.Class));

    /// <summary>
    /// The Id field name and selector of the custom input class.
    /// </summary>
    public BitNameSelectorPair<TItem, string?> Id { get; set; } = new(nameof(BitChoiceGroupItem<TValue>.Id));

    /// <summary>
    /// The IsEnabled field name and selector of the custom input class.
    /// </summary>
    public BitNameSelectorPair<TItem, bool> IsEnabled { get; set; } = new(nameof(BitChoiceGroupItem<TValue>.IsEnabled));

    /// <summary>
    /// The IconName field name and selector of the custom input class.
    /// </summary>
    public BitNameSelectorPair<TItem, string?> IconName { get; set; } = new(nameof(BitChoiceGroupItem<TValue>.IconName));

    /// <summary>
    /// The ImageSrc field name and selector of the custom input class.
    /// </summary>
    public BitNameSelectorPair<TItem, string?> ImageSrc { get; set; } = new(nameof(BitChoiceGroupItem<TValue>.ImageSrc));

    /// <summary>
    /// The ImageAlt field name and selector of the custom input class.
    /// </summary>
    public BitNameSelectorPair<TItem, string?> ImageAlt { get; set; } = new(nameof(BitChoiceGroupItem<TValue>.ImageAlt));

    /// <summary>
    /// The ImageSize field name and selector of the custom input class.
    /// </summary>
    public BitNameSelectorPair<TItem, BitImageSize?> ImageSize { get; set; } = new(nameof(BitChoiceGroupItem<TValue>.ImageSize));

    /// <summary>
    /// The Prefix field name and selector of the custom input class.
    /// </summary>
    public BitNameSelectorPair<TItem, string?> Prefix { get; set; } = new(nameof(BitChoiceGroupItem<TValue>.Prefix));

    /// <summary>
    /// The SelectedImageSrc field name and selector of the custom input class.
    /// </summary>
    public BitNameSelectorPair<TItem, string?> SelectedImageSrc { get; set; } = new(nameof(BitChoiceGroupItem<TValue>.SelectedImageSrc));

    /// <summary>
    /// The CSS style field name and selector of the custom input class.
    /// </summary>
    public BitNameSelectorPair<TItem, string?> Style { get; set; } = new(nameof(BitChoiceGroupItem<TValue>.Style));

    /// <summary>
    /// Template field name and selector of the custom input class.
    /// </summary>
    public BitNameSelectorPair<TItem, RenderFragment<TItem>?> Template { get; set; } = new(nameof(BitChoiceGroupItem<TValue>.Template));

    /// <summary>
    /// The Text field name and selector of the custom input class.
    /// </summary>
    public BitNameSelectorPair<TItem, string?> Text { get; set; } = new(nameof(BitChoiceGroupItem<TValue>.Text));

    /// <summary>
    /// The Value field name and selector of the custom input class.
    /// </summary>
    public BitNameSelectorPair<TItem, TValue?> Value { get; set; } = new(nameof(BitChoiceGroupItem<TValue>.Value));



    /// <summary>
    /// The Index field name of the custom input class. This property's value is set by the component at render.
    /// </summary>
    public string Index { get; set; } = nameof(BitChoiceGroupItem<TValue>.Index);

    /// <summary>
    /// The IsSelected field name of the custom input class. This property's value is assigned by the component.
    /// </summary>
    public string IsSelected { get; set; } = nameof(BitChoiceGroupItem<TValue>.IsSelected);
}
