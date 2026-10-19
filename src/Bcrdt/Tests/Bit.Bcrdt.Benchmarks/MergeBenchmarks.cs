using BenchmarkDotNet.Attributes;

namespace Bit.Bcrdt.Benchmarks;

/// <summary>
/// Measures what convergence costs: merging work that two replicas did without seeing each other.
/// </summary>
/// <remarks>
/// <para>
/// This is the scenario that separates a CRDT from a text box. Two people edit the same paragraph
/// while offline, or on a flaky connection, or simply in the two hundred milliseconds before a
/// broadcast lands, and then their edits have to be reconciled with no server arbitrating and no
/// possibility of asking either of them to resolve anything. The fixtures deliberately diverge in
/// two different shapes - one replica appends, which run-length encoding collapses into a handful of
/// blocks, and the other inserts at the very front, which cannot collapse at all and produces one
/// block per edit - because the cost of integration depends far more on block count than on
/// character count.
/// </para>
/// <para>
/// The primary budget defended here is <i>merge two 50 000-op divergent replicas</i> under 200 ms
/// from the Performance section of the Bcrdt README. Two secondary claims are also measured. Its
/// wire format section says idempotency is structural - a block already covered by the local state
/// vector is skipped without its content being inspected - so re-applying a whole document a
/// replica already holds should cost close to nothing; <see cref="ApplyUpdateAlreadyHeld"/> is that
/// claim. And the same section says updates concatenate, which is what lets persistence batch and
/// compact its log, and would let a relay coalesce a burst; comparing
/// <see cref="MergeManyIncrementalUpdates"/> against <see cref="MergeOneCoalescedUpdate"/> prices
/// that optimization.
/// </para>
/// </remarks>
[MemoryDiagnoser]
[InvocationCount(1, unrollFactor: 1)]
public class MergeBenchmarks
{
    private const string RootName = "body";
    private const int AncestorLength = 1_000;

    /// <summary>How many times <see cref="ApplyUpdateAlreadyHeld"/> re-applies the update per invocation.</summary>
    private const int RepeatedApplications = 100;

    private byte[] _sharedAncestor = null!;
    private byte[] _appendingReplicaDelta = null!;
    private byte[] _prependingReplicaDelta = null!;
    private byte[] _appendingReplicaFullState = null!;
    private ReadOnlyMemory<byte>[] _incrementalUpdates = null!;
    private byte[] _coalescedUpdate = null!;

    private BcrdtDocument _divergedReplica = null!;
    private BcrdtDocument _atAncestor = null!;
    private BcrdtDocument _fullySynced = null!;

    /// <summary>How many single-character edits each replica makes while unaware of the other.</summary>
    [Params(1_000, 10_000, 50_000)]
    public int EditsPerReplica;

    /// <summary>Builds a common ancestor and two divergent histories on top of it.</summary>
    /// <remarks>
    /// The ancestor matters: without it the "merge" would just be one replica receiving a document
    /// it has never seen any part of, which exercises decoding rather than integration. With it,
    /// every incoming block anchors to content the receiver already has, and the engine has to
    /// resolve real concurrent insertions at shared positions.
    /// </remarks>
    [GlobalSetup]
    public void GlobalSetup()
    {
        using var ancestor = new BcrdtDocument(new BcrdtDocumentOptions { ClientId = 1 });
        ancestor.GetText(RootName).Insert(0, new string('-', AncestorLength));
        _sharedAncestor = ancestor.EncodeStateAsUpdate();
        BcrdtStateVector ancestorState = ancestor.GetStateVector();

        // Replica A appends. Adjacent clocks and adjacent positions mean these blocks merge, so the
        // delta stays a few blocks however many keystrokes produced it.
        using var appending = new BcrdtDocument(new BcrdtDocumentOptions { ClientId = 10 });
        appending.ApplyUpdate(_sharedAncestor);
        for (int i = 0; i < EditsPerReplica; i++) appending.GetText(RootName).Insert(appending.GetText(RootName).Count, "a");
        _appendingReplicaDelta = appending.EncodeStateAsUpdate(ancestorState);
        _appendingReplicaFullState = appending.EncodeStateAsUpdate();

        // Replica B types at the very front of the document. Each character lands to the left of the
        // one before it, so clock order runs opposite to list order and nothing can be merged: this
        // delta carries one block per edit and is the expensive side of the merge.
        using var prepending = new BcrdtDocument(new BcrdtDocumentOptions { ClientId = 20 });
        var captured = new List<ReadOnlyMemory<byte>>(EditsPerReplica);
        prepending.ApplyUpdate(_sharedAncestor);
        prepending.Updated += args => captured.Add(args.Update);
        for (int i = 0; i < EditsPerReplica; i++) prepending.GetText(RootName).Insert(0, "b");
        _prependingReplicaDelta = prepending.EncodeStateAsUpdate(ancestorState);

        // The same history in two shapes: as the peer received it keystroke by keystroke, and as a
        // relay or a compacting store would have folded it before sending.
        _incrementalUpdates = [.. captured];
        _coalescedUpdate = BcrdtUpdateOperations.Merge(_incrementalUpdates);
    }

