using BenchmarkDotNet.Attributes;

namespace Bit.Borm.Benchmarks;

/// <summary>
/// A typed invoice whose rows each compute a line total and whose total sums them: what editing one
/// row, adding one and removing one cost as the invoice grows. This is the shape where per-change
/// sweeps over every live row add up fastest.
/// </summary>
[MemoryDiagnoser]
public class RepeaterBenchmarks
{
    [Params(10, 200)]
    public int Lines { get; set; }

    private BormDefinition _form = null!;
    private BormState _state = null!;
    private int _tick;

    [GlobalSetup]
    public void Setup()
    {
        _form = Workloads.InvoiceForm();
        _state = new BormState(_form, new BormModelDataAccessor(Workloads.InvoiceModel(Lines)));
        _state.StateChanged += () => { };
    }

    [GlobalCleanup]
    public void Cleanup() => _state.Dispose();

    [Benchmark(Description = "Construct state (invoice)")]
    public BormState Construct()
    {
        var state = new BormState(_form, new BormModelDataAccessor(Workloads.InvoiceModel(Lines)));
        state.Dispose();
        return state;
    }

    /// <summary>A quantity typed into the middle row: its line total and the invoice total both move.</summary>
    [Benchmark(Description = "SetValue (row quantity -> line + invoice totals)")]
    public void SetValue_Quantity() => _state.SetValue($"Lines[{Lines / 2}].Quantity", 1 + ++_tick % 9);

    [Benchmark(Description = "SetValue (row product, nothing reads it)")]
    public void SetValue_Product() => _state.SetValue($"Lines[{Lines / 2}].Product", (++_tick & 1) == 0 ? "a" : "b");

    /// <summary>Adding a row at the end and removing it again, so the invoice stays the same size.</summary>
    [Benchmark(Description = "Add + remove a row")]
    public void AddRemove()
    {
        var field = _form.FindField(nameof(Invoice.Lines))!;
        var index = _state.AddArrayItem(field, nameof(Invoice.Lines));
        _state.RemoveArrayItem(nameof(Invoice.Lines), index);
    }

    [Benchmark(Description = "ValidateAsync (invoice)")]
    public async Task<bool> Validate() => await _state.ValidateAsync();
}
