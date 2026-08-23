using Microsoft.AspNetCore.Components.Routing;

namespace Bit.BlazorUI;

/// <summary>
/// A bar of navigation links to the main areas of an app, the way a mobile app puts its top-level
/// destinations along the bottom of the screen or a rail puts them down its side.
/// </summary>
public partial class BitNavBar<TItem> : BitComponentBase where TItem : class
{
    internal List<TItem> _items = [];

    private string? _containerId;
    private bool _selectionDirty;
    private bool _scrollToSelectedItem;
    private TItem? _focusedItem;
    private IList<TItem>? _oldItems;
    private bool _optionsOrderDirty;
    private readonly Dictionary<TItem, ElementReference> _itemElements = [];



    [Inject] private IJSRuntime _js { get; set; } = default!;

    [Inject] private NavigationManager _navigationManager { get; set; } = default!;



    /// <summary>
    /// Selects an item programmatically, exactly like a click on that item would in the manual mode.
    /// </summary>
    public Task SelectItem(TItem? item) => SetSelectedItem(item);

    /// <summary>
    /// Moves the focus to an item of the navbar.
    /// </summary>
    public ValueTask FocusItem(TItem item) => FocusItemElement(item);

    /// <summary>
    /// Brings an item into the visible area of a <see cref="Scrollable"/> navbar, without selecting it or
    /// moving the focus onto it. It is a no-op on a navbar that does not scroll.
    /// </summary>
    public ValueTask ScrollItemIntoView(TItem item) => ScrollItemElementIntoView(item);



    internal void RegisterOption(BitNavBarOption option)
    {
        _items.Add((option as TItem)!);
        _selectionDirty = true;
        _optionsOrderDirty = true;
        StateHasChanged();
    }

    internal void UnregisterOption(BitNavBarOption option)
    {
        if (IsDisposed) return;

        var item = (option as TItem)!;

        _items.Remove(item);
        _itemElements.Remove(item);
        _selectionDirty = true;
        _optionsOrderDirty = true;

        // The options render their own items and a re-render of the navbar does not reach them, so the ones
        // that are left are pushed a render of their own: what the removed one used to hold (the roving tab
        // stop, for instance) has to move onto one of them.
        RefreshOptions();
        StateHasChanged();
    }

    // Flags that the Automatic-mode selection needs to be recomputed. Called by the options as they
    // register (and as their URL changes) instead of matching immediately, so a batch of registrations
    // collapses into a single match pass in OnAfterRender rather than one O(n) pass per option.
    internal void MarkSelectionDirty()
    {
        _selectionDirty = true;
    }

    // Reorders the registered options by the DOM order of the markers they rendered, since an option that
    // is rendered conditionally (or moved) after the first render registers itself at the end of the list
    // no matter where in the markup it sits. The order of that list is what the keyboard moves along and
    // what the single tab stop falls back to, so a bar whose options change would otherwise be walked in
    // an order other than the one it is read in. Opt-in via AutoReorderOptions.
    internal void ReorderOptions(string[] orderedOptionIds)
    {
        if (orderedOptionIds.Length == 0) return;

        List<TItem> ordered = new(_items.Count);

        foreach (var optionId in orderedOptionIds)
        {
            var item = _items.FirstOrDefault(i => (i as BitNavBarOption)?._OptionId == optionId);
            if (item is null || ordered.Contains(item)) continue;

            ordered.Add(item);
        }

        if (ordered.Count == 0) return;

        // An option that has registered but has not rendered its marker yet keeps its place at the end
        // rather than being dropped from the navbar altogether.
        ordered.AddRange(_items.Except(ordered));

        if (ordered.SequenceEqual(_items)) return;

        _items = ordered;

        // The automatic mode selects the first item that matches the current URL, so the item that wins a
        // tie between two matching options is the one that comes first in the new order.
        MarkSelectionDirty();

        RefreshOptions();
        StateHasChanged();
    }

