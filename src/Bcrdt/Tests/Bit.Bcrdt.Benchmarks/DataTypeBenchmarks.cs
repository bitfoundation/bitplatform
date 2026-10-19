using BenchmarkDotNet.Attributes;

namespace Bit.Bcrdt.Benchmarks;

/// <summary>
/// Measures the collaborative collections that applications reach for when the shared state is not
/// prose: lists, dictionaries and sets.
/// </summary>
/// <remarks>
/// <para>
/// These model the everyday shapes of collaborative application state rather than an editor. A
/// <see cref="BcrdtArray"/> is a kanban column, a playlist or a table's rows. A
/// <see cref="BcrdtMap"/> is a settings object, a form or a record. A <see cref="BcrdtSet"/> is a
/// tag list or a set of selected identifiers. They are benchmarked together because the data model
/// section of the Bcrdt README makes a strong structural claim about them - all four public types
/// are projections of one item list over one integration algorithm - and the practical consequence
/// of that claim is a performance one: a map lookup should be constant time because the type
/// maintains a key index, while an array lookup by index has to resolve a position in a sequence.
/// </para>
/// <para>
/// No single line of the performance budget table covers these types directly. What they defend is the
/// technique list underneath it: binary search over the per-client struct list, the cached
/// index-to-item search markers, and transaction batching. Bulk insertion of
/// <see cref="ElementCount"/> values in one call should cost far less than
/// <see cref="ElementCount"/> separate insertions would, because it is one transaction, one update
/// message and one event flush.
/// </para>
/// </remarks>
[MemoryDiagnoser]
[InvocationCount(1, unrollFactor: 1)]
public class DataTypeBenchmarks
{
    /// <summary>How many elements, keys or members each fixture holds.</summary>
    private const int ElementCount = 10_000;

    /// <summary>How many operations the batched benchmarks perform per invocation.</summary>
    /// <remarks>
    /// An indexed read or a key lookup is measured in nanoseconds, which is far too short to time at
    /// one invocation per iteration. Repeating each operation a fixed number of times and declaring
    /// that count through <c>OperationsPerInvoke</c> produces an interval long enough to measure
    /// while still reporting a per-operation result.
    /// </remarks>
    private const int Probes = 1_000;

    private object?[] _bulkValues = null!;
    private int[] _probeIndices = null!;
    private string[] _probeKeys = null!;
    private string[] _probeMembers = null!;
    private string[] _freshMembers = null!;

    private BcrdtDocument _readOnlyDocument = null!;
    private BcrdtArray _readOnlyArray = null!;
    private BcrdtMap _readOnlyMap = null!;
    private BcrdtSet _readOnlySet = null!;

    private BcrdtDocument _mutableDocument = null!;
    private BcrdtArray _emptyArray = null!;
    private BcrdtArray _mutableArray = null!;
    private BcrdtMap _mutableMap = null!;
    private BcrdtSet _mutableSet = null!;

    /// <summary>Builds the inputs and the read-only fixtures.</summary>
    /// <remarks>
    /// The read-only fixtures live here rather than in <see cref="IterationSetup"/> because nothing
    /// in this class mutates them. Keeping them separate from the mutable ones is what makes that
    /// guarantee checkable: a benchmark that writes cannot reach the collections the benchmarks that
    /// read depend on.
    /// </remarks>
    [GlobalSetup]
    public void GlobalSetup()
    {
        _bulkValues = new object?[ElementCount];
        for (int i = 0; i < ElementCount; i++) _bulkValues[i] = (long)i;

        // Array indices are drawn from a fixed seed, so every iteration probes the same scattered
        // positions in the same order; a workload that varies between iterations reports its own
        // noise rather than the engine's behaviour.
        var random = new Random(20260907);
        _probeIndices = new int[Probes];
        for (int i = 0; i < Probes; i++) _probeIndices[i] = random.Next(ElementCount);

        // Keys and members are spread evenly and are all distinct, because the destructive
        // benchmarks below have to delete a thousand different entries rather than repeatedly
        // deleting one that has already gone.
        _probeKeys = new string[Probes];
        _probeMembers = new string[Probes];
        _freshMembers = new string[Probes];
        for (int i = 0; i < Probes; i++)
        {
            _probeKeys[i] = Key(i * (ElementCount / Probes));
            _probeMembers[i] = Member(i * (ElementCount / Probes));
            _freshMembers[i] = $"added-{i:D5}";
        }

        _readOnlyDocument = new BcrdtDocument(new BcrdtDocumentOptions { ClientId = 1 });
        _readOnlyArray = _readOnlyDocument.GetArray("array");
        _readOnlyMap = _readOnlyDocument.GetMap("map");
        _readOnlySet = _readOnlyDocument.GetSet("set");
        Populate(_readOnlyDocument, _readOnlyArray, _readOnlyMap, _readOnlySet);
    }

