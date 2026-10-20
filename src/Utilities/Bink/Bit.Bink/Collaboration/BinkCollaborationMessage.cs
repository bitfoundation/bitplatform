using System.Numerics;

namespace Bit.Bink;

/// <summary>The kinds of message a session exchanges.</summary>
public enum BinkCollaborationMessageKind : byte
{
    /// <summary>A document change, carrying one operation.</summary>
    Operation = 0,
    /// <summary>A participant's cursor, selection or in-progress stroke.</summary>
    Presence = 1,
    /// <summary>A full document, sent when a participant joins or resynchronizes.</summary>
    Snapshot = 2,
    /// <summary>A request for a snapshot.</summary>
    SnapshotRequest = 3,
    /// <summary>A participant left.</summary>
    Leave = 4,
}

/// <summary>
/// One message exchanged between participants. Messages are self-contained, and <see cref="Sequence"/> makes
/// them idempotent: a retried message is applied once. A message that arrives after a later one from the same
/// sender is discarded, so a transport must deliver each sender's messages in order.
/// </summary>
public sealed record BinkCollaborationMessage
{
    /// <summary>What the message carries.</summary>
    public required BinkCollaborationMessageKind Kind { get; init; }

    /// <summary>Who sent it.</summary>
    public required string SenderId { get; init; }

    /// <summary>
    /// The sender's own message counter. A message whose sequence is not greater than the last one seen from
    /// that sender is discarded as a duplicate or a late arrival.
    /// </summary>
    public long Sequence { get; init; }

    /// <summary>
    /// The document the message applies to. A session ignores messages for other documents when
    /// <see cref="BinkCollaborationOptions.IgnoreOtherDocuments"/> is set.
    /// </summary>
    public Guid DocumentId { get; init; }

    /// <summary>
    /// The participant the message is for, or null when it is for everyone. A transport may use it to deliver the
    /// message to that participant alone; a session ignores messages addressed to someone else, so a transport
    /// that broadcasts everything is still correct.
    /// </summary>
    public string? RecipientId { get; init; }

    /// <summary>The serialized operation, for <see cref="BinkCollaborationMessageKind.Operation"/>.</summary>
    public string? Operation { get; init; }

    /// <summary>The serialized document, for <see cref="BinkCollaborationMessageKind.Snapshot"/>.</summary>
    public string? Snapshot { get; init; }

    /// <summary>The sender's presence, for <see cref="BinkCollaborationMessageKind.Presence"/>.</summary>
    public BinkPresenceState? Presence { get; init; }
}

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

/// <summary>
/// Moves messages between participants. Implement this over SignalR, WebSockets, WebRTC or anything else; the
/// session makes no assumptions beyond eventual delivery.
/// </summary>
/// <remarks>
/// The transport is responsible for delivery and for excluding the sender from its own broadcasts. It must
/// deliver each sender's messages in order; a message that arrives after a later one from the same sender is
/// discarded. Duplicates are harmless. Ordering between different senders does not matter.
/// </remarks>
public interface IBinkCollaborationTransport
{
    /// <summary>True when the transport is connected and messages will be delivered.</summary>
    bool IsConnected { get; }

    /// <summary>Sends a message to the other participants.</summary>
    Task SendAsync(BinkCollaborationMessage message, CancellationToken cancellationToken = default);

    /// <summary>Raised for each message received from another participant.</summary>
    event Func<BinkCollaborationMessage, Task>? MessageReceived;

    /// <summary>Raised when the connection state changes. The session flushes its queue when it becomes true.</summary>
    event Action<bool>? ConnectionChanged;
}
