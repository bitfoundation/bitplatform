namespace Bit.BlazorUI;

/// <summary>Tick configuration for a scale.</summary>
public sealed class BitChartTickOptions
{
    public bool Display { get; set; } = true;
    public string Color { get; set; } = "var(--bit-clr-fg-sec, #525252)";
    public BitChartFont Font { get; set; } = new();
    public double Padding { get; set; } = 3;
    public double? StepSize { get; set; }
    public int? Count { get; set; }
    public int? MaxTicksLimit { get; set; } = 11;
    public int? Precision { get; set; }
    public double Rotation { get; set; }
    /// <summary>Maximum auto-rotation (degrees) for category tick labels that don't fit. Chart.js default 50.</summary>
    public double MaxRotation { get; set; } = 50;
    /// <summary>Minimum auto-rotation (degrees) for tick labels.</summary>
    public double MinRotation { get; set; }
    /// <summary>Tick label alignment relative to the tick (start/center/end).</summary>
    public BitChartAlign Align { get; set; } = BitChartAlign.Center;
    /// <summary>Extra pixel shift of the tick label along the axis, mirroring Chart.js <c>ticks.labelOffset</c>.</summary>
    public double LabelOffset { get; set; }
    /// <summary>Render value-axis tick labels inside the chart area.</summary>
    public bool Mirror { get; set; }
    /// <summary>
    /// Drop labels that would not fit. Category axes skip whole labels; value axes reduce the tick count
    /// to what the axis length can show without the labels colliding.
    /// </summary>
    public bool AutoSkip { get; set; } = true;
    /// <summary>Optional formatting callback applied to each numeric/category tick value.</summary>
    public Func<double, int, string>? Callback { get; set; }
    /// <summary>Standard or custom .NET numeric format string used for numeric tick labels (e.g. "N0", "P1", "C").</summary>
    public string? Format { get; set; }
    public string? Prefix { get; set; }
    public string? Suffix { get; set; }
}
