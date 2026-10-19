using BenchmarkDotNet.Attributes;
using Bit.Blex;

namespace Bit.Blex.Benchmarks;

/// <summary>
/// The single hottest path: assigning a reactive state property. Measures the per-assignment cost
/// of <c>BlexStoreBase.SetState</c> for a real change, a no-op (equal value, short-circuited) and a
/// batched multi-field action. Allocations per op are reported by <see cref="MemoryDiagnoserAttribute"/>.
/// </summary>
[MemoryDiagnoser]
public class StateMutationBenchmarks
{
    private BenchCounterStore _store = null!;
    private long _seed;

    [GlobalSetup]
    public void Setup()
    {
        _store = new BenchCounterStore();
        // A subscriber so the StateChanged invocation path is actually exercised, mirroring a
        // live UI component being subscribed.
        _store.StateChanged += () => { };
    }

    [Benchmark(Baseline = true, Description = "SetState (value changes -> notifies)")]
    public void SetState_Changing() => _store.Count = ++_seed;

    [Benchmark(Description = "SetState (same value -> short-circuit)")]
    public void SetState_NoChange() => _store.Count = 7;

    [Benchmark(Description = "Action (single field)")]
    public void Action_Increment() => _store.Increment();

    [Benchmark(Description = "Action (two fields, batched)")]
    public void Action_Batched() => _store.Bump(2);

    [Benchmark(Description = "Async action (awaits, single recorded action)")]
    public async Task Action_Async() => await _store.LoadFast();
}
