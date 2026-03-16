namespace Bit.BlazorUI;

/// <summary>
/// The parameters for <see cref="BitTag"/> component.
/// </summary>
public class BitTagParams : BitComponentBaseParams, IBitComponentParams
{
    /// <summary>
    /// Represents the parameter name used to identify the <see cref="BitTag"/> cascading parameters within <see cref="BitParams"/>.
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
    /// The icon to show inside the tag using custom CSS classes for external icon libraries.
    /// Takes precedence over <see cref="IconName"/> when both are set.
    /// </summary>
    public BitIconInfo? Icon { get; set; }

    /// <summary>
    /// The icon to show inside the tag.
    /// </summary>
    public string? IconName { get; set; }

    /// <summary>
    /// The icon to use for the dismiss button using custom CSS classes for external icon libraries.
    /// Takes precedence over <see cref="DismissIconName"/> when both are set.
    /// </summary>
    public BitIconInfo? DismissIcon { get; set; }

    /// <summary>
    /// The name of the icon to use for the dismiss button from the built-in Fluent UI icons.
    /// </summary>
    public string? DismissIconName { get; set; }

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
    /// </param>
    public void UpdateParameters(BitTag bitTag)
    {
        if (bitTag is null) return;

        UpdateBaseParameters(bitTag);

        if (Classes is not null && bitTag.HasNotBeenSet(nameof(Classes)))
        {
            bitTag.Classes = Classes;

            bitTag.ClassBuilder.Reset();
        }

        if (Color.HasValue && bitTag.HasNotBeenSet(nameof(Color)))
        {
            bitTag.Color = Color.Value;
        }

        if (DismissIcon is not null && bitTag.HasNotBeenSet(nameof(DismissIcon)))
        {
            bitTag.DismissIcon = DismissIcon;
        }

        if (DismissIconName.HasValue() && bitTag.HasNotBeenSet(nameof(DismissIconName)))
        {
            bitTag.DismissIconName = DismissIconName;
        }

        if (Icon is not null && bitTag.HasNotBeenSet(nameof(Icon)))
        {
            bitTag.Icon = Icon;
        }

        if (IconName.HasValue() && bitTag.HasNotBeenSet(nameof(IconName)))
        {
            bitTag.IconName = IconName;
        }

        if (Reversed.HasValue && bitTag.HasNotBeenSet(nameof(Reversed)))
        {
            bitTag.Reversed = Reversed.Value;

            bitTag.ClassBuilder.Reset();
        }

        if (Size.HasValue && bitTag.HasNotBeenSet(nameof(Size)))
        {
            bitTag.Size = Size.Value;

            bitTag.ClassBuilder.Reset();
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

            bitTag.ClassBuilder.Reset();
        }
    }
}
