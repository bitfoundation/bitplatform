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
    /// Gets or sets a value indicating whether Gap, Size and MinGap apply to the block (vertical) axis instead of the inline (horizontal) one.
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

        if (Element.HasValue())
        {
            bitSpacer.TakeFromCascade(nameof(Element), Element, static s => s.Element, static (s, v) => s.Element = v);
        }

        if (Gap.HasValue())
        {
            bitSpacer.TakeFromCascade(nameof(Gap), Gap, static s => s.Gap, static (s, v) => s.Gap = v);
        }

        if (Grow.HasValue())
        {
            bitSpacer.TakeFromCascade(nameof(Grow), Grow, static s => s.Grow, static (s, v) => s.Grow = v);
        }

        if (Height.HasValue)
        {
            bitSpacer.TakeFromCascade(nameof(Height), Height.Value, static s => s.Height, static (s, v) => s.Height = v);
        }

        if (MinGap.HasValue())
        {
            bitSpacer.TakeFromCascade(nameof(MinGap), MinGap, static s => s.MinGap, static (s, v) => s.MinGap = v);
        }

        if (Size.HasValue)
        {
            bitSpacer.TakeFromCascade(nameof(Size), Size.Value, static s => s.Size, static (s, v) => s.Size = v);
        }

        if (Vertical.HasValue)
        {
            bitSpacer.TakeFromCascade(nameof(Vertical), Vertical.Value, static s => s.Vertical, static (s, v) => s.Vertical = v);
        }

        if (Width.HasValue)
        {
            bitSpacer.TakeFromCascade(nameof(Width), Width.Value, static s => s.Width, static (s, v) => s.Width = v);
        }
    }
}
