using System.Diagnostics.CodeAnalysis;

namespace Bit.BlazorUI;

public partial class _BitDropdownItem<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] TItem, TValue> : ComponentBase where TItem : class, new()
{
    [Parameter] public TItem Item { get; set; } = default!;

    [Parameter] public BitDropdown<TItem, TValue> Dropdown { get; set; } = default!;

    [Parameter] public string? ItemCheckIconCss { get; set; }
}
