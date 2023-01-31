using System.Text.RegularExpressions;

namespace Bit.BlazorUI.Components.Nav;

#pragma warning disable CA1707 // Identifiers should not contain underscores
public partial class _BitNavChild
#pragma warning restore CA1707 // Identifiers should not contain underscores
{
    private static Dictionary<BitNavItemAriaCurrent, string> _AriaCurrentMap = new()
    {
        [BitNavItemAriaCurrent.Page] = "page",
        [BitNavItemAriaCurrent.Step] = "step",
        [BitNavItemAriaCurrent.Location] = "location",
        [BitNavItemAriaCurrent.Time] = "time",
        [BitNavItemAriaCurrent.Date] = "date",
        [BitNavItemAriaCurrent.True] = "true"
    };

    [CascadingParameter] protected BitNav Nav { get; set; } = default!;

    [CascadingParameter] protected _BitNavChild? Parent { get; set; }

    [Parameter] public BitNavItem Item { get; set; } = new();

    [Parameter] public int Depth { get; set; }

    private async Task HandleOnClick()
    {
        if (Nav is null) return;
        if (Item.IsEnabled == false) return;

        await Nav.OnItemClick.InvokeAsync(Item);

        if (Item.Items.Any() && Item.Url.HasNoValue())
        {
            await ToggleItem();
        }
        else if (Nav.Mode == BitNavMode.Manual)
        {
            await Nav.SetSelectedItem(Item);
        }
    }

    private async Task ToggleItem()
    {
        if (Nav is null) return;
        if (Item.IsEnabled is false) return;
        if (Item.Items.Any() is false) return;

        Item.IsExpanded = !Item.IsExpanded;

        await Nav.OnItemToggle.InvokeAsync(Item);
    }

    private static bool IsRelativeUrl(string? url) => url.HasValue() && new Regex("!/^[a-z0-9+-.]+:\\/\\//i").IsMatch(url!);
}
