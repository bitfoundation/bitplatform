
namespace Bit.BlazorUI.Components.Nav;

public partial class BitNavChild
{
    private int depth;

    [CascadingParameter] protected BitNav ParentBitNav { get; set; } = default!;

    [Parameter] public BitNavItem Item { get; set; } = default!;
}
