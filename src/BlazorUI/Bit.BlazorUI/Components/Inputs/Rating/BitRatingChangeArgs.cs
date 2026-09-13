namespace Bit.BlazorUI;

/// <summary>
/// Arguments for the OnChanging callback of BitRating.
/// Set <see cref="Cancel"/> to true to keep the rating at its current value.
/// </summary>
public class BitRatingChangeArgs
{
    /// <summary>
    /// Creates a new instance of <see cref="BitRatingChangeArgs"/>.
    /// </summary>
    /// <param name="value">
    /// The rating value the component is about to move to.
    /// </param>
    /// <param name="oldValue">
    /// The rating value the component is moving away from.
    /// </param>
    public BitRatingChangeArgs(double value, double oldValue)
    {
        Value = value;
        OldValue = oldValue;
    }

    /// <summary>
    /// The rating value the component is about to move to.
    /// </summary>
    public double Value { get; }

    /// <summary>
    /// The rating value the component is moving away from.
    /// </summary>
    public double OldValue { get; }

    /// <summary>
    /// Set to true to cancel the change and keep the current value of the rating.
    /// </summary>
    public bool Cancel { get; set; }
}
