namespace Bit.Bcrdt.Demo.Client.Collaboration;

/// <summary>
/// One independent participant in a demo: its own service, its own document, its own connection.
/// </summary>
/// <remarks>
/// <para>
/// This is the type that makes the demos honest. Each replica owns a separate
/// <see cref="BcrdtCollaborationService"/>, and therefore a separate <c>BcrdtDocument</c> with its own
/// client identifier, its own presence entry, its own private store and its own transport. Two
/// replicas of the same document know nothing about each other except what arrives over the
/// network, which is the only arrangement in which "watch them merge" means anything.
/// </para>
/// <para>
/// Going offline is a real disconnect rather than a UI flag: the transport leaves the room, so
/// nothing it produces reaches anyone and nothing anyone produces reaches it. Edits keep working
/// and accumulate in the document, and the handshake reconciles the divergence on reconnect.
/// </para>
/// </remarks>
public sealed class DemoReplica : IAsyncDisposable
{
    private readonly BcrdtCollaborationService _service;
    private readonly DemoTransportFactory _transports;
    private bool _wantsConnection;

    private DemoReplica(
        string label,
        BcrdtCollaboratorInfo user,
        BcrdtCollaborationService service,
        DemoTransportFactory transports,
        BcrdtCollaborativeDocument document)
    {
        Label = label;
        User = user;
        _service = service;
        _transports = transports;
        Document = document;
        _wantsConnection = true;
    }

    /// <summary>The name shown on the replica's panel, for example "Replica A".</summary>
    public string Label { get; }

    /// <summary>Who this replica publishes itself as.</summary>
    public BcrdtCollaboratorInfo User { get; }

    /// <summary>The open document. Everything a page edits goes through this.</summary>
    public BcrdtCollaborativeDocument Document { get; }

    /// <summary>The replica's presence colour, used for its panel header.</summary>
    public string Color => User.Color;

    /// <summary>This replica's session identity, as the engine sees it.</summary>
    public ulong ClientId => Document.ClientId;

    /// <summary>The client identity, shortened to something a person can compare at a glance.</summary>
    public string ShortClientId => (Document.ClientId % 100_000).ToString("D5");

    /// <summary>Where this replica's connection stands.</summary>
    public BcrdtConnectionStatus Status => Document.ConnectionStatus;

    /// <summary>Whether the replica is meant to be connected, regardless of momentary state.</summary>
    public bool IsOnline => _wantsConnection;

    /// <summary>The underlying in-memory transport, for demonstrating an abrupt connection loss.</summary>
    public BcrdtInMemoryTransport? Transport => _transports.Transport;

    internal static async Task<DemoReplica> OpenAsync(
        BcrdtInMemoryNetwork network,
        IBcrdtDocumentStore store,
        string documentId,
        string label,
        BcrdtCollaboratorInfo user,
        bool keepHistory = false)
    {
        var transports = new DemoTransportFactory(network);

        var options = new BcrdtCollaborationOptions
        {
            // Deliberately brisk. The defaults (30 s presence, 10 s heartbeat) are right for a real
            // application; in a demo nobody will wait half a minute for a departed replica to fade.
            PresenceTimeout = TimeSpan.FromSeconds(12),
            Sync = new BcrdtSyncProviderOptions { HeartbeatInterval = TimeSpan.FromSeconds(4) },

            // A snapshot names deleted content as well as live content, so restoring one needs every
            // tombstone the document ever had. Only the version-history page asks for that.
            GarbageCollect = !keepHistory,
        };

        var service = new BcrdtCollaborationService(options, transports, store);
        BcrdtCollaborativeDocument document = await service
            .OpenAsync(documentId, new BcrdtOpenDocumentOptions { User = user })
            .ConfigureAwait(false);

        return new DemoReplica(label, user, service, transports, document);
    }

    /// <summary>Connects the replica and republishes who it is.</summary>
    /// <remarks>
    /// Republishing is necessary rather than belt-and-braces: disconnecting clears the local
    /// awareness entry so peers see the departure immediately, and awareness is never restored from
    /// the document because it is not part of it.
    /// </remarks>
    public async Task GoOnlineAsync()
    {
        if (_wantsConnection) return;
        _wantsConnection = true;

        await Document.ConnectAsync().ConfigureAwait(false);
        Document.Presence.SetUser(User);
    }

    /// <summary>Disconnects the replica, leaving it fully editable.</summary>
    public async Task GoOfflineAsync()
    {
        if (!_wantsConnection) return;
        _wantsConnection = false;

        await Document.DisconnectAsync().ConfigureAwait(false);
    }

    /// <summary>Flips the replica between online and offline.</summary>
    public Task ToggleConnectionAsync() => _wantsConnection ? GoOfflineAsync() : GoOnlineAsync();

    /// <summary>Drops the connection without a graceful goodbye, as a flaky network would.</summary>
    /// <remarks>
    /// Unlike <see cref="GoOfflineAsync"/> this leaves the replica wanting a connection, so the sync
    /// provider's backoff loop reconnects on its own and the handshake repairs the divergence.
    /// </remarks>
    public void SimulateNetworkDrop() => Transport?.SimulateDrop();

    /// <inheritdoc/>
    public ValueTask DisposeAsync() => _service.DisposeAsync();
}