    /// <summary>Rebuilds the writable collections before every measured iteration.</summary>
    /// <remarks>
    /// Six of the ten benchmarks below write, and each of them changes what the next invocation
    /// would find: a deleted key cannot be deleted twice, an added member is already present the
    /// second time, and repeated middle insertions fragment the blocks they split until later
    /// invocations are doing measurably more work than earlier ones. Rebuilding from scratch each
    /// iteration is the only way the numbers stay comparable, and the class is pinned to one
    /// invocation per iteration so the rebuild happens as often as the mutation does.
    /// </remarks>
    [IterationSetup]
    public void IterationSetup()
    {
        _mutableDocument?.Dispose();
        _mutableDocument = new BcrdtDocument(new BcrdtDocumentOptions { ClientId = 2 });

        _emptyArray = _mutableDocument.GetArray("empty-array");
        _mutableArray = _mutableDocument.GetArray("array");
        _mutableMap = _mutableDocument.GetMap("map");
        _mutableSet = _mutableDocument.GetSet("set");
        Populate(_mutableDocument, _mutableArray, _mutableMap, _mutableSet);

        // After populating, so the fixture does not pay for an encoding nobody measures, and before
        // measuring, so every measured write encodes its update as a real document's does. See
        // UpdateSink.
        UpdateSink.Attach(_mutableDocument);
    }

    /// <summary>Releases the fixtures.</summary>
    [GlobalCleanup]
    public void GlobalCleanup()
    {
        _readOnlyDocument?.Dispose();
        _mutableDocument?.Dispose();
    }

    /// <summary>Appends <see cref="ElementCount"/> values in a single call.</summary>
    /// <remarks>
    /// A paste, an import, or the initial load of a list from a server. One transaction, so one
    /// update message and one event flush however many values it carries.
    /// </remarks>
    [Benchmark(Description = "Array: bulk push of 10 000 values")]
    public int Array_BulkPush()
    {
        _emptyArray.Push(_bulkValues);
        return _emptyArray.Count;
    }

    /// <summary>Inserts single values into the middle of a populated array.</summary>
    /// <remarks>
    /// Reordering a list or inserting a row. Every insertion splits the run-length-encoded block it
    /// lands inside, so this is the array equivalent of the text search-marker path.
    /// </remarks>
    [Benchmark(OperationsPerInvoke = Probes, Description = "Array: insert a value in the middle")]
    public int Array_InsertInMiddle()
    {
        BcrdtArray array = _mutableArray;
        for (int i = 0; i < Probes; i++) array.Insert(array.Count / 2, (long)i);
        return array.Count;
    }

    /// <summary>Reads scattered indices from a populated array.</summary>
    [Benchmark(OperationsPerInvoke = Probes, Description = "Array: indexed read")]
    public long Array_IndexedRead()
    {
        BcrdtArray array = _readOnlyArray;
        int[] indices = _probeIndices;
        long total = 0;
        for (int i = 0; i < indices.Length; i++) total += (long)array[indices[i]]!;
        return total;
    }

    /// <summary>Walks the whole array in order.</summary>
    /// <remarks>
    /// What rendering a list costs. This is a single pass over the block list with deleted blocks
    /// skipped, so it should be the cheapest way to read many elements by a wide margin - far
    /// cheaper per element than <see cref="Array_IndexedRead"/>, which resolves each position
    /// independently.
    /// </remarks>
    [Benchmark(OperationsPerInvoke = ElementCount, Description = "Array: iterate every element")]
    public long Array_Iterate()
    {
        long total = 0;
        foreach (object? value in _readOnlyArray) total += (long)value!;
        return total;
    }

