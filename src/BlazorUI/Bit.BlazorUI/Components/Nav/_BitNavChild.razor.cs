using System.Text;
using System.Text.RegularExpressions;

namespace Bit.BlazorUI;

#pragma warning disable CA1707 // Identifiers should not contain underscores
public partial class _BitNavChild<TItem> where TItem : class
#pragma warning restore CA1707 // Identifiers should not contain underscores
{
    private static Dictionary<BitNavAriaCurrent, string> _AriaCurrentMap = new()
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


    private async void HandleOnClick()
    {
        if (Nav is null) return;
        if (Nav.GetIsEnabled(Item) == false) return;

        await Nav.OnItemClick.InvokeAsync(Item);

        if (Nav.GetChildItems(Item).Any() && Nav.GetUrl(Item).HasNoValue())
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

        if (Nav.GetIsEnabled(Item) is false || Nav.GetChildItems(Item).Any() is false) return;

        Nav.SetItemExpanded(Item, !Nav.GetItemExpanded(Item));

        await Nav.OnItemToggle.InvokeAsync(Item);
    }

    private string GetItemContainerClasses()
    {
        var sb = new StringBuilder();

        if (Nav.GetIsEnabled(Item) is false)
        {
            sb.Append("disabled ");
        }

        if (Nav.SelectedItem == Item)
        {
            sb.Append("selected ")
              .Append(Nav.ClassStyles?.SelectedItemContainer?.Class)
              .Append(' ');
        }

        sb.Append(Nav.ClassStyles?.ItemContainer?.Class);

        return sb.ToString();
    }
    private string GetItemContainerStyles()
    {
        var sb = new StringBuilder();
        sb.Append(Nav.ClassStyles?.ItemContainer?.Style);

        if (Nav.SelectedItem == Item)
        {
            sb.Append(' ')
              .Append(Nav.ClassStyles?.SelectedItemContainer?.Style);
        }

        return sb.ToString();
    }

    private string GetItemClasses()
    {
        var sb = new StringBuilder();
        sb.Append(Nav.ClassStyles?.Item?.Class);

        if (Nav.SelectedItem == Item)
        {
            sb.Append(' ')
              .Append(Nav.ClassStyles?.SelectedItem?.Class);
        }

        return sb.ToString();
    }
    private string GetItemStyles()
    {
        var sb = new StringBuilder();
        sb.Append(Nav.ClassStyles?.Item?.Style);

        if (Nav.SelectedItem == Item)
        {
            sb.Append(' ')
              .Append(Nav.ClassStyles?.SelectedItem?.Style);
        }

        return sb.ToString();
    }


    private static bool IsRelativeUrl(string? url) => url.HasValue() && new Regex("!/^[a-z0-9+-.]+:\\/\\//i").IsMatch(url!);
}
