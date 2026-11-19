namespace Bit.Bink;

/// <summary>Lifecycle phase of a pointer sample.</summary>
public enum BinkPointerPhase : byte
{
    /// <summary>Pointer pressed (button down / touch start / pen contact).</summary>
    Down = 0,
    /// <summary>Pointer moved while pressed.</summary>
    Move = 1,
    /// <summary>Pointer released.</summary>
    Up = 2,
    /// <summary>The browser cancelled the interaction (e.g. scroll took over or the pen left range).</summary>
    Cancel = 3,
    /// <summary>Pointer moved while not pressed (hover). Only forwarded when hover tracking is enabled.</summary>
    Hover = 4,
}