    // Emits the marker attribute the DOM read-back recovers the markup order of the options from. Only
    // rendered while AutoReorderOptions is enabled (to keep the attribute off every other navbar) and only
    // for options: the Items collection is already in the order it is rendered in.
    internal Dictionary<string, object>? GetItemMarkerAttributes(TItem item)
    {
        if (AutoReorderOptions is false) return null;
        if (item is not BitNavBarOption option) return null;

        return new() { [BitNavBarOption._OPTION_ID_ATTRIBUTE] = option._OptionId };
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
        if (AreEqual(_focusedItem, item)) return;

        _focusedItem = item;

        // The single stop of the roving tab index follows the focus, so Tab comes back to the item the
        // reader left the bar on. Nothing else is driven by the focused item, so the re-render is only
        // worth it in that mode.
        if (SingleTabStop is false) return;

        RefreshOptions();
        StateHasChanged();
    }

    // The focus reaching an item is what moves the roving tab stop onto it, and - while the navbar was asked
    // to have its selection follow the focus - what selects it as well, the way the tabs of a tab list do.
    internal async Task HandleOnFocusIn(TItem item)
    {
        SetFocusedItem(item);

        if (SelectOnFocus is false) return;
        // Only the manual mode owns its selection: in the automatic mode the current URL is what selects an
        // item, and a selection made here would be undone by the very next match anyway.
        if (Mode is not BitNavMode.Manual) return;
        if (IsEnabled is false) return;
        if (GetIsEnabled(item) is false) return;

        await SetSelectedItem(item);
    }

    /// <summary>
    /// Whether an item is the selected one. The comparison goes through the default equality comparer of
    /// the item type, so a record or any other value-equal item type highlights the selection correctly.
    /// </summary>
    internal bool IsSelected(TItem? item) => AreEqual(item, SelectedItem);



    protected override string RootElementClass => "bit-nbr";

    protected override void RegisterCssClasses()
    {
        ClassBuilder.Register(() => Classes?.Root);

        ClassBuilder.Register(() => FitWidth ? "bit-nbr-ftw" : string.Empty);
        ClassBuilder.Register(() => FullWidth ? "bit-nbr-flw" : string.Empty);

        ClassBuilder.Register(() => IconOnly ? "bit-nbr-ion" : string.Empty);
        ClassBuilder.Register(() => (IconOnly is false && HideUnselectedText) ? "bit-nbr-hut" : string.Empty);
        ClassBuilder.Register(() => InlineText ? "bit-nbr-inl" : string.Empty);
        ClassBuilder.Register(() => Justified ? "bit-nbr-jst" : string.Empty);
        ClassBuilder.Register(() => Vertical ? "bit-nbr-vrt" : string.Empty);
        ClassBuilder.Register(() => SafeArea ? "bit-nbr-sfa" : string.Empty);

        ClassBuilder.Register(() => Scrollable ? "bit-nbr-scr" : string.Empty);

        // The indicator is what marks the selected item beyond its color: a line along the edge of the item,
        // or the pill a Material navigation bar draws behind the icon of its current destination.
        ClassBuilder.Register(() => Indicator switch
        {
            BitNavBarIndicator.Line => "bit-nbr-lin",
            BitNavBarIndicator.Pill => "bit-nbr-pil",
            _ => string.Empty
        });

        // Baseline and Stretch describe how an item sits across the bar rather than how the items are
        // distributed along it, so neither one carries a distribution of its own here.
        ClassBuilder.Register(() => Alignment switch
        {
            BitAlignment.Start => "bit-nbr-str",
            BitAlignment.End => "bit-nbr-end",
            BitAlignment.Center => "bit-nbr-ctr",
            BitAlignment.SpaceBetween => "bit-nbr-sbt",
            BitAlignment.SpaceAround => "bit-nbr-sar",
            BitAlignment.SpaceEvenly => "bit-nbr-sev",
            _ => string.Empty
        });

        ClassBuilder.Register(() => Size switch
        {
            BitSize.Small => "bit-nbr-sm",
            BitSize.Medium => "bit-nbr-md",
            BitSize.Large => "bit-nbr-lg",
            _ => "bit-nbr-md"
        });

        ClassBuilder.Register(() => Color switch
        {
            BitColor.Primary => "bit-nbr-pri",
            BitColor.Secondary => "bit-nbr-sec",
            BitColor.Tertiary => "bit-nbr-ter",
            BitColor.Info => "bit-nbr-inf",
            BitColor.Success => "bit-nbr-suc",
            BitColor.Warning => "bit-nbr-wrn",
            BitColor.SevereWarning => "bit-nbr-swr",
            BitColor.Error => "bit-nbr-err",
            BitColor.PrimaryBackground => "bit-nbr-pbg",
            BitColor.SecondaryBackground => "bit-nbr-sbg",
            BitColor.TertiaryBackground => "bit-nbr-tbg",
            BitColor.PrimaryForeground => "bit-nbr-pfg",
            BitColor.SecondaryForeground => "bit-nbr-sfg",
            BitColor.TertiaryForeground => "bit-nbr-tfg",
            BitColor.PrimaryBorder => "bit-nbr-pbr",
            BitColor.SecondaryBorder => "bit-nbr-sbr",
            BitColor.TertiaryBorder => "bit-nbr-tbr",
            _ => "bit-nbr-pri",
        });

        ClassBuilder.Register(() => Filled ? "bit-nbr-fil" : string.Empty);
    }

