namespace Bit.BlazorUI;

/// <summary>Tick configuration for a scale.</summary>
public sealed class BitChartTickOptions
{
    public bool Display { get; set; } = true;
    public string Color { get; set; } = "var(--bit-Chart-tick-color, var(--bit-clr-fg-sec))";
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
    /// <summary>
    /// Where a label of a horizontal axis lies against its tick: centered on it, or beginning (Start) or ending (End)
    /// at it. Start and End follow the reading direction, as they do everywhere in the chart: a label at the Start runs
    /// rightwards from its tick in a left-to-right chart and leftwards in a right-to-left one. A label turned by a
    /// rotation always ends at its tick.
    /// </summary>
    public BitPlacement Align { get; set; } = BitPlacement.Center;
    /// <summary>Extra pixel shift of the tick label along the axis, mirroring Chart.js <c>ticks.labelOffset</c>.</summary>
    public double LabelOffset { get; set; }
    /// <summary>Render value-axis tick labels inside the chart area.</summary>
    public bool Mirror { get; set; }
    /// <summary>
    /// Drop labels that would not fit. Category axes skip whole labels; value axes reduce the tick count
    /// to what the axis length can show without the labels colliding. A category axis keeps every n-th label from
    /// the first one, and the last one too when the limit allows two or more; a limit of one keeps the first only.
    /// </summary>
    public bool AutoSkip { get; set; } = true;
    /// <summary>
    /// Optional callback returning the label of each tick: (value, index) => text, where index is the tick's position
    /// among the labelled ticks drawn (the unlabelled minor ticks of a logarithmic axis are not counted). On a numeric
    /// axis the value is the tick's value; on a category axis it is the category's index into the chart's labels, as
    /// in Chart.js - read the category's name from the labels by that index. An empty string draws no label for that
    /// tick. Time axes do not call it.
    /// </summary>
    public Func<double, int, string>? Callback { get; set; }
    /// <summary>Standard or custom .NET numeric format string used for numeric tick labels (e.g. "N0", "P1", "C").</summary>
    public string? Format { get; set; }
    public string? Prefix { get; set; }
    public string? Suffix { get; set; }
}
