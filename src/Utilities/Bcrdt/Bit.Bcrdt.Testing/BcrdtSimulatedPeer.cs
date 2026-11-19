namespace Bit.Bcrdt;

/// <summary>
/// A replica attached to a simulated network.
/// </summary>
/// <remarks>
/// Owns a document and an awareness store and speaks the real wire protocol. Its handshake decisions
/// are made by <see cref="BcrdtSyncExchange"/>, the same code <see cref="BcrdtSyncProvider"/> runs, so a test
/// exercises the production encoding, handshake and integration paths rather than a copy of them.
/// What it leaves out is the connection state machine, which a deterministic network has no use for.
/// </remarks>
public sealed class BcrdtSimulatedPeer : IDisposable
{
    private readonly BcrdtSimulatedNetwork _network;

    internal BcrdtSimulatedPeer(BcrdtSimulatedNetwork network, string name, ulong clientId, bool garbageCollect)
    {
        _network = network;
        Name = name;

        Doc = new BcrdtDocument(new BcrdtDocumentOptions
        {
            Id = name,
            ClientId = clientId,
            GarbageCollect = garbageCollect,
        });

        // On the network's clock, so presence expiry is as deterministic as everything else.
        Awareness = new BcrdtAwarenessStore(clientId, timeProvider: network.Clock);

        Doc.Updated += args =>
        {
            // Never echo back what the network just delivered.
            if (ReferenceEquals(args.Origin, this)) return;
            _network.Send(this, BcrdtSyncProtocol.EncodeUpdate(args.Update));
        };

        Awareness.Updated += update => _network.Send(this, BcrdtSyncProtocol.EncodeAwarenessUpdate(update));
    }

    /// <summary>A readable name, used in failure messages.</summary>
    public string Name { get; }

    /// <summary>The replica's document.</summary>
    public BcrdtDocument Doc { get; }

    /// <summary>The replica's presence registry.</summary>
    public BcrdtAwarenessStore Awareness { get; }

    /// <summary>
    /// Whether the peer is currently reachable.
    /// </summary>
    /// <remarks>
    /// An offline peer keeps accepting local edits - that is the whole point of offline-first - but
    /// neither sends nor receives. Bringing it back online runs the real handshake, so a test of
    /// "edit for an hour on a plane, then reconnect" is exactly a test of the production code path.
    /// </remarks>
    public bool IsOnline { get; private set; } = true;

    /// <summary>How many messages this peer has received.</summary>
    public int MessagesReceived { get; private set; }

    /// <summary>Disconnects the peer from the network.</summary>
    public void GoOffline() => IsOnline = false;

    /// <summary>
    /// Reconnects the peer and runs the handshake, reconciling however far it has diverged - in both
    /// directions, deletions included.
    /// </summary>
    public void GoOnline()
    {
        if (IsOnline) return;
        IsOnline = true;
        RequestSync();
    }

    /// <summary>Announces this replica's state to every reachable peer, opening a handshake.</summary>
    public void RequestSync()
    {
        _network.Send(this, BcrdtSyncExchange.CreateAnnouncement(Doc));
        if (Awareness.LocalState is not null)
        {
            _network.Send(this, BcrdtSyncProtocol.EncodeAwarenessUpdate(
                Awareness.EncodeUpdate([Awareness.LocalClientId])));
        }
    }

    internal void Receive(ReadOnlyMemory<byte> raw)
    {
        if (!IsOnline) return;
        MessagesReceived++;

        BcrdtSyncMessage message = BcrdtSyncProtocol.Decode(raw);
        switch (message.Kind)
        {
            case BcrdtSyncMessageKind.SyncStep1:
            case BcrdtSyncMessageKind.SyncStep1Reply:
            case BcrdtSyncMessageKind.SyncStep2:
            case BcrdtSyncMessageKind.Update:
            {
                List<byte[]> replies = [];
                BcrdtSyncExchange.Handle(Doc, message, this, replies);
                foreach (byte[] reply in replies) _network.Send(this, reply);
                break;
            }

            case BcrdtSyncMessageKind.AwarenessUpdate:
                Awareness.ApplyUpdate(message.Payload, origin: this);
                break;

            case BcrdtSyncMessageKind.AwarenessQuery:
                // Answer for ourselves only, as the provider does.
                if (Awareness.LocalState is not null)
                {
                    _network.Send(this, BcrdtSyncProtocol.EncodeAwarenessUpdate(
                        Awareness.EncodeUpdate([Awareness.LocalClientId])));
                }
                break;
        }
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        Awareness.Dispose();
        Doc.Dispose();
    }

    /// <inheritdoc/>
    public override string ToString() => Name;
}
