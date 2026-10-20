namespace Bit.Bcrdt.Testing;

/// <summary>How badly the simulated network misbehaves.</summary>
public sealed class BcrdtSimulatedNetworkOptions
{
    /// <summary>The random seed. Every run with the same seed produces the same schedule.</summary>
    public int Seed { get; init; } = 1;

    /// <summary>The chance that a message is silently discarded.</summary>
    public double DropProbability { get; init; }

    /// <summary>The chance that a message is delivered twice.</summary>
    public double DuplicateProbability { get; init; }

    /// <summary>The largest number of delivery steps a message may be delayed by.</summary>
    /// <remarks>
    /// Non-zero latency is what produces reordering: messages sent in one order arrive in another,
    /// which is precisely the condition the convergence proof has to hold under. With zero latency,
    /// messages are delivered in exactly the order they were sent.
    /// </remarks>
    public int MaxLatencySteps { get; init; }

    /// <summary>How far the simulated clock advances with each delivery step.</summary>
    /// <remarks>
    /// Drives presence expiry deterministically: a peer that falls silent disappears from the
    /// others' participant lists after a fixed number of steps, not after a wall-clock interval.
    /// </remarks>
    public TimeSpan StepDuration { get; init; } = TimeSpan.FromMilliseconds(100);
}

/// <summary>
/// A clock that only moves when the simulation says so.
/// </summary>
/// <remarks>
/// Starts at a fixed instant rather than now, so that two runs with the same seed see the same times.
/// </remarks>
public sealed class BcrdtSimulatedClock : TimeProvider
{
    private DateTimeOffset _now = new(2000, 1, 1, 0, 0, 0, TimeSpan.Zero);

    /// <inheritdoc/>
    public override DateTimeOffset GetUtcNow() => _now;

    /// <summary>Moves the clock forward.</summary>
    public void Advance(TimeSpan delta)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(delta, TimeSpan.Zero);
        _now += delta;
    }
}

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

/// <summary>
/// A deterministic, adversarial network for testing convergence.
/// </summary>
/// <remarks>
/// <para>
/// Distributed-systems bugs almost never show up under a well-behaved network; they show up when
/// messages are lost, duplicated, reordered, or arrive after a peer has been offline for an hour.
/// A test suite that only ever exercises the happy path proves very little about a CRDT.
/// </para>
/// <para>
/// Everything here is driven by a seeded generator, a step counter and a <see cref="BcrdtSimulatedClock"/>,
/// with no threads and no wall clock. That is what makes a failure actionable: it reproduces exactly,
/// every time, from the seed printed in the failure message, and it can be shrunk by hand.
/// </para>
/// </remarks>
public sealed class BcrdtSimulatedNetwork : IDisposable
{
    private readonly List<BcrdtSimulatedPeer> _peers = [];
    private readonly List<PendingMessage> _inFlight = [];
    private readonly HashSet<(string, string)> _partitions = [];
    private readonly BcrdtSimulatedNetworkOptions _options;
    private readonly Random _random;
    private long _step;
    private ulong _nextClientId = 1;

    /// <summary>Creates a network.</summary>
    public BcrdtSimulatedNetwork(BcrdtSimulatedNetworkOptions? options = null)
    {
        _options = options ?? new BcrdtSimulatedNetworkOptions();
        _random = new Random(_options.Seed);
    }

    /// <summary>The seed this network was created with. Print it in failure messages.</summary>
    public int Seed => _options.Seed;

    /// <summary>The clock every peer's presence registry runs on. Advances with each <see cref="Step"/>.</summary>
    public BcrdtSimulatedClock Clock { get; } = new();

    /// <summary>The peers on the network.</summary>
    public IReadOnlyList<BcrdtSimulatedPeer> Peers => _peers;

    /// <summary>How many messages are currently in flight.</summary>
    public int InFlightCount => _inFlight.Count;

    /// <summary>Total messages the network has carried.</summary>
    public int MessagesSent { get; private set; }

    /// <summary>Total messages the network has discarded.</summary>
    public int MessagesDropped { get; private set; }

    /// <summary>Raised as each message is handed to its target, in delivery order, for tracing and assertions.</summary>
    public event Action<BcrdtSimulatedPeer, ReadOnlyMemory<byte>>? Delivered;

    /// <summary>Adds a peer.</summary>
    public BcrdtSimulatedPeer AddPeer(string? name = null, bool garbageCollect = true)
    {
        ulong clientId = _nextClientId++;
        var peer = new BcrdtSimulatedPeer(this, name ?? $"peer-{clientId}", clientId, garbageCollect);
        _peers.Add(peer);
        return peer;
    }

