namespace Bit.Bink;

/// <summary>Opcodes of the canvas command buffer. Kept in sync with the <c>OP</c> table in <c>bit-bink.js</c>.</summary>
internal enum BinkCanvasCommand : byte
{
    BeginPath = 0,
    MoveTo = 1,
    LineTo = 2,
    QuadTo = 3,
    CubicTo = 4,
    ClosePath = 5,
    Fill = 6,
    Stroke = 7,
    Save = 8,
    Restore = 9,
    Transform = 10,
    ClipRect = 11,
    DrawImage = 12,
    DrawText = 13,
    PushGroup = 14,
    PopGroup = 15,
    Clear = 16,
    SetTransform = 17,
    SetDash = 18,
    ClearRect = 19,
    /// <summary>Slot: the path verbs that follow build a retained path instead of the current one.</summary>
    DefinePath = 20,
    /// <summary>Ends the retained path started by <see cref="DefinePath"/>.</summary>
    EndPath = 21,
    /// <summary>Slot: the next fill or stroke paints a retained path.</summary>
    UsePath = 22,
    /// <summary>Count, then that many path slots the browser can release.</summary>
    FreePaths = 23,
    /// <summary>Slot, string index: keeps a string of this frame's table under a slot.</summary>
    RetainString = 24,
    /// <summary>Count, then that many string slots the browser can release.</summary>
    FreeStrings = 25,
}
