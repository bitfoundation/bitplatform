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
    private bool _setupReorderable;
    private bool _isMenuOpen;
    private bool _slideAtEnd;
    private bool _slideAtStart = true;
    private bool _slideHasOverflow;
    private bool _rendered;
    private bool _focusAfterRender;
    private bool _keyCheckNeeded;
    private bool _orderCheckNeeded;
    private bool _focusMenuAfterRender;
    private bool _preventKeyDownDefault;
    private bool _preventMenuKeyDownDefault;
    private bool _preventMoreKeyDownDefault;
    private int _menuFocusIndex = -1;
    private ElementReference _moreRef;
    private ElementReference _menuRef;
    private ElementReference _headerRef;
    private BitPivotItem? _dragItem;
    private BitPivotItem? _dragOverItem;
    private BitPivotItem? _focusedItem;
    private BitPivotItem? _scrollTarget;
    private BitPivotItem? _selectedItem;
    private int[] _overflowItemIndexes = [];
    private List<BitPivotItem> _allItems = [];
    private HashSet<BitPivotItem> _mountedItems = [];
    private BitPivotOverflowBehavior? _setupBehavior;
    private DotNetObjectReference<BitPivot>? _dotnetObj;



    [Inject] private IJSRuntime _js { get; set; } = default!;



    /// <summary>
    /// Renders a button at the end of the pivot items that reports a request for a new tab through the
    /// <see cref="OnAdd"/> callback. The pivot does not add an item itself, since the items belong to the
    /// markup that declares them, so the handler is what puts the new one into the list.
    /// </summary>
    [Parameter] public bool Addable { get; set; }

    /// <summary>
    /// The aria-label of the add button of the pivot (default: Add).
    /// </summary>
    [Parameter] public string? AddAriaLabel { get; set; }

    /// <summary>
    /// Gets or sets the icon of the add button of the pivot using custom CSS classes for external icon libraries.
    /// Takes precedence over <see cref="AddIconName"/> when both are set.
    /// </summary>
    [Parameter] public BitIconInfo? AddIcon { get; set; }

    /// <summary>
    /// Gets or sets the name of the icon of the add button of the pivot from the built-in Fluent UI icons (default: Add).
    /// </summary>
    [Parameter] public string? AddIconName { get; set; }

    /// <summary>
    /// The title (tooltip) of the add button of the pivot (default: Add).
    /// </summary>
    [Parameter] public string? AddTitle { get; set; }

    /// <summary>
    /// Determines the alignment of the header section of the pivot.
    /// </summary>
    [Parameter, ResetStyleBuilder]
    public BitAlignment? Alignment { get; set; }

    /// <summary>
    /// The id of the element that labels the header of the pivot (rendered into the aria-labelledby of the
    /// tablist), which is what names a pivot sitting under a heading of its own. It wins over the AriaLabel.
    /// </summary>
    [Parameter] public string? AriaLabelledBy { get; set; }

    /// <summary>
    /// Renders the next and previous buttons of the Slide overflow behavior only while there actually
    /// is something to slide to, instead of leaving them in place in their disabled state.
    /// </summary>
    [Parameter] public bool AutoHideSlideButtons { get; set; }

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
    /// The gap between the pivot items of the header.
    /// </summary>
    [Parameter, ResetStyleBuilder]
    public string? Gap { get; set; }

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
    /// Keeps the content of every tab that has been shown at least once mounted, so that leaving a tab
    /// and coming back to it finds the content in the state it was left in. Unlike <see cref="MountAll"/>,
    /// a tab that has never been selected is not rendered at all.
    /// </summary>
    [Parameter] public bool KeepMounted { get; set; }

    /// <summary>
    /// Wraps the keyboard navigation of the header around at both of its ends, so that the next key
    /// on the last item lands on the first one (default: true).
    /// </summary>
    [Parameter] public bool Loop { get; set; } = true;

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
    /// Callback for when the add button of an <see cref="Addable"/> pivot is clicked.
    /// </summary>
    [Parameter] public EventCallback OnAdd { get; set; }

    /// <summary>
    /// Callback for when the selected pivot item changes.
    /// </summary>
    [Parameter]
    public EventCallback<BitPivotItem> OnChange { get; set; }

    /// <summary>
    /// Callback for just before the selected pivot item changes, which can call the change off by setting
    /// the Cancel of its arguments, so that a tab holding unsaved work can refuse to be left.
    /// </summary>
    [Parameter] public EventCallback<BitPivotChangeArgs> OnChanging { get; set; }

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
    /// Callback for when a pivot item is dragged onto another one, or moved with the Ctrl+Arrow keys,
    /// in a <see cref="Reorderable"/> pivot. The pivot does not move the item itself, since the items
    /// belong to the markup that declares them, so the handler is what reorders the list.
    /// </summary>
    [Parameter] public EventCallback<BitPivotReorderEventArgs> OnItemReorder { get; set; }

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
    /// Lets the pivot items be dragged onto one another, and the focused one be moved with the
    /// Ctrl+Arrow keys, to ask for a new order through the <see cref="OnItemReorder"/> callback.
    /// A single item opts in or out of it on its own using the Reorderable parameter of the BitPivotItem.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public bool Reorderable { get; set; }

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
    /// The pivot items in the order they are declared in.
    /// </summary>
    public IReadOnlyList<BitPivotItem> Items => _allItems;

    /// <summary>
    /// Selects the pivot item carrying the given key, if such an item exists and is enabled.
    /// </summary>
    public async Task SelectItemByKey(string? key)
    {
        var item = _allItems.FirstOrDefault(i => i.Key == key);

        if (item is null || item == _selectedItem || IsItemSelectable(item) is false) return;

        await SelectItem(item);
    }



    protected override string RootElementClass => "bit-pvt";

    private bool _isVertical => Position is BitPivotPosition.Start or BitPivotPosition.End;

    private string _MenuId => $"{_Id}-mnu";

    // The slide affordances go away with nothing to slide to when the pivot asks for it, so that a
    // header that happens to fit does not carry a pair of permanently disabled buttons.
    private bool _ShowSlideButtons => OverflowBehavior is BitPivotOverflowBehavior.Slide
                                   && (AutoHideSlideButtons is false || _slideHasOverflow);

    // The selected tab can be one of the tabs the Menu behavior folded away, and a header showing no
    // selection at all reads as if nothing were selected, so the button that holds it says so.
    private bool _IsSelectedOverflowed => _selectedItem is not null
                                       && _overflowItemIndexes.Any(i => i >= 0 && i < _allItems.Count && _allItems[i] == _selectedItem);

    private string? _ActiveMenuItemId => (_isMenuOpen && _menuFocusIndex >= 0 && _menuFocusIndex < _overflowItemIndexes.Length)
                                            ? GetMenuItemId(_menuFocusIndex)
                                            : null;

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
            BitPivotOverflowBehavior.Wrap => "bit-pvt-wrp",
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

        ClassBuilder.Register(() => Reorderable ? "bit-pvt-reo" : string.Empty);
    }

    protected override void RegisterCssStyles()
    {
        StyleBuilder.Register(() => Styles?.Root);

        StyleBuilder.Register(() => Gap.HasValue() ? $"--bit-pvt-gap:{Gap}" : string.Empty);

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

        if (_orderCheckNeeded)
        {
            _orderCheckNeeded = false;

            await SortItemsByDomOrder();
        }

        if (_keyCheckNeeded)
        {
            _keyCheckNeeded = false;

            // The tab the key was waiting for never showed up, so the bound value goes back to the tab
            // that is actually selected rather than being left pointing at nothing.
            if (_allItems.Exists(i => i.Key == SelectedKey) is false)
            {
                RevertSelectedKey();
            }
        }

        _rendered = true;

        var behavior = OverflowBehavior ?? BitPivotOverflowBehavior.None;
        // A reorderable header needs a bit of JS of its own: a drag that carries nothing on its data
        // transfer never starts in some browsers, and Blazor has no way of filling that in from C#.
        var reorderable = Reorderable || _allItems.Exists(i => i.Reorderable is true);
        var needsJs = behavior is BitPivotOverflowBehavior.Menu or BitPivotOverflowBehavior.Slide || reorderable;
        var rtl = Dir is BitDir.Rtl;
        var vertical = Position is BitPivotPosition.Start or BitPivotPosition.End;

        if (_jsSetupRunning is false && (_setupBehavior != behavior
                                      || _setupReorderable != reorderable
                                      || (_jsSetup && (_setupRtl != rtl || _setupVertical != vertical))))
        {
            // OnAfterRenderAsync gets called again while the interop calls below are still in flight,
            // so the setup state is captured and the branch is locked before the first await, otherwise
            // the next pass re-enters here and disposes the object reference that the JS instance of the
            // in-flight setup is still holding on to (which then fails its invocations).
            _jsSetupRunning = true;
            _setupBehavior = behavior;
            _setupRtl = rtl;
            _setupVertical = vertical;
            _setupReorderable = reorderable;

            try
            {
                if (_dotnetObj is not null)
                {
                    _jsSetup = false;
                    await _js.BitPivotDispose(_Id);
                    _dotnetObj.Dispose();
                    _dotnetObj = null;
                }

                CloseMenu();
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
                        reorderable,
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
        // on an item scrolled out of sight, so the header is asked to bring that item back into view.
        if (_scrollTarget is not null)
        {
            var target = _scrollTarget;
            _scrollTarget = null;

            if (behavior is BitPivotOverflowBehavior.Scroll or BitPivotOverflowBehavior.Slide)
            {
                try
                {
                    await _js.BitPivotScrollToItem(target.RootElement);
                }
                catch (JSDisconnectedException) { } // we can ignore this exception here
                catch (JSException) { } // the element is not in the dom anymore
            }
        }

        // The item that should hold the focus may not have been rendered yet when the key was handled
        // (the tab taking the place of a dismissed one), so the focus is moved after the render.
        if (_focusAfterRender)
        {
            _focusAfterRender = false;

            await FocusItem(_focusedItem);
        }

        // The menu is what takes the focus while it is open, and it is only in the dom to take it once
        // the render that opened it has gone through.
        if (_focusMenuAfterRender)
        {
            _focusMenuAfterRender = false;

            if (_isMenuOpen)
            {
                try
                {
                    await _menuRef.FocusAsync();
                }
                catch (JSDisconnectedException) { } // we can ignore this exception here
                catch (InvalidOperationException) { } // the element is not in the dom anymore
            }
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
            CloseMenu();
        }
        else if (_menuFocusIndex >= _overflowItemIndexes.Length)
        {
            _menuFocusIndex = _overflowItemIndexes.Length - 1;
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
        // Only the tabs whose panel is actually rendered have something to point at, and an
        // aria-controls pointing at nothing is worse than no aria-controls at all.
        if (IsItemPanelRendered(item) is false) return null;

        return $"{item._Id}-pnl";
    }

    internal bool IsItemPanelRendered(BitPivotItem item)
    {
        if (HeaderOnly) return false;

        if (MountAll) return true;

        if (item == _selectedItem) return true;

        return KeepMounted && _mountedItems.Contains(item);
    }

    internal bool GetItemDismissible(BitPivotItem item)
    {
        return item.Dismissible ?? Dismissible;
    }

    internal bool GetItemReorderable(BitPivotItem item)
    {
        return IsEnabled && item.IsEnabled && (item.Reorderable ?? Reorderable);
    }

    internal string? GetItemDragClass(BitPivotItem item)
    {
        if (_dragItem == item) return "bit-pvti-drg";

        if (_dragOverItem != item) return null;

        // The dragged tab takes the place of the one it is dropped on, so the edge the indicator is
        // drawn on is the edge it would come to rest against: the far one when it travels forward.
        return _allItems.IndexOf(_dragItem!) < _allItems.IndexOf(item)
                ? "bit-pvti-dro bit-pvti-dro-end"
                : "bit-pvti-dro";
    }

    internal string GetDismissAriaLabel(BitPivotItem item)
    {
        // A tab given a header template of its own has no header text to be named by, so the label falls
        // back to whatever else names it rather than reading as a Remove with nothing after it.
        var name = item.HeaderText.HasValue() ? item.HeaderText
                 : item.AriaLabel.HasValue() ? item.AriaLabel
                 : item.Title;

        return name.HasValue()
                ? string.Format(DismissAriaLabelFormat ?? "Remove {0}", name)
                : (DismissTitle ?? "Remove");
    }

    internal string GetMenuItemId(int index)
    {
        return $"{_MenuId}-{index}";
    }

    internal async Task SelectItem(BitPivotItem item)
    {
        if (SelectedKeyHasBeenSet && SelectedKeyChanged.HasDelegate is false) return;

        if (item == _selectedItem) return;

        // The change is offered to the handler before anything moves, so that a tab holding unsaved work
        // can refuse to be left.
        if (OnChanging.HasDelegate)
        {
            var args = new BitPivotChangeArgs(item);

            await OnChanging.InvokeAsync(args);

            if (args.Cancel)
            {
                // A change called off after the bound key was already moved would leave that key pointing
                // at a tab that is not shown, so it goes back to the tab that is actually selected.
                if (SelectedKeyHasBeenSet && _selectedItem?.Key != SelectedKey)
                {
                    await AssignSelectedKey(_selectedItem?.Key);
                }

                StateHasChanged();
                return;
            }
        }

        MoveFocus(item);

        _selectedItem?.SetIsSelected(false);
        item.SetIsSelected(true);

        _selectedItem = item;
        _scrollTarget = item;
        _mountedItems.Add(item);

        await AssignSelectedKey(item.Key);

        await OnChange.InvokeAsync(item);

        StateHasChanged();
    }

    internal void RegisterItem(BitPivotItem item)
    {
        _allItems.Add(item);

        // An item that shows up after the first render is created last whatever its place in the markup,
        // so the list has to be put back into the order the header is actually laid out in.
        if (_rendered)
        {
            _orderCheckNeeded = true;
        }

        // An item that declares itself selected wins over the key, so a pivot driven by the IsSelected
        // of its items still ends up with exactly one selected tab.
        if (item.IsSelected && IsItemFocusable(item))
        {
            _selectedItem?.SetIsSelected(false);
            _selectedItem = item;
            _scrollTarget = item;
            _ = AssignSelectedKey(item.Key);
            StateHasChanged();
            return;
        }

        // A disabled or collapsed item cannot take the selection here either, for the same reason
        // SelectKeyInternal refuses it: the key would be left pointing at a tab that is not shown.
        // Falling through leaves the selection to the first item that can actually hold it.
        if (SelectedKey is not null && SelectedKey == item.Key && IsItemSelectable(item))
        {
            _selectedItem?.SetIsSelected(false);
            item.SetIsSelected(true);
            _selectedItem = item;
            _scrollTarget = item;
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
            _scrollTarget = item;
            StateHasChanged();
        }
    }

    internal void UnregisterItem(BitPivotItem item)
    {
        var index = _allItems.IndexOf(item);

        _allItems.Remove(item);
        _mountedItems.Remove(item);

        if (_focusedItem == item)
        {
            _focusedItem = null;
        }

        if (_scrollTarget == item)
        {
            _scrollTarget = null;
        }

        if (_dragItem == item)
        {
            _dragItem = null;
        }

        if (_dragOverItem == item)
        {
            _dragOverItem = null;
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
        _scrollTarget = next;
        _mountedItems.Add(next);
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

    internal void HandleItemDragStart(BitPivotItem item)
    {
        if (GetItemReorderable(item) is false) return;

        _dragItem = item;
        _dragOverItem = null;

        RefreshItems();
    }

    internal void HandleItemDragEnter(BitPivotItem item)
    {
        if (_dragItem is null || _dragItem == item) return;
        if (GetItemReorderable(item) is false) return;
        if (_dragOverItem == item) return;

        _dragOverItem = item;

        RefreshItems();
    }

    internal void HandleItemDragEnd()
    {
        if (_dragItem is null && _dragOverItem is null) return;

        _dragItem = null;
        _dragOverItem = null;

        RefreshItems();
    }

    internal async Task HandleItemDrop(BitPivotItem item)
    {
        var dragged = _dragItem;

        HandleItemDragEnd();

        if (dragged is null || dragged == item) return;
        if (GetItemReorderable(item) is false) return;

        await ReorderItem(dragged, _allItems.IndexOf(item));
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

        var isRtl = Dir == BitDir.Rtl;

        // Ctrl and an arrow moves the focused tab itself rather than the focus, which is the keyboard
        // half of the drag that reorders a header.
        if (e.CtrlKey && current is not null && GetItemReorderable(current))
        {
            var forward = _isVertical
                            ? e.Key is "ArrowDown"
                            : (isRtl ? e.Key is "ArrowLeft" : e.Key is "ArrowRight");
            var backward = _isVertical
                            ? e.Key is "ArrowUp"
                            : (isRtl ? e.Key is "ArrowRight" : e.Key is "ArrowLeft");

            if (forward || backward)
            {
                var from = _allItems.IndexOf(current);
                var step = forward ? 1 : -1;
                var to = from + step;

                // A tab that is not shown is not a place another one can be moved to either, so the move
                // steps over it and lands on the next tab that is really in the header.
                while (to >= 0 && to < _allItems.Count && IsItemSelectable(_allItems[to]) is false)
                {
                    to += step;
                }

                await ReorderItem(current, to);

                _focusAfterRender = true;
                return;
            }
        }

        if (Navigable is false) return;

        var focusables = _allItems.Where(IsItemFocusable).ToList();
        if (focusables.Count == 0) return;

        var index = current is null ? -1 : focusables.IndexOf(current);

        int next;
        switch (e.Key)
        {
            case "ArrowRight":
                if (_isVertical) return;
                next = index < 0 ? 0 : (isRtl ? index - 1 : index + 1);
                break;
            case "ArrowLeft":
                if (_isVertical) return;
                next = index < 0 ? 0 : (isRtl ? index + 1 : index - 1);
                break;
            case "ArrowDown":
                if (_isVertical is false) return;
                next = index < 0 ? 0 : index + 1;
                break;
            case "ArrowUp":
                if (_isVertical is false) return;
                next = index < 0 ? 0 : index - 1;
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

        // The navigation wraps around at both ends of the header unless it is asked not to, in which
        // case it simply stops there.
        if (next < 0)
        {
            if (Loop is false) return;
            next = focusables.Count - 1;
        }
        else if (next >= focusables.Count)
        {
            if (Loop is false) return;
            next = 0;
        }

        var item = focusables[next];

        _scrollTarget = item;

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
    // Whether the item can hold the selection at all, which is the focusable test without the fold:
    // an overflowed tab is out of the header, but its panel is still what the pivot would show.
    private static bool IsItemSelectable(BitPivotItem item)
    {
        return item.IsEnabled && item.Visibility == BitVisibility.Visible;
    }

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

    private async Task ReorderItem(BitPivotItem item, int newIndex)
    {
        if (OnItemReorder.HasDelegate is false) return;

        var oldIndex = _allItems.IndexOf(item);

        if (oldIndex < 0 || newIndex < 0 || newIndex >= _allItems.Count || oldIndex == newIndex) return;

        // An item that is not reorderable is pinned where it is, so it is not a place another one can
        // be moved to either.
        if (GetItemReorderable(_allItems[newIndex]) is false) return;

        await OnItemReorder.InvokeAsync(new BitPivotReorderEventArgs
        {
            Item = item,
            OldIndex = oldIndex,
            NewIndex = newIndex
        });
    }

    // The items are registered as they are created, which stops being the order they are declared in as
    // soon as one of them is added after the first render: it is created last, whatever its place in the
    // markup. Everything the pivot does by position - the arrow keys, the neighbour a dismissed tab
    // hands the focus to, the indexes the overflow fold is reported with - reads the header the way it
    // is laid out, so the list is put back into the order the elements themselves are in.
    private async Task SortItemsByDomOrder()
    {
        string[]? ids = null;

        try
        {
            ids = await _js.BitPivotGetItemsOrder(_headerRef);
        }
        catch (JSDisconnectedException) { } // we can ignore this exception here
        catch (JSException) { } // the header is not in the dom anymore

        if (ids is null || ids.Length != _allItems.Count) return;

        var order = new Dictionary<string, int>(ids.Length);
        for (var i = 0; i < ids.Length; i++)
        {
            order[ids[i]] = i;
        }

        var changed = false;
        for (var i = 0; i < _allItems.Count; i++)
        {
            if (order.TryGetValue(_allItems[i]._Id, out var index) && index == i) continue;

            changed = true;
            break;
        }

        if (changed is false) return;

        _allItems = [.. _allItems.OrderBy(i => order.TryGetValue(i._Id, out var index) ? index : int.MaxValue)];

        RefreshItems();

        StateHasChanged();
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

    // Called from the render of the panels: the selected tab is the one that has been shown, and
    // KeepMounted is what keeps its panel around after the selection has moved on.
    private void TrackMountedItem()
    {
        if (_selectedItem is null) return;

        _mountedItems.Add(_selectedItem);
    }

    private void SelectKeyInternal(string? key)
    {
        var newItem = _allItems.FirstOrDefault(i => i.Key == key);

        if (newItem is null)
        {
            // The key can be the key of a tab that is being added in the very same render - adding a tab
            // and selecting it is one gesture - and that tab has not registered itself yet, so the key is
            // left alone and looked at again once the render it came with has gone through.
            _keyCheckNeeded = true;
            return;
        }

        if (newItem == _selectedItem || IsItemSelectable(newItem) is false)
        {
            RevertSelectedKey();
            return;
        }

        _ = SelectItem(newItem);
    }

    // The new key cannot be honored, so the bound value goes back to the key of the item that is
    // actually selected instead of being left pointing at a tab that is not shown. Assigning the key
    // that is already there is a no-op, which is what keeps this from looping.
    private void RevertSelectedKey()
    {
        if (_selectedItem is not null && _selectedItem.Key != SelectedKey)
        {
            _ = AssignSelectedKey(_selectedItem.Key);
        }
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
        if (IsEnabled is false) return;

        if (_isMenuOpen)
        {
            CloseMenu();
            return;
        }

        OpenMenu();
    }

    // The menu opens on the item it would be closed on: the selected one when it is one of the folded
    // ones, so that the menu of a header whose selection is out of sight starts where the user left it.
    private void OpenMenu()
    {
        _isMenuOpen = true;
        _menuFocusIndex = -1;

        for (var i = 0; i < _overflowItemIndexes.Length; i++)
        {
            if (GetMenuItem(i) != _selectedItem) continue;

            _menuFocusIndex = i;
            break;
        }

        if (_menuFocusIndex < 0)
        {
            MoveMenuFocusFrom(0, 1);
        }

        _focusMenuAfterRender = true;
    }

    private void CloseMenu()
    {
        _isMenuOpen = false;
        _menuFocusIndex = -1;
    }

    private BitPivotItem? GetMenuItem(int index)
    {
        if (index < 0 || index >= _overflowItemIndexes.Length) return null;

        var itemIndex = _overflowItemIndexes[index];

        return (itemIndex >= 0 && itemIndex < _allItems.Count) ? _allItems[itemIndex] : null;
    }

    private void MoveMenuFocus(int step)
    {
        var count = _overflowItemIndexes.Length;
        if (count == 0) return;

        var start = _menuFocusIndex + step;
        if (start < 0) start = count - 1;
        else if (start >= count) start = 0;

        MoveMenuFocusFrom(start, step);
    }

    private void MoveMenuFocusFrom(int start, int step)
    {
        var count = _overflowItemIndexes.Length;
        if (count == 0) return;

        var index = start;
        for (var i = 0; i < count; i++)
        {
            if (GetMenuItem(index)?.IsEnabled is true)
            {
                _menuFocusIndex = index;
                return;
            }

            index += step;
            if (index < 0) index = count - 1;
            else if (index >= count) index = 0;
        }
    }

    // The keys of the WAI-ARIA menu pattern, which the overflow menu answers to on its own: the
    // tablist around it navigates tabs, and the menu is a menu.
    private async Task HandleMenuKeyDown(KeyboardEventArgs e)
    {
        var prevent = e.Key is "ArrowUp" or "ArrowDown" or "Home" or "End" or " " or "Enter";
        if (_preventMenuKeyDownDefault != prevent)
        {
            _preventMenuKeyDownDefault = prevent;
            StateHasChanged();
        }

        if (_isMenuOpen is false) return;

        switch (e.Key)
        {
            case "Escape":
                await CloseMenuAndFocusButton();
                return;

            case "Tab":
                // Closing the menu takes the element the focus is on out of the page, so the focus goes
                // back to the button that holds it rather than dropping all the way to the document body.
                await CloseMenuAndFocusButton();
                return;

            case "Enter":
            case " ":
                var item = GetMenuItem(_menuFocusIndex);
                if (item is null) return;
                await SelectFromMenu(item);
                return;

            case "ArrowDown":
                MoveMenuFocus(1);
                break;

            case "ArrowUp":
                MoveMenuFocus(-1);
                break;

            case "Home":
                MoveMenuFocusFrom(0, 1);
                break;

            case "End":
                MoveMenuFocusFrom(_overflowItemIndexes.Length - 1, -1);
                break;

            default:
                return;
        }

        StateHasChanged();
    }

    // The button that opens the menu keeps the keys that belong to it: the arrows open it and step
    // into it, and Escape closes it again without the tablist ever seeing any of them.
    private void HandleMoreKeyDown(KeyboardEventArgs e)
    {
        // Space is deliberately left alone: preventing its default on a button is what would keep it
        // from raising the click that opens the menu.
        var prevent = e.Key is "ArrowUp" or "ArrowDown";
        if (_preventMoreKeyDownDefault != prevent)
        {
            _preventMoreKeyDownDefault = prevent;
            StateHasChanged();
        }

        if (IsEnabled is false) return;

        if (e.Key is "Escape")
        {
            CloseMenu();
            return;
        }

        if (e.Key is not ("ArrowUp" or "ArrowDown")) return;

        if (_isMenuOpen is false)
        {
            OpenMenu();

            if (e.Key is "ArrowUp")
            {
                MoveMenuFocusFrom(_overflowItemIndexes.Length - 1, -1);
            }

            return;
        }

        MoveMenuFocus(e.Key is "ArrowDown" ? 1 : -1);

        _focusMenuAfterRender = true;
    }

    private async Task CloseMenuAndFocusButton()
    {
        CloseMenu();

        StateHasChanged();

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

        if (IsEnabled is false || item.IsEnabled is false)
        {
            StateHasChanged();
            return;
        }

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

        // The tab the menu was closed on stays folded away, so the focus goes back to the button that
        // holds it rather than to an element that is not in the header anymore.
        try
        {
            await _moreRef.FocusAsync();
        }
        catch (JSDisconnectedException) { } // we can ignore this exception here
        catch (InvalidOperationException) { } // the element is not in the dom anymore
    }

    private async Task HandleAddClick()
    {
        if (IsEnabled is false) return;

        await OnAdd.InvokeAsync();
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
