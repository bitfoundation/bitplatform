namespace Bit.BlazorUI;

/// <summary>
/// The Pivot control and related tabs pattern are used for navigating frequently accessed, distinct content categories. Pivots allow for navigation between two or more content views and rely on text headers to articulate the different sections of content.
/// </summary>
public partial class BitPivot : BitComponentBase
{
    private bool _jsSetup;
    private bool _jsSetupRunning;
    private bool _setupRtl;
    private bool _setupVertical;
    private bool _isMenuOpen;
    private bool _slideAtEnd;
    private bool _slideAtStart = true;
    private bool _slideHasOverflow;
    private bool _focusAfterRender;
    private bool _scrollAfterRender;
    private bool _preventKeyDownDefault;
    private ElementReference _moreRef;
    private ElementReference _headerRef;
    private BitPivotItem? _focusedItem;
    private BitPivotItem? _selectedItem;
    private int[] _overflowItemIndexes = [];
    private List<BitPivotItem> _allItems = [];
    private BitPivotOverflowBehavior? _setupBehavior;
    private DotNetObjectReference<BitPivot>? _dotnetObj;



    [Inject] private IJSRuntime _js { get; set; } = default!;



    /// <summary>
    /// Determines the alignment of the header section of the pivot.
    /// </summary>
    [Parameter, ResetStyleBuilder]
    public BitAlignment? Alignment { get; set; }

    /// <summary>
    /// The content of pivot.
    /// </summary>
    [Parameter] public RenderFragment? ChildContent { get; set; }

    /// <summary>
    /// Custom CSS classes for different parts of the pivot.
    /// </summary>
    [Parameter] public BitPivotClassStyles? Classes { get; set; }

    /// <summary>
    /// The general color of the pivot.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public BitColor? Color { get; set; }

    /// <summary>
    /// Default selected key for the pivot.
    /// </summary>
    [Parameter] public string? DefaultSelectedKey { get; set; }

    /// <summary>
    /// Renders a dismiss button on every pivot item, which reports the item to dismiss through the
    /// <see cref="OnItemDismiss"/> callback. A single item opts in or out of it on its own using the
    /// Dismissible parameter of the BitPivotItem.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public bool Dismissible { get; set; }

    /// <summary>
    /// The format of the aria-label of the dismiss button of the pivot items (default: "Remove {0}"),
    /// where the placeholder is filled with the header text of the item.
    /// </summary>
    [Parameter] public string? DismissAriaLabelFormat { get; set; }

    /// <summary>
    /// Gets or sets the icon of the dismiss button of the pivot items using custom CSS classes for external icon libraries.
    /// Takes precedence over <see cref="DismissIconName"/> when both are set.
    /// </summary>
    [Parameter] public BitIconInfo? DismissIcon { get; set; }

    /// <summary>
    /// Gets or sets the name of the icon of the dismiss button of the pivot items from the built-in Fluent UI icons (default: Cancel).
    /// </summary>
    [Parameter] public string? DismissIconName { get; set; }

    /// <summary>
    /// The title (tooltip) of the dismiss button of the pivot items (default: Remove).
    /// </summary>
    [Parameter] public string? DismissTitle { get; set; }

    /// <summary>
    /// Stretches the pivot items to share the whole width (or the whole height in a vertical pivot) of the header.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public bool FullWidth { get; set; }

    /// <summary>
    /// The content rendered at the end of the header, after the pivot items and after the overflow
    /// or slide affordances, which is where the actions belonging to the whole pivot usually go.
    /// </summary>
    [Parameter] public RenderFragment? HeaderEnd { get; set; }

    /// <summary>
    /// Whether to skip rendering the tabpanel with the content of the selected tab.
    /// </summary>
    [Parameter] public bool HeaderOnly { get; set; }

    /// <summary>
    /// The content rendered at the start of the header, before the pivot items and before the
    /// overflow or slide affordances.
    /// </summary>
    [Parameter] public RenderFragment? HeaderStart { get; set; }

    /// <summary>
    /// The type of the pivot header items.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public BitPivotHeaderType? HeaderType { get; set; }

    /// <summary>
    /// Mounts all tabs at render time and hide non-selected tabs with CSS styles instead of not-rendering them (useful for processing/extracting data).
    /// </summary>
    [Parameter] public bool MountAll { get; set; }

