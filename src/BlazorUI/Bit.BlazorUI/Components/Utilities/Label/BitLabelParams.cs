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

        if (Classes is not null)
        {
            bitLabel.TakeFromCascade(nameof(Classes), Classes, static l => l.Classes, static (l, v) => l.Classes = v);
        }

        if (Color.HasValue)
        {
            bitLabel.TakeFromCascade(nameof(Color), Color.Value, static l => l.Color, static (l, v) => l.Color = v);
        }

        if (Element.HasValue())
        {
            bitLabel.TakeFromCascade(nameof(Element), Element, static l => l.Element, static (l, v) => l.Element = v);
        }

        if (NoSelect.HasValue)
        {
            bitLabel.TakeFromCascade(nameof(NoSelect), NoSelect.Value, static l => l.NoSelect, static (l, v) => l.NoSelect = v);
        }

        if (NoWrap.HasValue)
        {
            bitLabel.TakeFromCascade(nameof(NoWrap), NoWrap.Value, static l => l.NoWrap, static (l, v) => l.NoWrap = v);
        }

        if (Optional.HasValue)
        {
            bitLabel.TakeFromCascade(nameof(Optional), Optional.Value, static l => l.Optional, static (l, v) => l.Optional = v);
        }

        if (OptionalText.HasValue())
        {
            bitLabel.TakeFromCascade(nameof(OptionalText), OptionalText, static l => l.OptionalText, static (l, v) => l.OptionalText = v);
        }

        if (Required.HasValue)
        {
            bitLabel.TakeFromCascade(nameof(Required), Required.Value, static l => l.Required, static (l, v) => l.Required = v);
        }

        if (RequiredText.HasValue())
        {
            bitLabel.TakeFromCascade(nameof(RequiredText), RequiredText, static l => l.RequiredText, static (l, v) => l.RequiredText = v);
        }

        if (Size.HasValue)
        {
            bitLabel.TakeFromCascade(nameof(Size), Size.Value, static l => l.Size, static (l, v) => l.Size = v);
        }

        if (Styles is not null)
        {
            bitLabel.TakeFromCascade(nameof(Styles), Styles, static l => l.Styles, static (l, v) => l.Styles = v);
        }

        if (VisuallyHidden.HasValue)
        {
            bitLabel.TakeFromCascade(nameof(VisuallyHidden), VisuallyHidden.Value, static l => l.VisuallyHidden, static (l, v) => l.VisuallyHidden = v);
        }
    }
}
