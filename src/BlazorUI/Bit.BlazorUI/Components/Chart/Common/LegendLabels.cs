namespace Bit.BlazorUI;

/// <summary>
/// The legend label configuration is nested below the legend configuration
/// </summary>
public class LegendLabels
{
    /// <summary>
    /// Gets or sets the width of the colored box.
    /// </summary>
    public int? BoxWidth { get; set; }

    /// <summary>
    /// Gets or sets the font size for the labels text.
    /// </summary>
    public int? FontSize { get; set; }

    /// <summary>
    /// Gets or sets the font style for the labels text.
    /// </summary>
    public FontStyle FontStyle { get; set; }

    /// <summary>
    /// Gets or sets the color of the text.
    /// <para>See <see cref="ColorUtil"/> for working with colors.</para>
    /// </summary>
    public string FontColor { get; set; }

    /// <summary>
    /// Gets or sets the padding between rows of colored boxes.
    /// </summary>
    public int? Padding { get; set; }

    /// <summary>
    /// Gets or sets the callback to generate legend items for a chart.
    /// Default implementation returns the text + styling for the color box.
    /// <para>See <see cref="JavaScriptHandler{T}"/> and <see cref="DelegateHandler{T}"/>.</para>
    /// </summary>
    public IMethodHandler<LegendLabelsGenerator> GenerateLabels { get; set; }

    /// <summary>
    /// Gets or sets the callback to filter legend items out of the legend.
    /// Receives 2 parameters, a <see cref="LegendItem"/> and the chart data. The chart data large so
    /// consider applying a <see cref="IgnoreCallbackValueAttribute"/> if you don't use the value.
    /// <para>See <see cref="JavaScriptHandler{T}"/> and <see cref="DelegateHandler{T}"/>.</para>
    /// </summary>
    public IMethodHandler<LegendLabelFilter> Filter { get; set; }

    /// <summary>
    /// Label style will match corresponding point style (size is based on <see cref="FontSize"/>, <see cref="BoxWidth"/> is not used in this case).
    /// </summary>
    public bool? UsePointStyle { get; set; }
}
