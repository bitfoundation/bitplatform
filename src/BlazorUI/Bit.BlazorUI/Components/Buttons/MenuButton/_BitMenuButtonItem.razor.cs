namespace Bit.BlazorUI;

public partial class _BitMenuButtonItem<TItem> : IBitMenuButtonSubmenu, IAsyncDisposable where TItem : class
{
    // How long the pointer rests on a row before the submenu beside it opens, or before the one already
    // open is taken away. Short enough to feel immediate, long enough that sweeping the pointer across
    // the menu neither opens every submenu on the way past nor closes the one being travelled towards.
    private const int SubmenuHoverDelay = 150;

    private bool _disposed;
    private bool _isSubmenuOpen;
    private bool _openedByPointer;
    private ElementReference _itemRef;
    private readonly string _uniqueId = BitShortId.NewId();
    private DotNetObjectReference<_BitMenuButtonItem<TItem>>? _dotnetObj;



    [Inject] private IJSRuntime _js { get; set; } = default!;



    [Parameter] public TItem Item { get; set; } = default!;

    [Parameter] public BitMenuButton<TItem> MenuButton { get; set; } = default!;

    /// <summary>
    /// The nested options of a submenu in the Option API, which render themselves in place inside it.
    /// The Items API has no fragment to render and walks its own child items instead.
    /// </summary>
    [Parameter] public RenderFragment? ChildrenFragment { get; set; }

    /// <summary>
    /// The level of the menu the item lives in: zero for the menu the button opens, one more per submenu.
    /// </summary>
    [Parameter] public int Level { get; set; }

    /// <summary>
    /// Whether the menu this item is one of reserves the check column on every row. It is decided per menu
    /// rather than per menu button: the labels that have to line up with each other are the ones inside the
    /// same list, and a submenu of plain commands beside one of check items has nothing to reserve it for.
    /// </summary>
    [Parameter] public bool ShowCheckColumn { get; set; }



    int IBitMenuButtonSubmenu.Level => Level;

    private string _itemId => $"BitMenuButton-{_uniqueId}-item";
    private string _submenuId => $"BitMenuButton-{_uniqueId}-submenu";
    // The menu inside the submenu's callout, which is what the row points at: aria-controls names the
    // popup itself rather than the element that carries it.
    private string _submenuMenuId => $"BitMenuButton-{_uniqueId}-submenu-menu";

    // The Option API knows it has a submenu from the fragment it was given rather than from the options
    // inside it: those only register once they render, which is inside the submenu this decides to draw.
    private bool _hasSubmenu => ChildrenFragment is not null || MenuButton.GetChildItems(Item).Count > 0;



    public ValueTask FocusAsync()
    {
        return _itemRef.FocusAsync();
    }

    public async Task CloseSubmenuAsync()
    {
        if (_isSubmenuOpen is false) return;

        _isSubmenuOpen = false;
        _openedByPointer = false;
        MenuButton.SubmenuClosed(this);

        StateHasChanged();

        await ToggleSubmenuCallout();

        try
        {
            await _js.BitMenuButtonsDisposeSubmenu(MenuButton._Id, _submenuId);
        }
        catch (JSDisconnectedException) { } // we can ignore this exception here
    }

    // The callout stack closes a submenu on its own when the page is interacted with outside of it, or
    // when another callout takes over, so the state it was drawn from has to follow it back down.
    [JSInvokable("CloseCallout")]
    public async Task CloseCalloutBeforeAnotherCalloutIsOpened()
    {
        if (_isSubmenuOpen is false) return;

        _isSubmenuOpen = false;
        _openedByPointer = false;
        MenuButton.SubmenuClosed(this);

        try
        {
            await _js.BitMenuButtonsDisposeSubmenu(MenuButton._Id, _submenuId);
        }
        catch (JSDisconnectedException) { } // we can ignore this exception here

        StateHasChanged();
    }



    private async Task OpenSubmenu(bool focusFirstItem, bool byPointer = false)
    {
        if (MenuButton.IsEnabled is false || MenuButton.GetIsEnabled(Item) is false) return;

        if (_isSubmenuOpen)
        {
            if (focusFirstItem)
            {
                _openedByPointer = false;
                await FocusFirstItem();
            }
            return;
        }

        // Whatever was open beside this item - and anything opened from inside that - goes first, so the
        // open submenus are always one path down the menu rather than a fan of them.
        await MenuButton.RegisterOpenSubmenu(this);

        _isSubmenuOpen = true;
        _openedByPointer = byPointer;

        StateHasChanged();

        _dotnetObj ??= DotNetObjectReference.Create(this);

        await ToggleSubmenuCallout();

        try
        {
            await _js.BitMenuButtonsSetupSubmenu(MenuButton._Id, _submenuId);
        }
        catch (JSDisconnectedException) { } // we can ignore this exception here

        if (focusFirstItem)
        {
            await FocusFirstItem();
        }
    }

