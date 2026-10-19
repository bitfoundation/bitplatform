using Bit.Blex;

namespace Bit.Blex.Samples.Wasm.Stores;

/// <summary>
/// A tiny counter store showing reactive state, a computed value and named actions.
/// Everything below the fields is generated: the <c>Count</c>/<c>Step</c> properties,
/// the memoized <c>IsEven</c>/<c>DoubleCount</c> values and the <c>Increment</c>/<c>Decrement</c>/
/// <c>SetStep</c>/<c>Reset</c> action wrappers.
/// </summary>
[BlexStoreAttribute(Name = "counter", Persist = true)]
public partial class CounterStore
{
    [BlexStateAttribute] private int _count;
    [BlexStateAttribute] private int _step = 1;

    [BlexComputedAttribute]
    private int ComputeDoubleCount() => Count * 2;

    [BlexComputedAttribute]
    private bool ComputeIsEven() => Count % 2 == 0;

    [BlexActionAttribute]
    private void OnIncrement() => Count += Step;

    [BlexActionAttribute]
    private void OnDecrement() => Count -= Step;

    [BlexActionAttribute]
    private void OnSetStep(int step) => Step = step;

    [BlexActionAttribute]
    private void OnReset()
    {
        Count = 0;
        Step = 1;
    }
}
