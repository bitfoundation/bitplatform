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

    internal List<TItem> _items = [];
    private bool _selectionDirty;
    private bool _expandSelectedPending;
    private TItem? _focusedItem;
    private TItem? _pendingFocusItem;
    private string _typeAheadBuffer = string.Empty;
    private DateTime _lastTypeAheadAt = DateTime.MinValue;
    internal Dictionary<TItem, bool> _itemExpandStates = [];
    private readonly HashSet<TItem> _initializedItems = [];
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

        if (SingleExpand && isExpanded)
        {
            // The single-expand mode keeps a single open path, so the whole tree is closed and only the
            // path down to this item is opened again. Reading the state of the tree rather than
            // remembering the last toggled item keeps it correct no matter how the branch that is
            // currently open got opened (a click, the selection, the initial IsExpanded of an item, ...).
            foreach (var root in _items.ToList())
            {
                ToggleItemAndChildren(root, false);
            }

            ToggleItemAndParents(_items, item, true);
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
    /// Expands an item, and does nothing when it is already expanded.
    /// </summary>
    public Task ExpandItem(TItem item) => GetItemExpanded(item) ? Task.CompletedTask : ToggleItem(item);

    /// <summary>
    /// Collapses an item, and does nothing when it is already collapsed.
    /// </summary>
    public Task CollapseItem(TItem item) => GetItemExpanded(item) ? ToggleItem(item) : Task.CompletedTask;

    /// <summary>
    /// Whether an item is currently expanded.
    /// </summary>
    public bool IsItemExpanded(TItem item) => GetItemExpanded(item);

    /// <summary>
    /// Selects an item programmatically, exactly like a click on that item would in the manual mode.
    /// </summary>
    public Task SelectItem(TItem? item) => SetSelectedItem(item);

    /// <summary>
    /// Moves the focus to an item of the nav, opening the branches it is nested in when it is not
    /// rendered yet.
    /// </summary>
    public ValueTask FocusItem(TItem item)
    {
        if (_itemElements.ContainsKey(item)) return FocusItemElement(item);

        // The item is inside a collapsed branch, so there is no element to focus yet: the path down to
        // it is opened and the focus is moved once the render that brings it on screen is done.
        _focusedItem = item;
        _pendingFocusItem = item;

        ToggleItemAndParents(_items, item, true);

        RefreshOptions();
        StateHasChanged();

        return ValueTask.CompletedTask;
    }



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

    // The element is handed over as well, so a caller only drops its own registration: an item that is
    // rendered by another child by now (a reordered collection re-keys the children onto other items)
    // keeps the registration that child has just made.
    internal void UnregisterItemElement(TItem item, ElementReference element)
    {
        if (IsDisposed) return;

        if (_itemElements.TryGetValue(item, out var registered) && registered.Id != element.Id) return;

        _itemElements.Remove(item);
    }

    internal void SetFocusedItem(TItem item)
    {
        _focusedItem = item;
    }

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
        SyncItems();

        // The subscription is not tied to the mode: the mode is a parameter that can flip after the
        // component is initialized, and a nav that switched to the automatic mode later on still has to
        // follow the URL. The handler itself is the one that checks the mode.
        _navigationManager.LocationChanged += OnLocationChanged;

        if (Mode == BitNavMode.Automatic)
        {
            SetSelectedItemByCurrentUrl();
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
        // The Items collection is re-read here rather than only when the parameter is assigned a new
        // instance, so a collection that is mutated in place (an item appended to the same list) is
        // picked up as well.
        SyncItems();

        ExpandSelectedPath();

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

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        // A focus request for an item of a collapsed branch had to wait for the render that opens the
        // branch, since only then is there an element to move the focus to.
        if (_pendingFocusItem is not null)
        {
            var item = _pendingFocusItem;
            _pendingFocusItem = null;

            await FocusItemElement(item);
        }

        await base.OnAfterRenderAsync(firstRender);
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
        if (IsDisposed) return;
        if (Mode is not BitNavMode.Automatic) return;

        // Both sides of the comparison are reduced to the same shape first: an app-relative URL that
        // starts with a slash. The path is kept apart from the query and the fragment, so an item URL
        // that carries none of them still matches a page that was reached with one.
        var currentUrl = ToRelativeUrl(_navigationManager.Uri);
        var separatorIndex = currentUrl.IndexOfAny(_UrlSeparators);
        var currentPath = separatorIndex < 0 ? currentUrl : currentUrl[..separatorIndex];

        var currentItem = Flatten(_items).FirstOrDefault(item =>
        {
            var match = GetMatch(item) ?? Match ?? BitNavMatch.Exact;

            if (IsMatch(GetUrl(item), match)) return true;

            return GetAdditionalUrls(item)?.Any(u => IsMatch(u, match)) is true;
        });

        _ = SetSelectedItem(currentItem);

        bool IsMatch(string? itemUrl, BitNavMatch match)
        {
            if (itemUrl.HasNoValue()) return false;

            // A pattern is matched as it was written: normalizing it (adding a leading slash, trimming a
            // trailing one) would corrupt the anchors and the quantifiers it is made of.
            if (match is BitNavMatch.Regex)
            {
                return IsRegexMatch(currentUrl, itemUrl!) ||
                       (currentPath != currentUrl && IsRegexMatch(currentPath, itemUrl!));
            }

            if (match is BitNavMatch.Wildcard)
            {
                var pattern = $"^{WildcardToRegex(itemUrl!)}$";
                return IsRegexMatch(currentUrl, pattern) ||
                       (currentPath != currentUrl && IsRegexMatch(currentPath, pattern));
            }

            var url = ToRelativeUrl(itemUrl!);
            var target = url.IndexOfAny(_UrlSeparators) < 0 ? currentPath : currentUrl;

            if (UrlEquals(target, url)) return true;

            return match is BitNavMatch.Prefix && IsStrictlyPrefixWithSeparator(target, url);
        }
    }



    private static readonly char[] _UrlSeparators = ['?', '#'];

    private const string DOUBLE_STAR_PLACEHOLDER = "___BIT_NAV_DOUBLESTAR_PLACEHOLDER___";

    private static string WildcardToRegex(string pattern)
    {
        pattern = Regex.Escape(pattern);

        pattern = pattern.Replace(@"\*\*", DOUBLE_STAR_PLACEHOLDER);
        pattern = pattern.Replace(@"\*", "[^/]*");
        pattern = pattern.Replace(@"\?", "[^/]");
        pattern = pattern.Replace(DOUBLE_STAR_PLACEHOLDER, ".*");

        return pattern;
    }

    // Turns anything an item may carry as its URL into the app-relative form the current URL is reduced
    // to: an absolute URL under the base of the app loses that base, and a relative one gains the leading
    // slash it is missing, so "products", "/products" and "https://host/app/products" all compare equal.
    private string ToRelativeUrl(string url)
    {
        url = url.Trim();

        var baseUri = _navigationManager.BaseUri;
        if (url.StartsWith(baseUri, StringComparison.OrdinalIgnoreCase))
        {
            // The base URI always ends with a slash, which is kept as the leading slash of the result.
            return url[(baseUri.Length - 1)..];
        }

        if (url.StartsWith("./", StringComparison.Ordinal)) return url[1..];

        return url.StartsWith('/') ? url : $"/{url}";
    }

    // URLs are compared the way the browser treats them: the case of the path does not distinguish two
    // pages, and a trailing slash does not either. This mirrors what Blazor's own NavLink does.
    private static bool UrlEquals(string current, string url)
    {
        if (string.Equals(current, url, StringComparison.OrdinalIgnoreCase)) return true;

        // "/products" is the same page as "/products/", whichever of the two carries the slash.
        if (current.Length == url.Length - 1 && url[^1] == '/')
        {
            return url.StartsWith(current, StringComparison.OrdinalIgnoreCase);
        }

        if (url.Length == current.Length - 1 && current[^1] == '/')
        {
            return current.StartsWith(url, StringComparison.OrdinalIgnoreCase);
        }

        return false;
    }

    // A prefix only matches on a path boundary, so "/product" does not light up on "/products" the way a
    // plain StartsWith would. The separators are the ones that end a path segment: "/", "?" and "#".
    private static bool IsStrictlyPrefixWithSeparator(string current, string prefix)
    {
        var prefixLength = prefix.Length;

        if (current.Length <= prefixLength) return false;

        if (current.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) is false) return false;

        return prefixLength == 0
            || prefix[prefixLength - 1] == '/'
            || current[prefixLength] == '/'
            || current[prefixLength] == '?'
            || current[prefixLength] == '#';
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
        if (IsDisposed) return;
        if (Mode is not BitNavMode.Automatic) return;

        SetSelectedItemByCurrentUrl();

        StateHasChanged();
    }

    // Reads the Items collection into the list the nav renders from. The content of the collection is
    // compared rather than only its identity, so a list that is mutated in place is picked up too.
    private void SyncItems()
    {
        if ((Options ?? ChildContent) is not null) return;

        var items = Items ?? [];

        if (_items.Count == items.Count && _items.SequenceEqual(items)) return;

        _items = [.. items];

        // The expansion state of the items that are gone is dropped, so a nav whose items are swapped
        // repeatedly (a filtered list, a reloaded menu, ...) does not keep growing.
        var live = Flatten(_items).ToHashSet();
        _initializedItems.RemoveWhere(item => live.Contains(item) is false);
        foreach (var item in _itemExpandStates.Keys.Where(item => live.Contains(item) is false).ToArray())
        {
            _itemExpandStates.Remove(item);
        }

        InitializeExpandStates();

        // The match is deferred to the end of the render instead of running here, because the parameters of
        // a single SetParametersAsync are assigned one by one: matching now would read a Mode (or a Match)
        // that the same parameter set is still about to change.
        MarkSelectionDirty();
    }

    // Applies the initial expansion state to the items the nav has not seen yet, so items that arrive
    // after the first render (loaded from a service, for instance) still honor AllExpanded and their own
    // IsExpanded, while the items already on screen keep whatever the user has expanded or collapsed.
    private void InitializeExpandStates()
    {
        foreach (var item in Flatten(_items))
        {
            if (_initializedItems.Add(item) is false) continue;

            SetItemExpanded(item, AllExpanded || (GetIsExpanded(item) ?? false));
        }
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
        _expandSelectedPending = true;

        ExpandSelectedPath();
    }

    // Opens the path down to the selected item, otherwise a selection deeper in the tree would not be
    // visible at all. The selection can be assigned before the items are (the parameters of a single
    // SetParametersAsync are assigned one by one, and a collection is often loaded later still), so the
    // request is kept pending until there is a tree to walk.
    private void ExpandSelectedPath()
    {
        if (_expandSelectedPending is false) return;

        if (SelectedItem is null)
        {
            _expandSelectedPending = false;
            return;
        }

        if (_items.Count == 0) return;

        _expandSelectedPending = false;

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

    // Both the mode and the matching behavior can change after the nav is rendered, and either one
    // changes which item the current URL points at, so the match is re-run once the change is in.
    private void OnUrlMatchingChanged()
    {
        if (Mode is not BitNavMode.Automatic) return;

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
    // The arrow, Home and End keys scroll the page by default. That default is cancelled by the
    // capture-phase guard installed in BitNav.ts, which (unlike @onkeydown:preventDefault, evaluated at
    // render time) can decide on the key actually pressed instead of lagging a keystroke behind and
    // swallowing the Tab that follows an arrow key.
    internal async Task HandleOnKeyDown(TItem source, KeyboardEventArgs e)
    {
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
            // level at once. It is inert while the nav hides its expanders, and it leaves the disabled
            // siblings alone, exactly like the chevron and the arrow keys do.
            case "*":
                if (current is null || NoCollapse || SingleExpand) return;
                foreach (var sibling in GetSiblingsOf(current).ToList())
                {
                    if (GetIsEnabled(sibling) && GetChildItems(sibling).Any() && GetItemExpanded(sibling) is false)
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
    // children of a collapsed item are skipped and a separator is never a stop. A disabled item is no stop
    // either: an item without a URL renders as a native disabled button, which takes no focus at all, so
    // walking onto one would leave the focus where it was while the nav believes it has moved.
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

                if (GetIsEnabled(item))
                {
                    result.Add(item);
                }

                // A disabled item is still walked through: it cannot be toggled itself, but an enabled
                // child of a branch that is already open is reachable on its own.
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

        _navigationManager.LocationChanged -= OnLocationChanged;

        await base.DisposeAsync(disposing);
    }
}
