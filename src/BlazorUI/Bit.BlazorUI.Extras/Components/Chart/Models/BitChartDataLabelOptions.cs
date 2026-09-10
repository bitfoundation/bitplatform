namespace Bit.BlazorUI;

/// <summary>Data label plugin options (renders the values on the chart itself).</summary>
public sealed class BitChartDataLabelOptions
{
    public bool Display { get; set; }
    public string Color { get; set; } = "var(--bit-clr-fg-pri, #1A1A1A)";
    public BitChartFont Font { get; set; } = new();
    /// <summary>Simple value formatter.</summary>
    public Func<double, string>? Formatter { get; set; }
    /// <summary>Rich formatter receiving (value, datasetIndex, dataIndex).</summary>
    public Func<double, int, int, string>? FormatterCtx { get; set; }
    /// <summary>Per-element display predicate (value, datasetIndex, dataIndex) => show.</summary>
    public Func<double, int, int, bool>? DisplayFn { get; set; }
    /// <summary>
    /// Where the label sits relative to the element: <see cref="BitChartAlign.Start"/> at the baseline
    /// end, <see cref="BitChartAlign.Center"/> in the middle, <see cref="BitChartAlign.End"/> at the tip.
    /// </summary>
    public BitChartAlign Anchor { get; set; } = BitChartAlign.End;
    /// <summary>
    /// Which side of the anchor the label is drawn on: <see cref="BitChartAlign.Start"/> pulls it inside
    /// the element, <see cref="BitChartAlign.End"/> pushes it outside, <see cref="BitChartAlign.Center"/>
    /// centers it on the anchor.
    /// </summary>
    public BitChartAlign Align { get; set; } = BitChartAlign.End;
    /// <summary>Extra distance (px) between the anchor and the label.</summary>
    public double Offset { get; set; } = 4;
    /// <summary>Also draw labels on line/scatter/radar point markers (bars and arcs always get them).</summary>
    public bool ShowOnPoints { get; set; } = true;
    /// <summary>Optional background color drawn behind the label.</summary>
    public string? BackgroundColor { get; set; }
    /// <summary>Corner radius of the label background.</summary>
    public double BorderRadius { get; set; } = 3;
    /// <summary>Padding inside the label background.</summary>
    public double Padding { get; set; } = 2;
    /// <summary>Rotation of the label text in degrees.</summary>
    public double Rotation { get; set; }
}
