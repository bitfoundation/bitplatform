namespace Bit.BlazorUI;

/// <summary>
/// The uncertainty around one data point, drawn as a whisker through it. The two arms are given
/// separately so an asymmetric interval - a confidence band that is not centered on the estimate -
/// can be expressed; assigning a single number gives a symmetric one.
/// </summary>
public readonly record struct BitChartErrorBar(double Minus, double Plus)
{
    /// <summary>A symmetric interval of <paramref name="amount"/> either side of the value.</summary>
    public static implicit operator BitChartErrorBar(double amount) => new(amount, amount);

    /// <summary>True when both arms are the same length.</summary>
    public bool IsSymmetric => Math.Abs(Minus - Plus) < 1e-9;
}
