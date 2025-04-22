namespace Bit.BlazorUI;

public partial class _BitChoiceGroupItem<TItem, TValue> : ComponentBase where TItem : class, new ()
{
    [Parameter] public TItem Item { get; set; } = default!;

    [Parameter] public BitChoiceGroup<TItem, TValue> ChoiceGroup { get; set; } = default!;
}
