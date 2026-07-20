namespace Bit.BlazorUI;

public partial class _BitMenuButtonItem<TItem> where TItem : class
{
    [Parameter] public TItem Item { get; set; } = default!;

    [Parameter] public BitMenuButton<TItem> MenuButton { get; set; } = default!;
}
