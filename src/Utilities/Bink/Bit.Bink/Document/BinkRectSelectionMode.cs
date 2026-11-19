namespace Bit.Bink;

/// <summary>How a rectangle query matches elements.</summary>
public enum BinkRectSelectionMode : byte
{
    /// <summary>Elements whose geometry intersects the rectangle.</summary>
    Intersect = 0,
    /// <summary>Elements entirely inside the rectangle.</summary>
    Contain = 1,
}
