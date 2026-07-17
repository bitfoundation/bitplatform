namespace Bit.BlazorUI;

public partial class _BitButtonGroupItem<TItem> where TItem : class
{
    [Parameter] public TItem Item { get; set; } = default!;

    [Parameter] public BitButtonGroup<TItem> ButtonGroup { get; set; } = default!;
}
