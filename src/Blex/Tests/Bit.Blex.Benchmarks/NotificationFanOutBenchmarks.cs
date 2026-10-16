using BenchmarkDotNet.Attributes;

namespace Bit.Blex.Benchmarks;

/// <summary>
/// Cost of a single state change as the number of <c>StateChanged</c> subscribers grows. This
/// simulates many UI components bound to one store; every change must notify all of them.
/// </summary>
[MemoryDiagnoser]
public class NotificationFanOutBenchmarks
{
    /// <summary>Number of subscribers attached to the store's StateChanged event.</summary>
    [Params(0, 1, 10, 100, 1000)]
    public int Subscribers;

    private BenchCounterStore _store = null!;
    private long _seed;

    [GlobalSetup]
    public void Setup()
    {
        _store = new BenchCounterStore();
        for (var i = 0; i < Subscribers; i++)
            _store.StateChanged += () => { };
    }

    [Benchmark(Description = "One state change, notify all subscribers")]
    public void ChangeAndNotify() => _store.Count = ++_seed;
}
