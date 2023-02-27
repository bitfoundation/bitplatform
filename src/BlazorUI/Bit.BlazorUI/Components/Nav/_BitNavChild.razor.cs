using System.Text.RegularExpressions;

namespace Bit.BlazorUI.Components.Nav;

#pragma warning disable CA1707 // Identifiers should not contain underscores
public partial class _BitNavChild<TItem> where TItem : class
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

    [CascadingParameter] protected BitNav<TItem> Nav { get; set; } = default!;

    [CascadingParameter] protected _BitNavChild<TItem>? Parent { get; set; }

    [Parameter] public TItem Item { get; set; } = default!;

    [Parameter] public int Depth { get; set; }

    protected override Task OnInitializedAsync()
    {
        var aa = Nav.GetItems(Item);

        return base.OnInitializedAsync();
    }

    private async void HandleOnClick()
    {
        if (Nav is null) return;
        if (Nav.GetIsEnabled(Item) == false) return;

        await Nav.OnItemClick.InvokeAsync(Item);

        if (Nav.GetItems(Item).Any() && Nav.GetUrl(Item).HasNoValue())
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

        if (Nav.GetIsEnabled(Item) is false || Nav.GetItems(Item).Any() is false) return;

        Nav.SetItemExpanded(Item, !Nav.GetItemExpanded(Item));

        await Nav.OnItemToggle.InvokeAsync(Item);
    }

    private static bool IsRelativeUrl(string? url) => url.HasValue() && new Regex("!/^[a-z0-9+-.]+:\\/\\//i").IsMatch(url!);
}
