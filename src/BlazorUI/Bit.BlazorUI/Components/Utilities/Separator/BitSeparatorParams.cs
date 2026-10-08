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
    public BitPlacement? AlignContent { get; set; }

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
    public BitLineStyle? LineStyle { get; set; }

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

        if (AlignContent.HasValue)
        {
            bitSeparator.TakeFromCascade(nameof(AlignContent), AlignContent.Value, static s => s.AlignContent, static (s, v) => s.AlignContent = v);
        }

        if (AutoSize.HasValue)
        {
            bitSeparator.TakeFromCascade(nameof(AutoSize), AutoSize.Value, static s => s.AutoSize, static (s, v) => s.AutoSize = v);
        }

        if (Background.HasValue)
        {
            bitSeparator.TakeFromCascade(nameof(Background), Background.Value, static s => s.Background, static (s, v) => s.Background = v);
        }

        if (Border.HasValue)
        {
            bitSeparator.TakeFromCascade(nameof(Border), Border.Value, static s => s.Border, static (s, v) => s.Border = v);
        }

        if (Classes is not null)
        {
            bitSeparator.TakeFromCascade(nameof(Classes), Classes, static s => s.Classes, static (s, v) => s.Classes = v);
        }

        if (Color.HasValue)
        {
            bitSeparator.TakeFromCascade(nameof(Color), Color.Value, static s => s.Color, static (s, v) => s.Color = v);
        }

        if (ContentOffset.HasValue())
        {
            bitSeparator.TakeFromCascade(nameof(ContentOffset), ContentOffset, static s => s.ContentOffset, static (s, v) => s.ContentOffset = v);
        }

        if (Decorative.HasValue)
        {
            bitSeparator.TakeFromCascade(nameof(Decorative), Decorative.Value, static s => s.Decorative, static (s, v) => s.Decorative = v);
        }

        if (Element.HasValue())
        {
            bitSeparator.TakeFromCascade(nameof(Element), Element, static s => s.Element, static (s, v) => s.Element = v);
        }

        if (Inset.HasValue())
        {
            bitSeparator.TakeFromCascade(nameof(Inset), Inset, static s => s.Inset, static (s, v) => s.Inset = v);
        }

        if (LineStyle.HasValue)
        {
            bitSeparator.TakeFromCascade(nameof(LineStyle), LineStyle.Value, static s => s.LineStyle, static (s, v) => s.LineStyle = v);
        }

        if (Size.HasValue)
        {
            bitSeparator.TakeFromCascade(nameof(Size), Size.Value, static s => s.Size, static (s, v) => s.Size = v);
        }

        if (Styles is not null)
        {
            bitSeparator.TakeFromCascade(nameof(Styles), Styles, static s => s.Styles, static (s, v) => s.Styles = v);
        }

        if (Thickness.HasValue())
        {
            bitSeparator.TakeFromCascade(nameof(Thickness), Thickness, static s => s.Thickness, static (s, v) => s.Thickness = v);
        }

        if (Vertical.HasValue)
        {
            bitSeparator.TakeFromCascade(nameof(Vertical), Vertical.Value, static s => s.Vertical, static (s, v) => s.Vertical = v);
        }
    }
}