    /// <summary>
    /// Enables the roving tabindex behavior, which turns the whole header into a single tab stop
    /// that is navigable using the arrow, Home, and End keys.
    /// </summary>
    [Parameter] public bool Navigable { get; set; } = true;

    /// <summary>
    /// The aria-label of the next button in the Slide overflow behavior (default: Next).
    /// </summary>
    [Parameter] public string? NextAriaLabel { get; set; }

    /// <summary>
    /// Gets or sets the icon of the next button in the Slide overflow behavior using custom CSS classes for external icon libraries.
    /// Takes precedence over <see cref="NextIconName"/> when both are set.
    /// </summary>
    [Parameter] public BitIconInfo? NextIcon { get; set; }

    /// <summary>
    /// Gets or sets the name of the icon of the next button in the Slide overflow behavior from the built-in Fluent UI icons (default: ChevronRight).
    /// </summary>
    [Parameter] public string? NextIconName { get; set; }

    /// <summary>
    /// Callback for when the selected pivot item changes.
    /// </summary>
    [Parameter]
    public EventCallback<BitPivotItem> OnChange { get; set; }

    /// <summary>
    /// Callback for when a pivot header item is clicked.
    /// </summary>
    [Parameter] public EventCallback<BitPivotItem> OnItemClick { get; set; }

    /// <summary>
    /// Callback for when the dismiss button of a pivot item is clicked, or the Delete key is pressed
    /// while it holds the focus. The pivot does not remove the item itself, since the items belong to
    /// the markup that declares them, so the handler is what takes the item out of the list.
    /// </summary>
    [Parameter] public EventCallback<BitPivotItem> OnItemDismiss { get; set; }

    /// <summary>
    /// The aria-label of the overflow menu button in the Menu overflow behavior (default: More).
    /// </summary>
    [Parameter] public string? OverflowAriaLabel { get; set; }

    /// <summary>
    /// Overflow behavior when there is not enough room to display all of the links/tabs.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public BitPivotOverflowBehavior? OverflowBehavior { get; set; }

    /// <summary>
    /// Gets or sets the icon of the overflow menu button in the Menu overflow behavior using custom CSS classes for external icon libraries.
    /// Takes precedence over <see cref="OverflowIconName"/> when both are set.
    /// </summary>
    [Parameter] public BitIconInfo? OverflowIcon { get; set; }

    /// <summary>
    /// Gets or sets the name of the icon of the overflow menu button in the Menu overflow behavior from the built-in Fluent UI icons (default: More).
    /// </summary>
    [Parameter] public string? OverflowIconName { get; set; }

    /// <summary>
    /// Position of the pivot header.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public BitPivotPosition? Position { get; set; }

    /// <summary>
    /// The aria-label of the previous button in the Slide overflow behavior (default: Previous).
    /// </summary>
    [Parameter] public string? PreviousAriaLabel { get; set; }

    /// <summary>
    /// Gets or sets the icon of the previous button in the Slide overflow behavior using custom CSS classes for external icon libraries.
    /// Takes precedence over <see cref="PreviousIconName"/> when both are set.
    /// </summary>
    [Parameter] public BitIconInfo? PreviousIcon { get; set; }

    /// <summary>
    /// Gets or sets the name of the icon of the previous button in the Slide overflow behavior from the built-in Fluent UI icons (default: ChevronLeft).
    /// </summary>
    [Parameter] public string? PreviousIconName { get; set; }

    /// <summary>
    /// Key of the selected pivot item.
    /// </summary>
    [Parameter, TwoWayBound]
    [CallOnSet(nameof(OnSetSelectedKey))]
    public string? SelectedKey { get; set; }

    /// <summary>
    /// Selects the focused pivot item while the header is navigated with the keyboard, so that the
    /// selection follows the focus (the automatic activation of the WAI-ARIA tabs pattern).
    /// </summary>
    [Parameter] public bool SelectOnFocus { get; set; }

    /// <summary>
    /// The size of the pivot header items.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public BitSize? Size { get; set; }

    /// <summary>
    /// Stacks the icon of the pivot items on top of their text instead of putting the two side by side.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public bool Stacked { get; set; }

