namespace Bit.BlazorUI;

/// <summary>One fitted line drawn over a dataset by <see cref="BitChartTrendlinePlugin"/>.</summary>
public sealed class BitChartTrendline
{
    /// <summary>Index of the dataset the line is fitted to.</summary>
    public int DatasetIndex { get; set; }

    /// <summary>Which curve is fitted.</summary>
    public BitChartTrendlineKind Kind { get; set; } = BitChartTrendlineKind.Linear;

    /// <summary>Window of the trailing moving average, used by <see cref="BitChartTrendlineKind.MovingAverage"/>.</summary>
    public int Period { get; set; } = 5;

    /// <summary>Line color. When null it follows the dataset's own border color, dimmed.</summary>
    public string? Color { get; set; }

    public double LineWidth { get; set; } = 2;

    /// <summary>Dash pattern; dashed by default so the fit never reads as another measured series.</summary>
    public List<double>? Dash { get; set; } = [6, 4];

    /// <summary>
    /// Projects a straight fit across the full width of the plot instead of stopping at the first and
    /// last data point. Ignored by <see cref="BitChartTrendlineKind.MovingAverage"/>, which has no
    /// meaning outside the data.
    /// </summary>
    public bool Extend { get; set; }

    /// <summary>Optional label drawn in a pill at the end of the line.</summary>
    public string? Label { get; set; }

    public string LabelColor { get; set; } = "#fff";

    /// <summary>Pill color behind the label. When null it follows the line color.</summary>
    public string? LabelBackground { get; set; }

    public BitChartFont LabelFont { get; set; } = new() { Size = 11, Weight = "bold" };

    /// <summary>Draw the line under the datasets rather than over them.</summary>
    public bool DrawBehindDatasets { get; set; }
}
