namespace Bit.BlazorUI;

/// <summary>
/// Represents a dataset for a bubble chart.
/// <para>As per documentation <a href="https://www.chartjs.org/docs/latest/charts/bubble.html#dataset-properties">here (Chart.js)</a>.</para>
/// </summary>
public class BubbleDataset : Dataset<BubblePoint>
{
    /// <summary>
    /// Creates a new instance of <see cref="BubbleDataset"/>.
    /// </summary>
    public BubbleDataset() : base(ChartType.Bubble) { }

    /// <summary>
    /// Creates a new instance of <see cref="BubbleDataset"/> with initial data.
    /// </summary>
    public BubbleDataset(IEnumerable<BubblePoint> data) : this()
    {
        AddRange(data);
    }

    /// <summary>
    /// Creates a new instance of <see cref="BubbleDataset"/> with
    /// a custom <see cref="ChartType"/>. Use this constructor when
    /// you implement a bubble-like chart.
    /// </summary>
    /// <param name="type">The <see cref="ChartType"/> to use instead of <see cref="ChartType.Bubble"/>.</param>
    protected BubbleDataset(ChartType type) : base(type) { }

    /// <summary>
    /// Gets or sets the bubble background color.
    /// <para>See <see cref="ColorUtil"/> for working with colors.</para>
    /// </summary>
    public IndexableOption<string> BackgroundColor { get; set; }

    /// <summary>
    /// Gets or sets the bubble border color.
    /// <para>See <see cref="ColorUtil"/> for working with colors.</para>
    /// </summary>
    public IndexableOption<string> BorderColor { get; set; }

    /// <summary>
    /// Gets or sets the bubble border width (in pixels).
    /// </summary>
    public IndexableOption<int> BorderWidth { get; set; }

    /// <summary>
    /// Gets or sets the bubble background color when hovered.
    /// <para>See <see cref="ColorUtil"/> for working with colors.</para>
    /// </summary>
    public IndexableOption<string> HoverBackgroundColor { get; set; }

    /// <summary>
    /// Gets or sets the bubble border color when hovered.
    /// <para>See <see cref="ColorUtil"/> for working with colors.</para>
    /// </summary>
    public IndexableOption<string> HoverBorderColor { get; set; }

    /// <summary>
    /// Gets or sets the bubble border width when hovered (in pixels).
    /// </summary>
    public IndexableOption<int> HoverBorderWidth { get; set; }

    /// <summary>
    /// Gets or sets the bubbles <b>additional</b> radius when hovered (in pixels).
    /// </summary>
    public IndexableOption<int> HoverRadius { get; set; }

    /// <summary>
    /// Gets or sets the bubbles <b>additional</b> radius for hit detection (in pixels).
    /// </summary>
    public IndexableOption<int> HitRadius { get; set; }

    /// <summary>
    /// Gets or sets the label for the dataset which appears in the legend and tooltips.
    /// </summary>
    public string Label { get; set; }

    /// <summary>
    /// Gets or sets the drawing order of this dataset.
    /// Also affects the order for stacking, tooltips, and the legend.
    /// </summary>
    public int? Order { get; set; }

    /// <summary>
    /// Gets or sets the <see cref="Common.Enums.PointStyle"/> for the bubbles in this dataset.
    /// </summary>
    public IndexableOption<PointStyle> PointStyle { get; set; }

    /// <summary>
    /// Gets or sets the bubble radius (in pixels).
    /// </summary>
    public IndexableOption<int> Radius { get; set; }

    /// <summary>
    /// Gets or sets the bubble rotation (in degrees).
    /// </summary>
    public IndexableOption<int> Rotation { get; set; }
}
