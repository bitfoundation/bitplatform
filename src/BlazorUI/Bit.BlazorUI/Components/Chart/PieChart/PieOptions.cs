namespace Bit.BlazorUI;

/// <summary>
/// The options-subconfig of a <see cref="PieConfig"/>.
/// </summary>
public class PieOptions : BaseConfigOptions
{
    /// <summary>
    /// Gets or sets the percentage of the chart that is cut out of the middle.
    /// <para>Default for Pie is 0, Default for Doughnut is 50. This will be filled in by Chart.js unless you specify a non-null value.</para>
    /// </summary>
    public int? CutoutPercentage { get; set; }

    /// <summary>
    /// Gets or sets the animation-configuration for this chart.
    /// </summary>
    public new ArcAnimation Animation { get; set; }

    /// <summary>
    /// Gets or sets the starting angle to draw arcs from.
    /// </summary>
    public double? Rotation { get; set; }

    /// <summary>
    /// Gets or sets the sweep to allow arcs to cover.
    /// </summary>
    public double? Circumference { get; set; }
}
