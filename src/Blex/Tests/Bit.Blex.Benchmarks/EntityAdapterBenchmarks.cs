using BenchmarkDotNet.Attributes;

namespace Bit.Blex.Benchmarks;

/// <summary>
/// Cost of the immutable <see cref="BlexEntityAdapter{TEntity, TKey}"/> operations at different
/// collection sizes, including the sorted-adapter variant. Every operation copies the id list
/// and entity map, so these numbers bound how large a normalized collection can get before
/// per-action costs become noticeable.
/// </summary>
[MemoryDiagnoser]
public class EntityAdapterBenchmarks
{
    public sealed record Item(int Id, string Name, bool Flag);

    private readonly BlexEntityAdapter<Item, int> _adapter = new(i => i.Id);
    private readonly BlexEntityAdapter<Item, int> _sortedAdapter = new(
        i => i.Id,
        System.Collections.Generic.Comparer<Item>.Create((a, b) => string.CompareOrdinal(a.Name, b.Name)));

    private BlexEntityState<Item, int> _state = null!;
    private BlexEntityState<Item, int> _sortedState = null!;
    private Item[] _seed = null!;

    [Params(100, 1000)]
    public int Count { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        _seed = new Item[Count];
        for (var i = 0; i < Count; i++)
            _seed[i] = new Item(i, $"item {i:D6}", false);
        _state = _adapter.SetAll(_adapter.GetInitialState(), _seed);
        _sortedState = _sortedAdapter.SetAll(_sortedAdapter.GetInitialState(), _seed);
    }

    [Benchmark(Baseline = true, Description = "UpsertOne (replace existing)")]
    public BlexEntityState<Item, int> UpsertOne()
        => _adapter.UpsertOne(_state, new Item(Count / 2, "updated", true));

    [Benchmark(Description = "AddOne (new id)")]
    public BlexEntityState<Item, int> AddOne()
        => _adapter.AddOne(_state, new Item(Count + 1, "new", false));

    [Benchmark(Description = "UpdateOne (record with-mutation)")]
    public BlexEntityState<Item, int> UpdateOne()
        => _adapter.UpdateOne(_state, Count / 2, i => i with { Flag = !i.Flag });

    [Benchmark(Description = "RemoveOne")]
    public BlexEntityState<Item, int> RemoveOne()
        => _adapter.RemoveOne(_state, Count / 2);

    [Benchmark(Description = "SetAll (rebuild)")]
    public BlexEntityState<Item, int> SetAll()
        => _adapter.SetAll(_state, _seed);

    [Benchmark(Description = "UpsertOne (sorted adapter)")]
    public BlexEntityState<Item, int> UpsertOneSorted()
        => _sortedAdapter.UpsertOne(_sortedState, new Item(Count / 2, "zzz updated", true));
}
