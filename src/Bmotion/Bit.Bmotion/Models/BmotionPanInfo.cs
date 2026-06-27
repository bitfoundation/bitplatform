namespace Bit.Bmotion;

/// <summary>
/// Information about a pan gesture provided to <c>OnPan</c> callbacks.
/// Matches the Framer Motion pan event info shape.
/// </summary>
public class BmotionPanInfo
{
    /// <summary>Current pointer position relative to the document.</summary>
    public required BmotionPointInfo Point { get; init; }

    /// <summary>Distance moved since the last event.</summary>
    public required BmotionPointInfo Delta { get; init; }

    /// <summary>Total distance moved since the pan gesture started.</summary>
    public required BmotionPointInfo Offset { get; init; }

    /// <summary>Current velocity of the pointer (pixels per second).</summary>
    public required BmotionPointInfo Velocity { get; init; }
}
