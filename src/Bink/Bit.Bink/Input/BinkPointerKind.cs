namespace Bit.Bink;

/// <summary>The kind of device that produced a pointer sample. Mirrors the Pointer Events <c>pointerType</c> values.</summary>
public enum BinkPointerKind : byte
{
    /// <summary>Unknown or unsupported device.</summary>
    Unknown = 0,
    /// <summary>A mouse or trackpad.</summary>
    Mouse = 1,
    /// <summary>A finger on a touch screen.</summary>
    Touch = 2,
    /// <summary>A stylus / pen.</summary>
    Pen = 3,
}

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
