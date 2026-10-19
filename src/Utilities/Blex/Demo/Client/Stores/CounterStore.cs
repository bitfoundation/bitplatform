namespace Bit.Blex.Demo.Client.Stores;

/// <summary>
/// The canonical first store. Exercises the four building blocks -- <c>[BlexStateAttribute]</c>,
/// <c>[BlexComputedAttribute]</c>, <c>[BlexActionAttribute]</c> and an action with a payload -- and opts in to
/// persistence so a page reload keeps the value.
/// </summary>
[BlexStoreAttribute(Name = "counter", Persist = true)]
public partial class CounterStore
{
    [BlexStateAttribute] private int _count;
    [BlexStateAttribute] private int _step = 1;

    [BlexComputedAttribute] private int ComputeDoubleCount() => Count * 2;

    [BlexComputedAttribute] private bool ComputeIsEven() => Count % 2 == 0;

    [BlexActionAttribute] private void OnIncrement() => Count += Step;

    [BlexActionAttribute] private void OnDecrement() => Count -= Step;

    [BlexActionAttribute] private void OnSetStep(int step) => Step = step;

    /// <summary>Two mutations inside one action: a single notification and one history entry.</summary>
    [BlexActionAttribute(Name = "Apply preset")]
    private void OnApplyPreset(int count, int step)
    {
        Count = count;
        Step = step;
    }
}
