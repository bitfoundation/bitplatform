namespace Bit.BlazorUI;

public partial class _BitDropdownItem<TItem, TValue> : ComponentBase where TItem : class, new()
{
    [Parameter] public TItem Item { get; set; } = default!;

    [Parameter] public BitDropdown<TItem, TValue> Dropdown { get; set; } = default!;
}
