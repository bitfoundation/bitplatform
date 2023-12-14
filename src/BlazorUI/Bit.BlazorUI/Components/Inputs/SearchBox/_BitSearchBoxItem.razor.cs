namespace Bit.BlazorUI;

public partial class _BitSearchBoxItem<TItem> : ComponentBase where TItem : class
{
    [Parameter] public TItem Item { get; set; } = default!;

    [Parameter] public BitSearchBox<TItem> SearchBox { get; set; } = default!;
}
