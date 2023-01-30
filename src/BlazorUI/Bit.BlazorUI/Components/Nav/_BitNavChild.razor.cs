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

    [CascadingParameter] protected BitNav? Parent { get; set; }

    [Parameter] public BitNavItem Item { get; set; } = new();

    [Parameter] public int Depth { get; set; }

    private async Task HandleOnClick()
    {
        if (Parent is null) return;
        if (Item.IsEnabled == false) return;

        await Parent.OnItemClick.InvokeAsync(Item);

        if (Item.Items.Any() && Item.Url.HasNoValue())
        {
            await ToggleItem();
        }
        else if (Parent.Mode == BitNavMode.Manual)
        {
            await Parent.SetSelectedItem(Item);
        }
    }

    private async Task ToggleItem()
    {
        if (Parent is null) return;
        if (Item.IsEnabled is false) return;
        if (Item.Items.Any() is false) return;

        Item.IsExpanded = !Item.IsExpanded;

        await Parent.OnItemToggle.InvokeAsync(Item);
    }

    private static bool IsRelativeUrl(string? url) => url.HasValue() && new Regex("!/^[a-z0-9+-.]+:\\/\\//i").IsMatch(url!);
}
