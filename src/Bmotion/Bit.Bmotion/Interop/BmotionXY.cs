namespace Bit.Bmotion;

/// <summary>
/// A simple x/y pair marshalled to JS at drag start (serialised as <c>{ x, y }</c>).
/// Used instead of a boxed anonymous type so the shape is explicit and trim/AOT friendly.
/// </summary>
public readonly struct BmotionXY
{
    public BmotionXY(double x, double y)
    {
        X = x;
        Y = y;
    }

    public double X { get; init; }
    public double Y { get; init; }
}
