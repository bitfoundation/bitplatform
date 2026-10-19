using BenchmarkDotNet.Attributes;

namespace Bit.Bcrdt.Benchmarks;

/// <summary>
/// Measures local text editing, which is the scenario the whole engine is shaped around.
/// </summary>
/// <remarks>
/// <para>
/// Every benchmark here models something a person actually does in an editor. Appending one
/// character at a time is somebody typing a paragraph, and it is the case run-length encoding is
/// supposed to make almost free. Inserting at scattered positions in an already-large document is
/// somebody clicking into the middle of a page to fix a word, which is the path the cached
/// index-to-item search marker exists to serve. Inserting repeatedly at index zero is the opposite
/// extreme: the position is resolved from the head of the list with no lookup at all, so it is the
/// baseline cost of a keystroke against which the scattered case's lookup can be priced. Reading the
/// whole document back is what a Blazor component does when it renders, and deleting a large range
/// is a select-all-and-retype.
/// </para>
/// <para>
/// These defend three budgets from the Bcrdt README's Performance section: <i>insert 100 000
/// characters sequentially</i> under 150 ms, <i>steady-state keystroke (local, 1 MB doc)</i> under
/// 50 µs with no gen-2 allocations, and - through the scattered case measured against the
/// insert-at-zero baseline - the claim that position lookup is amortized constant rather than
/// linear in document length. Watch the per-operation cost as <see cref="Size"/> grows by two
/// orders of magnitude: if the scattered case grows with it, the search marker has stopped working.
/// </para>
/// <para>
/// Every document a benchmark mutates has an <see cref="UpdateSink"/> attached, because a real
/// document always has subscribers and therefore always encodes each transaction's update.
/// </para>
/// </remarks>
[MemoryDiagnoser]
[InvocationCount(1, unrollFactor: 1)]
public class TextEditingBenchmarks
{
    private const string RootName = "body";

    /// <summary>How many edits the batched benchmarks perform per invocation.</summary>
    /// <remarks>
    /// A single keystroke into a large document is a few microseconds, which is too short to time
    /// reliably when the class is pinned to one invocation per iteration. Performing a fixed batch
    /// and declaring it through <c>OperationsPerInvoke</c> gets both: a measurable interval and a
    /// result BenchmarkDotNet still reports per keystroke.
    /// </remarks>
    private const int BatchedEdits = 100;

    /// <summary>How many whole-document reads <see cref="ReadWholeDocument"/> performs per invocation.</summary>
    private const int BatchedReads = 100;

    private byte[] _prebuiltDocument = null!;
    private int[] _scatteredPositions = null!;

    private BcrdtDocument _emptyDocument = null!;
    private BcrdtText _emptyText = null!;
    private BcrdtDocument _largeDocument = null!;
    private BcrdtText _largeText = null!;

    /// <summary>The document size in UTF-16 characters.</summary>
    [Params(1_000, 10_000, 100_000)]
    public int Size;

    /// <summary>Builds the immutable inputs that every iteration is restored from.</summary>
    /// <remarks>
    /// The large document is captured once as an encoded update rather than kept as a live
    /// document, because restoring it per iteration then costs one decode instead of one edit per
    /// character. Run-length encoding is what makes the shortcut faithful: a single insert of
    /// <see cref="Size"/> characters collapses to the same handful of blocks that typing them one at
    /// a time collapses to, so the fixture is structurally what a typed document looks like.
    /// </remarks>
    [GlobalSetup]
    public void GlobalSetup()
    {
        using var source = new BcrdtDocument(new BcrdtDocumentOptions { ClientId = 1 });
        source.GetText(RootName).Insert(0, new string('a', Size));
        _prebuiltDocument = source.EncodeStateAsUpdate();

        // A fixed seed, so every iteration edits exactly the same positions in exactly the same
        // order. A benchmark whose workload varies between iterations reports its own noise.
        var random = new Random(20260907);
        _scatteredPositions = new int[BatchedEdits];
        for (int i = 0; i < BatchedEdits; i++) _scatteredPositions[i] = random.Next(Size);
    }

