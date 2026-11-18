namespace Bit.Bink;

/// <summary>The shape a drag on empty space selects with.</summary>
public enum BinkMarqueeShape : byte
{
    /// <summary>A rectangle: everything it touches is selected.</summary>
    Rectangle = 0,

    /// <summary>
    /// A freehand loop: what it encloses is selected. The natural way to pick up handwriting, where a rectangle
    /// around one word also catches the lines above and below it.
    /// </summary>
    Lasso = 1,
}
