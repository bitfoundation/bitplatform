using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Components.Routing;

namespace Bit.BlazorUI;

/// <summary>
/// A navigation pane (Nav) provides links to the main areas of an app or site.
/// </summary>
/// <remarks>
/// The nav renders a list of links (or buttons, for the items without a URL) that the Tab key reaches one by
/// one, and the arrow keys, Home, End and type-ahead move through as the WAI-ARIA tree pattern describes:
/// Up and Down walk the visible items, Right expands a collapsed item and then steps into it, Left collapses
/// an expanded item and then steps out to its parent, and the asterisk expands every sibling at a level.
/// <br />
/// Give the nav an accessible name through <see cref="BitComponentBase.AriaLabel"/> when a page holds more
/// than one navigation landmark, since assistive technologies cannot tell two unlabeled ones apart.
/// </remarks>
public partial class BitNav<TItem> : BitComponentBase where TItem : class
{
    private const int TYPE_AHEAD_RESET_MS = 1000;

    internal TItem? _currentItem;
    internal List<TItem> _items = [];
    private bool _selectionDirty;
    private TItem? _focusedItem;
    private string _typeAheadBuffer = string.Empty;
    private bool _preventKeyDownDefault;
    private DateTime _lastTypeAheadAt = DateTime.MinValue;
    private IEnumerable<TItem>? _oldItems;
    internal Dictionary<TItem, bool> _itemExpandStates = [];
    private readonly Dictionary<TItem, ElementReference> _itemElements = [];



    [Inject] private NavigationManager _navigationManager { get; set; } = default!;



    /// <summary>
    /// Collapses all items and children.
    /// </summary>
    public void CollapseAll(TItem? item = null)
    {
        (item is null ? _items : [item]).ToList().ForEach(it => ToggleItemAndChildren(it, false));

        RefreshOptions();
        StateHasChanged();
    }

    /// <summary>
    /// Expands all items and children in non-SingleExpand mode.
    /// </summary>
    public void ExpandAll(TItem? item = null)
    {
        if (SingleExpand) return;

        (item is null ? _items : [item]).ToList().ForEach(it => ToggleItemAndChildren(it, true));

        RefreshOptions();
        StateHasChanged();
    }

    /// <summary>
    /// Toggles an item.
    /// </summary>
    public async Task ToggleItem(TItem item)
    {
        var isExpanded = GetItemExpanded(item) is false;

        if (SingleExpand)
        {
            if (isExpanded)
            {
                if (_currentItem is not null)
                {
                    ToggleItemAndParents(_items, _currentItem, false);
                }

                ToggleItemAndParents(_items, item, isExpanded);
            }
            else
            {
                SetItemExpanded(item, isExpanded);
            }

            _currentItem = item;
        }
        else
        {
            SetItemExpanded(item, isExpanded);
        }

        RefreshOptions();
        StateHasChanged();

        await OnItemToggle.InvokeAsync(item);
    }

    /// <summary>
    /// Selects an item programmatically, exactly like a click on that item would in the manual mode.
    /// </summary>
    public Task SelectItem(TItem? item) => SetSelectedItem(item);

    /// <summary>
    /// Moves the focus to an item of the nav.
    /// </summary>
    public ValueTask FocusItem(TItem item) => FocusItemElement(item);



    internal void RegisterOption(BitNavOption option)
    {
        var item = (option as TItem)!;

        _items.Add(item);

        StateHasChanged();
    }

    internal void UnregisterOption(BitNavOption option)
    {
        if (IsDisposed) return;

        var item = (option as TItem)!;

        _items.Remove(item);
        _itemExpandStates.Remove(item);
        _itemElements.Remove(item);

        StateHasChanged();
    }

    internal void RegisterItemElement(TItem item, ElementReference element)
    {
        _itemElements[item] = element;
    }

    internal void UnregisterItemElement(TItem item)
    {
        if (IsDisposed) return;

        _itemElements.Remove(item);
    }

    internal void SetFocusedItem(TItem item)
    {
        _focusedItem = item;
    }

    /// <summary>
    /// Whether the default action of the key that is currently being handled has to be suppressed. Read by
    /// the items at render time, since Blazor evaluates the preventDefault directive there rather than when
    /// the event is dispatched.
    /// </summary>
    internal bool PreventKeyDownDefault => _preventKeyDownDefault;

