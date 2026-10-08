namespace Bit.BlazorUI;

/// <summary>
/// The parameters for <see cref="BitChoiceGroup{TItem, TValue}"/> component.
/// </summary>
/// <remarks>
/// The choice group is generic over its item and its value types, but the parameters that are worth sharing
/// between the choice groups of a page are not: the ones typed over TItem or TValue - the items themselves,
/// the templates rendering one, the comparer, the name selectors and the event callbacks - stay on the
/// instance, which is what keeps this object usable from a single non-generic <see cref="BitParams"/> list
/// no matter which of the three item APIs each choice group under it uses.
/// </remarks>
public class BitChoiceGroupParams : BitComponentBaseParams, IBitComponentParams
{
    /// <summary>
    /// Represents the parameter name used to identify the <see cref="BitChoiceGroup{TItem, TValue}"/> cascading parameters within <see cref="BitParams"/>.
    /// </summary>
    /// <remarks>
    /// This constant is typically used when referencing or accessing the BitChoiceGroup value in
    /// parameterized APIs or configuration settings. Using this constant helps ensure consistency and reduces the risk
    /// of typographical errors.
    /// </remarks>
    public const string ParamName = $"{nameof(BitParams)}.{nameof(BitChoiceGroup<object, object>)}";



    public string Name => ParamName;



    /// <summary>
    /// Id of an element to use as the aria label for the ChoiceGroup.
    /// </summary>
    public string? AriaLabelledBy { get; set; }

    /// <summary>
    /// Determines if the ChoiceGroup is auto focused on first render, focusing its checked item, or its first
    /// enabled item when nothing is checked. Nothing is focused when the ChoiceGroup is read-only or the
    /// target item is disabled.
    /// </summary>
    public bool? AutoFocus { get; set; }

    /// <summary>
    /// Keeps the assigned Index of each option in sync with the markup order of the options, even when
    /// an option is added, removed, or reordered conditionally after the first render. It only affects the
    /// options API (ChildContent/Options); the items API already follows the order of the Items collection.
    /// </summary>
    public bool? AutoReorderOptions { get; set; }

    /// <summary>
    /// Custom CSS classes for different parts of the BitChoiceGroup.
    /// </summary>
    public BitChoiceGroupClassStyles? Classes { get; set; }

    /// <summary>
    /// The general color of the BitChoiceGroup.
    /// </summary>
    public BitColor? Color { get; set; }

    /// <summary>
    /// The description (helper text) of the ChoiceGroup, rendered under its label. The group references it
    /// through its aria-describedby, so screen readers announce it along with the name of the group.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Custom RenderFragment for the description (helper text) of the ChoiceGroup.
    /// Takes precedence over <see cref="Description"/> when both are set.
    /// </summary>
    public RenderFragment? DescriptionTemplate { get; set; }

    /// <summary>
    /// Expands the ChoiceGroup to the full width of its container instead of hugging its widest item.
    /// In the horizontal layout the items also share that width equally.
    /// </summary>
    public bool? FullWidth { get; set; }

    /// <summary>
    /// The gap between the items of the ChoiceGroup.
    /// </summary>
    public string? Gap { get; set; }

    /// <summary>
    /// Renders the items in the ChoiceGroup horizontally.
    /// </summary>
    public bool? Horizontal { get; set; }

    /// <summary>
    /// Renders the icons and images in a single line with the items in the ChoiceGroup.
    /// </summary>
    public bool? Inline { get; set; }

    /// <summary>
    /// The label for the ChoiceGroup.
    /// </summary>
    public string? Label { get; set; }

    /// <summary>
    /// The position of the content of each item relative to its radio circle. The default is
    /// <see cref="BitPlacement.End"/>, which renders the circle first and the content after it.
    /// Items rendered as image or icon tiles lay their own content out and ignore this parameter.
    /// </summary>
    public BitPlacement? LabelPlacement { get; set; }

    /// <summary>
    /// Custom RenderFragment for the label of the ChoiceGroup.
    /// </summary>
    public RenderFragment? LabelTemplate { get; set; }

    /// <summary>
    /// Removes the circle from the start of each item.
    /// </summary>
    public bool? NoCircle { get; set; }

    /// <summary>
    /// The size of the BitChoiceGroup.
    /// </summary>
    public BitSize? Size { get; set; }

    /// <summary>
    /// Stretches the label of each item over the full width of its row and spreads its content, which puts
    /// the circle at the far edge of the row instead of right beside the item text.
    /// </summary>
    public bool? StretchItemLabel { get; set; }

    /// <summary>
    /// Custom CSS styles for different parts of the BitChoiceGroup.
    /// </summary>
    public BitChoiceGroupClassStyles? Styles { get; set; }

    /// <summary>
    /// The visual style of the items of the ChoiceGroup. The default is <see cref="BitVariant.Text"/>, which
    /// draws no surface at all and renders each item as a bare radio row.
    /// </summary>
    public BitVariant? Variant { get; set; }



