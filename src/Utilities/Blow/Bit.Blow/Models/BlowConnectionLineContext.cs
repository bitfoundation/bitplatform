namespace Bit.Blow;

/// <summary>
/// Live state of an in-progress connection, passed to a custom connection-line template
/// and raised by <c>BlowCanvas.OnConnectionChange</c>. Mirrors the data React Flow exposes
/// through its connection line component / <c>useConnection</c> hook.
/// </summary>
public sealed class BlowConnectionLineContext
{
    /// <summary>The node the connection started from.</summary>
    public required string FromNode { get; init; }

    /// <summary>The handle id the connection started from (may be null).</summary>
    public string? FromHandle { get; init; }

    /// <summary>Whether the originating handle is a source or target.</summary>
    public BlowHandleType FromHandleType { get; init; }

    /// <summary>The side the originating handle is anchored to.</summary>
    public BlowPosition FromPosition { get; init; }

    /// <summary>Start point in flow coordinates (the originating handle anchor).</summary>
    public BlowXYPosition From { get; init; }

    /// <summary>Current pointer point in flow coordinates.</summary>
    public BlowXYPosition To { get; init; }

    /// <summary>Computed end side used for path direction.</summary>
    public BlowPosition ToPosition { get; init; }

    /// <summary>True when the pointer is currently over a valid drop target.</summary>
    public bool IsValid { get; init; }

    /// <summary>
    /// Why the handle the line ends at - the one under the pointer, or the nearest within snapping distance -
    /// would refuse the connection, or <see cref="BlowConnectionRejection.None"/> when it would not, or when there
    /// is no handle nearby. A custom connection line can show the reason as the user drags.
    /// </summary>
    public BlowConnectionRejection Rejection { get; init; }
}
