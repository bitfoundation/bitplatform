namespace Bit.Bortable;

/// <summary>
/// What a list tells <c>bit-bortable.js</c> about itself when it registers, and again whenever one of these values
/// changes: everything the browser layer needs to route the pointer and hit-test the list without asking .NET.
/// A record, so an unchanged set is never sent twice.
/// </summary>
internal sealed record BortableJsOptions
{
    public required string Id { get; init; }

    public string? Handle { get; init; }

    public double SwapThreshold { get; init; }

    public bool InvertSwap { get; init; }

    public double InvertedSwapThreshold { get; init; }

    /// <summary><c>auto</c>, <c>horizontal</c> or <c>vertical</c>.</summary>
    public required string Direction { get; init; }

    public int Animation { get; init; }

    /// <summary>Swap mode: the hit test picks the item under the pointer instead of a gap between items.</summary>
    public bool Swap { get; init; }

    /// <summary>Several items can be selected and dragged together (Shift+Space selects from the keyboard).</summary>
    public bool MultiDrag { get; init; }

    /// <summary>The items can be picked up, moved and dropped from the keyboard.</summary>
    public bool Keyboard { get; init; }

    /// <summary>How long (ms) a mouse or pen press must be held before a drag starts.</summary>
    public int Delay { get; init; }

    /// <summary>How long (ms) a touch must be held before a drag starts.</summary>
    public int TouchDelay { get; init; }

    /// <summary>How far (px) the pointer may move while a delay runs.</summary>
    public int DelayTolerance { get; init; }

    /// <summary>The mouse drags with pointer events and a preview, like a finger.</summary>
    public bool ForceFallback { get; init; }

    /// <summary>The class of the preview a pointer drag shows.</summary>
    public required string DragClass { get; init; }

    /// <summary>A pointer drag near an edge scrolls what is there.</summary>
    public bool Scroll { get; init; }

    /// <summary>How close (px) to an edge scrolls it.</summary>
    public int ScrollSensitivity { get; init; }

    /// <summary>The most pixels per frame an edge scrolls.</summary>
    public int ScrollSpeed { get; init; }
}
