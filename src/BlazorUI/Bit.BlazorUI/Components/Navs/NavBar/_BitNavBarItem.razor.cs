namespace Bit.BlazorUI;

public partial class _BitNavBarItem<TItem> where TItem : class
{
    [Parameter] public TItem Item { get; set; } = default!;

    [Parameter] public BitNavBar<TItem> NavBar { get; set; } = default!;
}
