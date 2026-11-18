namespace Bit.Bink;

/// <summary>Information about a stroke event.</summary>
/// <param name="Stroke">The stroke, once it exists. Null when a stroke has only just started.</param>
/// <param name="Device">The device that drew it.</param>
public readonly record struct BinkStrokeEventArgs(BinkStroke? Stroke, BinkPointerKind Device);
