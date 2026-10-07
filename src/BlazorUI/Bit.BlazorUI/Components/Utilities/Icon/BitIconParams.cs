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
    /// Overrides how long one cycle of the animation takes, as any CSS time.
    /// </summary>
    public string? AnimationDuration { get; set; }

    /// <summary>
    /// Waits this long before the animation starts, as any CSS time.
    /// </summary>
    public string? AnimationDelay { get; set; }

    /// <summary>
    /// How many times the animation plays before it stops. Left unset, it loops.
    /// </summary>
    public int? AnimationIterationCount { get; set; }

    /// <summary>
    /// Draws the icon in a circle rather than in the rounded box of the design system.
    /// </summary>
    public bool? Circular { get; set; }

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
    /// Names the icon set that the IconName of every icon of the subtree is a name in.
    /// </summary>
    public Func<string, BitIconInfo?>? IconResolver { get; set; }

    /// <summary>
    /// Drops the icon slightly below the baseline so that it sits centered on the line of text it is written in.
    /// </summary>
    public bool? Inline { get; set; }

    /// <summary>
    /// Turns the icon by a quarter, a half, or three quarters of a turn.
    /// </summary>
    public BitIconRotate? Rotate { get; set; }

    /// <summary>
    /// Turns the icon by an angle of your own, in degrees.
    /// </summary>
    public int? RotateAngle { get; set; }

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

        if (Animation.HasValue)
        {
            bitIcon.TakeFromCascade(nameof(Animation), Animation.Value, static i => i.Animation, static (i, v) => i.Animation = v);
        }

        if (AnimationDuration.HasValue())
        {
            bitIcon.TakeFromCascade(nameof(AnimationDuration), AnimationDuration, static i => i.AnimationDuration, static (i, v) => i.AnimationDuration = v);
        }

        if (AnimationDelay.HasValue())
        {
            bitIcon.TakeFromCascade(nameof(AnimationDelay), AnimationDelay, static i => i.AnimationDelay, static (i, v) => i.AnimationDelay = v);
        }

        if (AnimationIterationCount.HasValue)
        {
            bitIcon.TakeFromCascade(nameof(AnimationIterationCount), AnimationIterationCount.Value, static i => i.AnimationIterationCount, static (i, v) => i.AnimationIterationCount = v);
        }

        if (Circular.HasValue)
        {
            bitIcon.TakeFromCascade(nameof(Circular), Circular.Value, static i => i.Circular, static (i, v) => i.Circular = v);
        }

        if (Color.HasValue)
        {
            bitIcon.TakeFromCascade(nameof(Color), Color.Value, static i => i.Color, static (i, v) => i.Color = v);
        }

        if (FixedWidth.HasValue)
        {
            bitIcon.TakeFromCascade(nameof(FixedWidth), FixedWidth.Value, static i => i.FixedWidth, static (i, v) => i.FixedWidth = v);
        }

        if (Flip.HasValue)
        {
            bitIcon.TakeFromCascade(nameof(Flip), Flip.Value, static i => i.Flip, static (i, v) => i.Flip = v);
        }

        if (FlipRtl.HasValue)
        {
            bitIcon.TakeFromCascade(nameof(FlipRtl), FlipRtl.Value, static i => i.FlipRtl, static (i, v) => i.FlipRtl = v);
        }

        if (FontSize.HasValue())
        {
            bitIcon.TakeFromCascade(nameof(FontSize), FontSize, static i => i.FontSize, static (i, v) => i.FontSize = v);
        }

        if (IconResolver is not null)
        {
            bitIcon.TakeFromCascade(nameof(IconResolver), IconResolver, static i => i.IconResolver, static (i, v) => i.IconResolver = v);
        }

        if (Inline.HasValue)
        {
            bitIcon.TakeFromCascade(nameof(Inline), Inline.Value, static i => i.Inline, static (i, v) => i.Inline = v);
        }

        if (Rotate.HasValue)
        {
            bitIcon.TakeFromCascade(nameof(Rotate), Rotate.Value, static i => i.Rotate, static (i, v) => i.Rotate = v);
        }

        if (RotateAngle.HasValue)
        {
            bitIcon.TakeFromCascade(nameof(RotateAngle), RotateAngle.Value, static i => i.RotateAngle, static (i, v) => i.RotateAngle = v);
        }

        if (Size.HasValue)
        {
            bitIcon.TakeFromCascade(nameof(Size), Size.Value, static i => i.Size, static (i, v) => i.Size = v);
        }

        if (Variant.HasValue)
        {
            bitIcon.TakeFromCascade(nameof(Variant), Variant.Value, static i => i.Variant, static (i, v) => i.Variant = v);
        }
    }
}
