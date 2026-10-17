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

        // Full pipeline: a middleware forces CaptureGlobalState() on every action.
        long sink = 0;
        _withMiddleware = new BlexManager(new[] { new BlexDelegateMiddleware(_ => sink++) });
        _pipelineStore = new BenchCounterStore();
        _withMiddleware.Register(_pipelineStore);
        FillStores(_withMiddleware, StoreCount - 1);
    }

    private static void FillStores(BlexManager manager, int extra)
    {
        for (var i = 0; i < extra; i++)
        {
            var w = new BenchWideStore();
            w.Touch(i);
            manager.Register(w);
        }
    }

    [Benchmark(Baseline = true, Description = "Dispatch (idle manager, pipeline skipped)")]
    public void Dispatch_Idle() => _idleStore.Increment();

    [Benchmark(Description = "Dispatch (middleware, full state capture)")]
    public void Dispatch_FullPipeline() => _pipelineStore.Increment();
}
