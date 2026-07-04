namespace Bit.BlazorUI;

/// <summary>Data label plugin options (renders values on the chart).</summary>
public sealed class BitChartDataLabelOptions
{
    public bool Display { get; set; }
    public string Color { get; set; } = "#333";
    public BitChartFont Font { get; set; } = new();
    /// <summary>Simple value formatter.</summary>
    public Func<double, string>? Formatter { get; set; }
    /// <summary>Rich formatter receiving (value, datasetIndex, dataIndex).</summary>
    public Func<double, int, int, string>? FormatterCtx { get; set; }
    /// <summary>Per-element display predicate (value, datasetIndex, dataIndex) => show.</summary>
    public Func<double, int, int, bool>? DisplayFn { get; set; }
    /// <summary>Anchor of the label relative to the element (start = baseline, center, end = tip).</summary>
    public BitChartAlign Anchor { get; set; } = BitChartAlign.Center;
    /// <summary>Optional background color drawn behind the label.</summary>
    public string? BackgroundColor { get; set; }
    /// <summary>Corner radius of the label background.</summary>
    public double BorderRadius { get; set; } = 3;
    /// <summary>BitChartPadding inside the label background.</summary>
    public double Padding { get; set; } = 2;
    /// <summary>Rotation of the label text in degrees.</summary>
    public double Rotation { get; set; }
}
