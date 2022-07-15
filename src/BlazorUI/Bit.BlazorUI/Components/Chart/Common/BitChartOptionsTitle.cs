namespace Bit.BlazorUI;

/// <summary>
/// The title-subconfig of <see cref="BitChartBaseConfigOptions"/>. Specifies how the chart title is displayed.
/// </summary>
public class BitChartOptionsTitle
{
    /// <summary>
    /// Gets or sets a value indicating whether the title should be displayed or not.
    /// </summary>
    public bool? Display { get; set; }

    /// <summary>
    /// Gets or sets the position of the title.
    /// </summary>
    public BitChartPosition Position { get; set; }

    /// <summary>
    /// Gets or sets the font size for the title text.
    /// </summary>
    public int? FontSize { get; set; }

    /// <summary>
    /// Gets or sets the font family for the title text.
    /// </summary>
    public string FontFamily { get; set; }

    /// <summary>
    /// Gets or sets the font color for the title text.
    /// <para>See <see cref="BitChartColorUtil"/> for working with colors.</para>
    /// </summary>
    public string FontColor { get; set; }

    /// <summary>
    /// Gets or sets the font style for the title text.
    /// </summary>
    public BitChartFontStyle FontStyle { get; set; }

    /// <summary>
    /// Gets or sets the number of pixels to add above and below the title text.
    /// </summary>
    public int? Padding { get; set; }

    /// <summary>
    /// Gets or sets the height of an individual line of text.
    /// <para>As per documentation <a href="https://developer.mozilla.org/en-US/docs/Web/CSS/line-height">here (MDN)</a>.</para>
    /// </summary>
    public double? LineHeight { get; set; }

    /// <summary>
    /// Gets or sets the title text to display. If specified as an array, text is rendered on multiple lines.
    /// </summary>
    public BitChartIndexableOption<string> Text { get; set; }
}
