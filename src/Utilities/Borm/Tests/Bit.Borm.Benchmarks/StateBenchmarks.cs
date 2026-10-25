using BenchmarkDotNet.Attributes;

namespace Bit.Borm.Benchmarks;

/// <summary>
/// The state engine with no UI attached: building a form, the per-keystroke cost of writing one value,
/// a full validation pass and a draft snapshot, as the form grows. A keystroke is the number that
/// matters most — it is paid on every input event of every user, on Blazor Server over the circuit.
/// </summary>
[MemoryDiagnoser]
public class StateBenchmarks
{
    [Params(50, 500, 2000)]
    public int Fields { get; set; }

    private BormDefinition _flat = null!;
    private BormDefinition _reactive = null!;
    private BormState _flatState = null!;
    private BormState _reactiveState = null!;
    private int _tick;

    [GlobalSetup]
    public void Setup()
    {
        _flat = Workloads.FlatForm(Fields);
        _reactive = Workloads.ReactiveForm(Fields);
        _flatState = new BormState(_flat, new BormDictionaryDataAccessor(Workloads.FlatData(Fields)));
        _flatState.StateChanged += () => { };
        _reactiveState = new BormState(_reactive, new BormDictionaryDataAccessor());
        _reactiveState.StateChanged += () => { };
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        _flatState.Dispose();
        _reactiveState.Dispose();
    }

    [Benchmark(Description = "Construct state (flat form)")]
    public BormState Construct_Flat()
    {
        var state = new BormState(_flat, new BormDictionaryDataAccessor(Workloads.FlatData(Fields)));
        state.Dispose();
        return state;
    }

    [Benchmark(Description = "Construct state (reactive form)")]
    public BormState Construct_Reactive()
    {
        var state = new BormState(_reactive, new BormDictionaryDataAccessor());
        state.Dispose();
        return state;
    }

    /// <summary>One keystroke into the last field of a plain form — the field furthest down the walk.</summary>
    [Benchmark(Description = "SetValue (flat form, last field)")]
    public void SetValue_Flat() => _flatState.SetValue($"f{Fields - 1}", (++_tick & 1) == 0 ? "a" : "b");

    /// <summary>A keystroke into a number the computed total reads.</summary>
    [Benchmark(Description = "SetValue (reactive form, computed dependency)")]
    public void SetValue_Reactive_Dependency() => _reactiveState.SetValue("n0", (double)(++_tick % 100));

    /// <summary>A keystroke into a field nothing else reads, on a form that has reactive machinery.</summary>
    [Benchmark(Description = "SetValue (reactive form, unrelated field)")]
    public void SetValue_Reactive_Unrelated() => _reactiveState.SetValue("f1", (++_tick & 1) == 0 ? "a" : "b");

    [Benchmark(Description = "ValidateAsync (flat form, all valid)")]
    public async Task<bool> Validate_Flat() => await _flatState.ValidateAsync();

    [Benchmark(Description = "Snapshot (flat form)")]
    public int Snapshot_Flat() => _flatState.Snapshot().Count;
}
