namespace Bit.BlazorUI;

/// <summary>
/// The parameters for <see cref="BitTag"/> component.
/// </summary>
public class BitTagParams : IBitComponentParams
{
    /// <summary>
    /// Represents the parameter name used to identify the BitTag cascading parameters within BitParams.
    /// </summary>
    /// <remarks>
    /// This constant is typically used when referencing or accessing the BitTag value in
    /// parameterized APIs or configuration settings. Using this constant helps ensure consistency and reduces the risk
    /// of typographical errors.
    /// </remarks>
    public const string ParamName = $"{nameof(BitParams)}.{nameof(BitTag)}";



    public string Name => ParamName;



    /// <summary>
    /// Custom CSS classes for different parts of the tag.
    /// </summary>
    public BitTagClassStyles? Classes { get; set; }

    /// <summary>
    /// The general color of the tag.
    /// </summary>
    public BitColor? Color { get; set; }

    /// <summary>
    /// The icon to show inside the tag.
    /// </summary>
    public string? IconName { get; set; }

    /// <summary>
    /// Reverses the direction flow of the content of the tag.
    /// </summary>
    public bool? Reversed { get; set; }

    /// <summary>
    /// The size of the tag.
    /// </summary>
    public BitSize? Size { get; set; }

    /// <summary>
    /// Custom CSS styles for different parts of the tag.
    /// </summary>
    public BitTagClassStyles? Styles { get; set; }

    /// <summary>
    /// The text of the tag.
    /// </summary>
    public string? Text { get; set; }

    /// <summary>
    /// The visual variant of the tag.
    /// </summary>
    public BitVariant? Variant { get; set; }



    /// <summary>
    /// Updates the properties of the specified <see cref="BitTag"/> instance with any values that have been set on
    /// this object, if those properties have not already been set on the <see cref="BitTag"/>.
    /// </summary>
    /// <remarks>
    /// Only properties that have a value set and have not already been set on the <paramref name="bitTag"/> will be updated. 
    /// This method does not overwrite existing values on <paramref name="bitTag"/>.
    /// </remarks>
    /// <param name="bitTag">
    /// The <see cref="BitTag"/> instance whose properties will be updated. Cannot be null.
    /// <summary>
    /// Applies configured values from this BitTagParams to the specified BitTag instance.
    /// </summary>
    /// <param name="bitTag">The BitTag to update; only parameters that are set on this BitTagParams and not already set on the target will be applied. If <c>bitTag</c> is null, no action is taken.</param>
    public void UpdateParameters(BitTag bitTag)
    {
        if (bitTag is null) return;

        if (Classes is not null && bitTag.HasNotBeenSet(nameof(Classes)))
        {
            bitTag.Classes = Classes;
        }

        if (Color.HasValue && bitTag.HasNotBeenSet(nameof(Color)))
        {
            bitTag.Color = Color.Value;
        }

        if (IconName.HasValue() && bitTag.HasNotBeenSet(nameof(IconName)))
        {
            bitTag.IconName = IconName;
        }

        if (Reversed.HasValue && bitTag.HasNotBeenSet(nameof(Reversed)))
        {
            bitTag.Reversed = Reversed.Value;
        }

        if (Size.HasValue && bitTag.HasNotBeenSet(nameof(Size)))
        {
            bitTag.Size = Size.Value;
        }

        if (Styles is not null && bitTag.HasNotBeenSet(nameof(Styles)))
        {
            bitTag.Styles = Styles;
        }

        if (Text.HasValue() && bitTag.HasNotBeenSet(nameof(Text)))
        {
            bitTag.Text = Text;
        }

        if (Variant.HasValue && bitTag.HasNotBeenSet(nameof(Variant)))
        {
            bitTag.Variant = Variant.Value;
        }
    }
}