    /// <summary>
    /// Updates the properties of the specified <see cref="BitChoiceGroup{TItem, TValue}"/> instance with any values that have been set on
    /// this object, if those properties have not already been set on the <see cref="BitChoiceGroup{TItem, TValue}"/>.
    /// </summary>
    /// <remarks>
    /// Only properties that have a value set and have not already been set on the <paramref name="bitChoiceGroup"/> will be updated.
    /// This method does not overwrite existing values on <paramref name="bitChoiceGroup"/>.
    /// </remarks>
    /// <param name="bitChoiceGroup">
    /// The <see cref="BitChoiceGroup{TItem, TValue}"/> instance whose properties will be updated. Cannot be null.
    /// </param>
    public void UpdateParameters<TItem, TValue>(BitChoiceGroup<TItem, TValue> bitChoiceGroup) where TItem : class, new()
    {
        if (bitChoiceGroup is null) return;

        UpdateBaseParameters(bitChoiceGroup);

        if (AriaLabelledBy.HasValue())
        {
            bitChoiceGroup.TakeFromCascade(nameof(AriaLabelledBy), AriaLabelledBy, static c => c.AriaLabelledBy, static (c, v) => c.AriaLabelledBy = v);
        }

        if (AutoFocus.HasValue)
        {
            bitChoiceGroup.TakeFromCascade(nameof(AutoFocus), AutoFocus.Value, static c => c.AutoFocus, static (c, v) => c.AutoFocus = v);
        }

        if (AutoReorderOptions.HasValue)
        {
            bitChoiceGroup.TakeFromCascade(nameof(AutoReorderOptions), AutoReorderOptions.Value, static c => c.AutoReorderOptions, static (c, v) => c.AutoReorderOptions = v);
        }

        if (Classes is not null)
        {
            bitChoiceGroup.TakeFromCascade(nameof(Classes), Classes, static c => c.Classes, static (c, v) => c.Classes = v);
        }

        if (Color.HasValue)
        {
            bitChoiceGroup.TakeFromCascade(nameof(Color), Color.Value, static c => c.Color, static (c, v) => c.Color = v);
        }

        if (Description.HasValue())
        {
            bitChoiceGroup.TakeFromCascade(nameof(Description), Description, static c => c.Description, static (c, v) => c.Description = v);
        }

        if (DescriptionTemplate is not null)
        {
            bitChoiceGroup.TakeFromCascade(nameof(DescriptionTemplate), DescriptionTemplate, static c => c.DescriptionTemplate, static (c, v) => c.DescriptionTemplate = v);
        }

        if (FullWidth.HasValue)
        {
            bitChoiceGroup.TakeFromCascade(nameof(FullWidth), FullWidth.Value, static c => c.FullWidth, static (c, v) => c.FullWidth = v);
        }

        if (Gap.HasValue())
        {
            bitChoiceGroup.TakeFromCascade(nameof(Gap), Gap, static c => c.Gap, static (c, v) => c.Gap = v);
        }

        if (Horizontal.HasValue)
        {
            bitChoiceGroup.TakeFromCascade(nameof(Horizontal), Horizontal.Value, static c => c.Horizontal, static (c, v) => c.Horizontal = v);
        }

        if (Inline.HasValue)
        {
            bitChoiceGroup.TakeFromCascade(nameof(Inline), Inline.Value, static c => c.Inline, static (c, v) => c.Inline = v);
        }

        if (Label.HasValue())
        {
            bitChoiceGroup.TakeFromCascade(nameof(Label), Label, static c => c.Label, static (c, v) => c.Label = v);
        }

        if (LabelPlacement.HasValue)
        {
            bitChoiceGroup.TakeFromCascade(nameof(LabelPlacement), LabelPlacement.Value, static c => c.LabelPlacement, static (c, v) => c.LabelPlacement = v);
        }

        if (LabelTemplate is not null)
        {
            bitChoiceGroup.TakeFromCascade(nameof(LabelTemplate), LabelTemplate, static c => c.LabelTemplate, static (c, v) => c.LabelTemplate = v);
        }

        if (NoCircle.HasValue)
        {
            bitChoiceGroup.TakeFromCascade(nameof(NoCircle), NoCircle.Value, static c => c.NoCircle, static (c, v) => c.NoCircle = v);
        }

        if (Size.HasValue)
        {
            bitChoiceGroup.TakeFromCascade(nameof(Size), Size.Value, static c => c.Size, static (c, v) => c.Size = v);
        }

        if (StretchItemLabel.HasValue)
        {
            bitChoiceGroup.TakeFromCascade(nameof(StretchItemLabel), StretchItemLabel.Value, static c => c.StretchItemLabel, static (c, v) => c.StretchItemLabel = v);
        }

        if (Styles is not null)
        {
            bitChoiceGroup.TakeFromCascade(nameof(Styles), Styles, static c => c.Styles, static (c, v) => c.Styles = v);
        }

        if (Variant.HasValue)
        {
            bitChoiceGroup.TakeFromCascade(nameof(Variant), Variant.Value, static c => c.Variant, static (c, v) => c.Variant = v);
        }
    }
}
