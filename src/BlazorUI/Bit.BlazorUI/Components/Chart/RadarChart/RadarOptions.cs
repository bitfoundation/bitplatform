namespace Bit.BlazorUI;

/// <summary>
/// Represents the options-subconfig of a <see cref="RadarConfig"/>.
/// </summary>
public class RadarOptions : BaseConfigOptions
{
    /// <summary>
    /// Gets or sets the scale configuration for this chart.
    /// </summary>
    public LinearRadialAxis Scale { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether or not line gaps (by NaN data) will be spanned.
    /// If <see langword="false"/>, NaN data causes a break in the line.
    /// </summary>
    public bool? SpanGaps { get; set; }
}
