namespace Bit.BlazorUI;

/// <summary>
/// The parameters for the <see cref="BitIcon"/> component.
/// </summary>
public class BitIconParams : BitComponentBaseParams, IBitComponentParams
{
    /// <summary>
    /// Represents the parameter name used to identify the BitIcon cascading parameters within BitParams.
    /// </summary>
    /// <remarks>
    /// This constant is typically used when referencing or accessing the BitIcon value in
    /// parameterized APIs or configuration settings. Using this constant helps ensure consistency and reduces the risk
    /// of typographical errors.
    /// </remarks>
    public const string ParamName = $"{nameof(BitParams)}.{nameof(BitIcon)}";



    public string Name => ParamName;



    /// <summary>
    /// Specifies a looping animation to play on the icon.
    /// </summary>
    public BitIconAnimation? Animation { get; set; }

    /// <summary>
    /// Specifies the color theme of the icon.
    /// </summary>
    public BitColor? Color { get; set; }

    /// <summary>
    /// Renders the icon in a box of a fixed width so that a column of icons of different widths lines up.
    /// </summary>
    public bool? FixedWidth { get; set; }

    /// <summary>
    /// Mirrors the icon on the horizontal axis, the vertical axis, or both.
    /// </summary>
    public BitIconFlip? Flip { get; set; }

    /// <summary>
    /// Mirrors the icon horizontally when it is rendered in a right-to-left direction.
    /// </summary>
    public bool? FlipRtl { get; set; }

    /// <summary>
    /// Specifies the font size of the icon, as any CSS length or the <c>inherit</c> keyword.
    /// </summary>
    public string? FontSize { get; set; }

    /// <summary>
    /// Turns the icon by a quarter, a half, or three quarters of a turn.
    /// </summary>
    public BitIconRotate? Rotate { get; set; }

    /// <summary>
    /// Specifies the size of the icon.
    /// </summary>
    public BitSize? Size { get; set; }

    /// <summary>
    /// Specifies the visual styling variant of the icon.
    /// </summary>
    public BitVariant? Variant { get; set; }



    /// <summary>
    /// Updates the properties of the specified <see cref="BitIcon"/> instance with any values that have been set on
    /// this object, if those properties have not already been set on the <see cref="BitIcon"/>.
    /// </summary>
    /// <remarks>
    /// Only properties that have a value set and have not already been set on the <paramref name="bitIcon"/> will be updated.
    /// This method does not overwrite existing values on <paramref name="bitIcon"/>.
    /// <br />
    /// The icon itself is deliberately not among them: a default glyph shared by every icon of a
    /// subtree is a subtree of identical icons, which is never what was meant.
    /// </remarks>
    /// <param name="bitIcon">
    /// The <see cref="BitIcon"/> instance whose properties will be updated. Cannot be null.
    /// </param>
    public void UpdateParameters(BitIcon bitIcon)
    {
        if (bitIcon is null) return;

        UpdateBaseParameters(bitIcon);

        if (Animation.HasValue && bitIcon.HasNotBeenSet(nameof(Animation)))
        {
            bitIcon.Animation = Animation.Value;

            bitIcon.ClassBuilder.Reset();
        }

        if (Color.HasValue && bitIcon.HasNotBeenSet(nameof(Color)))
        {
            bitIcon.Color = Color.Value;

            bitIcon.ClassBuilder.Reset();
        }

        if (FixedWidth.HasValue && bitIcon.HasNotBeenSet(nameof(FixedWidth)))
        {
            bitIcon.FixedWidth = FixedWidth.Value;

            bitIcon.ClassBuilder.Reset();
        }

        if (Flip.HasValue && bitIcon.HasNotBeenSet(nameof(Flip)))
        {
            bitIcon.Flip = Flip.Value;

            bitIcon.ClassBuilder.Reset();
        }

        if (FlipRtl.HasValue && bitIcon.HasNotBeenSet(nameof(FlipRtl)))
        {
            bitIcon.FlipRtl = FlipRtl.Value;

            bitIcon.ClassBuilder.Reset();
        }

        if (FontSize.HasValue() && bitIcon.HasNotBeenSet(nameof(FontSize)))
        {
            bitIcon.FontSize = FontSize;

            bitIcon.StyleBuilder.Reset();
        }

        if (Rotate.HasValue && bitIcon.HasNotBeenSet(nameof(Rotate)))
        {
            bitIcon.Rotate = Rotate.Value;

            bitIcon.ClassBuilder.Reset();
        }

        if (Size.HasValue && bitIcon.HasNotBeenSet(nameof(Size)))
        {
            bitIcon.Size = Size.Value;

            bitIcon.ClassBuilder.Reset();
        }

        if (Variant.HasValue && bitIcon.HasNotBeenSet(nameof(Variant)))
        {
            bitIcon.Variant = Variant.Value;

            bitIcon.ClassBuilder.Reset();
        }
    }
}
