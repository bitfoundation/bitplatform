namespace Bit.Bmotion;

/// <summary>Which side of the straight line an arc bulges towards.</summary>
public enum BmotionArcDirection
{
    /// <summary>
    /// Bulge upward on screen wherever the movement isn't purely vertical (and consistently to one
    /// side when it is). The default: it reads as "thrown", which is what an arc is usually for.
    /// </summary>
    Auto,

    /// <summary>Bulge clockwise from the start point, in screen coordinates.</summary>
    Clockwise,

    /// <summary>Bulge counter-clockwise from the start point, in screen coordinates.</summary>
    CounterClockwise,
}
