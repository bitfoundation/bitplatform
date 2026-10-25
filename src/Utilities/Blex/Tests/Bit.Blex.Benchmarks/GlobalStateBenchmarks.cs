using System.Text.Json.Nodes;
using BenchmarkDotNet.Attributes;
using Bit.Blex;

namespace Bit.Blex.Benchmarks;

/// <summary>
/// Measures whole-application state operations as the store count scales: building the global
/// state tree (<c>CaptureGlobalState</c>) and applying a time-travel snapshot
/// (<c>RestoreGlobalState</c>). These are the operations that fire on every DevTools-tracked
/// action, so their cost at scale matters for large apps.
/// </summary>
[MemoryDiagnoser]
public class GlobalStateBenchmarks
{
    [Params(10, 100, 500)]
    public int StoreCount;

    private BlexManager _manager = null!;
    private JsonObject _snapshot = null!;

    [GlobalSetup]
    public void Setup()
    {
        _manager = new BlexManager();
        for (var i = 0; i < StoreCount; i++)
        {
            var w = new BenchWideStore();
            w.Touch(i);
            _manager.Register(w);
        }

        _snapshot = _manager.CaptureGlobalState();
    }

    [Benchmark(Baseline = true, Description = "CaptureGlobalState (serialize every store)")]
    public JsonObject Capture() => _manager.CaptureGlobalState();

    [Benchmark(Description = "RestoreGlobalState (time-travel apply)")]
    public void Restore() => _manager.RestoreGlobalState(_snapshot);
}
