using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Components.Routing;

namespace Bit.BlazorUI;

/// <summary>
/// A navigation pane (Nav) provides links to the main areas of an app or site.
/// </summary>
public partial class BitNav<TItem> : BitComponentBase where TItem : class
{
    internal TItem? _currentItem;
    internal List<TItem> _items = [];
    private IEnumerable<TItem>? _oldItems;
    internal Dictionary<TItem, bool> _itemExpandStates = [];



    [Inject] private NavigationManager _navigationManager { get; set; } = default!;



    /// <summary>
    /// Collapses all items and children.
    /// </summary>
    public void CollapseAll(TItem? item = null)
    {
        (item is null ? _items : [item]).ToList().ForEach(it => ToggleItemAndChildren(it, false));
    }

    /// <summary>
    /// Expands all items and children in non-SingleExpand mode.
    /// </summary>
    public void ExpandAll(TItem? item = null)
    {
        if (SingleExpand) return;

        (item is null ? _items : [item]).ToList().ForEach(it => ToggleItemAndChildren(it, true));
    }

    /// <summary>
    /// Toggles an item.
    /// </summary>
    public async Task ToggleItem(TItem Item)
    {
        var isExpanded = GetItemExpanded(Item) is false;

        if (SingleExpand)
        {
            if (isExpanded)
            {
                if (_currentItem is not null)
                {
                    ToggleItemAndParents(_items, _currentItem, false);
                }

                ToggleItemAndParents(_items, Item, isExpanded);
            }
            else
            {
                SetItemExpanded(Item, isExpanded);
            }

            StateHasChanged();

            _currentItem = Item;
        }
        else
        {
            SetItemExpanded(Item, isExpanded);
        }

        await OnItemToggle.InvokeAsync(Item);
    }



    internal void RegisterOption(BitNavOption option)
    {
        var item = (option as TItem)!;

        _items.Add(item);

        StateHasChanged();
    }

    internal void UnregisterOption(BitNavOption option)
    {
        _items.Remove((option as TItem)!);

        StateHasChanged();
    }



    protected override string RootElementClass => "bit-nav";

    protected override void RegisterCssClasses()
    {
        ClassBuilder.Register(() => Classes?.Root);

        ClassBuilder.Register(() => FitWidth ? "bit-nav-ftw" : string.Empty);
        ClassBuilder.Register(() => FullWidth ? "bit-nav-flw" : string.Empty);

        ClassBuilder.Register(() => IconOnly ? "bit-nav-ion" : string.Empty);

        ClassBuilder.Register(() => Accent switch
        {
            BitColor.Primary => "bit-nav-apri",
            BitColor.Secondary => "bit-nav-asec",
            BitColor.Tertiary => "bit-nav-ater",
            BitColor.Info => "bit-nav-ainf",
            BitColor.Success => "bit-nav-asuc",
            BitColor.Warning => "bit-nav-awrn",
            BitColor.SevereWarning => "bit-nav-aswr",
            BitColor.Error => "bit-nav-aerr",
            BitColor.PrimaryBackground => "bit-nav-apbg",
            BitColor.SecondaryBackground => "bit-nav-asbg",
            BitColor.TertiaryBackground => "bit-nav-atbg",
            BitColor.PrimaryForeground => "bit-nav-apfg",
            BitColor.SecondaryForeground => "bit-nav-asfg",
            BitColor.TertiaryForeground => "bit-nav-atfg",
            BitColor.PrimaryBorder => "bit-nav-apbr",
            BitColor.SecondaryBorder => "bit-nav-asbr",
            BitColor.TertiaryBorder => "bit-nav-atbr",
            _ => "bit-nav-apbg",
        });

        ClassBuilder.Register(() => Color switch
        {
            BitColor.Primary => "bit-nav-pri",
            BitColor.Secondary => "bit-nav-sec",
            BitColor.Tertiary => "bit-nav-ter",
            BitColor.Info => "bit-nav-inf",
            BitColor.Success => "bit-nav-suc",
            BitColor.Warning => "bit-nav-wrn",
            BitColor.SevereWarning => "bit-nav-swr",
            BitColor.Error => "bit-nav-err",
            BitColor.PrimaryBackground => "bit-nav-pbg",
            BitColor.SecondaryBackground => "bit-nav-sbg",
            BitColor.TertiaryBackground => "bit-nav-tbg",
            BitColor.PrimaryForeground => "bit-nav-pfg",
            BitColor.SecondaryForeground => "bit-nav-sfg",
            BitColor.TertiaryForeground => "bit-nav-tfg",
            BitColor.PrimaryBorder => "bit-nav-pbr",
            BitColor.SecondaryBorder => "bit-nav-sbr",
            BitColor.TertiaryBorder => "bit-nav-tbr",
            _ => "bit-nav-pri",
        });
    }

    protected override void RegisterCssStyles()
    {
        StyleBuilder.Register(() => Styles?.Root);
    }

