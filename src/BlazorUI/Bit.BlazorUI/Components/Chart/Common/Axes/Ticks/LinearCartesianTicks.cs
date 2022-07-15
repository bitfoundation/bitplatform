namespace Bit.BlazorUI;

/// <summary>
/// The ticks-subconfig of <see cref="LinearCartesianAxis"/>.
/// <para>As per documentation <a href="https://www.chartjs.org/docs/latest/axes/cartesian/linear.html#tick-configuration-options">here (Chart.js)</a>.</para>
/// </summary>
public class LinearCartesianTicks : CartesianTicks
{
    /// <summary>
    /// If true, scale will include 0 if it is not already included.
    /// </summary>
    public bool? BeginAtZero { get; set; }

    /// <summary>
    /// Gets or sets the maximum number of ticks and gridlines to show.
    /// </summary>
    public int? MaxTicksLimit { get; set; }

    /// <summary>
    /// If defined and <see cref="StepSize"/> is not specified, the step size will be rounded to this many decimal places.
    /// </summary>
    public int? Precision { get; set; }

    /// <summary>
    /// Gets or sets the user defined fixed step size for the scale.
    /// <para>See <a href="https://www.chartjs.org/docs/latest/axes/cartesian/linear.html#step-size"/> for details.</para>
    /// </summary>
    public double? StepSize { get; set; }

    /// <summary>
    /// Gets or sets the adjustment used when calculating the maximum data value.
    /// This value is used as the highest value if it's higher than the maximum data-value.
    /// </summary>
    public int? SuggestedMax { get; set; }

    /// <summary>
    /// Gets or sets the adjustment used when calculating the minimum data value.
    /// This value is used as the lowest value if it's lower than the minimum data-value.
    /// </summary>
    public int? SuggestedMin { get; set; }
}
