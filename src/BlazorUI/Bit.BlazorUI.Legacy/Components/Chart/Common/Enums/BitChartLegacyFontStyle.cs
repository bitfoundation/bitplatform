namespace Bit.BlazorUI.Legacy;

/// <summary>
/// Specifies the css font style of text.
/// <para>As per documentation <a href="https://developer.mozilla.org/en-US/docs/Web/CSS/font-style">here (MDN)</a>.</para>
/// </summary>
public sealed class BitChartLegacyFontStyle : BitChartLegacyStringEnum
{
    /// <summary>
    /// Specifies a font that is classified as normal within a font-family.
    /// </summary>
    public static BitChartLegacyFontStyle Normal => new BitChartLegacyFontStyle("normal");

    /// <summary>
    /// Specifies a font that is classified as italic. If no italic version of the face is available, one classified as oblique is used instead. If neither is available, the style is artificially simulated.
    /// </summary>
    public static BitChartLegacyFontStyle Italic => new BitChartLegacyFontStyle("italic");

    /// <summary>
    /// Specifies a font that is classified as oblique. If no oblique version of the face is available, one classified as italic is used instead. If neither is available, the style is artificially simulated.
    /// </summary>
    public static BitChartLegacyFontStyle Oblique => new BitChartLegacyFontStyle("oblique");

    /// <summary>
    /// Specifies a font with its font style set to its default value.
    /// </summary>
    public static BitChartLegacyFontStyle Initial => new BitChartLegacyFontStyle("initial");

    /// <summary>
    /// Specifies a font that inherits the font style from its parent element.
    /// </summary>
    public static BitChartLegacyFontStyle Inherit => new BitChartLegacyFontStyle("inherit");

    /// <summary>
    /// Creates a new instance of the <see cref="BitChartLegacyFontStyle"/> class.
    /// </summary>
    /// <param name="stringValue">The <see cref="string"/> value to set.</param>
    private BitChartLegacyFontStyle(string stringValue) : base(stringValue) { }
}