    private ValueTask FocusFirstItem()
    {
        return MenuButton.FocusItemOf(_submenuId, "first");
    }

    private async Task ToggleSubmenuCallout()
    {
        if (_dotnetObj is null) return;

        try
        {
            await _js.BitCalloutToggleCallout(
                dotnetObj: _dotnetObj,
                componentId: _itemId,
                component: null,
                calloutId: _submenuId,
                callout: null,
                // No overlay of its own: the menu the submenu was opened from already covers the page with
                // one, and a second would take the clicks meant for the items underneath it.
                overlayId: string.Empty,
                isCalloutOpen: _isSubmenuOpen,
                responsiveMode: BitResponsiveMode.None,
                dropDirection: BitDropDirection.All,
                isRtl: MenuButton.Dir is BitDir.Rtl,
                // The submenu caps itself to the room the viewport leaves and scrolls inside it, the way
                // the menu it was opened from does.
                scrollContainerId: _submenuId,
                scrollOffset: 0,
                headerId: string.Empty,
                footerId: string.Empty,
                // A submenu is as wide as its own labels rather than as wide as the row it opens from.
                setCalloutWidth: false,
                fixedCalloutWidth: false,
                maxWindowWidth: 0,
                maxHeight: 0,
                arrowId: string.Empty,
                gap: 0,
                noDismiss: false,
                // A submenu opens beside the row it belongs to, flipping to the other side only when the
                // screen leaves no room for it there.
                preferredSide: "end",
                alignment: string.Empty,
                noFlip: false,
                collisionPadding: 0,
                alignmentOffset: 0,
                arrowPadding: 0);
        }
        catch (JSDisconnectedException) { } // we can ignore this exception here
    }



    private async Task HandleOnClick()
    {
        if (_hasSubmenu is false)
        {
            await MenuButton.HandleOnItemClick(Item);
            return;
        }

        if (MenuButton.IsEnabled is false || MenuButton.GetIsEnabled(Item) is false) return;

        // A submenu the pointer opened on its own is not one the click that follows should take away
        // again: the click is the user arriving at it, so it moves the keyboard into it instead.
        if (_isSubmenuOpen && _openedByPointer is false)
        {
            await CloseSubmenuAsync();
            await FocusAsync();
            return;
        }

        await OpenSubmenu(focusFirstItem: true);
    }

    // The key that walks INTO a submenu, which is the one that points away from the menu in the
    // direction it is laid out in. Walking back out of it is handled by the submenu itself.
    private async Task HandleOnItemKeyDown(KeyboardEventArgs e)
    {
        if (_hasSubmenu is false) return;

        var openKey = MenuButton.Dir is BitDir.Rtl ? "ArrowLeft" : "ArrowRight";

        if (e.Key != openKey) return;

        await OpenSubmenu(focusFirstItem: true);
    }

    private Task HandleOnSubmenuKeyDown(KeyboardEventArgs e)
    {
        return MenuButton.HandleOnMenuKeyDown(e, _submenuId, this);
    }

    // Every row of a menu that has submenus answers the pointer: one with a submenu opens it, one
    // without takes away the submenu that was open beside it, and the submenu itself cancels both - which
    // is what lets the pointer travel diagonally from a row into the submenu it opened.
    private void HandleOnPointerEnter(MouseEventArgs e)
    {
        var token = MenuButton.NextHoverToken();

        if (MenuButton.IsEnabled is false) return;

        if (_hasSubmenu && MenuButton.GetIsEnabled(Item))
        {
            if (_isSubmenuOpen) return;

            _ = RunAfterHoverDelay(token, () => OpenSubmenu(focusFirstItem: false, byPointer: true));
        }
        else
        {
            _ = RunAfterHoverDelay(token, () => MenuButton.CloseSubmenusFrom(Level));
        }
    }

    private void HandleOnSubmenuPointerEnter(MouseEventArgs e)
    {
        MenuButton.NextHoverToken();
    }

    private async Task RunAfterHoverDelay(int token, Func<Task> action)
    {
        await Task.Delay(SubmenuHoverDelay);

        // The ticket is stale as soon as the pointer has moved on, and the component may have gone with
        // the menu that closed under it - which is a race the delay is long enough to lose.
        if (_disposed || MenuButton.IsHoverTokenCurrent(token) is false) return;

        try
        {
            await InvokeAsync(action);
        }
        catch (ObjectDisposedException) { } // the renderer went away while the delay was running
        catch (JSDisconnectedException) { } // we can ignore this exception here
    }



    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;

        _disposed = true;

        if (_dotnetObj is not null)
        {
            _dotnetObj.Dispose();
            _dotnetObj = null;

            try
            {
                await _js.BitCalloutClearCallout(_submenuId);
                await _js.BitMenuButtonsDisposeSubmenu(MenuButton._Id, _submenuId);
            }
            catch (JSDisconnectedException) { } // we can ignore this exception here
        }

        MenuButton.SubmenuClosed(this);

        GC.SuppressFinalize(this);
    }
}
