namespace Bit.BlazorUI;

/// <summary>
/// The parameters for the <see cref="BitLabel"/> component.
/// </summary>
public class BitLabelParams : BitComponentBaseParams, IBitComponentParams
{
    /// <summary>
    /// Represents the parameter name used to identify the BitLabel cascading parameters within BitParams.
    /// </summary>
    /// <remarks>
    /// This constant is typically used when referencing or accessing the BitLabel value in
    /// parameterized APIs or configuration settings. Using this constant helps ensure consistency and reduces the risk
    /// of typographical errors.
    /// </remarks>
    public const string ParamName = $"{nameof(BitParams)}.{nameof(BitLabel)}";



    public string Name => ParamName;



    /// <summary>
    /// Custom CSS classes for the different parts of the label.
    /// </summary>
    public BitLabelClassStyles? Classes { get; set; }

    /// <summary>
    /// The general color of the label.
    /// </summary>
    public BitColor? Color { get; set; }

    /// <summary>
    /// The custom html element used for the root node. The default is "label".
    /// </summary>
    public string? Element { get; set; }

    /// <summary>
    /// Prevents the text of the label from being selected.
    /// </summary>
    public bool? NoSelect { get; set; }

    /// <summary>
    /// Keeps the label on a single line and truncates the overflow with an ellipsis.
    /// </summary>
    public bool? NoWrap { get; set; }

    /// <summary>
    /// Whether the associated field is optional, which renders an indicator after the content of the label.
    /// </summary>
    public bool? Optional { get; set; }

    /// <summary>
    /// The text of the optional indicator of the label. The default is "(optional)".
    /// </summary>
    public string? OptionalText { get; set; }

    /// <summary>
    /// Whether the associated field is required, which renders an indicator after the content of the label.
    /// </summary>
    public bool? Required { get; set; }

    /// <summary>
    /// The text of the required indicator of the label. The default is "*".
    /// </summary>
    public string? RequiredText { get; set; }

    /// <summary>
    /// The size of the label.
    /// </summary>
    public BitSize? Size { get; set; }

    /// <summary>
    /// Custom CSS styles for the different parts of the label.
    /// </summary>
    public BitLabelClassStyles? Styles { get; set; }

    /// <summary>
    /// Removes the label from the page while keeping it available to assistive technologies.
    /// </summary>
    public bool? VisuallyHidden { get; set; }



    /// <summary>
    /// Updates the properties of the specified <see cref="BitLabel"/> instance with any values that have been set on
    /// this object, if those properties have not already been set on the <see cref="BitLabel"/>.
    /// </summary>
    /// <remarks>
    /// Only properties that have a value set and have not already been set on the <paramref name="bitLabel"/> will be updated.
    /// This method does not overwrite existing values on <paramref name="bitLabel"/>.
    /// </remarks>
    /// <param name="bitLabel">
    /// The <see cref="BitLabel"/> instance whose properties will be updated. Cannot be null.
    /// </param>
    public void UpdateParameters(BitLabel bitLabel)
    {
        if (bitLabel is null) return;

        UpdateBaseParameters(bitLabel);

        if (Classes is not null && bitLabel.HasNotBeenSet(nameof(Classes)))
        {
            bitLabel.Classes = Classes;

            bitLabel.ClassBuilder.Reset();
        }

        if (Color.HasValue && bitLabel.HasNotBeenSet(nameof(Color)))
        {
            bitLabel.Color = Color.Value;

            bitLabel.ClassBuilder.Reset();
        }

        if (Element.HasValue() && bitLabel.HasNotBeenSet(nameof(Element)))
        {
            bitLabel.Element = Element;
        }

        if (NoSelect.HasValue && bitLabel.HasNotBeenSet(nameof(NoSelect)))
        {
            bitLabel.NoSelect = NoSelect.Value;

            bitLabel.ClassBuilder.Reset();
        }

        if (NoWrap.HasValue && bitLabel.HasNotBeenSet(nameof(NoWrap)))
        {
            bitLabel.NoWrap = NoWrap.Value;

            bitLabel.ClassBuilder.Reset();
        }

        if (Optional.HasValue && bitLabel.HasNotBeenSet(nameof(Optional)))
        {
            bitLabel.Optional = Optional.Value;

            bitLabel.ClassBuilder.Reset();
        }

        if (OptionalText.HasValue() && bitLabel.HasNotBeenSet(nameof(OptionalText)))
        {
            bitLabel.OptionalText = OptionalText;
        }

        if (Required.HasValue && bitLabel.HasNotBeenSet(nameof(Required)))
        {
            bitLabel.Required = Required.Value;

            bitLabel.ClassBuilder.Reset();
        }

        if (RequiredText.HasValue() && bitLabel.HasNotBeenSet(nameof(RequiredText)))
        {
            bitLabel.RequiredText = RequiredText;
        }

        if (Size.HasValue && bitLabel.HasNotBeenSet(nameof(Size)))
        {
            bitLabel.Size = Size.Value;

            bitLabel.ClassBuilder.Reset();
        }

        if (Styles is not null && bitLabel.HasNotBeenSet(nameof(Styles)))
        {
            bitLabel.Styles = Styles;

            bitLabel.StyleBuilder.Reset();
        }

        if (VisuallyHidden.HasValue && bitLabel.HasNotBeenSet(nameof(VisuallyHidden)))
        {
            bitLabel.VisuallyHidden = VisuallyHidden.Value;

            bitLabel.ClassBuilder.Reset();
        }
    }
}
