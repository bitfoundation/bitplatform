namespace Bit.BlazorUI;

/// <summary>
/// The chart legend displays data about the datasets that are appearing on the chart.
/// <para>As per documentation <a href="http://www.chartjs.org/docs/latest/configuration/legend.html">here (Chart.js)</a>.</para>
/// </summary>
public class Legend
{
    /// <summary>
    /// Determines if the legend is displayed
    /// </summary>
    public bool? Display { get; set; }

    /// <summary>
    /// Position of the legend
    /// </summary>
    public Position Position { get; set; }

    /// <summary>
    /// Marks that this box should take the full width of the canvas (pushing down other boxes).
    /// This is unlikely to need to be changed in day-to-day use.
    /// </summary>
    public bool? FullWidth { get; set; }

    /// <summary>
    /// Gets or sets the callback to call when a click event is registered on a label item.
    /// <para>See <see cref="JavaScriptHandler{T}"/> and <see cref="DelegateHandler{T}"/>.</para>
    /// </summary>
    public IMethodHandler<LegendItemMouseEvent> OnClick { get; set; }

    /// <summary>
    /// Gets or sets the callback to call when a <see cref="BrowserEvent.MouseMove"/> event is registered on top of a label item.
    /// <para>See <see cref="JavaScriptHandler{T}"/> and <see cref="DelegateHandler{T}"/>.</para>
    /// </summary>
    public IMethodHandler<LegendItemMouseEvent> OnHover { get; set; }

    /// <summary>
    /// Legend will show datasets in reverse order.
    /// </summary>
    public bool? Reverse { get; set; }

    /// <summary>
    /// Configuration options for the legend-labels
    /// </summary>
    public LegendLabels Labels { get; set; }
}
