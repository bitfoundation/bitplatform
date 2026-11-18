namespace Bit.Bcrdt;

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
