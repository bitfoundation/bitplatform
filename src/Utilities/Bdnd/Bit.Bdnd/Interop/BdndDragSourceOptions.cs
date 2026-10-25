namespace Bit.Bdnd;

/// <summary>Options of a drag source registration in <c>bit-bdnd.js</c>.</summary>
internal sealed record BdndDragSourceOptions
{
    /// <summary>The draggable's handler id (also part of the drag's DataTransfer type).</summary>
    public string Id { get; init; } = "";

    /// <summary>Whether a drag may start.</summary>
    public bool CanDrag { get; init; }

    /// <summary>Whether a drag must start on a <see cref="BdndDragHandle"/>.</summary>
    public bool UseHandle { get; init; }

    /// <summary>Hide the native drag image (custom drag layers).</summary>
    public bool UseEmptyPreview { get; init; }

    /// <summary>The value assigned to <c>dataTransfer.effectAllowed</c>.</summary>
    public string EffectAllowed { get; init; } = "move";

    /// <summary>
    /// Whether to report the pointer on every animation frame of a drag (only a drag layer, or another
    /// <see cref="BdndDragDropManager.OffsetChanged"/> listener, follows it).
    /// </summary>
    public bool TrackPointer { get; init; }

    /// <summary>Whether Space or Enter on the source (or on its handle) picks it up for a keyboard drag.</summary>
    public bool Keyboard { get; init; }

    /// <summary>The id of the element holding the keyboard instructions the source is described by.</summary>
    public string? InstructionsId { get; init; }

    /// <summary>The keyboard instructions, which the module puts in the element <see cref="InstructionsId"/> names.</summary>
    public string? Instructions { get; init; }

    /// <summary>Which pointers drag through pointer events: <c>touch</c> (touch and pen), <c>all</c> or <c>none</c>.</summary>
    public string PointerDrag { get; init; } = "touch";

    /// <summary>How long, in milliseconds, a touch rests on the source before a pointer drag starts.</summary>
    public int TouchDelay { get; init; } = 250;

    /// <summary>How far, in CSS pixels, a pointer moves before a pointer drag starts (or, during the delay, gives it up).</summary>
    public double DragThreshold { get; init; } = 5;

    /// <summary>Whether a pointer drag scrolls the containers (and the page) it nears the edge of.</summary>
    public bool AutoScroll { get; init; } = true;

    /// <summary>The axis the copy following the pointer keeps to: <c>x</c>, <c>y</c>, or null for none.</summary>
    public string? PreviewAxis { get; init; }

    /// <summary>
    /// Data put on a native drag's <c>DataTransfer</c> at <c>dragstart</c>, by type, for destinations outside the page.
    /// The same instance is reused while the data is unchanged, so unchanged options compare equal.
    /// </summary>
    public Dictionary<string, string>? NativeData { get; init; }
}
