using BenchmarkDotNet.Attributes;

namespace Bit.Bcrdt.Benchmarks;

/// <summary>
/// Measures how the engine scales with the number of participants rather than with document size.
/// </summary>
/// <remarks>
/// <para>
/// Two things get more expensive as a room fills up, and they get more expensive for different
/// reasons. Document convergence costs more because every replica's edits are concurrent with every
/// other replica's, so the number of conflicts the integration algorithm has to resolve at a shared
/// position grows with the participant count. Awareness costs more because presence is a broadcast:
/// one cursor movement has to reach everybody, and it happens on every keystroke and every mouse
/// drag rather than once per commit.
/// </para>
/// <para>
/// The convergence benchmark performs a full all-to-all exchange, which is deliberately the
/// pessimistic topology. Real deployments put a server in the middle and pay <c>O(n)</c> per update
/// instead of <c>O(n²)</c>, but the server is just a well-connected peer running this same code, so
/// the all-to-all number bounds it. The awareness benchmark defends the <i>awareness update fan-out
/// (100 clients)</i> budget of under 1 ms from the Performance section of the Bcrdt README; the
/// convergence benchmark backs up the claim of its synchronization protocol section that the
/// protocol reconciles an arbitrary divergence in one round trip by pricing what that round trip
/// costs.
/// </para>
/// </remarks>
[MemoryDiagnoser]
[InvocationCount(1, unrollFactor: 1)]
public class ConcurrencyBenchmarks
{
    private const string RootName = "body";
    private const int AncestorLength = 1_000;

    /// <summary>How many characters each replica types before anybody synchronizes.</summary>
    /// <remarks>
    /// Deliberately small. An all-to-all exchange performs <c>n²</c> encodings of a document whose
    /// size is itself proportional to <c>n</c> times this constant, so the total work grows with the
    /// cube of the participant count and raising this by a factor of five makes the 50-replica case
    /// twenty-five times more expensive. Twenty edits each is enough to give every replica several
    /// concurrent blocks competing for the same position, which is what the benchmark is for.
    /// </remarks>
    private const int EditsPerReplica = 20;

    /// <summary>How many cursor movements <see cref="AwarenessFanOut"/> publishes per invocation.</summary>
    private const int CursorMovements = 100;

    private byte[] _sharedAncestor = null!;
    private byte[][] _replicaDeltas = null!;
    private BcrdtDocument[] _replicas = null!;

    private BcrdtAwarenessStore _author = null!;
    private BcrdtAwarenessStore[] _peers = null!;
    private byte[] _lastAuthorUpdate = null!;
    private int _cursorPosition;

    /// <summary>How many replicas take part.</summary>
    [Params(2, 10, 50)]
    public int ReplicaCount;

    /// <summary>Builds each replica's independent history and the awareness participants.</summary>
    /// <remarks>
    /// Every replica types at index zero of the same shared ancestor, so all of their runs are
    /// concurrent insertions competing for one position - the case that actually exercises the
    /// dual-origin conflict rule rather than letting each replica's blocks land in their own corner
    /// of the document.
    /// </remarks>
    [GlobalSetup]
    public void GlobalSetup()
    {
        using var ancestor = new BcrdtDocument(new BcrdtDocumentOptions { ClientId = 1 });
        ancestor.GetText(RootName).Insert(0, new string('-', AncestorLength));
        _sharedAncestor = ancestor.EncodeStateAsUpdate();
        BcrdtStateVector ancestorState = ancestor.GetStateVector();

        _replicaDeltas = new byte[ReplicaCount][];
        for (int replica = 0; replica < ReplicaCount; replica++)
        {
            using var author = new BcrdtDocument(new BcrdtDocumentOptions { ClientId = 100UL + (ulong)replica });
            author.ApplyUpdate(_sharedAncestor);
            char glyph = (char)('a' + replica % 26);
            for (int edit = 0; edit < EditsPerReplica; edit++) author.GetText(RootName).Insert(0, glyph.ToString());
            _replicaDeltas[replica] = author.EncodeStateAsUpdate(ancestorState);
        }

        // Awareness is not a CRDT and has no document, so its fixture is independent: one client
        // publishing, and ReplicaCount peers receiving. The Updated event is the production path -
        // a sync provider subscribes there and broadcasts whatever it is handed - so the benchmark
        // takes its bytes from the same place rather than encoding them a second time.
        _author = new BcrdtAwarenessStore(localClientId: 1);
        _author.Updated += update => _lastAuthorUpdate = update;
        _author.SetLocalField("user", "author");

        _peers = new BcrdtAwarenessStore[ReplicaCount];
        for (int peer = 0; peer < ReplicaCount; peer++)
        {
            _peers[peer] = new BcrdtAwarenessStore(localClientId: 1_000UL + (ulong)peer);
        }
    }

