namespace Bit.BlazorUI;

/// <summary>
/// The parameters for the <see cref="BitSeparator"/> component.
/// </summary>
/// <remarks>
/// Every parameter of the separator but its content is here, and each is a default rather than an override: a
/// separator that sets a parameter for itself keeps its own value, and only what it left unset is filled in from the
/// cascade. What a subtree of separators usually shares is how they look - the color, the weight and the style of the
/// line, the inset of a list's rows - and how they stand, such as the vertical, decorative separators of a toolbar.
/// </remarks>
public class BitSeparatorParams : BitComponentBaseParams, IBitComponentParams
{
    /// <summary>
    /// Represents the parameter name used to identify the BitSeparator cascading parameters within BitParams.
    /// </summary>
    /// <remarks>
    /// This constant is typically used when referencing or accessing the BitSeparator value in
    /// parameterized APIs or configuration settings. Using this constant helps ensure consistency and reduces the risk
    /// of typographical errors.
    /// </remarks>
    public const string ParamName = $"{nameof(BitParams)}.{nameof(BitSeparator)}";



    public string Name => ParamName;



    /// <summary>
    /// Where the content should be aligned in the separator.
    /// <br />
    /// <see cref="BitSeparator.AlignContent"/>.
    /// </summary>
    public BitSeparatorAlignContent? AlignContent { get; set; }

    /// <summary>
    /// Renders the separator with auto width or height.
    /// <br />
    /// <see cref="BitSeparator.AutoSize"/>.
    /// </summary>
    public bool? AutoSize { get; set; }

    /// <summary>
    /// The color kind of the background of the content of the separator.
    /// <br />
    /// <see cref="BitSeparator.Background"/>.
    /// </summary>
    public BitColorKind? Background { get; set; }

    /// <summary>
    /// The color kind of the line of the separator, out of the neutral border tiers of the theme.
    /// <br />
    /// <see cref="BitSeparator.Border"/>.
    /// </summary>
    public BitColorKind? Border { get; set; }

    /// <summary>
    /// Custom CSS classes for different parts of the separator.
    /// <br />
    /// <see cref="BitSeparator.Classes"/>.
    /// </summary>
    public BitSeparatorClassStyles? Classes { get; set; }

    /// <summary>
    /// The general color of the line of the separator.
    /// <br />
    /// <see cref="BitSeparator.Color"/>.
    /// </summary>
    public BitColor? Color { get; set; }

    /// <summary>
    /// The offset of the content from the edge of the line it is aligned to, as any CSS length.
    /// <br />
    /// <see cref="BitSeparator.ContentOffset"/>.
    /// </summary>
    public string? ContentOffset { get; set; }

    /// <summary>
    /// Removes the separator from the accessibility tree, for a separator that is purely visual.
    /// <br />
    /// <see cref="BitSeparator.Decorative"/>.
    /// </summary>
    public bool? Decorative { get; set; }

    /// <summary>
    /// The custom html element used for the root node. The default is "div".
    /// <br />
    /// <see cref="BitSeparator.Element"/>.
    /// </summary>
    public string? Element { get; set; }

    /// <summary>
    /// Holds the separator off the ends of its container: one CSS length for both ends, or two for the start
    /// and the end.
    /// <br />
    /// <see cref="BitSeparator.Inset"/>.
    /// </summary>
    public string? Inset { get; set; }

    /// <summary>
    /// The style the line of the separator is drawn in: solid, dashed, dotted or double.
    /// <br />
    /// <see cref="BitSeparator.LineStyle"/>.
    /// </summary>
    public BitSeparatorLineStyle? LineStyle { get; set; }

    /// <summary>
    /// The size of the line of the separator, out of the sizes of the theme.
    /// <br />
    /// <see cref="BitSeparator.Size"/>.
    /// </summary>
    public BitSize? Size { get; set; }

    /// <summary>
    /// Custom CSS styles for different parts of the separator.
    /// <br />
    /// <see cref="BitSeparator.Styles"/>.
    /// </summary>
    public BitSeparatorClassStyles? Styles { get; set; }

    /// <summary>
    /// The thickness of the line of the separator, as any CSS length.
    /// <br />
    /// <see cref="BitSeparator.Thickness"/>.
    /// </summary>
    public string? Thickness { get; set; }

