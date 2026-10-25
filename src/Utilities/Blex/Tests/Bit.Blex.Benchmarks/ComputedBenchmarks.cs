using BenchmarkDotNet.Attributes;
using Bit.Blex;

namespace Bit.Blex.Benchmarks;

/// <summary>A store with an expensive computed over a list and a flag nothing derived reads.</summary>
[BlexStoreAttribute(Name = "benchCatalog")]
public partial class BenchCatalogStore
{
    [BlexStateAttribute] private int[] _prices = [.. Enumerable.Range(1, 1_000)];
    [BlexStateAttribute] private bool _menuOpen;

    [BlexComputedAttribute]
    private long ComputeTotal() => Prices.Sum(p => (long)p);

    [BlexActionAttribute]
    private void OnToggleMenu() => MenuOpen = !MenuOpen;

    [BlexActionAttribute]
    private void OnReprice(int index, int price)
    {
        var prices = (int[])Prices.Clone();
        prices[index] = price;
        Prices = prices;
    }
}

/// <summary>
/// What a write costs a computed that does not read what was written: with dependencies tracked it stays memoized; when
/// every write invalidates every computed, the next read recomputes it.
/// </summary>
[MemoryDiagnoser]
public class ComputedBenchmarks
{
    private BenchCatalogStore _store = null!;

    [GlobalSetup]
    public void Setup()
    {
        _store = new BenchCatalogStore();
        new BlexManager().Register(_store);
        _ = _store.Total;
    }

    [Benchmark(Description = "Unrelated write, then read the computed")]
    public long UnrelatedWrite()
    {
        _store.ToggleMenu();
        return _store.Total;
    }

    [Benchmark(Description = "Write it reads, then read the computed")]
    public long RelatedWrite()
    {
        _store.Reprice(0, 2);
        return _store.Total;
    }
}