    /// <summary>Restores the three receiving documents before every measured iteration.</summary>
    /// <remarks>
    /// Every benchmark here except <see cref="ApplyUpdateAlreadyHeld"/> permanently changes the
    /// document it merges into, and a second merge of the same update would take the idempotent fast
    /// path and report a number an order of magnitude too good. Rebuilding from pre-encoded updates
    /// keeps the reset out of the measurement, and the class runs one invocation per iteration so
    /// the reset and the mutation happen equally often.
    /// </remarks>
    [IterationSetup]
    public void IterationSetup()
    {
        _divergedReplica?.Dispose();
        _atAncestor?.Dispose();
        _fullySynced?.Dispose();

        _divergedReplica = new BcrdtDocument(new BcrdtDocumentOptions { ClientId = 11 });
        _divergedReplica.ApplyUpdate(_sharedAncestor);
        _divergedReplica.ApplyUpdate(_appendingReplicaDelta);

        _atAncestor = new BcrdtDocument(new BcrdtDocumentOptions { ClientId = 12 });
        _atAncestor.ApplyUpdate(_sharedAncestor);

        _fullySynced = new BcrdtDocument(new BcrdtDocumentOptions { ClientId = 13 });
        _fullySynced.ApplyUpdate(_appendingReplicaFullState);

        // A real receiver re-encodes every update it integrates, because its store (and any relay
        // downstream of it) is subscribed. Attached after the fixtures are loaded, so only the
        // measured merges pay for it. See UpdateSink.
        UpdateSink.Attach(_divergedReplica);
        UpdateSink.Attach(_atAncestor);
        UpdateSink.Attach(_fullySynced);
    }

    /// <summary>Releases the fixtures.</summary>
    [GlobalCleanup]
    public void GlobalCleanup()
    {
        _divergedReplica?.Dispose();
        _atAncestor?.Dispose();
        _fullySynced?.Dispose();
    }

    /// <summary>Merges the other replica's entire divergent history in one update.</summary>
    /// <remarks>
    /// The headline convergence number. The receiver already holds the ancestor and its own edits,
    /// so this is pure YATA integration: for each incoming block, locate its origins, resolve it
    /// against everything concurrent, splice it in, then compact.
    /// </remarks>
    [Benchmark(Description = "Merge a divergent replica's whole history")]
    public int MergeDivergentReplicas()
    {
        _divergedReplica.ApplyUpdate(_prependingReplicaDelta);
        return _divergedReplica.GetText(RootName).Count;
    }

    /// <summary>Applies a full document update to a replica that already holds all of it.</summary>
    /// <remarks>
    /// The idempotency fast path, and the reason the sync protocol can be careless about
    /// at-least-once delivery. Nothing integrates and nothing changes; the cost should be one
    /// decode plus one state-vector comparison per block, which run-length encoding keeps to a
    /// handful regardless of <see cref="EditsPerReplica"/>. This is the one non-destructive
    /// benchmark in the class - by definition, since applying it twice must be indistinguishable
    /// from applying it once - which is also what allows it to be batched: a single application
    /// takes microseconds, too short to time once per iteration, so it is repeated and reported per
    /// application.
    /// </remarks>
    [Benchmark(OperationsPerInvoke = RepeatedApplications, Description = "Apply an update the replica already holds in full")]
    public int ApplyUpdateAlreadyHeld()
    {
        BcrdtDocument document = _fullySynced;
        byte[] update = _appendingReplicaFullState;
        for (int i = 0; i < RepeatedApplications; i++) document.ApplyUpdate(update);
        return document.GetText(RootName).Count;
    }

    /// <summary>Applies one update per remote keystroke, in order.</summary>
    /// <remarks>
    /// What a connected peer actually experiences during a burst of remote typing: one decode, one
    /// transaction, one event flush and one compaction pass per keystroke. Compare with
    /// <see cref="MergeOneCoalescedUpdate"/> to see what the per-update overhead is worth.
    /// </remarks>
    [Benchmark(Description = "Merge N small updates, one per remote keystroke")]
    public int MergeManyIncrementalUpdates()
    {
        ReadOnlyMemory<byte>[] updates = _incrementalUpdates;
        for (int i = 0; i < updates.Length; i++) _atAncestor.ApplyUpdate(updates[i]);
        return _atAncestor.GetText(RootName).Count;
    }

    /// <summary>Applies the same history pre-folded into a single equivalent update.</summary>
    /// <remarks>
    /// The result is byte-for-byte the same document as
    /// <see cref="MergeManyIncrementalUpdates"/> produces. Any difference in time is the fixed cost
    /// per update - decode, transaction, observers, compaction - which is exactly what persistence
    /// compaction eliminates, and what a coalescing relay would.
    /// </remarks>
    [Benchmark(Description = "Merge one update equivalent to those N")]
    public int MergeOneCoalescedUpdate()
    {
        _atAncestor.ApplyUpdate(_coalescedUpdate);
        return _atAncestor.GetText(RootName).Count;
    }
}
