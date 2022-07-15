namespace Bit.BlazorUI;

/// <summary>
/// The ticks sub-config of the <see cref="LinearRadialAxis"/>.
/// <para>As per documentation <a href="https://www.chartjs.org/docs/latest/axes/radial/linear.html#point-label-options">here (Chart.js)</a>.</para>
/// </summary>
public class LinearRadialTicks : Ticks
{
    /// <summary>
    /// Gets or sets the color of the label backdrops.
    /// <para>See <see cref="ColorUtil"/> for working with colors.</para>
    /// </summary>
    public string BackdropColor { get; set; }

    /// <summary>
    /// Gets or sets the horizontal padding of label backdrop.
    /// </summary>
    public int? BackdropPaddingX { get; set; }

    /// <summary>
    /// Gets or sets the vertical padding of label backdrop.
    /// </summary>
    public int? BackdropPaddingY { get; set; }

    /// <summary>
    /// Gets or sets the value indicating whether the scale will include 0 if it is not already included.
    /// </summary>
    public bool? BeginAtZero { get; set; }

    /// <summary>
    /// Gets or sets the user defined minimum number for the scale, overrides minimum value from data.
    /// <para>As per documentation <a href="https://www.chartjs.org/docs/latest/axes/radial/linear.html#axis-range-settings">here (Chart.js)</a>.</para>
    /// </summary>
    public double? Min { get; set; }

    /// <summary>
    /// Gets or sets the user defined maximum number for the scale, overrides minimum value from data.
    /// <para>As per documentation <a href="https://www.chartjs.org/docs/latest/axes/radial/linear.html#axis-range-settings">here (Chart.js)</a>.</para>
    /// </summary>
    public double? Max { get; set; }

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
    /// <para>As per documentation <a href="https://www.chartjs.org/docs/latest/axes/radial/linear.html#axis-range-settings">here (Chart.js)</a>.</para>
    /// </summary>
    public double? StepSize { get; set; }

    /// <summary>
    /// Gets or sets the adjustment used when calculating the maximum data value.
    /// <para>As per documentation <a href="https://www.chartjs.org/docs/latest/axes/radial/linear.html#axis-range-settings">here (Chart.js)</a>.</para>
    /// </summary>
    public double? SuggestedMax { get; set; }

    /// <summary>
    /// Gets or sets the adjustment used when calculating the minimum data value.
    /// <para>As per documentation <a href="https://www.chartjs.org/docs/latest/axes/radial/linear.html#axis-range-settings">here (Chart.js)</a>.</para>
    /// </summary>
    public double? SuggestedMin { get; set; }

    /// <summary>
    /// Gets or sets the value indicating whether a background is drawn behind the tick labels.
    /// </summary>
    public bool? ShowLabelBackdrop { get; set; }
}