    /// <summary>
    /// Custom CSS styles for different parts of the pivot.
    /// </summary>
    [Parameter] public BitPivotClassStyles? Styles { get; set; }



    /// <summary>
    /// The pivot item that is currently selected.
    /// </summary>
    public BitPivotItem? SelectedItem => _selectedItem;

    /// <summary>
    /// Selects the pivot item carrying the given key, if such an item exists and is enabled.
    /// </summary>
    public async Task SelectItemByKey(string? key)
    {
        var item = _allItems.FirstOrDefault(i => i.Key == key);

        if (item is null || item == _selectedItem || item.IsEnabled is false) return;

        await SelectItem(item);
    }



    protected override string RootElementClass => "bit-pvt";

    private bool _isVertical => Position is BitPivotPosition.Start or BitPivotPosition.End;

    private string _MenuId => $"{_Id}-mnu";

    protected override void RegisterCssClasses()
    {
        ClassBuilder.Register(() => Classes?.Root);

        ClassBuilder.Register(() => Size switch
        {
            BitSize.Small => "bit-pvt-sm",
            BitSize.Medium => "bit-pvt-md",
            BitSize.Large => "bit-pvt-lg",
            _ => "bit-pvt-md"
        });

        ClassBuilder.Register(() => Color switch
        {
            BitColor.Primary => "bit-pvt-pri",
            BitColor.Secondary => "bit-pvt-sec",
            BitColor.Tertiary => "bit-pvt-ter",
            BitColor.Info => "bit-pvt-inf",
            BitColor.Success => "bit-pvt-suc",
            BitColor.Warning => "bit-pvt-wrn",
            BitColor.SevereWarning => "bit-pvt-swr",
            BitColor.Error => "bit-pvt-err",
            BitColor.PrimaryBackground => "bit-pvt-pbg",
            BitColor.SecondaryBackground => "bit-pvt-sbg",
            BitColor.TertiaryBackground => "bit-pvt-tbg",
            BitColor.PrimaryForeground => "bit-pvt-pfg",
            BitColor.SecondaryForeground => "bit-pvt-sfg",
            BitColor.TertiaryForeground => "bit-pvt-tfg",
            BitColor.PrimaryBorder => "bit-pvt-pbr",
            BitColor.SecondaryBorder => "bit-pvt-sbr",
            BitColor.TertiaryBorder => "bit-pvt-tbr",
            _ => "bit-pvt-pri"
        });

        ClassBuilder.Register(() => HeaderType switch
        {
            BitPivotHeaderType.Link => "bit-pvt-lnk",
            BitPivotHeaderType.Tab => "bit-pvt-tab",
            _ => "bit-pvt-lnk"
        });

        ClassBuilder.Register(() => OverflowBehavior switch
        {
            BitPivotOverflowBehavior.Menu => "bit-pvt-mnu",
            BitPivotOverflowBehavior.Scroll => "bit-pvt-scr",
            BitPivotOverflowBehavior.Slide => "bit-pvt-sld",
            BitPivotOverflowBehavior.None => "bit-pvt-non",
            _ => "bit-pvt-non"
        });

        ClassBuilder.Register(() => Position switch
        {
            BitPivotPosition.Top => "bit-pvt-top",
            BitPivotPosition.Bottom => "bit-pvt-btm",
            BitPivotPosition.Start => "bit-pvt-sta",
            BitPivotPosition.End => "bit-pvt-end",
            _ => "bit-pvt-top"
        });

        ClassBuilder.Register(() => FullWidth ? "bit-pvt-fwd" : string.Empty);

        ClassBuilder.Register(() => Stacked ? "bit-pvt-stk" : string.Empty);

        ClassBuilder.Register(() => Dismissible ? "bit-pvt-dsm" : string.Empty);
    }

    protected override void RegisterCssStyles()
    {
        StyleBuilder.Register(() => Styles?.Root);

        StyleBuilder.Register(() => Alignment switch
        {
            BitAlignment.Start => "--bit-pvt-hal:flex-start",
            BitAlignment.End => "--bit-pvt-hal:flex-end",
            BitAlignment.Center => "--bit-pvt-hal:center",
            BitAlignment.SpaceBetween => "--bit-pvt-hal:space-between",
            BitAlignment.SpaceAround => "--bit-pvt-hal:space-around",
            BitAlignment.SpaceEvenly => "--bit-pvt-hal:space-evenly",
            BitAlignment.Baseline => "--bit-pvt-hal:baseline",
            BitAlignment.Stretch => "--bit-pvt-hal:stretch",
            _ => "--bit-pvt-hal:flex-start"
        });
    }

