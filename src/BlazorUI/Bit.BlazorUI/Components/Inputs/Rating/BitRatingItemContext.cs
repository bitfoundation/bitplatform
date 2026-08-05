namespace Bit.BlazorUI;

/// <summary>
/// The context passed to the ItemTemplate of the BitRating, describing the rating item being rendered.
/// </summary>
public class BitRatingItemContext
{
    /// <summary>
    /// Creates a new instance of <see cref="BitRatingItemContext"/>.
    /// </summary>
    /// <param name="index">
    /// The one-based position of the item in the rating.
    /// </param>
    /// <param name="max">
    /// The number of items the rating renders.
    /// </param>
    /// <param name="percentage">
    /// How much of the item is filled, from 0 to 100.
    /// </param>
    /// <param name="displayValue">
    /// The value the item is rendered from, which is the hovered value while a hover preview is active.
    /// </param>
    /// <param name="value">
    /// The committed value of the rating, regardless of any hover preview.
    /// </param>
    public BitRatingItemContext(int index, int max, double percentage, double displayValue, double value)
    {
        Index = index;
        Max = max;
        Percentage = percentage;
        DisplayValue = displayValue;
        Value = value;
    }

    /// <summary>
    /// The one-based position of the item in the rating.
    /// </summary>
    public int Index { get; }

    /// <summary>
    /// The number of items the rating renders.
    /// </summary>
    public int Max { get; }

    /// <summary>
    /// How much of the item is filled, from 0 to 100. A partially filled item is the fractional part of the value.
    /// </summary>
    public double Percentage { get; }

    /// <summary>
    /// The value the item is rendered from, which is the hovered value while a hover preview is active,
    /// and the committed value otherwise.
    /// </summary>
    public double DisplayValue { get; }

    /// <summary>
    /// The committed value of the rating, regardless of any hover preview.
    /// </summary>
    public double Value { get; }

    /// <summary>
    /// Whether the item is filled at all, meaning its <see cref="Percentage"/> is greater than zero.
    /// </summary>
    public bool IsSelected => Percentage > 0;

    /// <summary>
    /// Whether the item is completely filled, meaning its <see cref="Percentage"/> is 100.
    /// </summary>
    public bool IsFull => Percentage >= 100;
}
