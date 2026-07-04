namespace Bit.BlazorUI;

/// <summary>Animation options. For the SVG renderer these map to CSS transitions.</summary>
public sealed class BitChartAnimationOptions
{
    public bool Animate { get; set; } = true;
    public int Duration { get; set; } = 600;
    public string Easing { get; set; } = "ease-out";
    /// <summary>Per-element entry delay (ms). When &gt; 0, elements animate in sequence (staggered).</summary>
    public double DelayBetween { get; set; }

    /// <summary>
    /// When true, line/area charts animate by progressively drawing the line from start to end
    /// (left to right) and revealing each point in sequence, mimicking time-based data gathering.
    /// Ideal for time-series charts. Ignored by non-line chart types (bar, pie, doughnut, etc.).
    /// </summary>
    public bool Progressive { get; set; }
}
