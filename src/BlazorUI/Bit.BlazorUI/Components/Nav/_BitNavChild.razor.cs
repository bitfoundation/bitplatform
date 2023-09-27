using System.Text.RegularExpressions;

namespace Bit.BlazorUI;

public partial class _BitNavChild<TItem> where TItem : class
{
    private static readonly Dictionary<BitNavAriaCurrent, string> _AriaCurrentMap = new()
    {
        [BitNavAriaCurrent.Page] = "page",
        [BitNavAriaCurrent.Step] = "step",
        [BitNavAriaCurrent.Location] = "location",
        [BitNavAriaCurrent.Time] = "time",
        [BitNavAriaCurrent.Date] = "date",
        [BitNavAriaCurrent.True] = "true"
    };


    [CascadingParameter] protected BitNav<TItem> Nav { get; set; } = default!;

    [CascadingParameter] protected _BitNavChild<TItem>? Parent { get; set; }


    [Parameter] public TItem Item { get; set; } = default!;

    [Parameter] public int Depth { get; set; }


    private async Task HandleOnClick()
    {
        if (Nav is null) return;
        if (Nav.GetIsEnabled(Item) is false) return;

        if (Nav.GetChildItems(Item).Any() && Nav.GetUrl(Item).HasNoValue())
        {
            await ToggleItem();
        }
        else if (Nav.Mode == BitNavMode.Manual)
        {
            await Nav.SetSelectedItem(Item);
        }

        await Nav.OnItemClick.InvokeAsync(Item);
    }

    private async Task ToggleItem()
    {
        if (Nav is null) return;

        if (Nav.GetIsEnabled(Item) is false || Nav.GetChildItems(Item).Count is 0) return;

        Nav.SetItemExpanded(Item, Nav.GetItemExpanded(Item) is false);

        await Nav.OnItemToggle.InvokeAsync(Item);
    }

    private string GetItemContainerClasses()
    {
        var classes = new List<string>();

        if (Nav.GetIsEnabled(Item) is false)
        {
            classes.Add("bit-nav-dis");
        }

        if (Nav.SelectedItem == Item)
        {
            classes.Add("bit-nav-sel");
            if (Nav.ClassStyles?.SelectedItemContainer?.Class is not null)
            {
                classes.Add(Nav.ClassStyles?.SelectedItemContainer?.Class!);
            }
        }

        if (Nav.ClassStyles?.ItemContainer?.Class is not null)
        {
            classes.Add(Nav.ClassStyles?.ItemContainer?.Class!);
        }

        return string.Join(" ", classes);
    }
    private string GetItemContainerStyles()
    {
        var classes = new List<string>();
        if (Nav.ClassStyles?.ItemContainer?.Style is not null)
        {
            classes.Add(Nav.ClassStyles?.ItemContainer?.Style!);
        }

        if (Nav.SelectedItem == Item && Nav.ClassStyles?.SelectedItemContainer?.Style is not null)
        {
            classes.Add(Nav.ClassStyles?.SelectedItemContainer?.Style!);
        }

        return string.Join(" ", classes);
    }

    private string GetItemClasses()
    {
        var classes = new List<string>();
        if (Nav.ClassStyles?.Item?.Class is not null)
        {
            classes.Add(Nav.ClassStyles?.Item?.Class!);
        }

        if (Nav.SelectedItem == Item && Nav.ClassStyles?.SelectedItem?.Class is not null)
        {
            classes.Add(Nav.ClassStyles?.SelectedItem?.Class!);
        }

        return string.Join(" ", classes);
    }
    private string GetItemStyles()
    {
        var classes = new List<string>();
        if (Nav.ClassStyles?.Item?.Style is not null)
        {
            classes.Add(Nav.ClassStyles?.Item?.Style!);
        }

        if (Nav.SelectedItem == Item && Nav.ClassStyles?.SelectedItem?.Style is not null)
        {
            classes.Add(Nav.ClassStyles?.SelectedItem?.Style!);
        }

        return string.Join(" ", classes);
    }


    private static bool IsRelativeUrl(string? url) => url.HasValue() && new Regex("!/^[a-z0-9+-.]+:\\/\\//i").IsMatch(url!);
}
