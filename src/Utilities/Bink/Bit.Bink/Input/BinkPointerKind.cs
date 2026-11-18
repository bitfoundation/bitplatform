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
