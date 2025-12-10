using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Bit.BlazorUI;

/// <summary>
/// The parameters for <see cref="BitText"/> component.
/// </summary>
public class BitTextParams : BitComponentBaseParams, IBitComponentParams
{
    /// <summary>
    /// Represents the parameter name used to identify the BitText cascading parameters within BitParams.
    /// </summary>
    /// <remarks>
    /// This constant is typically used when referencing or accessing the BitText value in
    /// parameterized APIs or configuration settings. Using this constant helps ensure consistency and reduces the risk
    /// of typographical errors.
    /// </remarks>
    public const string ParamName = $"{nameof(BitParams)}.{nameof(BitText)}";



    public string Name => ParamName;



    /// <summary>
    /// Sets the horizontal alignment of the text content.
    /// </summary>
    public BitTextAlign? Align { get; set; }

    /// <summary>
    /// The general color of the text.
    /// </summary>
    public BitColor? Color { get; set; }

    /// <summary>
    /// The custom html element used for the root node.
    /// </summary>
    public string? Element { get; set; }

    /// <summary>
    /// Forces the text to always break at the end.
    /// </summary>
    public bool? ForceBreak { get; set; }

    /// <summary>
    /// The kind of the foreground color of the text.
    /// </summary>
    public BitColorKind? Foreground { get; set; }

    /// <summary>
    /// If true, the text will have a bottom margin.
    /// </summary>
    public bool? Gutter { get; set; }

    /// <summary>
    /// If true, the text will not wrap, but instead will truncate with a text overflow ellipsis.
    /// Note that text overflow can only happen with block or inline-block level elements(the element needs to have a width in order to overflow).
    /// </summary>
    public bool? NoWrap { get; set; }

    /// <summary>
    /// The typography of the text.
    /// </summary>
    public BitTypography? Typography { get; set; }



    /// <summary>
    /// Updates the properties of the specified <see cref="BitText"/> instance with any values that have been set on
    /// this object, if those properties have not already been set on the <see cref="BitText"/>.
    /// </summary>
    /// <remarks>
    /// Only properties that have a value set and have not already been set on the <paramref name="bitText"/> will be updated. 
    /// This method does not overwrite existing values on <paramref name="bitText"/>.
    /// </remarks>
    /// <param name="bitText">
    /// The <see cref="BitText"/> instance whose properties will be updated. Cannot be null.
    /// </param>
    public void UpdateParameters(BitText bitText)
    {
        if (bitText is null) return;

        UpdateBaseParameters(bitText);

        if (Align.HasValue && bitText.HasNotBeenSet(nameof(Align)))
        {
            bitText.Align = Align.Value;

            bitText.StyleBuilder.Reset();
        }

        if (Color.HasValue && bitText.HasNotBeenSet(nameof(Color)))
        {
            bitText.Color = Color.Value;

            bitText.ClassBuilder.Reset();
        }

        if (Element.HasValue() && bitText.HasNotBeenSet(nameof(Element)))
        {
            bitText.Element = Element;
        }

        if (ForceBreak.HasValue && bitText.HasNotBeenSet(nameof(ForceBreak)))
        {
            bitText.ForceBreak = ForceBreak.Value;

            bitText.ClassBuilder.Reset();
        }

        if (Foreground.HasValue && bitText.HasNotBeenSet(nameof(Foreground)))
        {
            bitText.Foreground = Foreground.Value;

            bitText.ClassBuilder.Reset();
        }

        if (Gutter.HasValue && bitText.HasNotBeenSet(nameof(Gutter)))
        {
            bitText.Gutter = Gutter.Value;

            bitText.ClassBuilder.Reset();
        }

        if (NoWrap.HasValue && bitText.HasNotBeenSet(nameof(NoWrap)))
        {
            bitText.NoWrap = NoWrap.Value;

            bitText.ClassBuilder.Reset();
        }

        if (Typography.HasValue && bitText.HasNotBeenSet(nameof(Typography)))
        {
            bitText.Typography = Typography.Value;

            bitText.ClassBuilder.Reset();
        }
    }
}