    /// <summary>Adds several peers at once.</summary>
    public BcrdtSimulatedPeer[] AddPeers(int count, bool garbageCollect = true)
    {
        var peers = new BcrdtSimulatedPeer[count];
        for (int i = 0; i < count; i++) peers[i] = AddPeer(garbageCollect: garbageCollect);
        return peers;
    }

    /// <summary>Cuts the link between two peers, in both directions.</summary>
    public void Partition(BcrdtSimulatedPeer left, BcrdtSimulatedPeer right)
    {
        _partitions.Add((left.Name, right.Name));
        _partitions.Add((right.Name, left.Name));
    }

    /// <summary>Restores every cut link and re-runs the handshake everywhere.</summary>
    public void HealPartitions()
    {
        _partitions.Clear();
        foreach (BcrdtSimulatedPeer peer in _peers)
        {
            if (peer.IsOnline) peer.RequestSync();
        }
    }

    /// <summary>Advances the simulated clock without delivering anything.</summary>
    public void AdvanceTime(TimeSpan delta) => Clock.Advance(delta);

    /// <summary>Delivers everything currently due, in the order it was sent, then advances one step.</summary>
    public void Step()
    {
        _step++;
        Clock.Advance(_options.StepDuration);

        // Snapshot before delivering: a delivery produces replies, and those belong to the next step
        // rather than to this one. Without the snapshot a single Step could cascade indefinitely.
        List<PendingMessage> due = [];
        foreach (PendingMessage message in _inFlight)
        {
            if (message.DeliverAtStep <= _step) due.Add(message);
        }
        _inFlight.RemoveAll(message => message.DeliverAtStep <= _step);

        foreach (PendingMessage message in due)
        {
            if (!message.Target.IsOnline) continue;
            Delivered?.Invoke(message.Target, message.Payload);
            message.Target.Receive(message.Payload);
        }
    }

    /// <summary>
    /// Runs until nothing is in flight, so every replica has seen everything it is going to.
    /// </summary>
    /// <remarks>
    /// This is the quiescence point convergence is asserted at. The step cap turns an accidental
    /// message loop into a clear failure rather than a hung test.
    /// </remarks>
    public void Flush(int maxSteps = 10_000)
    {
        int steps = 0;
        while (_inFlight.Count > 0)
        {
            if (++steps > maxSteps)
            {
                throw new InvalidOperationException(
                    $"Network did not settle within {maxSteps} steps with {_inFlight.Count} message(s) " +
                    $"still in flight (seed {Seed}). This usually means messages are being generated " +
                    "in response to their own delivery.");
            }
            Step();
        }
    }

    /// <summary>Brings every peer fully up to date, bypassing loss and latency.</summary>
    /// <remarks>
    /// Used to establish a known-good baseline before the interesting part of a test, and to
    /// reconcile at the end when the assertion is about the final state rather than about delivery.
    /// </remarks>
    public void SynchronizeAll()
    {
        foreach (BcrdtSimulatedPeer source in _peers)
        {
            foreach (BcrdtSimulatedPeer target in _peers)
            {
                if (ReferenceEquals(source, target)) continue;
                target.Doc.ApplyUpdate(
                    source.Doc.EncodeStateAsUpdate(target.Doc.GetStateVector()), origin: target);
            }
        }
    }

    internal void Send(BcrdtSimulatedPeer sender, byte[] payload)
    {
        if (!sender.IsOnline) return;

        foreach (BcrdtSimulatedPeer target in _peers)
        {
            if (ReferenceEquals(sender, target)) continue;
            if (_partitions.Contains((sender.Name, target.Name))) continue;

            MessagesSent++;

            if (_options.DropProbability > 0 && _random.NextDouble() < _options.DropProbability)
            {
                MessagesDropped++;
                continue;
            }

            Enqueue(target, payload);

            if (_options.DuplicateProbability > 0 && _random.NextDouble() < _options.DuplicateProbability)
            {
                Enqueue(target, payload);
            }
        }
    }

    private void Enqueue(BcrdtSimulatedPeer target, byte[] payload)
    {
        long latency = _options.MaxLatencySteps > 0 ? _random.Next(0, _options.MaxLatencySteps + 1) : 0;
        _inFlight.Add(new PendingMessage(target, payload, _step + latency + 1));
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        foreach (BcrdtSimulatedPeer peer in _peers) peer.Dispose();
        _peers.Clear();
        _inFlight.Clear();
    }

    private sealed record PendingMessage(BcrdtSimulatedPeer Target, byte[] Payload, long DeliverAtStep);
}
