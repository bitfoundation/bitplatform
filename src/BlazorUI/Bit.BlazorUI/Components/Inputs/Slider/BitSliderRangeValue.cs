namespace Bit.BlazorUI;

/// <summary>
/// The pair of values a ranged <see cref="BitSlider"/> selects.
/// </summary>
/// <remarks>
/// This is a record so that two instances holding the same numbers compare equal. The slider relies on
/// that: an ordinary class would compare by reference, and every render would then look like a change and
/// echo a new value back to whatever the parameter is bound to.
/// </remarks>
public record BitSliderRangeValue
{
    /// <summary>
    /// Creates an empty range, with both ends at zero.
    /// </summary>
    public BitSliderRangeValue() { }

    /// <summary>
    /// Creates a range from its two ends.
    /// </summary>
    public BitSliderRangeValue(double lower, double upper)
    {
        Lower = lower;
        Upper = upper;
    }

    /// <summary>
    /// The lower value of the ranged Slider.
    /// </summary>
    public double Lower { get; set; }

    /// <summary>
    /// The upper value of the ranged Slider.
    /// </summary>
    public double Upper { get; set; }

    /// <summary>
    /// The distance between the two ends of the range.
    /// </summary>
    public double Length => Upper - Lower;

    public void Deconstruct(out double lower, out double upper)
    {
        lower = Lower;
        upper = Upper;
    }
}