    protected override async Task OnInitializedAsync()
    {
        if (SelectedKeyHasBeenSet is false && DefaultSelectedKey is not null)
        {
            await AssignSelectedKey(DefaultSelectedKey);
        }

        await base.OnInitializedAsync();
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (IsDisposed)
        {
            await base.OnAfterRenderAsync(firstRender);
            return;
        }

        var behavior = OverflowBehavior ?? BitPivotOverflowBehavior.None;
        var needsJs = behavior is BitPivotOverflowBehavior.Menu or BitPivotOverflowBehavior.Slide;
        var rtl = Dir is BitDir.Rtl;
        var vertical = Position is BitPivotPosition.Start or BitPivotPosition.End;

        if (_jsSetupRunning is false && (_setupBehavior != behavior || (_jsSetup && (_setupRtl != rtl || _setupVertical != vertical))))
        {
            // OnAfterRenderAsync gets called again while the interop calls below are still in flight,
            // so the setup state is captured and the branch is locked before the first await, otherwise
            // the next pass re-enters here and disposes the object reference that the JS instance of the
            // in-flight setup is still holding on to (which then fails its invocations).
            _jsSetupRunning = true;
            _setupBehavior = behavior;
            _setupRtl = rtl;
            _setupVertical = vertical;

            try
            {
                if (_dotnetObj is not null)
                {
                    _jsSetup = false;
                    await _js.BitPivotDispose(_Id);
                    _dotnetObj.Dispose();
                    _dotnetObj = null;
                }

                _isMenuOpen = false;
                _slideAtEnd = false;
                _slideAtStart = true;
                _slideHasOverflow = false;
                _overflowItemIndexes = [];

                if (needsJs && IsDisposed is false)
                {
                    _dotnetObj = DotNetObjectReference.Create(this);

                    await _js.BitPivotSetup(
                        _Id,
                        _headerRef,
                        behavior is BitPivotOverflowBehavior.Menu ? _moreRef : null,
                        behavior is BitPivotOverflowBehavior.Menu,
                        behavior is BitPivotOverflowBehavior.Slide,
                        rtl,
                        vertical,
                        _dotnetObj);

                    _jsSetup = true;
                }
            }
            catch (JSDisconnectedException) { } // we can ignore this exception here
            finally
            {
                _jsSetupRunning = false;
            }
        }
        else if (_jsSetup)
        {
            try
            {
                await _js.BitPivotRefresh(_Id);
            }
            catch (JSDisconnectedException) { } // we can ignore this exception here
        }

        // A selection made from the keyboard, from the overflow menu, or from the bound key can land
        // on an item scrolled out of sight, so the header is asked to bring it back into view.
        if (_scrollAfterRender)
        {
            _scrollAfterRender = false;

            if (behavior is BitPivotOverflowBehavior.Scroll or BitPivotOverflowBehavior.Slide)
            {
                try
                {
                    await _js.BitPivotScrollToSelected(_headerRef);
                }
                catch (JSDisconnectedException) { } // we can ignore this exception here
            }
        }

        // The item that should hold the focus may not have been rendered yet when the key was handled
        // (the tab taking the place of a dismissed one), so the focus is moved after the render.
        if (_focusAfterRender)
        {
            _focusAfterRender = false;

            await FocusItem(_focusedItem);
        }

        await base.OnAfterRenderAsync(firstRender);
    }



    [JSInvokable("OnSetOverflowItems")]
    public void OnSetOverflowItems(int[] indexes)
    {
        if (IsDisposed) return;

        _overflowItemIndexes = indexes ?? [];

        if (_overflowItemIndexes.Length == 0)
        {
            _isMenuOpen = false;
        }

        // Which items can hold the tabindex of the header changed along with the fold, and the tabindex
        // is rendered by the items themselves.
        RefreshItems();

        StateHasChanged();
    }

