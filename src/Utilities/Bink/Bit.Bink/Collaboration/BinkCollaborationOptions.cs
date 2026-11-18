namespace Bit.Bink;

/// <summary>Options for a <see cref="BinkCollaborationSession"/>.</summary>
public sealed record BinkCollaborationOptions
{
    /// <summary>How long a participant's presence is kept after their last update.</summary>
    public TimeSpan PresenceTimeout { get; init; } = TimeSpan.FromSeconds(20);

    /// <summary>Minimum interval between outgoing presence updates. Presence is throttled, operations are not.</summary>
    public TimeSpan PresenceInterval { get; init; } = TimeSpan.FromMilliseconds(50);

    /// <summary>Maximum number of operations queued while disconnected. Older ones are dropped past this point.</summary>
    public int MaxOfflineQueue { get; init; } = 5000;

    /// <summary>Limits applied to incoming operations, which are untrusted.</summary>
    public BinkDocumentLimits Limits { get; init; } = BinkDocumentLimits.Default;

    /// <summary>Serialization options for outgoing operations.</summary>
    public BinkSerializerOptions SerializerOptions { get; init; } = BinkSerializerOptions.Default;

    /// <summary>Whether the session sends a snapshot when another participant asks for one.</summary>
    public bool ServeSnapshots { get; init; } = true;

    /// <summary>
    /// Whether messages that name another document are ignored. Every message carries the id of the document its
    /// sender edits, and a transport shared by several documents (one hub with no per-document group) delivers all
    /// of them to every session; without this, another document's strokes would be applied to this one. Off by
    /// default because a participant may join with a fresh document of its own id and take the content from a
    /// snapshot. Turn it on when every participant constructs its document with the shared id before joining.
    /// </summary>
    public bool IgnoreOtherDocuments { get; init; }
}
