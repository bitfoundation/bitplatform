using System.Numerics;

namespace Bit.Bink;

/// <summary>
/// What another participant is doing right now: where their pointer is, what they have selected, and the stroke
/// they are drawing. Presence is transient and never recorded in history or persisted with the document.
/// </summary>
public sealed record BinkPresenceState
{
    /// <summary>Stable identifier of the participant.</summary>
    public required string ParticipantId { get; init; }

    /// <summary>Display name.</summary>
    public string? Name { get; init; }

    /// <summary>Colour used for their cursor and selection outline, as a CSS colour.</summary>
    public string? Color { get; init; }

    /// <summary>Pointer position in document coordinates, or null when the pointer is away.</summary>
    public Vector2? Cursor { get; init; }

    /// <summary>Ids of the elements they have selected.</summary>
    public IReadOnlyList<Guid>? Selection { get; init; }

    /// <summary>
    /// The points of the stroke they are currently drawing, in document coordinates, so other participants can
    /// see ink appear as it is drawn rather than only when the pen lifts.
    /// </summary>
    public IReadOnlyList<Vector2>? LiveStroke { get; init; }

    /// <summary>Colour of the live stroke, as a CSS colour.</summary>
    public string? LiveStrokeColor { get; init; }

    /// <summary>Width of the live stroke in document units.</summary>
    public float LiveStrokeWidth { get; init; }

    /// <summary>When the state was produced, used to expire stale participants.</summary>
    public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.UtcNow;

    /// <summary>Bounding box of the live stroke, used to limit redraws.</summary>
    public BinkRect LiveStrokeBounds
    {
        get
        {
            if (LiveStroke is not { Count: > 0 }) return BinkRect.Empty;
            BinkRect bounds = BinkRect.Empty;
            foreach (Vector2 p in LiveStroke) bounds = bounds.Union(p);
            return bounds.Inflate(LiveStrokeWidth);
        }
    }
}