    [JSInvokable("OnSetSlideState")]
    public void OnSetSlideState(bool hasOverflow, bool atStart, bool atEnd)
    {
        if (IsDisposed) return;

        _slideHasOverflow = hasOverflow;
        _slideAtStart = atStart;
        _slideAtEnd = atEnd;

        StateHasChanged();
    }



    // The whole header is a single tab stop: the selected item, or the last focused one, holds the
    // tabindex and the arrow keys move the focus between the items, as the WAI-ARIA tabs pattern
    // describes. Everything else in the header keeps its own natural place in the tab order.
    internal string? GetItemTabIndex(BitPivotItem item)
    {
        if (Navigable is false) return IsItemFocusable(item) ? "0" : "-1";

        return GetActiveItem() == item ? "0" : "-1";
    }

    internal string? GetItemPanelId(BitPivotItem item)
    {
        if (HeaderOnly) return null;

        // Only the selected tab has a panel to point at while the others are not rendered at all,
        // and an aria-controls pointing at nothing is worse than no aria-controls at all.
        if (MountAll is false && item != _selectedItem) return null;

        return $"{item._Id}-pnl";
    }

    internal bool GetItemDismissible(BitPivotItem item)
    {
        return item.Dismissible ?? Dismissible;
    }

    internal string GetDismissAriaLabel(BitPivotItem item)
    {
        return string.Format(DismissAriaLabelFormat ?? "Remove {0}", item.HeaderText);
    }

    internal async Task SelectItem(BitPivotItem item)
    {
        if (SelectedKeyHasBeenSet && SelectedKeyChanged.HasDelegate is false) return;

        if (item == _selectedItem) return;

        MoveFocus(item);

        _selectedItem?.SetIsSelected(false);
        item.SetIsSelected(true);

        _selectedItem = item;
        _scrollAfterRender = true;

        await AssignSelectedKey(item.Key);

        await OnChange.InvokeAsync(item);

        StateHasChanged();
    }

    internal void RegisterItem(BitPivotItem item)
    {
        _allItems.Add(item);

        // An item that declares itself selected wins over the key, so a pivot driven by the IsSelected
        // of its items still ends up with exactly one selected tab.
        if (item.IsSelected && IsItemFocusable(item))
        {
            _selectedItem?.SetIsSelected(false);
            _selectedItem = item;
            _ = AssignSelectedKey(item.Key);
            StateHasChanged();
            return;
        }

        if (SelectedKey is not null && SelectedKey == item.Key)
        {
            _selectedItem?.SetIsSelected(false);
            item.SetIsSelected(true);
            _selectedItem = item;
            StateHasChanged();
            return;
        }

        // Nothing is selected yet, so the first item that can take the selection gets it. That also
        // covers a SelectedKey (or a DefaultSelectedKey) matching none of the items at all, which
        // would otherwise leave the pivot showing an empty panel.
        if (_selectedItem is null && IsItemFocusable(item))
        {
            item.SetIsSelected(true);
            _selectedItem = item;
            StateHasChanged();
        }
    }

    internal void UnregisterItem(BitPivotItem item)
    {
        var index = _allItems.IndexOf(item);

        _allItems.Remove(item);

        if (_focusedItem == item)
        {
            _focusedItem = null;
        }

        if (_selectedItem != item) return;

        // The selected tab is going away, so the selection moves to the nearest one that can take it
        // instead of leaving the pivot with a selected key pointing at nothing.
        _selectedItem = null;

        var next = _allItems.Skip(index).FirstOrDefault(IsItemFocusable)
                ?? _allItems.Take(index).LastOrDefault(IsItemFocusable);

        if (next is null)
        {
            _ = AssignSelectedKey(null);
            StateHasChanged();
            return;
        }

        next.SetIsSelected(true);
        _selectedItem = next;
        _ = AssignSelectedKey(next.Key);

        StateHasChanged();
    }

    internal void Refresh()
    {
        StateHasChanged();
    }

    internal async Task HandleItemClick(BitPivotItem item)
    {
        if (IsEnabled is false || item.IsEnabled is false) return;

        MoveFocus(item);

        await SelectItem(item);

        await item.OnClick.InvokeAsync();

        await OnItemClick.InvokeAsync(item);
    }