    /// <summary>Restores both fixtures to their pristine state before every measured iteration.</summary>
    /// <remarks>
    /// Four of the five benchmarks below mutate their document. Without this reset the second
    /// iteration would measure a larger and more fragmented document than the first, and
    /// <see cref="DeleteLargeRange"/> would find nothing left to delete and report the cost of doing
    /// nothing. The class is pinned to one invocation per iteration precisely so that this reset
    /// runs exactly as often as the mutation it undoes.
    /// </remarks>
    [IterationSetup]
    public void IterationSetup()
    {
        _emptyDocument?.Dispose();
        _largeDocument?.Dispose();

        _emptyDocument = UpdateSink.Attach(new BcrdtDocument(new BcrdtDocumentOptions { ClientId = 2 }));
        _emptyText = _emptyDocument.GetText(RootName);

        _largeDocument = new BcrdtDocument(new BcrdtDocumentOptions { ClientId = 3 });
        _largeDocument.ApplyUpdate(_prebuiltDocument);
        _largeText = _largeDocument.GetText(RootName);

        // Attached after loading, so restoring the fixture does not pay for an encoding nobody
        // measures, and before measuring, so every measured keystroke does. See UpdateSink.
        UpdateSink.Attach(_largeDocument);
    }

    /// <summary>Releases the fixtures.</summary>
    [GlobalCleanup]
    public void GlobalCleanup()
    {
        _emptyDocument?.Dispose();
        _largeDocument?.Dispose();
    }

    /// <summary>Types <see cref="Size"/> characters into an empty document, one transaction each.</summary>
    /// <remarks>
    /// The headline number. Each iteration of the loop is a complete keystroke - position lookup,
    /// integration, block merge, delete-set compaction and update encoding (the document has an
    /// <see cref="UpdateSink"/> attached, as a real one has a sync provider and a store) - so this
    /// is the budget that says a 100 000-character paste-by-typing stays under 150 ms.
    /// </remarks>
    [Benchmark(Description = "Append N characters, one keystroke per transaction")]
    public int SequentialAppend()
    {
        BcrdtText text = _emptyText;
        for (int i = 0; i < Size; i++) text.Insert(text.Count, "a");
        return text.Count;
    }

    /// <summary>Inserts single characters at scattered positions in a large document.</summary>
    /// <remarks>
    /// The search-marker path. Positions are drawn once from a seeded generator and are always below
    /// the document's original length, so they stay valid as the batch grows the document.
    /// </remarks>
    [Benchmark(OperationsPerInvoke = BatchedEdits, Description = "Insert at a scattered position in a large document")]
    public int InsertAtScatteredPositions()
    {
        BcrdtText text = _largeText;
        int[] positions = _scatteredPositions;
        for (int i = 0; i < positions.Length; i++) text.Insert(positions[i], "z");
        return text.Count;
    }

    /// <summary>Inserts single characters at index zero of a large document.</summary>
    /// <remarks>
    /// The baseline with no position lookup at all: index zero is resolved from the head of the
    /// list without consulting the marker cache, so this costs the same at any <see cref="Size"/>.
    /// What it does still pay for is everything else in a keystroke - integration, a block that can
    /// never merge with its neighbour (each character lands to the left of the previous one), and
    /// the update encoding. The gap between this and <see cref="InsertAtScatteredPositions"/> is the
    /// price of finding a position; if that gap grows with <see cref="Size"/>, the cache has stopped
    /// working.
    /// </remarks>
    [Benchmark(OperationsPerInvoke = BatchedEdits, Description = "Insert at index zero of a large document")]
    public int InsertAtBeginning()
    {
        BcrdtText text = _largeText;
        for (int i = 0; i < BatchedEdits; i++) text.Insert(0, "z");
        return text.Count;
    }

    /// <summary>Materializes the whole document as a string.</summary>
    /// <remarks>
    /// What rendering costs. This walks the block list once and appends every live run, so it should
    /// be linear in content and indifferent to how fragmented the blocks are. Read-only, and at the
    /// smaller sizes a single read is a microsecond or two - too short to time once per iteration -
    /// so it is batched like the keystroke benchmarks and reported per read.
    /// </remarks>
    [Benchmark(OperationsPerInvoke = BatchedReads, Description = "ToString() over a large document")]
    public int ReadWholeDocument()
    {
        BcrdtText text = _largeText;
        int total = 0;
        for (int i = 0; i < BatchedReads; i++) total += text.ToString().Length;
        return total;
    }

    /// <summary>Deletes the middle half of the document in one transaction.</summary>
    /// <remarks>
    /// Destructive, and restored by <see cref="IterationSetup"/>. A range delete splits the blocks
    /// at both ends, tombstones everything between them, compacts the delete set and - because
    /// garbage collection is on by default - drops the content of what it tombstoned.
    /// </remarks>
    [Benchmark(Description = "Delete a large range in one transaction")]
    public int DeleteLargeRange()
    {
        BcrdtText text = _largeText;
        text.Delete(Size / 4, Size / 2);
        return text.Count;
    }
}
