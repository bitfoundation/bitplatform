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
}