    internal async Task HandleItemDismiss(BitPivotItem item)
    {
        if (IsEnabled is false) return;
        if (GetItemDismissible(item) is false) return;

        // The focus is parked on a neighbour before the item leaves, otherwise removing the element
        // the focus sits on drops it all the way back to the document body.
        var index = _allItems.IndexOf(item);
        var next = _allItems.Skip(index + 1).FirstOrDefault(IsItemFocusable)
                ?? _allItems.Take(index).LastOrDefault(IsItemFocusable);

        MoveFocus(next);

        _focusAfterRender = next is not null;

        await item.OnDismiss.InvokeAsync();

        await OnItemDismiss.InvokeAsync(item);
    }

    internal void HandleItemFocusIn(BitPivotItem item)
    {
        MoveFocus(item);
    }

    // The tabindex of an item is state the pivot owns, but the item is what renders it, and Blazor
    // skips re-rendering a child whose own parameters have not changed - so the items are asked to
    // render themselves whenever the roving tabindex moves from one of them to another. Nothing is
    // asked of them when the move leaves the same item holding it, which is the common case of a
    // click: the focus lands on the tab a moment before the click that selects it.
    private void MoveFocus(BitPivotItem? item)
    {
        if (IsDisposed) return;

        var previous = GetActiveItem();

        _focusedItem = item;

        if (GetActiveItem() == previous) return;

        RefreshItems();
    }

    private void RefreshItems()
    {
        if (IsDisposed) return;

        foreach (var item in _allItems)
        {
            item.Refresh();
        }
    }

    // ArrowUp, ArrowDown, Home, End and Space scroll the page by default, so their default action is
    // suppressed while the header navigates with them. Kept key-scoped so Tab, Enter and the
    // horizontal arrows outside of a vertical header still behave normally.
    internal async Task HandleOnKeyDown(KeyboardEventArgs e)
    {
        // Blazor decides whether to prevent a default from the value of the last render, so the flag
        // is re-rendered as soon as it changes rather than only on the paths that navigate - left
        // standing after an arrow key, it would swallow the Tab that takes the focus out of the header.
        var prevent = IsEnabled && e.Key is "ArrowUp" or "ArrowDown" or "Home" or "End" or " ";
        if (_preventKeyDownDefault != prevent)
        {
            _preventKeyDownDefault = prevent;
            StateHasChanged();
        }

        if (IsEnabled is false) return;

        var current = _focusedItem is not null && IsItemFocusable(_focusedItem) ? _focusedItem : GetActiveItem();

        // The tab is a div rather than a button (it can hold a dismiss button of its own), so the
        // activation keys of the WAI-ARIA tabs pattern are handled here instead of natively.
        if (e.Key is "Enter" or " ")
        {
            if (current is null) return;

            await HandleItemClick(current);
            return;
        }

        if (e.Key is "Delete")
        {
            if (current is null || GetItemDismissible(current) is false) return;

            await HandleItemDismiss(current);
            return;
        }

        if (Navigable is false) return;

        var focusables = _allItems.Where(IsItemFocusable).ToList();
        if (focusables.Count == 0) return;

        var index = current is null ? -1 : focusables.IndexOf(current);
        var isRtl = Dir == BitDir.Rtl;

        int next;
        switch (e.Key)
        {
            case "ArrowRight":
                if (_isVertical) return;
                next = isRtl ? index - 1 : index + 1;
                break;
            case "ArrowLeft":
                if (_isVertical) return;
                next = isRtl ? index + 1 : index - 1;
                break;
            case "ArrowDown":
                if (_isVertical is false) return;
                next = index + 1;
                break;
            case "ArrowUp":
                if (_isVertical is false) return;
                next = index - 1;
                break;
            case "Home":
                next = 0;
                break;
            case "End":
                next = focusables.Count - 1;
                break;
            default:
                return;
        }

        // The navigation wraps around at both ends of the header.
        if (next < 0) next = focusables.Count - 1;
        else if (next >= focusables.Count) next = 0;

        var item = focusables[next];

        _scrollAfterRender = true;

        MoveFocus(item);

        await FocusItem(item);

        if (SelectOnFocus)
        {
            await SelectItem(item);
        }

        StateHasChanged();
    }