    protected override async Task OnInitializedAsync()
    {
        if (ChildContent is null && Items.Any())
        {
            _items = [.. Items];
            _oldItems = Items;
        }

        foreach (var item in Flatten(_items))
        {
            if (AllExpanded)
            {
                SetIsExpanded(item, true);
            }

            SetItemExpanded(item, GetIsExpanded(item) ?? false);
        }

        if (Mode == BitNavMode.Automatic)
        {
            SetSelectedItemByCurrentUrl();
            _navigationManager.LocationChanged += OnLocationChanged;
        }
        else
        {
            if (DefaultSelectedItem is not null && SelectedItemHasBeenSet is false)
            {
                await AssignSelectedItem(DefaultSelectedItem);
            }
        }

        await base.OnInitializedAsync();
    }



    internal void SetItemExpanded(TItem item, bool value)
    {
        var isExpanded = GetIsExpanded(item);

        if (isExpanded is not null)
        {
            SetIsExpanded(item, value);
            return;
        }

        if (_itemExpandStates.ContainsKey(item))
        {
            _itemExpandStates[item] = value;
        }
        else
        {
            _itemExpandStates.Add(item, value);
        }

    }

    internal bool GetItemExpanded(TItem item)
    {
        var isExpanded = GetIsExpanded(item);

        if (isExpanded is not null)
        {
            return isExpanded.Value;
        }

        return _itemExpandStates[item];
    }

    internal async Task SetSelectedItem(TItem? item)
    {
        if (item == SelectedItem && Reselectable is false) return;

        if (await AssignSelectedItem(item) is false) return;

        await OnSelectItem.InvokeAsync(item);

        StateHasChanged();
    }

    internal string GetItemKey(TItem item, string defaultKey)
    {
        return GetKey(item) ?? $"{UniqueId}-{defaultKey}";
    }

    internal void SetSelectedItemByCurrentUrl()
    {
        if (Mode is not BitNavMode.Automatic) return;

        string currentUrl = _navigationManager.Uri.Replace(_navigationManager.BaseUri, "/", StringComparison.Ordinal);

        var currentItem = Flatten(_items).FirstOrDefault(item =>
        {
            var match = GetMatch(item) ?? Match ?? BitNavMatch.Exact;

            if (IsMatch(GetUrl(item), match)) return true;

            return GetAdditionalUrls(item)?.Any(u => IsMatch(u, match)) is true;
        });

        _ = SetSelectedItem(currentItem);

        const string DOUBLE_STAR_PLACEHOLDER = "___BIT_NAV_DOUBLESTAR_PLACEHOLDER___";
        bool IsMatch(string? itemUrl, BitNavMatch? match)
        {
            if (itemUrl is null) return false;

            return match switch
            {
                BitNavMatch.Exact => itemUrl == currentUrl,
                BitNavMatch.Prefix => currentUrl.StartsWith(itemUrl, StringComparison.Ordinal),
                BitNavMatch.Regex => Regex.IsMatch(currentUrl, itemUrl),
                BitNavMatch.Wildcard => IsWildcardMatch(currentUrl, itemUrl),
                _ => itemUrl == currentUrl,
            };

            bool IsWildcardMatch(string input, string pattern)
            {
                string regexPattern = $"^{WildcardToRegex(pattern)}$";
                return Regex.IsMatch(input, regexPattern);
            }

            string WildcardToRegex(string pattern)
            {
                pattern = Regex.Escape(pattern);

                pattern = pattern.Replace(@"\*\*", DOUBLE_STAR_PLACEHOLDER);
                pattern = pattern.Replace(@"\*", "[^/]*");
                pattern = pattern.Replace(@"\?", "[^/]");
                pattern = pattern.Replace(DOUBLE_STAR_PLACEHOLDER, ".*");

                return pattern;
            }
        }
    }



    private List<TItem> Flatten(IList<TItem> e) => [.. e.SelectMany(c => Flatten(GetChildItems(c))), .. e];

    private void OnLocationChanged(object? sender, LocationChangedEventArgs args)
    {
        SetSelectedItemByCurrentUrl();

        StateHasChanged();
    }

    private void ToggleItemAndChildren(TItem item, bool isExpanded = false)
    {
        SetIsExpanded(item, isExpanded);

        foreach (var child in GetChildItems(item))
        {
            ToggleItemAndChildren(child, isExpanded);
        }
    }

    private void OnSetSelectedItem()
    {
        if (SelectedItem is null) return;

        ToggleItemAndParents(_items, SelectedItem, true);
    }

    private void OnSetMode()
    {
        if (Mode is not BitNavMode.Automatic) return;

        SetSelectedItemByCurrentUrl();
    }

    private void OnSetParameters()
    {
        if (ChildContent is not null || Options is not null || Items == _oldItems) return;

        _items = Items?.ToList() ?? [];
        _oldItems = Items;

        SetSelectedItemByCurrentUrl();
    }

    private bool ToggleItemAndParents(IList<TItem> items, TItem item, bool isExpanded)
    {
        foreach (var parent in items)
        {
            var childItems = GetChildItems(parent);
            if (parent == item || (childItems.Any() && ToggleItemAndParents(childItems, item, isExpanded)))
            {
                SetItemExpanded(parent, isExpanded);
                return true;
            }
        }

        return false;
    }



    protected override async ValueTask DisposeAsync(bool disposing)
    {
        if (IsDisposed || disposing is false) return;

        if (Mode == BitNavMode.Automatic)
        {
            _navigationManager.LocationChanged -= OnLocationChanged;
        }

        await base.DisposeAsync(disposing);
    }
}
