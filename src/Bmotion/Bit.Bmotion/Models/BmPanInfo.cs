namespace Bit.Bmotion;

/// <summary>
/// Information about a pan gesture provided to <c>OnPan</c> callbacks.
/// Matches the Framer Motion pan event info shape.
/// </summary>
public class BmPanInfo
{
    /// <summary>Current pointer position relative to the document.</summary>
    public required BmPoint Point { get; init; }

    /// <summary>Distance moved since the last event.</summary>
    public required BmPoint Delta { get; init; }

    /// <summary>Total distance moved since the pan gesture started.</summary>
    public required BmPoint Offset { get; init; }

    /// <summary>Current velocity of the pointer (pixels per second).</summary>
    public required BmPoint Velocity { get; init; }
}