    // The item that owns the header's tabindex: the last focused one, otherwise the selected one,
    // otherwise the first item that can take the focus.
    private BitPivotItem? GetActiveItem()
    {
        if (_focusedItem is not null && IsItemFocusable(_focusedItem)) return _focusedItem;

        if (_selectedItem is not null && IsItemFocusable(_selectedItem)) return _selectedItem;

        return _allItems.FirstOrDefault(IsItemFocusable);
    }

    // An item folded into the overflow menu is hidden, so it can neither hold the tabindex of the
    // header nor be reached by the arrow keys; the menu is what gets to it instead.
    private bool IsItemFocusable(BitPivotItem item)
    {
        if (item.IsEnabled is false) return false;

        if (item.Visibility != BitVisibility.Visible) return false;

        if (_overflowItemIndexes.Length == 0) return true;

        return _overflowItemIndexes.Contains(_allItems.IndexOf(item)) is false;
    }

    private async Task FocusItem(BitPivotItem? item)
    {
        if (item is null) return;

        try
        {
            await item.RootElement.FocusAsync();
        }
        catch (JSDisconnectedException) { } // we can ignore this exception here
        catch (InvalidOperationException) { } // the element is not in the dom anymore
    }

    private void SelectKeyInternal(string? key)
    {
        var newItem = _allItems.FirstOrDefault(i => i.Key == key);

        if (newItem is null || newItem == _selectedItem || newItem.IsEnabled is false)
        {
            // The new key cannot be honored, so the bound value goes back to the key of the item that
            // is actually selected instead of being left pointing at a tab that is not shown. Assigning
            // the key that is already there is a no-op, which is what keeps this from looping.
            if (_selectedItem is not null && _selectedItem.Key != SelectedKey)
            {
                _ = AssignSelectedKey(_selectedItem.Key);
            }

            return;
        }

        _ = SelectItem(newItem);
    }

    private string GetItemStyle(BitPivotItem? item)
    {
        List<string?> list =
        [
            // The same mapping the base class uses for the visibility of any element, which this had
            // the two values of the other way around.
            item?.Visibility switch
            {
                BitVisibility.Hidden => "visibility:hidden",
                BitVisibility.Collapsed => "display:none",
                _ => string.Empty
            },
            Styles?.Body,
            item?.BodyStyle,
            item != _selectedItem ? "display:none" : string.Empty
        ];

        return string.Join(';', list.Where(s => s.HasValue()));
    }

    private string GetItemClass(BitPivotItem? item)
    {
        List<string?> list =
        [
            (item?.IsEnabled is false) ? "disabled" : string.Empty,
            Classes?.Body,
            item?.BodyClass
        ];

        return string.Join(' ', list.Where(s => s.HasValue()));
    }

    private void OnSetSelectedKey()
    {
        SelectKeyInternal(SelectedKey);
    }

    private void ToggleMenu()
    {
        _isMenuOpen = !_isMenuOpen;
    }

    private void CloseMenu()
    {
        _isMenuOpen = false;
    }

    private async Task HandleMenuKeyDown(KeyboardEventArgs e)
    {
        if (e.Key is not "Escape") return;

        CloseMenu();

        try
        {
            await _moreRef.FocusAsync();
        }
        catch (JSDisconnectedException) { } // we can ignore this exception here
        catch (InvalidOperationException) { } // the element is not in the dom anymore
    }

    private async Task SelectFromMenu(BitPivotItem item)
    {
        CloseMenu();

        if (IsEnabled is false || item.IsEnabled is false) return;

        await SelectItem(item);

        await item.OnClick.InvokeAsync();

        await OnItemClick.InvokeAsync(item);

        if (_jsSetup)
        {
            try
            {
                await _js.BitPivotRefresh(_Id);
            }
            catch (JSDisconnectedException) { } // we can ignore this exception here
        }
    }

    private async Task Slide(bool forward)
    {
        if (IsEnabled is false || _jsSetup is false) return;

        await _js.BitPivotSlide(_Id, forward);
    }

    protected override async ValueTask DisposeAsync(bool disposing)
    {
        if (IsDisposed || disposing is false) return;

        if (_dotnetObj is not null)
        {
            try
            {
                await _js.BitPivotDispose(_Id);
            }
            catch (JSDisconnectedException) { } // we can ignore this exception here

            _dotnetObj.Dispose();
            _dotnetObj = null;
        }

        await base.DisposeAsync(disposing);
    }
}