    protected override void RegisterCssStyles()
    {
        StyleBuilder.Register(() => Styles?.Root);
    }

    protected override async Task OnInitializedAsync()
    {
        _containerId = $"BitNavBar-{UniqueId}-container";

        SyncItems();

        // The subscription is not tied to the mode: the mode is a parameter that can flip after the
        // component is initialized, and a navbar that switched to the automatic mode later on still has to
        // follow the URL. The handler itself is the one that checks the mode.
        _navigationManager.LocationChanged += OnLocationChanged;

        if (Mode == BitNavMode.Automatic)
        {
            await SetSelectedItemByCurrentUrl();
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

        // Options render their items themselves and Blazor skips re-rendering them when only the navbar's
        // own parameters (Styles, IconOnly, ItemTemplate, ...) change, so push a re-render to each one.
        RefreshOptions();

        // A pure reorder of the options registers and unregisters nothing, so the read-back is flagged to
        // run after this render to detect it. It only mutates the list when the order actually changed, so
        // a set of options that stayed put costs a single DOM read.
        if (AutoReorderOptions && (Options ?? ChildContent) is not null)
        {
            _optionsOrderDirty = true;
        }

        base.OnParametersSet();
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        // The order is recovered before the match runs, so the item that wins a tie between two options
        // pointing at the same URL is the one that comes first in the markup.
        await ReorderOptionsByDomOrder();

        // Each option flags a selection recompute as it registers instead of matching immediately, so
        // registering n options collapses into a single match pass here rather than one O(n) pass each.
        if (_selectionDirty)
        {
            _selectionDirty = false;

            // A selection that actually moves pushes the render to the options it moved between itself, so
            // a pass that changes nothing leaves the options (and the element references they hand over)
            // exactly as they are.
            await InvokeAsync(SetSelectedItemByCurrentUrl);
        }

        // A navbar that scrolls has to bring its selected item into view as the selection moves, since the
        // selection moves without a pointer or a keyboard of its own behind it (the URL changed, the binding
        // was written to), and the item it lands on can be well outside the scrolled area.
        if (firstRender)
        {
            _scrollToSelectedItem = true;
        }

        await ScrollSelectedItemIntoView();

        await base.OnAfterRenderAsync(firstRender);
    }



    internal async Task HandleOnClick(TItem item)
    {
        if (IsEnabled is false) return;
        if (GetIsEnabled(item) is false) return;

        // The selection is read before the click is handled: the manual mode selects the clicked item right
        // here, and asking afterwards would report every freshly clicked item as the already-selected one
        // and swallow the click callback.
        var wasSelected = IsSelected(item);

        if (Mode == BitNavMode.Manual)
        {
            await SetSelectedItem(item);
        }

        if (wasSelected is false || Reselectable)
        {
            if (GetUrl(item).HasValue())
            {
                await Task.Yield(); // wait for the link to navigate first
            }

            await OnItemClick.InvokeAsync(item);
        }
    }

    // The keyboard navigation is wired to the item's own element rather than to the root of the navbar, so
    // a focusable element rendered by a template (an input, a checkbox, ...) keeps its own key handling
    // instead of being read as a move along the bar.
    // The arrow keys, Home and End scroll the page by default. That default is cancelled by the
    // capture-phase guard installed in BitNavBar.ts, which (unlike @onkeydown:preventDefault, evaluated at
    // render time) can decide on the key actually pressed instead of lagging a keystroke behind and
    // swallowing the Tab that follows an arrow key.
    internal async Task HandleOnKeyDown(TItem source, KeyboardEventArgs e)
    {
        if (IsEnabled is false) return;
        if (e.CtrlKey || e.AltKey || e.MetaKey) return;

        // The focus event of the item that received the key has already run, so the focused item is known;
        // the item the event came from is only the fallback for a navbar that has never seen a focus event.
        _focusedItem ??= source;

        var items = GetFocusableItems();
        if (items.Count == 0) return;

        var index = _focusedItem is null ? -1 : items.FindIndex(i => AreEqual(i, _focusedItem));
        var isRtl = (Dir ?? CascadingDir) == BitDir.Rtl;

        // Both axes move along the bar, whichever way it is laid out: the pair that matches the orientation
        // is the expected one, and the other pair is what a reader of a bar or a rail reaches for anyway.
        switch (e.Key)
        {
            case "ArrowRight":
                await FocusItemAt(items, index + (isRtl ? -1 : 1));
                return;

            case "ArrowLeft":
                await FocusItemAt(items, index + (isRtl ? 1 : -1));
                return;

            case "ArrowDown":
                await FocusItemAt(items, index + 1);
                return;

            case "ArrowUp":
                await FocusItemAt(items, index - 1);
                return;

            case "Home":
                await FocusItemAt(items, 0);
                return;

            case "End":
                await FocusItemAt(items, items.Count - 1);
                return;
        }
    }

    // The tab index of an item. By default the navbar is a set of links that Tab reaches one by one, which
    // is how a navigation landmark behaves; SingleTabStop turns it into the roving tab index of a toolbar,
    // where the whole bar is a single stop and the arrow keys move inside it.
    internal string? GetItemTabIndex(TItem item, bool isEnabled)
    {
        if (isEnabled is false) return "-1";

        if (SingleTabStop is false) return null;

        return IsTabStop(item) ? "0" : "-1";
    }

    // The template that replaces an item, if the item is rendered by one: the template of the item in the
    // Replace mode, or the navbar's own template while that one is in the Replace mode. Everything the
    // navbar puts around an item - the anchor or the button, the click, the focus, the accessible name -
    // is then the template's own business, so a replaced item is also left out of the keyboard navigation.
    internal RenderFragment<TItem>? GetReplacedTemplate(TItem item)
    {
        var template = GetTemplate(item);

        // The template of an item wins over the navbar's own, so the mode that goes with it wins as well.
        if (template is not null)
        {
            return GetTemplateRenderMode(item) is BitNavItemTemplateRenderMode.Replace ? template : null;
        }

        return (ItemTemplate is not null && ItemTemplateRenderMode is BitNavItemTemplateRenderMode.Replace)
                ? ItemTemplate
                : null;
    }

    internal string GetItemCssStyle(TItem item)
    {
        // The fragments are declarations, so they are joined with a semicolon: a space would run two of
        // them together into a single malformed declaration.
        return string.Join(';', new[] { Styles?.Item, GetStyle(item), IsSelected(item) ? Styles?.SelectedItem : null }
                                    .Where(s => s.HasValue()));
    }

    internal string GetItemCssClass(TItem item, bool isEnabled)
    {
        return string.Join(' ', new[]
        {
            "bit-nbr-itm",
            Classes?.Item,
            GetClass(item),
            IsSelected(item) ? "bit-nbr-sel" : null,
            IsSelected(item) ? Classes?.SelectedItem : null,
            isEnabled ? null : "bit-nbr-dis"
        }.Where(c => c.HasValue()));
    }



    private static bool AreEqual(TItem? first, TItem? second) => EqualityComparer<TItem?>.Default.Equals(first, second);

    private async Task ReorderOptionsByDomOrder()
    {
        if (AutoReorderOptions is false) return;
        if ((Options ?? ChildContent) is null) return;
        if (_optionsOrderDirty is false) return;

        _optionsOrderDirty = false;

        try
        {
            var orderedOptionIds = await _js.BitUtilsGetChildrenAttributes(_containerId!, BitNavBarOption._OPTION_ID_ATTRIBUTE);

            if (IsDisposed) return;

            if (orderedOptionIds is not null)
            {
                ReorderOptions(orderedOptionIds);
            }
        }
        catch (JSDisconnectedException) { } // the circuit is gone (the reader navigated away), nothing to reorder
        catch (JSException) { } // a failure on the JS side is not fatal here, the current order is kept
    }

    // Brings the element of an item into the scrolled area of the navbar, which is what the public
    // ScrollItemIntoView is: the item is handed over as the element it rendered there, since that caller
    // can ask for any item rather than only for the selected one. Only the container is scrolled (rather
    // than every scrollable ancestor the way Element.scrollIntoView does), so bringing an item into view
    // never drags the page the navbar sits on along with it.
    private async ValueTask ScrollItemElementIntoView(TItem item)
    {
        if (Scrollable is false) return;
        if (_itemElements.TryGetValue(item, out var element) is false) return;

        try
        {
            await _js.BitNavBarScrollItemIntoView(_containerId!, element);
        }
        catch (JSDisconnectedException) { } // the circuit is gone (the reader navigated away), nothing to scroll
        catch (JSException) { } // a failure on the JS side leaves the scroll position as it is, which is not fatal
    }

    // Brings the item the selection has just landed on into the scrolled area of the navbar.
    private async Task ScrollSelectedItemIntoView()
    {
        if (_scrollToSelectedItem is false) return;

        _scrollToSelectedItem = false;

        if (Scrollable is false) return;
        if (SelectedItem is null) return;

        try
        {
            // Which element is the selected one is read off the DOM rather than out of the elements the
            // items have handed over, since a selection made on the very first render lands before they
            // have handed anything over.
            await _js.BitNavBarScrollSelectedItemIntoView(_containerId!);
        }
        catch (JSDisconnectedException) { } // the circuit is gone (the reader navigated away), nothing to scroll
        catch (JSException) { } // a failure on the JS side leaves the scroll position as it is, which is not fatal
    }

    // The items the keyboard moves between: the enabled ones the navbar renders itself, in the order they
    // are rendered. A disabled item renders as a native disabled button (or as a link without an href),
    // which takes no focus at all, and an item whose template replaced it renders no element of the
    // navbar's at all, so walking onto either would leave the focus where it was while the navbar believes
    // it has moved.
    private List<TItem> GetFocusableItems() => [.. _items.Where(i => GetIsEnabled(i) && GetReplacedTemplate(i) is null)];

    // The single stop of the roving tab index is the item the focus was last on, and the selected one
    // before the bar has ever been focused, so Tab returns to where the reader left it either way. A navbar
    // with neither (nothing matches the URL yet, for instance) still has to be reachable itself, so its
    // first focusable item holds the stop instead.
    private bool IsTabStop(TItem item)
    {
        // The focused item is only followed while it is still one of the items the navbar renders: an item
        // that is gone (an option removed conditionally, for instance) would otherwise hold a stop no
        // element carries, and take the whole bar out of the tab sequence with it.
        var focusable = GetFocusableItems();

        var current = _focusedItem is not null && focusable.Any(i => AreEqual(i, _focusedItem))
                        ? _focusedItem
                        : focusable.FirstOrDefault(IsSelected);

        return AreEqual(current ?? focusable.FirstOrDefault(), item);
    }

    private async Task FocusItemAt(List<TItem> items, int index)
    {
        if (items.Count == 0) return;

        // The navigation stops at both ends of the navbar instead of wrapping around, so a bar keeps a
        // stable notion of a first and a last item; WrapNavigation opts into the wrap of the toolbar
        // pattern instead. Home and End land inside the range either way, so only the arrow keys reach
        // past an end and only they are wrapped.
        if (WrapNavigation)
        {
            index = ((index % items.Count) + items.Count) % items.Count;
        }
        else
        {
            index = Math.Clamp(index, 0, items.Count - 1);
        }

        await FocusItemElement(items[index]);
    }

    private async ValueTask FocusItemElement(TItem item)
    {
        SetFocusedItem(item);

        if (_itemElements.TryGetValue(item, out var element) is false) return;

        try
        {
            await element.FocusAsync();
        }
        catch (JSDisconnectedException) { } // we can ignore this exception here
        catch (JSException) { } // the focus call itself failed, which is nothing to tear the navbar down for
        catch (InvalidOperationException) { } // the element is no longer in the DOM
    }

    private void OnSetSelectedItem()
    {
        // The selection moved, so a scrolling navbar brings the item it landed on into view after the render.
        _scrollToSelectedItem = true;

        RefreshOptions();
    }

    private void RefreshOptions()
    {
        // In the Items API there are no registered options, so there is nothing to refresh.
        if ((Options ?? ChildContent) is null) return;

        foreach (var item in _items)
        {
            (item as BitNavBarOption)?.InternalStateHasChanged();
        }
    }

    private void OnLocationChanged(object? sender, LocationChangedEventArgs args)
    {
        if (IsDisposed) return;
        if (Mode is not BitNavMode.Automatic) return;

        // The event is raised outside the renderer's synchronization context, so the match (and the
        // binding and the callback it invokes) is dispatched onto it rather than run right here.
        _ = InvokeAsync(async () =>
        {
            await SetSelectedItemByCurrentUrl();

            // The dispatch and the match itself both give the component a chance to be disposed in the
            // meantime (the navigation that raised the event is what takes it off the page, after all),
            // and a render requested after that point throws instead of doing anything.
            if (IsDisposed) return;

            RefreshOptions();
            StateHasChanged();
        });
    }

    private async Task SetSelectedItemByCurrentUrl()
    {
        if (IsDisposed) return;
        if (Mode is not BitNavMode.Automatic) return;

        var (currentUrl, currentPath) = BitNavUrlMatcher.GetCurrentUrl(_navigationManager);
        var baseUri = _navigationManager.BaseUri;

        var currentItem = _items.FirstOrDefault(item =>
        {
            var match = GetMatch(item) ?? Match ?? BitNavMatch.Exact;

            if (IsMatch(GetUrl(item), match)) return true;

            return GetAdditionalUrls(item)?.Any(u => IsMatch(u, match)) is true;
        });

        await SetSelectedItem(currentItem);

        bool IsMatch(string? itemUrl, BitNavMatch match)
        {
            return BitNavUrlMatcher.IsMatch(itemUrl, match, currentUrl, currentPath, baseUri);
        }
    }

    // Reads the Items collection into the list the navbar renders from. The content of the collection is
    // compared rather than only its identity, so a list that is mutated in place is picked up too.
    private void SyncItems()
    {
        if ((Options ?? ChildContent) is not null) return;

        var items = Items ?? [];

        if (_oldItems is not null && _items.Count == items.Count && _items.SequenceEqual(items)) return;

        _items = [.. items];
        _oldItems = items;

        // The elements of the items that are gone are dropped, so a navbar whose items are swapped
        // repeatedly (a filtered list, a reloaded menu, ...) does not keep growing.
        foreach (var item in _itemElements.Keys.Where(i => _items.Contains(i) is false).ToArray())
        {
            _itemElements.Remove(item);
        }

        // The match is deferred to the end of the render instead of running here, because the parameters of
        // a single SetParametersAsync are assigned one by one: matching now would read a Mode (or a Match)
        // that the same parameter set is still about to change.
        MarkSelectionDirty();
    }

    // Both the mode and the matching behavior can change after the navbar is rendered, and either one
    // changes which item the current URL points at, so the match is re-run once the change is in.
    private void OnUrlMatchingChanged()
    {
        if (Mode is not BitNavMode.Automatic) return;

        MarkSelectionDirty();
    }

    private async Task SetSelectedItem(TItem? item)
    {
        if (IsSelected(item) && Reselectable is false) return;

        if (await AssignSelectedItem(item) is false) return;

        await OnSelectItem.InvokeAsync(item);

        RefreshOptions();
        StateHasChanged();
    }

    private string GetItemKey(TItem item, string defaultKey)
    {
        return GetKey(item) ?? $"{UniqueId}-{defaultKey}";
    }



    protected override async ValueTask DisposeAsync(bool disposing)
    {
        if (IsDisposed || disposing is false) return;

        _navigationManager.LocationChanged -= OnLocationChanged;

        await base.DisposeAsync(disposing);
    }
}
