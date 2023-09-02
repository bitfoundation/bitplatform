namespace Bit.BlazorUI;

public partial class _BitDropdownItem : ComponentBase
{
    [Parameter] public BitDropdownItem Item { get; set; } = default!;

    [Parameter] public BitDropdown Dropdown { get; set; } = default!;
}
