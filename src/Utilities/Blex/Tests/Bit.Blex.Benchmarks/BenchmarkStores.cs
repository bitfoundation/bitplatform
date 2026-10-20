using Bit.Blex;

namespace Bit.Blex.Benchmarks;

// Stores used by the benchmarks. Decorated like any real store so the source generator emits the
// actual reactive API the benchmarks exercise.

[BlexStoreAttribute(Name = "benchCounter")]
public partial class BenchCounterStore
{
    [BlexStateAttribute] private long _count;
    [BlexStateAttribute] private string _label = "idle";

    [BlexComputedAttribute]
    private long ComputeDoubleCount() => Count * 2;

    [BlexActionAttribute]
    private void OnIncrement() => Count++;

    [BlexActionAttribute]
    private void OnBump(long amount)
    {
        Count += amount;
        Label = "bumped";
    }

    [BlexActionAttribute(Name = "LoadFast")]
    private async Task OnLoadFast()
    {
        Label = "loading";
        await Task.Yield();
        Count++;
        Label = "loaded";
    }
}

// A wider store so global-state capture and serialization have realistic work to do.
[BlexStoreAttribute(Name = "benchWide")]
public partial class BenchWideStore
{
    [BlexStateAttribute] private int _a;
    [BlexStateAttribute] private int _b;
    [BlexStateAttribute] private int _c;
    [BlexStateAttribute] private string _title = "";
    [BlexStateAttribute] private bool _flag;
    [BlexStateAttribute] private double _ratio;

    [BlexActionAttribute]
    private void OnTouch(int seed)
    {
        A = seed;
        B = seed + 1;
        C = seed + 2;
        Title = "row-" + seed;
        Flag = (seed & 1) == 0;
        Ratio = seed / 3.0;
    }
}