    /// <summary>
    /// Whether an item is the selected one. The comparison goes through the default equality comparer of
    /// the item type, so a record or any other value-equal item type highlights the selection correctly.
    /// </summary>
    internal bool IsSelected(TItem? item) => AreEqual(item, SelectedItem);



    protected override string RootElementClass => "bit-nav";

    protected override void RegisterCssClasses()
    {
        ClassBuilder.Register(() => Classes?.Root);

        ClassBuilder.Register(() => FitWidth ? "bit-nav-ftw" : string.Empty);
        ClassBuilder.Register(() => FullWidth ? "bit-nav-flw" : string.Empty);

        ClassBuilder.Register(() => IconOnly ? "bit-nav-ion" : string.Empty);

        ClassBuilder.Register(() => Size switch
        {
            BitSize.Small => "bit-nav-sm",
            BitSize.Medium => "bit-nav-md",
            BitSize.Large => "bit-nav-lg",
            _ => "bit-nav-md"
        });

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
        if ((Options ?? ChildContent) is null && Items.Any())
        {
            _items = [.. Items];
            _oldItems = Items;
        }

        foreach (var item in Flatten(_items))
        {
            SetItemExpanded(item, AllExpanded || (GetIsExpanded(item) ?? false));
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

    protected override void OnParametersSet()
    {
        // Options render their items themselves and Blazor skips re-rendering them when only the
        // nav's own parameters (Styles, IconOnly, ItemTemplate, ...) change, so push a re-render to each one.
        RefreshOptions();

        base.OnParametersSet();
    }

    protected override void OnAfterRender(bool firstRender)
    {
        // In Automatic mode each option requests a URL match as it registers. Running the match per
        // option is O(n^2) (each pass flattens the whole tree and matches every item), so the options
        // only flag it and the match runs once here, after the registration batch has rendered.
        if (_selectionDirty)
        {
            _selectionDirty = false;
            SetSelectedItemByCurrentUrl();
        }

        base.OnAfterRender(firstRender);
    }



    internal void SetItemExpanded(TItem item, bool value)
    {
        var isExpanded = GetIsExpanded(item);

        if (isExpanded is not null)
        {
            SetIsExpanded(item, value);
            return;
        }

        _itemExpandStates[item] = value;
    }

    internal bool GetItemExpanded(TItem item)
    {
        var isExpanded = GetIsExpanded(item);

        if (isExpanded is not null)
        {
            return isExpanded.Value;
        }

        // An item that has not been through SetItemExpanded yet (added after the first render, for
        // instance) is simply collapsed, so the lookup must not throw for a missing key.
        return _itemExpandStates.TryGetValue(item, out var state) && state;
    }

    internal async Task SetSelectedItem(TItem? item)
    {
        if (IsSelected(item) && Reselectable is false) return;

        if (await AssignSelectedItem(item) is false) return;

        await OnSelectItem.InvokeAsync(item);

        RefreshOptions();
        StateHasChanged();
    }

    internal string GetItemKey(TItem item, string defaultKey)
    {
        return GetKey(item) ?? $"{UniqueId}-{defaultKey}";
    }

    // Flags that the Automatic-mode selection needs to be recomputed. Called by options as they register
    // instead of matching immediately, so a batch of registrations collapses into a single match pass
    // in OnAfterRender rather than one O(n) pass per option.
    internal void MarkSelectionDirty()
    {
        _selectionDirty = true;
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
                BitNavMatch.Regex => IsRegexMatch(currentUrl, itemUrl),
                BitNavMatch.Wildcard => IsWildcardMatch(currentUrl, itemUrl),
                _ => itemUrl == currentUrl,
            };

            bool IsWildcardMatch(string input, string pattern)
            {
                string regexPattern = $"^{WildcardToRegex(pattern)}$";
                return IsRegexMatch(input, regexPattern);
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



    // The Regex and Wildcard modes run a pattern that comes from the item, so the match is given a
    // timeout to keep a pathological pattern from hanging the render, and a malformed one is simply
    // treated as a non-match instead of tearing the whole nav down.
    private static bool IsRegexMatch(string input, string pattern)
    {
        try
        {
            return Regex.IsMatch(input, pattern, RegexOptions.None, TimeSpan.FromSeconds(1));
        }
        catch (RegexMatchTimeoutException) { return false; }
        catch (ArgumentException) { return false; }
    }

    private static bool AreEqual(TItem? first, TItem? second) => EqualityComparer<TItem?>.Default.Equals(first, second);

    // Kept lazy (and in the original order: every descendant before the items of the level it belongs to)
    // so a URL match stops at the first hit instead of materializing the whole tree on every pass.
    private IEnumerable<TItem> Flatten(IList<TItem> items) => items.SelectMany(i => Flatten(GetChildItems(i))).Concat(items);

    private void OnLocationChanged(object? sender, LocationChangedEventArgs args)
    {
        SetSelectedItemByCurrentUrl();

        StateHasChanged();
    }

    private void ToggleItemAndChildren(TItem item, bool isExpanded = false)
    {
        SetItemExpanded(item, isExpanded);

        foreach (var child in GetChildItems(item))
        {
            ToggleItemAndChildren(child, isExpanded);
        }
    }

    private void OnSetSelectedItem()
    {
        if (SelectedItem is null) return;

        ToggleItemAndParents(_items, SelectedItem, true);

        // The selection affects the previously and newly selected items, which render themselves in
        // the options mode, so push a re-render to all of them after the expansion state is updated.
        RefreshOptions();
    }

    private void RefreshOptions()
    {
        // In the Items API there are no registered options, so there is nothing to refresh.
        if ((Options ?? ChildContent) is null) return;

        foreach (var item in _items)
        {
            (item as BitNavOption)?.InternalRecursiveStateHasChanged();
        }
    }

    private void OnSetMode()
    {
        if (Mode is not BitNavMode.Automatic) return;

        MarkSelectionDirty();
    }

    private void OnSetParameters()
    {
        if (ChildContent is not null || Options is not null || Items == _oldItems) return;

        _items = Items?.ToList() ?? [];
        _oldItems = Items;

        // The match is deferred to the end of the render instead of running here, because the parameters of
        // a single SetParametersAsync are assigned one by one: matching now would read a Mode (or a Match)
        // that the same parameter set is still about to change.
        MarkSelectionDirty();
    }

    private bool ToggleItemAndParents(IList<TItem> items, TItem item, bool isExpanded)
    {
        foreach (var parent in items)
        {
            var childItems = GetChildItems(parent);
            if (AreEqual(parent, item) || (childItems.Any() && ToggleItemAndParents(childItems, item, isExpanded)))
            {
                SetItemExpanded(parent, isExpanded);
                return true;
            }
        }

        return false;
    }



    // The nav is a list of links that Tab reaches one by one, and the arrow keys, Home, End, the asterisk
    // and type-ahead move through it as the WAI-ARIA tree pattern describes.
    // The arrow, Home and End keys scroll the page by default, so their default action is suppressed while
    // the nav navigates with them. It is kept key-scoped so Tab, Enter and Space still behave normally, and
    // it goes through a flag rather than a constant because Blazor evaluates the directive at render time.
    internal async Task HandleOnKeyDown(TItem source, KeyboardEventArgs e)
    {
        _preventKeyDownDefault = IsEnabled && e.Key is "ArrowUp" or "ArrowDown" or "ArrowLeft" or "ArrowRight" or "Home" or "End";

        if (IsEnabled is false) return;
        if (e.CtrlKey || e.AltKey || e.MetaKey) return;

        // The focus event of the item that received the key has already run, so the focused item is known;
        // the item the event came from is only the fallback for a nav that has never seen a focus event.
        _focusedItem ??= source;

        var visibleItems = GetVisibleItems();
        if (visibleItems.Count == 0) return;

        var index = _focusedItem is null ? -1 : visibleItems.FindIndex(i => AreEqual(i, _focusedItem));
        var current = index < 0 ? null : visibleItems[index];
        var isRtl = (Dir ?? CascadingDir) == BitDir.Rtl;

        switch (e.Key)
        {
            case "ArrowDown":
                await FocusItemAt(visibleItems, index + 1);
                return;

            case "ArrowUp":
                await FocusItemAt(visibleItems, index - 1);
                return;

            case "Home":
                await FocusItemAt(visibleItems, 0);
                return;

            case "End":
                await FocusItemAt(visibleItems, visibleItems.Count - 1);
                return;

            case "ArrowRight":
            case "ArrowLeft":
                var isForward = (e.Key is "ArrowRight") != isRtl;
                if (current is null) return;
                if (isForward)
                {
                    await StepIn(current);
                }
                else
                {
                    await StepOut(current, visibleItems);
                }
                return;

            // The asterisk expands every sibling of the focused item, which is how a tree opens a whole
            // level at once.
            case "*":
                if (current is null) return;
                foreach (var sibling in GetSiblingsOf(current))
                {
                    if (GetChildItems(sibling).Any() && GetItemExpanded(sibling) is false)
                    {
                        await ToggleItem(sibling);
                    }
                }
                return;

            default:
                // Space is the activation key of the chevron, never the start of a type-ahead search.
                if (e.Key.Length != 1 || e.Key == " ") return;
                await TypeAhead(e.Key, visibleItems, index);
                return;
        }
    }

    private async Task StepIn(TItem item)
    {
        var childItems = GetChildItems(item);
        if (childItems.Count == 0) return;

        if (GetItemExpanded(item) is false)
        {
            if (NoCollapse || GetIsEnabled(item) is false) return;

            await ToggleItem(item);
            return;
        }

        var firstChild = childItems.FirstOrDefault(i => GetIsSeparator(i) is false);
        if (firstChild is not null)
        {
            await FocusItemElement(firstChild);
        }
    }

    private async Task StepOut(TItem item, List<TItem> visibleItems)
    {
        if (GetChildItems(item).Any() && GetItemExpanded(item) && NoCollapse is false && GetIsEnabled(item))
        {
            await ToggleItem(item);
            return;
        }

        var parent = FindParentOf(_items, item);
        if (parent is not null && visibleItems.Any(i => AreEqual(i, parent)))
        {
            await FocusItemElement(parent);
        }
    }

    private async Task TypeAhead(string key, List<TItem> visibleItems, int index)
    {
        // Consecutive keystrokes build a search term; a pause starts a new one, exactly like a native
        // list box. Repeating the same character walks the items starting with it instead.
        var now = DateTime.UtcNow;
        _typeAheadBuffer = (now - _lastTypeAheadAt).TotalMilliseconds > TYPE_AHEAD_RESET_MS ? key : _typeAheadBuffer + key;
        _lastTypeAheadAt = now;

        var term = _typeAheadBuffer;
        if (term.Length > 1 && term.Distinct().Count() == 1)
        {
            term = term[..1];
        }

        for (var i = 1; i <= visibleItems.Count; i++)
        {
            var candidate = visibleItems[(index + i + visibleItems.Count) % visibleItems.Count];
            if (GetText(candidate)?.StartsWith(term, StringComparison.OrdinalIgnoreCase) is true)
            {
                await FocusItemElement(candidate);
                return;
            }
        }
    }

    private async Task FocusItemAt(List<TItem> visibleItems, int index)
    {
        if (visibleItems.Count == 0) return;

        // The navigation stops at both ends of the nav instead of wrapping around, so a long list keeps
        // a stable notion of a first and a last item.
        index = Math.Clamp(index, 0, visibleItems.Count - 1);

        await FocusItemElement(visibleItems[index]);
    }

    private async ValueTask FocusItemElement(TItem item)
    {
        _focusedItem = item;

        if (_itemElements.TryGetValue(item, out var element) is false) return;

        try
        {
            await element.FocusAsync();
        }
        catch (JSDisconnectedException) { } // we can ignore this exception here
        catch (InvalidOperationException) { } // the element is no longer in the DOM
    }

    // The items the keyboard can reach: the rendered ones, in the order they appear, which means the
    // children of a collapsed item are skipped and a separator is never a stop.
    private List<TItem> GetVisibleItems()
    {
        List<TItem> result = [];

        Collect(_items);

        return result;

        void Collect(IList<TItem> items)
        {
            foreach (var item in items)
            {
                if (GetIsSeparator(item)) continue;

                result.Add(item);

                var childItems = GetChildItems(item);
                if (childItems.Count > 0 && GetItemExpanded(item))
                {
                    Collect(childItems);
                }
            }
        }
    }

    private TItem? FindParentOf(IList<TItem> items, TItem item, TItem? parent = null)
    {
        foreach (var candidate in items)
        {
            if (AreEqual(candidate, item)) return parent;

            var found = FindParentOf(GetChildItems(candidate), item, candidate);
            if (found is not null) return found;
        }

        return null;
    }

    private List<TItem> GetSiblingsOf(TItem item)
    {
        var parent = FindParentOf(_items, item);

        return parent is null ? _items : GetChildItems(parent);
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
