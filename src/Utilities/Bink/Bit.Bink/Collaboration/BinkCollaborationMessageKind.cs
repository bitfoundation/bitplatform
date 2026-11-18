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
