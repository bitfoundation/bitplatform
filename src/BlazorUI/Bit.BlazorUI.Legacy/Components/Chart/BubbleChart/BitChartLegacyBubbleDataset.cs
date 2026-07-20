namespace Bit.BlazorUI.Legacy;

/// <summary>
/// Represents a dataset for a bubble chart.
/// <para>As per documentation <a href="https://www.chartjs.org/docs/latest/charts/bubble.html#dataset-properties">here (Chart.js)</a>.</para>
/// </summary>
public class BitChartLegacyBubbleDataset : BitChartLegacyDataset<BitChartLegacyBubblePoint>
{
    /// <summary>
    /// Creates a new instance of <see cref="BitChartLegacyBubbleDataset"/>.
    /// </summary>
    public BitChartLegacyBubbleDataset() : base(BitChartLegacyChartType.Bubble) { }

    /// <summary>
    /// Creates a new instance of <see cref="BitChartLegacyBubbleDataset"/> with initial data.
    /// </summary>
    public BitChartLegacyBubbleDataset(IEnumerable<BitChartLegacyBubblePoint> data) : this()
    {
        AddRange(data);
    }

    /// <summary>
    /// Creates a new instance of <see cref="BitChartLegacyBubbleDataset"/> with
    /// a custom <see cref="BitChartLegacyChartType"/>. Use this constructor when
    /// you implement a bubble-like chart.
    /// </summary>
    /// <param name="type">The <see cref="BitChartLegacyChartType"/> to use instead of <see cref="BitChartLegacyChartType.Bubble"/>.</param>
    protected BitChartLegacyBubbleDataset(BitChartLegacyChartType type) : base(type) { }

    /// <summary>
    /// Gets or sets the bubble background color.
    /// <para>See <see cref="BitChartLegacyColorUtil"/> for working with colors.</para>
    /// </summary>
    public BitChartLegacyIndexableOption<string>? BackgroundColor { get; set; }

    /// <summary>
    /// Gets or sets the bubble border color.
    /// <para>See <see cref="BitChartLegacyColorUtil"/> for working with colors.</para>
    /// </summary>
    public BitChartLegacyIndexableOption<string>? BorderColor { get; set; }

    /// <summary>
    /// Gets or sets the bubble border width (in pixels).
    /// </summary>
    public BitChartLegacyIndexableOption<int>? BorderWidth { get; set; }

    /// <summary>
    /// Gets or sets the bubble background color when hovered.
    /// <para>See <see cref="BitChartLegacyColorUtil"/> for working with colors.</para>
    /// </summary>
    public BitChartLegacyIndexableOption<string>? HoverBackgroundColor { get; set; }

    /// <summary>
    /// Gets or sets the bubble border color when hovered.
    /// <para>See <see cref="BitChartLegacyColorUtil"/> for working with colors.</para>
    /// </summary>
    public BitChartLegacyIndexableOption<string>? HoverBorderColor { get; set; }

    /// <summary>
    /// Gets or sets the bubble border width when hovered (in pixels).
    /// </summary>
    public BitChartLegacyIndexableOption<int>? HoverBorderWidth { get; set; }

    /// <summary>
    /// Gets or sets the bubbles <b>additional</b> radius when hovered (in pixels).
    /// </summary>
    public BitChartLegacyIndexableOption<int>? HoverRadius { get; set; }

    /// <summary>
    /// Gets or sets the bubbles <b>additional</b> radius for hit detection (in pixels).
    /// </summary>
    public BitChartLegacyIndexableOption<int>? HitRadius { get; set; }

    /// <summary>
    /// Gets or sets the label for the dataset which appears in the legend and tooltips.
    /// </summary>
    public string? Label { get; set; }

    /// <summary>
    /// Gets or sets the drawing order of this dataset.
    /// Also affects the order for stacking, tooltips, and the legend.
    /// </summary>
    public int? Order { get; set; }

    /// <summary>
    /// Gets or sets the <see cref="BitChartLegacyPointStyle"/> for the bubbles in this dataset.
    /// </summary>
    public BitChartLegacyIndexableOption<BitChartLegacyPointStyle>? PointStyle { get; set; }

    /// <summary>
    /// Gets or sets the bubble radius (in pixels).
    /// </summary>
    public BitChartLegacyIndexableOption<int>? Radius { get; set; }

    /// <summary>
    /// Gets or sets the bubble rotation (in degrees).
    /// </summary>
    public BitChartLegacyIndexableOption<int>? Rotation { get; set; }
}
