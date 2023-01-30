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

    [CascadingParameter] protected BitNavList<TItem>? Parent { get; set; }

    [Parameter] public TItem Item { get; set; } = default!;

    [Parameter] public int Depth { get; set; }

    private async void HandleOnClick()
    {
        if (Parent is null) return;
        if (Parent.GetIsEnabled(Item) == false) return;

        await Parent.OnItemClick.InvokeAsync(Item);

        if (Parent.GetItems(Item).Any() && Parent.GetUrl(Item).HasNoValue())
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

        if (Parent.GetIsEnabled(Item) is false || Parent.GetItems(Item).Any() is false) return;

        Parent.SetItemExpanded(Item, !Parent.GetItemExpanded(Item));

        await Parent.OnItemToggle.InvokeAsync(Item);
    }

    private static bool IsRelativeUrl(string? url) => url.HasValue() && new Regex("!/^[a-z0-9+-.]+:\\/\\//i").IsMatch(url!);
}
