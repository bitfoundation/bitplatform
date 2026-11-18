namespace Bit.Bink;

/// <summary>Fill rule used when a path self-intersects.</summary>
public enum BinkFillRule : byte
{
    /// <summary>Non-zero winding: self-overlapping regions of a freehand outline stay solid.</summary>
    NonZero = 0,
    /// <summary>Even-odd: overlapping regions alternate between filled and empty.</summary>
    EvenOdd = 1,
}
