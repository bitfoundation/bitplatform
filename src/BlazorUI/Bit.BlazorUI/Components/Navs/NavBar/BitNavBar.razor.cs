using System.Diagnostics.CodeAnalysis;
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
    private bool _wheelIsSetUp;
    private bool _defaultSelectedKeyPending;
    private bool _selectedKeyPending;
    private bool _selectedKeyStale;
    private TItem? _focusedItem;
    private IList<TItem>? _oldItems;
    private bool _optionsOrderDirty;
    private readonly Dictionary<TItem, ElementReference> _itemElements = [];



    [Inject] private IJSRuntime _js { get; set; } = default!;

    [Inject] private NavigationManager _navigationManager { get; set; } = default!;



    /// <summary>
    /// Gets or sets the cascading parameters for the navbar component.
    /// </summary>
    /// <remarks>
    /// This property receives its value from an ancestor component via Blazor's cascading parameter mechanism.
    /// <br />
    /// The intended use is to allow shared configuration or settings to be applied to multiple navbar components through the <see cref="BitParams"/> component.
    /// </remarks>
    [CascadingParameter(Name = BitNavBarParams.ParamName)]
    public BitNavBarParams? CascadingParameters { get; set; }



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



    internal async Task RegisterOption(BitNavBarOption option)
    {
        var item = (option as TItem)!;

        _items.Add(item);
        _selectionDirty = true;
        _optionsOrderDirty = true;
        StateHasChanged();

        // The options of the first render register during the very render a prerender (or a static SSR page)
        // sends as HTML, and neither of those ever gets to the after-render pass that resolves the selection
        // waiting for them, so the option that selection is waiting for is selected as it registers instead.
        // Every later option is left to that pass, which handles a whole batch of them at once.
        if (IsRendered) return;

        await SelectRegisteringOption(item);
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

        ClassBuilder.Register(() => FlipIndicator ? "bit-nbr-fli" : string.Empty);

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

    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(BitNavBarParams))]
    protected override async Task OnInitializedAsync()
    {
        // The cascade is applied here as well as in OnParametersSet, because this method already reads the
        // Mode it would otherwise only get after the first render.
        CascadingParameters?.UpdateParameters(this);

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
            if (DefaultSelectedItem is not null && IsSelectionBound() is false)
            {
                await AssignSelectedItem(DefaultSelectedItem);
            }
            else if (DefaultSelectedKey.HasValue() && IsSelectionBound() is false)
            {
                // The options register themselves only as they render, after this point, so the key is kept
                // pending and applied as soon as an item carrying it is there.
                _defaultSelectedKeyPending = true;

                await ApplyDefaultSelectedKey();
            }
        }

        await base.OnInitializedAsync();
    }

    protected override void OnParametersSet()
    {
        CascadingParameters?.UpdateParameters(this);

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

    protected override async Task OnParametersSetAsync()
    {
        // Run once every parameter of the set is in, since the Items and the NameSelectors a key is looked up
        // through may be assigned after the SelectedKey itself.
        await ApplySelectedKey();
        await SyncSelectedKey();

        await base.OnParametersSetAsync();
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        // The order is recovered before the match runs, so the item that wins a tie between two options
        // pointing at the same URL is the one that comes first in the markup.
        await ReorderOptionsByDomOrder();

        await ApplyDefaultSelectedKey();

        // Options register as they render, so a SelectedKey naming one of them is only found from here on.
        await ApplySelectedKey();

        // Each option flags a selection recompute as it registers instead of matching immediately, so
        // registering n options collapses into a single match pass here rather than one O(n) pass each.
        if (_selectionDirty)
        {
            _selectionDirty = false;

            // A selection that actually moves pushes the render to the options it moved between itself, so
            // a pass that changes nothing leaves the options (and the element references they hand over)
            // exactly as they are.
            await InvokeAsync(() => SetSelectedItemByCurrentUrl());
        }

        // A navbar that scrolls has to bring its selected item into view as the selection moves, since the
        // selection moves without a pointer or a keyboard of its own behind it (the URL changed, the binding
        // was written to), and the item it lands on can be well outside the scrolled area.
        if (firstRender)
        {
            _scrollToSelectedItem = true;
        }

        await SyncSelectedKey();

        await ScrollSelectedItemIntoView();

        await SetupWheel();

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
    // A disabled item carries none at all: it is a native disabled button or an anchor without an href, neither
    // of which is focusable, and a tabindex of -1 would make the anchor focusable by a click again.
    internal string? GetItemTabIndex(TItem item, bool isEnabled)
    {
        if (isEnabled is false) return null;

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

    // Selects the item the DefaultSelectedKey names, once: the first time an item with that key is among the
    // items. Only the options API ever gets here with the item still missing, since options register as they
    // render; a selection made in the meantime (a click) takes the place of the default for good.
    private async Task ApplyDefaultSelectedKey()
    {
        if (_defaultSelectedKeyPending is false) return;

        if (SelectedItem is not null || Mode is not BitNavMode.Manual)
        {
            _defaultSelectedKeyPending = false;
            return;
        }

        var item = _items.FirstOrDefault(i => GetKey(i) == DefaultSelectedKey);
        if (item is null) return;

        _defaultSelectedKeyPending = false;

        // A successful assignment refreshes the options itself (OnSetSelectedItem).
        if (await AssignSelectedItem(item) is false) return;

        StateHasChanged();
    }

    // A horizontal scrolling navbar hides its scrollbar, so the JS side turns a vertical mouse wheel into a
    // horizontal scroll of the list. The listener is installed once, the first time the navbar scrolls, and
    // checks the mode at the time of each event, so a navbar that stops scrolling needs no teardown.
    private async Task SetupWheel()
    {
        if (_wheelIsSetUp) return;
        if (Scrollable is false || Vertical) return;

        _wheelIsSetUp = true;

        try
        {
            await _js.BitNavBarSetupWheel(_containerId!);
        }
        catch (JSDisconnectedException) { } // the circuit is gone (the reader navigated away), nothing to set up
        catch (JSException) { } // without the wheel the bar still scrolls by touch, keyboard and selection
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

        // The key follows the item once the parameters are all in: a SelectedItem assigned by the parent is
        // assigned before the NameSelectors its key is read through, when both arrive in the same set.
        _selectedKeyStale = true;

        RefreshOptions();
    }

    // A key that the selection already carries is the navbar writing its own key back; any other one was
    // written from outside and is applied to the selection once the items (and the options) are there.
    private void OnSetSelectedKey()
    {
        _selectedKeyPending = SelectedKey != GetSelectedItemKey();
    }

    private string? GetSelectedItemKey() => SelectedItem is null ? null : GetKey(SelectedItem);

    private bool IsSelectionBound() => SelectedItemHasBeenSet || SelectedKeyHasBeenSet;

    // Selects the item the SelectedKey names. A key no item carries yet stays pending, since the options
    // register only as they render; a selection made in the meantime (a click) writes its own key over it.
    // The automatic mode owns its selection, so a key written there is answered with the one it selected.
    private async Task ApplySelectedKey()
    {
        if (_selectedKeyPending is false) return;

        if (Mode is not BitNavMode.Manual)
        {
            _selectedKeyPending = false;
            _selectedKeyStale = true;
            return;
        }

        TItem? item = null;

        if (SelectedKey is not null)
        {
            item = _items.FirstOrDefault(i => GetKey(i) == SelectedKey);
            if (item is null) return;
        }

        _selectedKeyPending = false;

        if (IsSelected(item)) return;

        if (await AssignSelectedItem(item) is false) return;

        StateHasChanged();
    }

    // Writes the key of the selected item back to SelectedKey, after the selection moved by any means.
    private async Task SyncSelectedKey()
    {
        if (_selectedKeyStale is false) return;

        _selectedKeyStale = false;

        var key = GetSelectedItemKey();

        if (key == SelectedKey) return;

        await AssignSelectedKey(key);
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
            await SetSelectedItemByCurrentUrl(isNavigation: true);

            // The dispatch and the match itself both give the component a chance to be disposed in the
            // meantime (the navigation that raised the event is what takes it off the page, after all),
            // and a render requested after that point throws instead of doing anything.
            if (IsDisposed) return;

            RefreshOptions();
            StateHasChanged();
        });
    }

    // Only a navigation can re-select the item that is already selected (a Reselectable navbar reports the
    // destination the reader went back to): every other match - the one of the first render, the ones after
    // the items or the matching rules changed - is the navbar re-checking where it already is, and reporting
    // that as a selection would fire OnSelectItem for nothing the reader did.
    private async Task SetSelectedItemByCurrentUrl(bool isNavigation = false)
    {
        if (IsDisposed) return;
        if (Mode is not BitNavMode.Automatic) return;

        var currentItem = _items.FirstOrDefault(GetCurrentUrlMatcher());

        if (isNavigation is false && IsSelected(currentItem)) return;

        await SetSelectedItem(currentItem);
    }

    // Whether an item points at the page the app currently sits on, through its own URL or one of its
    // additional ones. The current URL is read once per matcher rather than once per item it is asked about.
    private Func<TItem, bool> GetCurrentUrlMatcher()
    {
        var (currentUrl, currentPath) = BitNavUrlMatcher.GetCurrentUrl(_navigationManager);
        var baseUri = _navigationManager.BaseUri;

        return item =>
        {
            var match = GetMatch(item) ?? Match ?? BitNavMatch.Exact;

            if (IsMatch(GetUrl(item))) return true;

            return GetAdditionalUrls(item)?.Any(IsMatch) is true;

            bool IsMatch(string? itemUrl)
            {
                return BitNavUrlMatcher.IsMatch(itemUrl, match, currentUrl, currentPath, baseUri);
            }
        };
    }

    // Selects an option of the first render as it registers, when it is the one the selection is waiting for:
    // the one pointing at the current URL in the automatic mode, the one carrying the pending SelectedKey or
    // DefaultSelectedKey in the manual one. Only such an option runs the lookup, which keeps the first render
    // from scanning every option once per option. The options of the first render register in their markup
    // order, so the first of them to match is the one the lookup over the whole bar would have picked as well.
    private async Task SelectRegisteringOption(TItem item)
    {
        if (Mode is BitNavMode.Automatic)
        {
            if (GetCurrentUrlMatcher()(item) is false) return;

            await SetSelectedItemByCurrentUrl();
            return;
        }

        var key = GetKey(item);
        if (key is null) return;

        if (_selectedKeyPending && key == SelectedKey)
        {
            await ApplySelectedKey();
        }
        else if (_defaultSelectedKeyPending && key == DefaultSelectedKey)
        {
            await ApplyDefaultSelectedKey();
        }

        // The key follows the item once it is selected, so a bound SelectedKey learns the default as well.
        await SyncSelectedKey();
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
    internal void OnUrlMatchingChanged()
    {
        if (Mode is not BitNavMode.Automatic) return;

        MarkSelectionDirty();
    }

    private async Task SetSelectedItem(TItem? item)
    {
        if (IsSelected(item) && Reselectable is false) return;

        // A SelectedKey bound one way holds the selection the same way a one-way SelectedItem does: moving the
        // item without being able to write the key back would leave the two naming different items.
        if (SelectedKeyHasBeenSet && SelectedKeyChanged.HasDelegate is false) return;

        if (await AssignSelectedItem(item) is false) return;

        // The key is written back before the callback runs, so a handler reading a bound key sees the new one,
        // just as it sees the new item through SelectedItemChanged.
        await SyncSelectedKey();

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
