using System.Text.RegularExpressions;

namespace Bit.BlazorUI;

public partial class _BitNavChild<TItem> where TItem : class
{
    [CascadingParameter] protected BitNav<TItem> Nav { get; set; } = default!;

    [CascadingParameter] protected _BitNavChild<TItem>? Parent { get; set; }



    [Parameter] public TItem Item { get; set; } = default!;

    [Parameter] public string? Key { get; set; }

    [Parameter] public int Depth { get; set; }



    private async Task HandleOnClick(bool renderLink)
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

        if (Nav.SelectedItem != Item || Nav.Reselectable)
        {
            if (renderLink)
            {
                await Task.Yield(); // wait for the link to navigate first
            }
            await Nav.OnItemClick.InvokeAsync(Item);
        }
    }
    private async Task ToggleItem()
    {
        if (Nav is null) return;

        if (Nav.GetIsEnabled(Item) is false || Nav.GetChildItems(Item).Count is 0) return;

        await Nav.ToggleItem(Item);
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
            if (Nav.Classes?.SelectedItemContainer is not null)
            {
                classes.Add(Nav.Classes?.SelectedItemContainer!);
            }
        }

        if (Nav.Classes?.ItemContainer is not null)
        {
            classes.Add(Nav.Classes?.ItemContainer!);
        }

        return string.Join(" ", classes);
    }
    private string GetItemContainerStyles()
    {
        var styles = new List<string>();

        if (Nav.FitWidth && Nav.IconOnly is false)
        {
            styles.Add($"padding-inline-end:{Nav.IndentPadding}px;");
        }

        if (Nav.Styles?.ItemContainer is not null)
        {
            styles.Add(Nav.Styles.ItemContainer);
        }

        if (Nav.SelectedItem == Item && Nav.Styles?.SelectedItemContainer is not null)
        {
            styles.Add(Nav.Styles?.SelectedItemContainer!);
        }

        return string.Join(" ", styles);
    }
    private string GetItemClasses()
    {
        var classes = new List<string>();
        if (Nav.Classes?.Item is not null)
        {
            classes.Add(Nav.Classes?.Item!);
        }

        if (Nav.SelectedItem == Item && Nav.Classes?.SelectedItem is not null)
        {
            classes.Add(Nav.Classes?.SelectedItem!);
        }

        return string.Join(" ", classes);
    }
    private string GetItemStyles()
    {
        var classes = new List<string>();
        if (Nav.Styles?.Item is not null)
        {
            classes.Add(Nav.Styles?.Item!);
        }

        if (Nav.SelectedItem == Item && Nav.Styles?.SelectedItem is not null)
        {
            classes.Add(Nav.Styles?.SelectedItem!);
        }

        return string.Join(" ", classes);
    }
    private static bool IsRelativeUrl(string? url) => url.HasValue() && new Regex("!/^[a-z0-9+-.]+:\\/\\//i").IsMatch(url!);



    private static readonly Dictionary<BitNavAriaCurrent, string> _AriaCurrentMap = new()
    {
        [BitNavAriaCurrent.Page] = "page",
        [BitNavAriaCurrent.Step] = "step",
        [BitNavAriaCurrent.Location] = "location",
        [BitNavAriaCurrent.Time] = "time",
        [BitNavAriaCurrent.Date] = "date",
        [BitNavAriaCurrent.True] = "true"
    };
}