    /// <summary>Rebuilds the replica set before every measured iteration.</summary>
    /// <remarks>
    /// Synchronization is destructive by nature: once the replicas have converged they have nothing
    /// left to exchange, and a second run of <see cref="ConvergeAllToAll"/> would measure empty
    /// deltas. Each iteration therefore starts from a fresh set of documents holding the ancestor
    /// and their own edits and nothing else. The awareness fixture needs no reset because its clock
    /// only moves forward, so every invocation delivers genuinely new state to every peer.
    /// </remarks>
    [IterationSetup]
    public void IterationSetup()
    {
        if (_replicas is not null)
        {
            foreach (BcrdtDocument replica in _replicas) replica.Dispose();
        }

        _replicas = new BcrdtDocument[ReplicaCount];
        for (int replica = 0; replica < ReplicaCount; replica++)
        {
            var document = new BcrdtDocument(new BcrdtDocumentOptions { ClientId = 200UL + (ulong)replica });
            document.ApplyUpdate(_sharedAncestor);
            document.ApplyUpdate(_replicaDeltas[replica]);

            // A real peer re-encodes what it integrates for its own subscribers. Attached after the
            // fixture is loaded, so only the measured exchange pays for it. See UpdateSink.
            _replicas[replica] = UpdateSink.Attach(document);
        }
    }

    /// <summary>Releases the fixtures.</summary>
    [GlobalCleanup]
    public void GlobalCleanup()
    {
        if (_replicas is not null)
        {
            foreach (BcrdtDocument replica in _replicas) replica.Dispose();
        }

        // Disposing a store that is still publishing is safe: Dispose announces the departure itself
        // before it marks the store disposed.
        _author?.Dispose();

        if (_peers is not null)
        {
            foreach (BcrdtAwarenessStore peer in _peers) peer.Dispose();
        }
    }

    /// <summary>Synchronizes every replica with every other until all of them agree.</summary>
    /// <remarks>
    /// <para>
    /// One ordered pass is enough to converge, and that is a property of the protocol rather than a
    /// coincidence: by the time replica <c>i</c> starts sending it has already received from every
    /// replica before it, so the last sender pushes the complete history to everybody.
    /// </para>
    /// <para>
    /// Each exchange is the real handshake - the receiver's state vector goes out, and only the
    /// blocks it lacks come back - so the total measures both the encoding of <c>n²</c> incremental
    /// deltas and the integration of every replica's edits into every other replica.
    /// </para>
    /// </remarks>
    [Benchmark(Description = "All-to-all synchronization until every replica converges")]
    public int ConvergeAllToAll()
    {
        BcrdtDocument[] replicas = _replicas;

        for (int sender = 0; sender < replicas.Length; sender++)
        {
            for (int receiver = 0; receiver < replicas.Length; receiver++)
            {
                if (sender == receiver) continue;
                byte[] delta = replicas[sender].EncodeStateAsUpdate(replicas[receiver].GetStateVector());
                replicas[receiver].ApplyUpdate(delta);
            }
        }

        return replicas[0].GetText(RootName).Count;
    }

    /// <summary>Publishes one cursor movement and delivers it to every peer.</summary>
    /// <remarks>
    /// The whole fan-out, end to end: the local write bumps the author's awareness clock and raises
    /// the update event, which encodes the one changed entry, and each peer then decodes it and
    /// merges it. Every movement carries a strictly higher clock than the last, so no peer takes
    /// the "already seen this" shortcut and the measurement stays honest across iterations.
    /// A single movement to a handful of peers takes microseconds - too short to time once per
    /// iteration - so a batch of movements is published per invocation and reported per movement.
    /// </remarks>
    [Benchmark(OperationsPerInvoke = CursorMovements, Description = "Awareness fan-out: encode one presence change, apply to N peers")]
    public int AwarenessFanOut()
    {
        BcrdtAwarenessStore[] peers = _peers;
        int total = 0;

        for (int movement = 0; movement < CursorMovements; movement++)
        {
            _author.SetLocalField("cursor", _cursorPosition++);

            byte[] update = _lastAuthorUpdate;
            for (int peer = 0; peer < peers.Length; peer++) peers[peer].ApplyUpdate(update);
            total += update.Length;
        }

        return total;
    }
}
