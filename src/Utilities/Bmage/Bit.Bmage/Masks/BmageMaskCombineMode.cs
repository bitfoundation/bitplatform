namespace Bit.Bmage;

/// <summary>How two masks combine, value by value.</summary>
/// <remarks>Soft masks combine as fuzzy sets: a union keeps the larger value, an intersection the smaller one.</remarks>
public enum BmageMaskCombineMode
{
    /// <summary>Covered where either mask is: the larger of the two values.</summary>
    Union = 0,
    /// <summary>Covered where both masks are: the smaller of the two values.</summary>
    Intersect,
    /// <summary>Covered where the first mask is and the second is not.</summary>
    Subtract,
    /// <summary>Covered where exactly one of the masks is.</summary>
    Exclude,
}
