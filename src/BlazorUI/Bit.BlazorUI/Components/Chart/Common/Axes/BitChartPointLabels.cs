namespace Bit.BlazorUI;

/// <summary>
/// The point labels sub-config of the linear-radial-axis-configuration (see <see cref="BitChartLinearRadialAxis"/>).
/// <para>As per documentation <a href="https://www.chartjs.org/docs/latest/axes/radial/linear.html#point-label-options">here (Chart.js)</a>.</para>
/// </summary>
public class BitChartPointLabels
{
    /// <summary>
    /// Gets or sets the font color for a point label.
    /// <para>See <see cref="BitChartColorUtil"/> for working with colors.</para>
    /// </summary>
    public BitChartIndexableOption<string> FontColor { get; set; }

    /// <summary>
    /// Gets or sets the font size in pixels.
    /// </summary>
    public int? FontSize { get; set; }

    /// <summary>
    /// Gets or sets the font style to use when rendering a point label.
    /// </summary>
    public BitChartFontStyle FontStyle { get; set; }

    /// <summary>
    /// Gets or sets the height of an individual line of text.
    /// <para>As per documentation <a href="https://developer.mozilla.org/en-US/docs/Web/CSS/line-height">here (MDN)</a>.</para>
    /// </summary>
    public double? LineHeight { get; set; }
}
