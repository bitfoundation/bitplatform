namespace Bit.BlazorUI;

/// <summary>
/// The animation-subconfig of the options for a radial chart.
/// </summary>
public class ArcAnimation : Animation
{
    /// <summary>
    /// Gets or sets a value indicating whether the chart will
    /// load in with a rotation animation or not.
    /// </summary>
    public bool? AnimateRotate { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the chart will
    /// load in with a scaling animation (from the center outwards) or not.
    /// </summary>
    public bool? AnimateScale { get; set; }
}
