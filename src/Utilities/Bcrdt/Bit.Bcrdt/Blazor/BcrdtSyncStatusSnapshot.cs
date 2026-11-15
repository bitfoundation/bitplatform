namespace Bit.Bcrdt.Blazor;

/// <summary>
/// Where a document's connection stood at one moment: what <see cref="BcrdtSyncStatus"/> renders, and what it hands a
/// <see cref="BcrdtSyncStatus.ChildContent"/> template.
/// </summary>
/// <remarks>
/// A value rather than the live provider, so everything a template shows belongs to one render: the provider can move
/// on - from handshaking to synced, say - on another thread while the template is half rendered, and a template reading
/// it directly could show the two halves of a change. Taken again on every render.
/// </remarks>
/// <param name="Status">Where the connection stands. <see cref="BcrdtConnectionStatus.Disconnected"/> when there is no transport.</param>
/// <param name="HasTransport">Whether the document has a connection at all. Without one it is kept on this device only.</param>
/// <param name="Access">What the server allows this replica to do with the document.</param>
/// <param name="LastSyncedAt">When the document last caught up with its peers, or <see langword="null"/> if it never has.</param>
public sealed record BcrdtSyncStatusSnapshot(
    BcrdtConnectionStatus Status,
    bool HasTransport,
    BcrdtDocumentAccess Access,
    DateTimeOffset? LastSyncedAt)
{
    /// <summary>Whether no transport is configured, so the document is this device's alone - and still persisted, if it has a store.</summary>
    public bool IsLocalOnly => !HasTransport;

    /// <summary>Whether the document is believed to match its peers' right now.</summary>
    public bool IsSynced => HasTransport && Status == BcrdtConnectionStatus.Synced;

    /// <summary>Whether the server granted read access only: edits stay in this replica.</summary>
    public bool IsReadOnly => Access == BcrdtDocumentAccess.ReadOnly;

    /// <summary>
    /// Whether the connection is down and nothing is trying to bring it back: disconnected on purpose, or given up on
    /// (refused, say). What a connect button offers to change.
    /// </summary>
    /// <remarks>A connection that is reconnecting by itself is not offline in this sense: it is on its way back.</remarks>
    public bool IsOffline => HasTransport && Status is BcrdtConnectionStatus.Disconnected or BcrdtConnectionStatus.Faulted;

    /// <summary>Whether the connection is on its way to being synced: connecting, handshaking or reconnecting.</summary>
    public bool IsBusy => HasTransport && Status is BcrdtConnectionStatus.Connecting or BcrdtConnectionStatus.Handshaking or BcrdtConnectionStatus.Reconnecting;

    /// <summary>Takes a snapshot of where <paramref name="document"/>'s connection stands now.</summary>
    public static BcrdtSyncStatusSnapshot Of(BcrdtCollaborativeDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);

        // Read off the provider once, so the four values belong together as far as one read each allows.
        BcrdtSyncProvider? sync = document.Sync;
        return sync is null
            ? new(BcrdtConnectionStatus.Disconnected, HasTransport: false, BcrdtDocumentAccess.ReadWrite, LastSyncedAt: null)
            : new(sync.Status, HasTransport: true, sync.Access, sync.LastSyncedAt);
    }
}
