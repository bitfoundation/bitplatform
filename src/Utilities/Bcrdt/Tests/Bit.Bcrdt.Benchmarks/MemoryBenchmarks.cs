using BenchmarkDotNet.Attributes;

namespace Bit.Bcrdt.Benchmarks;

/// <summary>
/// Measures what a document costs to hold in memory, and shows that the cost depends on how it was
/// written rather than on how much it contains.
/// </summary>
/// <remarks>
/// <para>
/// The Bcrdt README's Performance section calls run-length blocks "the single biggest win" and
/// states the claim concretely: 100 000 characters typed sequentially is one block, not 100 000
/// objects. That claim only holds for edits that are adjacent in both clock order and list order,
/// which is exactly what typing produces and exactly what scattered editing destroys. The two
/// benchmarks here write identical content - the same number of characters into the same
/// collaborative text - and differ only in where each character goes, so any difference between
/// them is entirely attributable to whether the blocks could merge.
/// </para>
/// <para>
/// The results table shows allocation churn, which is what the <c>[MemoryDiagnoser]</c> measures and
/// what matters for the "no gen-2 allocations per keystroke" half of the steady-state budget.
/// Churn is not the same thing as footprint, though, so <see cref="GlobalCleanup"/> additionally
/// reports the block count, the retained bytes after a full collection, and the encoded size for
/// both shapes - the numbers that say whether a long editing session is affordable to keep open.
/// </para>
/// </remarks>
[MemoryDiagnoser]
[InvocationCount(1, unrollFactor: 1)]
public class MemoryBenchmarks
{
    private const string RootName = "body";

    private int[] _scatteredPositions = null!;
    private BcrdtDocument _target = null!;
    private BcrdtText _text = null!;

    /// <summary>How many characters each document is built from.</summary>
    [Params(1_000, 10_000, 100_000)]
    public int Size;

    /// <summary>Precomputes the scattered insertion positions.</summary>
    /// <remarks>
    /// Position <c>i</c> is drawn from the document length as it will be at that point, so every
    /// insertion is valid and the sequence is identical on every iteration. A fixed seed matters
    /// more here than elsewhere: the whole point is to compare two edit patterns, and that
    /// comparison is meaningless if one of them changes between runs.
    /// </remarks>
    [GlobalSetup]
    public void GlobalSetup()
    {
        var random = new Random(20260907);
        _scatteredPositions = new int[Size];
        for (int i = 0; i < Size; i++) _scatteredPositions[i] = random.Next(i + 1);
    }

    /// <summary>Provides a fresh, empty document before every measured iteration.</summary>
    /// <remarks>
    /// Both benchmarks build a document from nothing, so both are destructive by construction: a
    /// second invocation against the same document would append to a document that is already full
    /// and would measure a different, larger workload. Creating the document here rather than inside
    /// the benchmark also keeps its construction out of the allocation figures. The document has an
    /// <see cref="UpdateSink"/> attached, so each keystroke's update is encoded and allocated as it is
    /// in an application.
    /// </remarks>
    [IterationSetup]
    public void IterationSetup()
    {
        _target?.Dispose();
        _target = UpdateSink.Attach(new BcrdtDocument(new BcrdtDocumentOptions { ClientId = 1 }));
        _text = _target.GetText(RootName);
    }

    /// <summary>Releases the fixture and reports the structural comparison.</summary>
    /// <remarks>
    /// Both shapes are profiled here, so each of the two benchmarks reports both and the comparison
    /// is legible whichever one you filtered for.
    /// </remarks>
    [GlobalCleanup]
    public void GlobalCleanup()
    {
        _target?.Dispose();

        // Built here rather than inside a benchmark because block counts and retained bytes are not
        // timings, and forcing a full collection to measure them would corrupt any measurement they
        // shared a process with.
        DocumentProfile sequential = Profile(text =>
        {
            for (int i = 0; i < Size; i++) text.Insert(text.Count, "a");
        });

        DocumentProfile scattered = Profile(text =>
        {
            for (int i = 0; i < Size; i++) text.Insert(_scatteredPositions[i], "a");
        });

        Console.WriteLine($"// document shape @ {Size:N0} chars");
        Console.WriteLine($"//   sequential typing: {Report(sequential, Size)}");
        Console.WriteLine($"//   scattered edits  : {Report(scattered, Size)}");
        Console.WriteLine(
            $"//   run-length collapse: {(double)scattered.Blocks / sequential.Blocks:F1}x fewer blocks, " +
            $"{(double)scattered.RetainedBytes / Math.Max(sequential.RetainedBytes, 1):F1}x less retained");
    }

    /// <summary>Types the whole document one character at a time at the end.</summary>
    /// <remarks>
    /// The case run-length encoding is built for. Each character's block is adjacent to the previous
    /// one in both clock and position, so transaction cleanup folds them together and the document
    /// converges on a handful of blocks no matter how long the session runs.
    /// </remarks>
    [Benchmark(Description = "Build a document by sequential typing")]
    public int SequentialTyping()
    {
        BcrdtText text = _text;
        for (int i = 0; i < Size; i++) text.Insert(text.Count, "a");
        return text.Count;
    }

    /// <summary>Types the same content one character at a time at scattered positions.</summary>
    /// <remarks>
    /// The pathological shape: no two consecutive characters are adjacent in the list, so nothing
    /// merges and the document keeps one block per character. Nobody edits like this, but every
    /// real session is somewhere between this and <see cref="SequentialTyping"/>, and the gap
    /// between the two is the size of the bet the architecture places on run-length encoding.
    /// </remarks>
    [Benchmark(Description = "Build a document by scattered single-character edits")]
    public int ScatteredEdits()
    {
        BcrdtText text = _text;
        int[] positions = _scatteredPositions;
        for (int i = 0; i < positions.Length; i++) text.Insert(positions[i], "a");
        return text.Count;
    }

    /// <summary>What one document shape costs.</summary>
    private readonly record struct DocumentProfile(int Blocks, long AllocatedBytes, long RetainedBytes, int EncodedBytes);

    private DocumentProfile Profile(Action<BcrdtText> build)
    {
        Collect();
        long retainedBefore = GC.GetTotalMemory(forceFullCollection: true);
        long allocatedBefore = GC.GetTotalAllocatedBytes(precise: true);

        using BcrdtDocument document = UpdateSink.Attach(new BcrdtDocument(new BcrdtDocumentOptions { ClientId = 1 }));
        build(document.GetText(RootName));

        long allocated = GC.GetTotalAllocatedBytes(precise: true) - allocatedBefore;

        // The document is still rooted by the local, so what survives a full collection here is
        // what the document actually holds on to rather than what it churned through on the way.
        long retained = GC.GetTotalMemory(forceFullCollection: true) - retainedBefore;

        int blocks = 0;
        foreach (List<BcrdtAbstractStruct> structs in document.Store.Clients.Values) blocks += structs.Count;

        return new DocumentProfile(blocks, allocated, retained, document.EncodeStateAsUpdate().Length);
    }

    private static string Report(DocumentProfile profile, int size) =>
        $"{profile.Blocks:N0} blocks, {profile.RetainedBytes:N0} B retained " +
        $"({(double)profile.RetainedBytes / size:F1} B/char), " +
        $"{profile.AllocatedBytes:N0} B allocated, {profile.EncodedBytes:N0} B encoded";

    private static void Collect()
    {
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
    }
}
