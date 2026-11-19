using System.Numerics;

namespace Bit.Bink;

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

    /// <summary>
    /// What the sender understands, set by the session: 0, which is also what a message from a version before this
    /// property reads as, or 1 when the sender can resolve an image referred to by its content id. A session refers
    /// to images that way, rather than sending their data again, only once every participant it has heard from
    /// understands it. A transport carries it like every other property.
    /// </summary>
    public int Protocol { get; init; }

    /// <summary>The serialized operation, for <see cref="BinkCollaborationMessageKind.Operation"/>.</summary>
    public string? Operation { get; init; }

    /// <summary>The serialized document, for <see cref="BinkCollaborationMessageKind.Snapshot"/>.</summary>
    public string? Snapshot { get; init; }

    /// <summary>The sender's presence, for <see cref="BinkCollaborationMessageKind.Presence"/>.</summary>
    public BinkPresenceState? Presence { get; init; }
}
