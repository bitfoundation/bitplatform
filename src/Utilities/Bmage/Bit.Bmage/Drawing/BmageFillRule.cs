namespace Bit.Bmage;

/// <summary>How overlapping contours combine when filling a path.</summary>
public enum BmageFillRule
{
    /// <summary>A point is inside when the winding number is non-zero. The usual choice.</summary>
    NonZero = 0,
    /// <summary>A point is inside when it is crossed an odd number of times, so overlaps cut holes.</summary>
    EvenOdd,
}