    /// <summary>
    /// Whether the element is a vertical separator.
    /// <br />
    /// <see cref="BitSeparator.Vertical"/>.
    /// </summary>
    public bool? Vertical { get; set; }



    /// <summary>
    /// Updates the properties of the specified <see cref="BitSeparator"/> instance with any values that have been set on
    /// this object, if those properties have not already been set on the <see cref="BitSeparator"/>.
    /// </summary>
    /// <remarks>
    /// Only properties that have a value set and have not already been set on the <paramref name="bitSeparator"/> will
    /// be updated. This method does not overwrite existing values on <paramref name="bitSeparator"/>.
    /// </remarks>
    /// <param name="bitSeparator">
    /// The <see cref="BitSeparator"/> instance whose properties will be updated. Cannot be null.
    /// </param>
    public void UpdateParameters(BitSeparator bitSeparator)
    {
        if (bitSeparator is null) return;

        UpdateBaseParameters(bitSeparator);

        if (AlignContent.HasValue && bitSeparator.HasNotBeenSet(nameof(AlignContent)))
        {
            bitSeparator.AlignContent = AlignContent.Value;

            bitSeparator.ClassBuilder.Reset();
        }

        if (AutoSize.HasValue && bitSeparator.HasNotBeenSet(nameof(AutoSize)))
        {
            bitSeparator.AutoSize = AutoSize.Value;

            bitSeparator.StyleBuilder.Reset();
        }

        if (Background.HasValue && bitSeparator.HasNotBeenSet(nameof(Background)))
        {
            bitSeparator.Background = Background.Value;

            bitSeparator.ClassBuilder.Reset();
        }

        if (Border.HasValue && bitSeparator.HasNotBeenSet(nameof(Border)))
        {
            bitSeparator.Border = Border.Value;

            bitSeparator.ClassBuilder.Reset();
        }

        if (Classes is not null && bitSeparator.HasNotBeenSet(nameof(Classes)))
        {
            bitSeparator.Classes = Classes;

            bitSeparator.ClassBuilder.Reset();
        }

        if (Color.HasValue && bitSeparator.HasNotBeenSet(nameof(Color)))
        {
            bitSeparator.Color = Color.Value;

            bitSeparator.ClassBuilder.Reset();
        }

        if (ContentOffset.HasValue() && bitSeparator.HasNotBeenSet(nameof(ContentOffset)))
        {
            bitSeparator.ContentOffset = ContentOffset;

            bitSeparator.StyleBuilder.Reset();
        }

        if (Decorative.HasValue && bitSeparator.HasNotBeenSet(nameof(Decorative)))
        {
            bitSeparator.Decorative = Decorative.Value;
        }

        if (Element.HasValue() && bitSeparator.HasNotBeenSet(nameof(Element)))
        {
            bitSeparator.Element = Element;
        }

        if (Inset.HasValue() && bitSeparator.HasNotBeenSet(nameof(Inset)))
        {
            bitSeparator.Inset = Inset;

            bitSeparator.StyleBuilder.Reset();
        }

        if (LineStyle.HasValue && bitSeparator.HasNotBeenSet(nameof(LineStyle)))
        {
            bitSeparator.LineStyle = LineStyle.Value;

            bitSeparator.ClassBuilder.Reset();
        }

        if (Size.HasValue && bitSeparator.HasNotBeenSet(nameof(Size)))
        {
            bitSeparator.Size = Size.Value;

            bitSeparator.ClassBuilder.Reset();
        }

        if (Styles is not null && bitSeparator.HasNotBeenSet(nameof(Styles)))
        {
            bitSeparator.Styles = Styles;

            bitSeparator.StyleBuilder.Reset();
        }

        if (Thickness.HasValue() && bitSeparator.HasNotBeenSet(nameof(Thickness)))
        {
            bitSeparator.Thickness = Thickness;

            bitSeparator.StyleBuilder.Reset();
        }

        if (Vertical.HasValue && bitSeparator.HasNotBeenSet(nameof(Vertical)))
        {
            bitSeparator.Vertical = Vertical.Value;

            bitSeparator.ClassBuilder.Reset();
            bitSeparator.StyleBuilder.Reset();
        }
    }
}
