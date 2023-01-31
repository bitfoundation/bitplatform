using System.Text.RegularExpressions;

namespace Bit.BlazorUI.Components.NavList;

public partial class _BitNavListChild<TItem> where TItem : class
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

    [CascadingParameter] protected BitNavList<TItem> NavList { get; set; } = default!;

    [CascadingParameter] protected _BitNavListChild<TItem>? Parent { get; set; }

    [Parameter] public TItem Item { get; set; } = default!;

    [Parameter] public int Depth { get; set; }

    private async void HandleOnClick()
    {
        if (NavList is null) return;
        if (NavList.GetIsEnabled(Item) == false) return;

        await NavList.OnItemClick.InvokeAsync(Item);

        if (NavList.GetItems(Item).Any() && NavList.GetUrl(Item).HasNoValue())
        {
            await ToggleItem();
        }
        else if (NavList.Mode == BitNavMode.Manual)
        {
            await NavList.SetSelectedItem(Item);
        }
    }

    private async Task ToggleItem()
    {
        if (NavList is null) return;

        if (NavList.GetIsEnabled(Item) is false || NavList.GetItems(Item).Any() is false) return;

        NavList.SetItemExpanded(Item, !NavList.GetItemExpanded(Item));

        await NavList.OnItemToggle.InvokeAsync(Item);
    }

    private static bool IsRelativeUrl(string? url) => url.HasValue() && new Regex("!/^[a-z0-9+-.]+:\\/\\//i").IsMatch(url!);
}
