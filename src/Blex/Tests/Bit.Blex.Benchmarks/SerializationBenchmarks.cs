using System.Text.Json.Nodes;
using BenchmarkDotNet.Attributes;

namespace Bit.Blex.Benchmarks;

/// <summary>
/// Per-store JSON snapshot cost: the generated <c>SerializeState</c>/<c>DeserializeState</c> that
/// back persistence, DevTools and time-travel. Measured against the wider, more representative store.
/// </summary>
[MemoryDiagnoser]
public class SerializationBenchmarks
{
    private BenchWideStore _source = null!;
    private BenchWideStore _target = null!;
    private JsonObject _state = null!;

    [GlobalSetup]
    public void Setup()
    {
        _source = new BenchWideStore();
        _source.Touch(12345);
        _target = new BenchWideStore();
        _state = _source.SerializeState();
    }

    [Benchmark(Baseline = true, Description = "SerializeState")]
    public JsonObject Serialize() => _source.SerializeState();

    [Benchmark(Description = "DeserializeState")]
    public void Deserialize() => _target.DeserializeState(_state);

    [Benchmark(Description = "Round trip (serialize + deserialize)")]
    public void RoundTrip()
    {
        var json = _source.SerializeState();
        _target.DeserializeState(json);
    }
}
