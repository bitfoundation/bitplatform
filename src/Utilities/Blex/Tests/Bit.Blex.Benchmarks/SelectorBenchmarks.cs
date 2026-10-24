using BenchmarkDotNet.Attributes;
using Bit.Blex;

namespace Bit.Blex.Benchmarks;

/// <summary>
/// What a memoized cross-store selector (<c>manager.CreateSelector</c>) costs the actions it watches: an
/// action plus one read, with no selector, with one alive, and with ten alive. A selector is pull-based,
/// so the ideal is that keeping it alive adds nothing to an action that nobody reads it after.
/// </summary>
[MemoryDiagnoser]
public class SelectorBenchmarks
{
    private BlexManager _plain = null!;
    private BenchCounterStore _plainStore = null!;

    private BlexManager _one = null!;
    private BenchCounterStore _oneStore = null!;
    private BlexSelector<long> _selector = null!;

    private BlexManager _ten = null!;
    private BenchCounterStore _tenStore = null!;
    private BlexSelector<long>[] _selectors = null!;

    [GlobalSetup]
    public void Setup()
    {
        _plain = new BlexManager();
        _plainStore = new BenchCounterStore();
        _plain.Register(_plainStore);

        _one = new BlexManager();
        _oneStore = new BenchCounterStore();
        _one.Register(_oneStore);
        _selector = _one.CreateSelector(() => _oneStore.Count * 2);

        _ten = new BlexManager();
        _tenStore = new BenchCounterStore();
        _ten.Register(_tenStore);
        _selectors = new BlexSelector<long>[10];
        for (var i = 0; i < _selectors.Length; i++)
        {
            var factor = i + 1;
            _selectors[i] = _ten.CreateSelector(() => _tenStore.Count * factor);
        }
    }

    [Benchmark(Baseline = true, Description = "Action, no selector")]
    public long Action_NoSelector()
    {
        _plainStore.Increment();
        return _plainStore.Count;
    }

    [Benchmark(Description = "Action + read, one selector alive")]
    public long Action_OneSelector()
    {
        _oneStore.Increment();
        return _selector.Value;
    }

    [Benchmark(Description = "Action, ten selectors alive, none read")]
    public long Action_TenSelectorsUnread()
    {
        _tenStore.Increment();
        return _tenStore.Count;
    }

    [Benchmark(Description = "Selector read, memo hit")]
    public long Read_MemoHit() => _selector.Value;
}
