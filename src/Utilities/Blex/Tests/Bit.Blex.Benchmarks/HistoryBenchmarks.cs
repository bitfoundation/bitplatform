using BenchmarkDotNet.Attributes;
using Bit.Blex;

namespace Bit.Blex.Benchmarks;

/// <summary>
/// What in-app undo/redo costs per action as the app grows: one store changes, <see cref="StoreCount"/>
/// stores are registered, and a <see cref="BlexHistory"/> records every action. Ideally the cost tracks
/// the store that changed, not the number of stores.
/// </summary>
[MemoryDiagnoser]
public class HistoryBenchmarks
{
    /// <summary>Number of registered stores besides the one that changes.</summary>
    [Params(1, 10, 50)]
    public int StoreCount;

    private BenchCounterStore _store = null!;
    private BlexHistory _history = null!;

    [GlobalSetup]
    public void Setup()
    {
        var manager = new BlexManager();
        _store = new BenchCounterStore();
        manager.Register(_store);
        // Scoped instances, so each has a name of its own ("benchWide:0", ...): stores registered under one name
        // share a slice of the state tree, and the benchmark would measure that misconfiguration instead.
        for (var i = 0; i < StoreCount; i++)
        {
            var wide = new BenchWideStore();
            wide.Touch(i);
            manager.CreateScope(wide, i.ToString(System.Globalization.CultureInfo.InvariantCulture));
        }

        _history = new BlexHistory(manager, maxEntries: 100);
        _history.Start();

        // Something to undo, so Undo + Redo moves rather than no-ops.
        for (var i = 0; i < 10; i++)
            _store.Increment();
    }

    [Benchmark(Description = "Action recorded by history")]
    public void Action_WithHistory() => _store.Increment();

    [Benchmark(Description = "Undo + redo")]
    public void UndoRedo()
    {
        _history.Undo();
        _history.Redo();
    }
}
