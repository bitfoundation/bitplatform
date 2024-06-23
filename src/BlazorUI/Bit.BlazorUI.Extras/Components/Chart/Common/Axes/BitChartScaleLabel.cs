namespace Bit.BlazorUI;

/// <summary>
/// Defines options for how to display an axis title.
/// </summary>
public class BitChartScaleLabel
{
    /// <summary>
    /// Creates a new instance of <see cref="BitChartScaleLabel"/>
    /// </summary>
    /// <param name="labelString">The initial value for <see cref="LabelString"/></param>
    public BitChartScaleLabel(string labelString = null)
    {
        LabelString = labelString;
    }

    /// <summary>
    /// If true, display the axis title.
    /// </summary>
    public bool? Display { get; set; }

    /// <summary>
    /// Gets or sets the text for the title (i.e. "# of clicks").
    /// </summary>
    public string LabelString { get; set; }

    /// <summary>
    /// Gets or sets the font color of the label.
    /// <para>See <see cref="BitChartColorUtil"/> for working with colors.</para>
    /// </summary>
    public string FontColor { get; set; }

    /// <summary>
    /// Gets or sets the font size for scale title.
    /// </summary>
    public int? FontSize { get; set; }
}