    /// <summary>Overwrites existing keys in a map of <see cref="ElementCount"/> keys.</summary>
    /// <remarks>
    /// Writing a key tombstones whatever was there and inserts a new item under the same key, so
    /// this measures a delete and an insert, not an in-place assignment.
    /// </remarks>
    [Benchmark(OperationsPerInvoke = Probes, Description = "Map: set a key")]
    public int Map_Set()
    {
        BcrdtMap map = _mutableMap;
        string[] keys = _probeKeys;
        for (int i = 0; i < keys.Length; i++) map.Set(keys[i], (long)i);
        return map.Count;
    }

    /// <summary>Reads existing keys from a map of <see cref="ElementCount"/> keys.</summary>
    /// <remarks>
    /// This is the claim the data model section makes about maps: the type keeps a key-to-newest-item index, so a
    /// read is a dictionary lookup and does not depend on how many keys the map holds.
    /// </remarks>
    [Benchmark(OperationsPerInvoke = Probes, Description = "Map: get a key")]
    public long Map_Get()
    {
        BcrdtMap map = _readOnlyMap;
        string[] keys = _probeKeys;
        long total = 0;
        for (int i = 0; i < keys.Length; i++) total += (long)map.Get(keys[i])!;
        return total;
    }

    /// <summary>Removes distinct existing keys from a map of <see cref="ElementCount"/> keys.</summary>
    [Benchmark(OperationsPerInvoke = Probes, Description = "Map: delete a key")]
    public int Map_Delete()
    {
        BcrdtMap map = _mutableMap;
        string[] keys = _probeKeys;
        for (int i = 0; i < keys.Length; i++) map.Delete(keys[i]);
        return map.Count;
    }

    /// <summary>Adds members that are not already present.</summary>
    /// <remarks>
    /// Every addition first has to establish that the value is absent, because a set is add-wins and
    /// re-adding a present member must be a no-op rather than a second copy. That check goes through
    /// the canonical encoding of the value, which is why set operations cost more than the map
    /// operations they are built on.
    /// </remarks>
    [Benchmark(OperationsPerInvoke = Probes, Description = "Set: add a member")]
    public int Set_Add()
    {
        BcrdtSet set = _mutableSet;
        string[] members = _freshMembers;
        for (int i = 0; i < members.Length; i++) set.Add(members[i]);
        return set.Count;
    }

    /// <summary>Tests membership for values that are present.</summary>
    [Benchmark(OperationsPerInvoke = Probes, Description = "Set: contains a member")]
    public int Set_Contains()
    {
        BcrdtSet set = _readOnlySet;
        string[] members = _probeMembers;
        int found = 0;
        for (int i = 0; i < members.Length; i++)
        {
            if (set.Contains(members[i])) found++;
        }
        return found;
    }

    /// <summary>Removes distinct members that are present.</summary>
    [Benchmark(OperationsPerInvoke = Probes, Description = "Set: remove a member")]
    public int Set_Remove()
    {
        BcrdtSet set = _mutableSet;
        string[] members = _probeMembers;
        for (int i = 0; i < members.Length; i++) set.Remove(members[i]);
        return set.Count;
    }

    private static void Populate(BcrdtDocument document, BcrdtArray array, BcrdtMap map, BcrdtSet set)
    {
        // One transaction for the whole fixture. Ten thousand separate transactions would spend most
        // of their time on commit bookkeeping that has nothing to do with what is being measured.
        document.Transact(_ =>
        {
            array.Push([.. Enumerable.Range(0, ElementCount).Select(i => (object?)(long)i)]);
            for (int i = 0; i < ElementCount; i++) map.Set(Key(i), (long)i);
            for (int i = 0; i < ElementCount; i++) set.Add(Member(i));
        });
    }

    private static string Key(int index) => $"key-{index:D5}";

    private static string Member(int index) => $"member-{index:D5}";
}
