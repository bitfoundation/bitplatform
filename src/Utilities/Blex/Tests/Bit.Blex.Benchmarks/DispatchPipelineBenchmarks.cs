using BenchmarkDotNet.Attributes;
using Bit.Blex;

namespace Bit.Blex.Benchmarks;

/// <summary>
/// Contrasts the two dispatch regimes documented in <c>BlexManager.RecordAction</c>:
/// the idle fast-path (no listeners -> the global-state capture is skipped entirely) versus the
/// full pipeline (a middleware is attached, so every action serializes the whole state tree).
/// The <see cref="StoreCount"/> parameter scales the global-state size to show how the full
/// pipeline cost grows with the number of registered stores.
/// </summary>
[MemoryDiagnoser]
public class DispatchPipelineBenchmarks
{
    /// <summary>Number of registered stores; grows the captured global-state tree.</summary>
    [Params(1, 10, 50)]
    public int StoreCount;

    private BlexManager _idle = null!;
    private BenchCounterStore _idleStore = null!;

    private BlexManager _withMiddleware = null!;
    private BenchCounterStore _pipelineStore = null!;

    [GlobalSetup]
    public void Setup()
    {
        // Idle manager: no middleware, no DevTools, no subscribers -> RecordAction short-circuits.
        _idle = new BlexManager();
        _idleStore = new BenchCounterStore();
        _idle.Register(_idleStore);
        FillStores(_idle, StoreCount - 1);

        // Full pipeline: a middleware that reads the global state, which is captured lazily -- one that never
        // reads it would not pay for the capture at all.
        long sink = 0;
        _withMiddleware = new BlexManager(new[] { new BlexDelegateMiddleware(ctx => sink += ctx.GlobalState.Count) });
        _pipelineStore = new BenchCounterStore();
        _withMiddleware.Register(_pipelineStore);
        FillStores(_withMiddleware, StoreCount - 1);
    }

    private static void FillStores(BlexManager manager, int extra)
    {
        // Scoped instances, so each has a name of its own ("benchWide:0", ...). Registered under one name they
        // would share a single slice of the captured tree, and StoreCount would not grow the state at all.
        for (var i = 0; i < extra; i++)
        {
            var w = new BenchWideStore();
            w.Touch(i);
            manager.CreateScope(w, i.ToString(System.Globalization.CultureInfo.InvariantCulture));
        }
    }

    [Benchmark(Baseline = true, Description = "Dispatch (idle manager, pipeline skipped)")]
    public void Dispatch_Idle() => _idleStore.Increment();

    [Benchmark(Description = "Dispatch (middleware, full state capture)")]
    public void Dispatch_FullPipeline() => _pipelineStore.Increment();
}
