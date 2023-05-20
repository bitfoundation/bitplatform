﻿using System.Linq;
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
            classes.Add(Nav.ClassStyles?.SelectedItemContainer?.Class ?? string.Empty);
        }

        classes.Add(Nav.ClassStyles?.ItemContainer?.Class ?? string.Empty);

        return string.Join(" ", classes);
    }
    private string GetItemContainerStyles()
    {
        var classes = new List<string>
        {
            Nav.ClassStyles?.ItemContainer?.Style ?? string.Empty
        };

        if (Nav.SelectedItem == Item)
        {
            classes.Add(Nav.ClassStyles?.SelectedItemContainer?.Style ?? string.Empty);
        }

        return string.Join(" ", classes);
    }

    private string GetItemClasses()
    {
        var classes = new List<string>
        {
            Nav.ClassStyles?.Item?.Class ?? string.Empty
        };

        if (Nav.SelectedItem == Item)
        {
            classes.Add(Nav.ClassStyles?.SelectedItem?.Class ?? string.Empty);
        }

        return string.Join(" ", classes);
    }
    private string GetItemStyles()
    {
        var classes = new List<string>
        {
            Nav.ClassStyles?.Item?.Style??string.Empty
        };

        if (Nav.SelectedItem == Item)
        {
            classes.Add(Nav.ClassStyles?.SelectedItem?.Style ?? string.Empty);
        }

        return string.Join(" ", classes);
    }


    private static bool IsRelativeUrl(string? url) => url.HasValue() && new Regex("!/^[a-z0-9+-.]+:\\/\\//i").IsMatch(url!);
}
