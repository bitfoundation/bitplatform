namespace Bit.BlazorUI;

/// <summary>
/// The options-subconfig of a <see cref="PolarAreaConfig"/>.
/// </summary>
public class PolarAreaOptions : BaseConfigOptions
{
    /// <summary>
    /// Gets or sets the starting angle to draw arcs for the first item in a dataset.
    /// </summary>
    public double? StartAngle { get; set; }

    /// <summary>
    /// Gets or sets the animation-configuration for this chart.
    /// </summary>
    public new ArcAnimation Animation { get; set; }

    /// <summary>
    /// The scale (axis) for this chart.
    /// </summary>
    public LinearRadialAxis Scale { get; set; }
}
