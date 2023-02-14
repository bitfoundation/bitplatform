
namespace Bit.BlazorUI;

public partial class BitMenuButtonOption
{
    [CascadingParameter] protected BitMenuButton<BitMenuButtonOption> Parent { get; set; } = default!;

    /// <summary>
    /// Name of an icon to render next to the option text
    /// </summary>
    [Parameter] public BitIconName? IconName { get; set; }

    /// <summary>
    /// Text to render in the option
    /// </summary>
    [Parameter] public string? Text { get; set; }

    /// <summary>
    /// A unique value to use as a key of the option
    /// </summary>
    [Parameter] public string? Key { get; set; }

    protected override string RootElementClass => "bit-mbgo";
}
