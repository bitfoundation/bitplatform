namespace Bit.BlazorUI;

/// <summary>
/// The base config for the options-subconfig of a chart.
/// </summary>
public class BaseConfigOptions
{
    /// <summary>
    /// Gets or sets the title of this chart.
    /// </summary>
    public OptionsTitle Title { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the chart canvas should be resized when its container is.
    /// <para>See important note: <a href="https://www.chartjs.org/docs/latest/general/responsive.html#important-note">here (Chart.js)</a>.</para>
    /// </summary>
    public bool? Responsive { get; set; }

    /// <summary>
    /// Gets or sets the canvas aspect ratio (i.e. width / height, a value of 1 representing a square canvas).
    /// <para>Note that this option is ignored if the height is explicitly defined either as attribute (of the canvas) or via the style.</para>
    /// </summary>
    public double? AspectRatio { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to maintain the original canvas aspect ratio (width / height) when resizing.
    /// </summary>
    public bool? MaintainAspectRatio { get; set; }

    /// <summary>
    /// Gets or sets the duration in milliseconds it takes to animate to new size after a resize event.
    /// </summary>
    public int? ResponsiveAnimationDuration { get; set; }

    /// <summary>
    /// Gets or sets the legend for this chart.
    /// </summary>
    public Legend Legend { get; set; }

    /// <summary>
    /// Gets or sets the tooltip options for this chart.
    /// </summary>
    public Tooltips Tooltips { get; set; }

    /// <summary>
    /// Gets or sets the animation-configuration for this chart.
    /// </summary>
    public Animation Animation { get; set; }

    /// <summary>
    /// Gets the plugin options. The key has to be the unique
    /// identification of the plugin.
    /// <para>
    /// Reference for Chart.js plugin options:
    /// <a href="https://www.chartjs.org/docs/latest/developers/plugins.html#plugin-options"/>
    /// </para>
    /// </summary>
    public Dictionary<string, object> Plugins { get; } = new Dictionary<string, object>();

    /// <summary>
    /// Gets or sets the browser events that the chart should listen to for tooltips and hovering.
    /// <para>
    /// If <see langword="null"/>, this includes <see cref="BrowserEvent.MouseMove"/>, <see cref="BrowserEvent.MouseOut"/>,
    /// <see cref="BrowserEvent.Click"/>, <see cref="BrowserEvent.TouchStart"/> and <see cref="BrowserEvent.TouchMove"/> (by default).
    /// </para>
    /// </summary>
    public BrowserEvent[] Events { get; set; }

    /// <summary>
    /// Gets or sets the callback to call when an event of type <see cref="BrowserEvent.MouseUp"/> or
    /// <see cref="BrowserEvent.Click"/> fires on the chart.
    /// Called in the context of the chart and passed the event and an array of active elements.
    /// <para>See <see cref="JavaScriptHandler{T}"/> and <see cref="DelegateHandler{T}"/>.</para>
    /// </summary>
    public IMethodHandler<ChartMouseEvent> OnClick { get; set; }

    /// <summary>
    /// Gets or sets the callback to call when any of the <see cref="Events"/> fire on the chart.
    /// Called in the context of the chart and passed the event and an array of
    /// active elements (bars, points, etc).
    /// <para>See <see cref="JavaScriptHandler{T}"/> and <see cref="DelegateHandler{T}"/>.</para>
    /// </summary>
    public IMethodHandler<ChartMouseEvent> OnHover { get; set; }

    /// <summary>
    /// Gets or sets the hover configuration for this chart.
    /// </summary>
    public Hover Hover { get; set; }

    /// <summary>
    /// This method tells json.net to only serialize the plugin options when
    /// there are plugin options, don't call it directly.
    /// </summary>
    [Obsolete("Only for json.net, don't call it.", true)]
    public bool ShouldSerializePlugins() => Plugins.Count > 0;
}
