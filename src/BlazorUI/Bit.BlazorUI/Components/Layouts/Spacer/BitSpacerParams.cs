namespace Bit.BlazorUI;

/// <summary>
/// The parameters for <see cref="BitSpacer"/> component.
/// </summary>
public class BitSpacerParams : BitComponentBaseParams, IBitComponentParams
{
    /// <summary>
    /// Represents the parameter name used to identify the <see cref="BitSpacer"/> cascading parameters within <see cref="BitParams"/>.
    /// </summary>
    /// <remarks>
    /// This constant is typically used when referencing or accessing the BitSpacer value in
    /// parameterized APIs or configuration settings. Using this constant helps ensure consistency and reduces the risk
    /// of typographical errors.
    /// </remarks>
    public const string ParamName = $"{nameof(BitParams)}.{nameof(BitSpacer)}";



    public string Name => ParamName;



    /// <summary>
    /// Gets or sets the custom html element used for the root node. The default is "div".
    /// </summary>
    public string? Element { get; set; }

    /// <summary>
    /// Gets or sets the fixed amount of space the spacer generates, using any CSS length value.
    /// </summary>
    public string? Gap { get; set; }

    /// <summary>
    /// Gets or sets how much of the leftover space of the container this spacer takes compared to its siblings.
    /// </summary>
    public string? Grow { get; set; }

    /// <summary>
    /// Gets or sets the fixed amount of space the spacer generates along the block (vertical) axis, in pixels.
    /// </summary>
    public int? Height { get; set; }

    /// <summary>
    /// Gets or sets the smallest amount of space a flexible spacer keeps when the container runs out of room.
    /// </summary>
    public string? MinGap { get; set; }

    /// <summary>
    /// Gets or sets the fixed amount of space the spacer generates, picked from the spacing scale of the theme.
    /// </summary>
    public BitSize? Size { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether Gap and MinGap apply to the block (vertical) axis instead of the inline (horizontal) one.
    /// </summary>
    public bool? Vertical { get; set; }

    /// <summary>
    /// Gets or sets the fixed amount of space the spacer generates along the inline (horizontal) axis, in pixels.
    /// </summary>
    public int? Width { get; set; }



    /// <summary>
    /// Updates the properties of the specified <see cref="BitSpacer"/> instance with any values that have been set on
    /// this object, if those properties have not already been set on the <see cref="BitSpacer"/>.
    /// </summary>
    /// <remarks>
    /// Only properties that have a value set and have not already been set on the <paramref name="bitSpacer"/> will be updated.
    /// This method does not overwrite existing values on <paramref name="bitSpacer"/>.
    /// </remarks>
    /// <param name="bitSpacer">
    /// The <see cref="BitSpacer"/> instance whose properties will be updated. Cannot be null.
    /// </param>
    public void UpdateParameters(BitSpacer bitSpacer)
    {
        if (bitSpacer is null) return;

        UpdateBaseParameters(bitSpacer);

        if (Element.HasValue() && bitSpacer.HasNotBeenSet(nameof(Element)))
        {
            bitSpacer.Element = Element;
        }

        if (Gap.HasValue() && bitSpacer.HasNotBeenSet(nameof(Gap)))
        {
            bitSpacer.Gap = Gap;

            bitSpacer.StyleBuilder.Reset();
        }

        if (Grow.HasValue() && bitSpacer.HasNotBeenSet(nameof(Grow)))
        {
            bitSpacer.Grow = Grow;

            bitSpacer.StyleBuilder.Reset();
        }

        if (Height.HasValue && bitSpacer.HasNotBeenSet(nameof(Height)))
        {
            bitSpacer.Height = Height.Value;

            bitSpacer.StyleBuilder.Reset();
        }

        if (MinGap.HasValue() && bitSpacer.HasNotBeenSet(nameof(MinGap)))
        {
            bitSpacer.MinGap = MinGap;

            bitSpacer.StyleBuilder.Reset();
        }

        if (Size.HasValue && bitSpacer.HasNotBeenSet(nameof(Size)))
        {
            bitSpacer.Size = Size.Value;

            bitSpacer.ClassBuilder.Reset();
            bitSpacer.StyleBuilder.Reset();
        }

        if (Vertical.HasValue && bitSpacer.HasNotBeenSet(nameof(Vertical)))
        {
            bitSpacer.Vertical = Vertical.Value;

            bitSpacer.StyleBuilder.Reset();
        }

        if (Width.HasValue && bitSpacer.HasNotBeenSet(nameof(Width)))
        {
            bitSpacer.Width = Width.Value;

            bitSpacer.StyleBuilder.Reset();
        }
    }
}
