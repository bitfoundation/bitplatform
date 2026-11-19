namespace Bit.Bcrdt;

/// <summary>Application-wide collaboration settings.</summary>
public sealed class BcrdtCollaborationOptions
{
    /// <summary>Where client session identifiers come from.</summary>
    public IBcrdtClientIdProvider ClientIdProvider { get; set; } = BcrdtRandomClientIdProvider.Instance;

    /// <summary>Whether documents reclaim deleted content. Turn off only if snapshots are needed.</summary>
    public bool GarbageCollect { get; set; } = true;

    /// <summary>How long a participant's presence survives without a refresh.</summary>
    public TimeSpan PresenceTimeout { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>Settings for the synchronization providers this service creates.</summary>
    public BcrdtSyncProviderOptions Sync { get; set; } = new();

    /// <summary>Settings for the persistence bindings this service creates.</summary>
    public BcrdtDocumentPersistenceOptions Persistence { get; set; } = new();

    /// <summary>Whether to connect a document as soon as it is opened.</summary>
    public bool ConnectOnOpen { get; set; } = true;

    /// <summary>
    /// Whether each document gets a default undo stack (<see cref="BcrdtCollaborativeDocument.Undo"/>).
    /// </summary>
    public bool CreateUndoManager { get; set; } = true;
}
