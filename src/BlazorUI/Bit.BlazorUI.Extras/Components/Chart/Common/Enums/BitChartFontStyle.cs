namespace Bit.BlazorUI;

/// <summary>
/// Specifies the css font style of text.
/// <para>As per documentation <a href="https://developer.mozilla.org/en-US/docs/Web/CSS/font-style">here (MDN)</a>.</para>
/// </summary>
public sealed class BitChartFontStyle : BitChartStringEnum
{
    /// <summary>
    /// Specifies a font that is classified as normal within a font-family.
    /// </summary>
    public static BitChartFontStyle Normal => new BitChartFontStyle("normal");

    /// <summary>
    /// Specifies a font that is classified as italic. If no italic version of the face is available, one classified as oblique is used instead. If neither is available, the style is artificially simulated.
    /// </summary>
    public static BitChartFontStyle Italic => new BitChartFontStyle("italic");

    /// <summary>
    /// Specifies a font that is classified as oblique. If no oblique version of the face is available, one classified as italic is used instead. If neither is available, the style is artificially simulated.
    /// </summary>
    public static BitChartFontStyle Oblique => new BitChartFontStyle("oblique");

    /// <summary>
    /// Specifies a font with its font style set to its default value.
    /// </summary>
    public static BitChartFontStyle Initial => new BitChartFontStyle("initial");

    /// <summary>
    /// Specifies a font that inherits the font style from its parent element.
    /// </summary>
    public static BitChartFontStyle Inherit => new BitChartFontStyle("inherit");

    /// <summary>
    /// Creates a new instance of the <see cref="BitChartFontStyle"/> class.
    /// </summary>
    /// <param name="stringValue">The <see cref="string"/> value to set.</param>
    private BitChartFontStyle(string stringValue) : base(stringValue) { }
}
