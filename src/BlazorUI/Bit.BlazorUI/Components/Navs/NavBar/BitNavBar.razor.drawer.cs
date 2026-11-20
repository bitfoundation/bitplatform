namespace Bit.BlazorUI;

// The drawer a navbar folds its items into on the screens its DrawerBreakpoint matches: the toggle button that opens
// it, the overlay behind it, and everything the open drawer holds while it covers the page - the page it stops from
// scrolling, the focus it keeps inside itself, the Escape that closes it.
public partial class BitNavBar<TItem>
{
    // How far a finger may travel between touching the overlay and lifting off it for the touch to still be a tap,
    // in CSS pixels: about the slop the browsers give a tap before they take it for a drag.
    private const double OverlayTapSlop = 10;

    private string? _drawerId;
    private bool _isDrawer;
    private bool _screenChecked;
    private BitScreenQuery? _checkedBreakpoint;
    private bool _scrollLocked;
    private bool _focusTrapped;
    private bool _focusTaken;
    private bool _focusOriginStored;
    private bool _escapeWatched;
    private ElementReference _toggleRef;
    private ElementReference _drawerRef;
    // Where the one finger on the overlay came down, or null once the touch is no tap (see HandleOnOverlayTouchEnd).
    private (double X, double Y)? _overlayTouch;
    // Bound to a receiver that is not the component: a touch that only notes where it began has nothing to render.
    private EventCallback<TouchEventArgs> _onOverlayTouchStart;
    // The same for the key handlers of the drawer and its toggle button: the drawer hears every key pressed on the
    // items inside it, the arrows that walk the bar included, and only the Escape that closes it renders anything.
    private EventCallback<KeyboardEventArgs> _onDrawerKeyDown;
    private EventCallback<KeyboardEventArgs> _onToggleKeyDown;
    // Which Escapes a part of the drawer claimed first (see _OnEscapeVerdict), for its key handler. Made on first use,
    // so a navbar disposed before it rendered never starts watching.
    private BitEscapeClaimWatch<BitNavBar<TItem>>? _escapeWatch;



    // The scroller of the application shell the navbar was declared inside of, cascaded by BitAppShell under this name.
    // What the open drawer holds while it covers the page is whatever actually scrolls behind it, which in a shell is
    // the region the shell scrolls rather than the document.
    [CascadingParameter(Name = "BitAppShell.Container")]
    private ElementReference? AppShellContainer { get; set; }



    /// <summary>
    /// Sent by the browser as each Escape goes down inside the drawer while it claims the key, ahead of the keydown
    /// Blazor then dispatches, with whether a part of the drawer claimed it first - a control an item template renders,
    /// say - so the key handler that runs next leaves that press alone.
    /// <br />
    /// <strong>This method is intended for internal use and should not be called directly.</strong>
    /// </summary>
    [JSInvokable("OnEscapeVerdict")]
    public void _OnEscapeVerdict(bool foreign)
    {
        EscapeWatch.SetVerdict(foreign);
    }



    // The open drawer is a modal surface while it covers the page: only on the screens the navbar is folded on, only
    // while it is open, and never while the navbar is disabled, which leaves nothing of it to dismiss it with.
    private bool _IsModalDrawer => DrawerBreakpoint is not null && _isDrawer && DrawerOpen && Disabled is false;

    // Whether an Escape closes the drawer, read by the key handlers and _EscapeClaim alike. The bar of a wide screen
    // is not a surface over the page, and closing it there would flip the open state - and report it - with nothing
    // on screen to show for it.
    private bool _ClosesOnEscape => _IsModalDrawer;

    // When the key handlers act on Escape, written onto the toggle button and the drawer for Utils.claimEscape: read
    // off the very state they decide on, so a surface the navbar sits in (a dialog, a BitPanel) does not close on the
    // same press. A closed drawer, or the bar of a wide screen, leaves the key alone.
    private string? _EscapeClaim => _ClosesOnEscape ? "claim" : null;

    // The name of the toggle button and of the drawer it opens.
    private string _ToggleLabel => ToggleAriaLabel ?? BitStrings.Current.Menu;

    // The class that gives the navbar its shape. Once the browser has reported the screen it is the shape itself (the
    // drawer, or the bar of the plain navbar), so the stylesheet and everything decided in C# - the dialog role, the
    // page held from scrolling, the focus held inside - read one and the same answer: the media query that answered is
    // built from the live theme breakpoints, which a stylesheet media query has no way to read. Until then (the first
    // render, a prerendered page) it is the query itself, which the stylesheet resolves at the default breakpoints - so
    // the navbar is never without a shape while the browser has not answered yet.
    private string _DrawerShapeClass => DrawerBreakpoint is not { } query
        ? string.Empty
        : _screenChecked
            ? (_isDrawer ? "bit-nbr-dsh" : string.Empty)
            : $"bit-nbr-dq-{query.ToString().ToLowerInvariant()}";

    private BitEscapeClaimWatch<BitNavBar<TItem>> EscapeWatch => _escapeWatch ??= new(_js, this);



    private void InitDrawer()
    {
        _drawerId = $"BitNavBar-{UniqueId}-drawer";

        _onOverlayTouchStart = EventCallback.Factory.Create<TouchEventArgs>(new object(), HandleOnOverlayTouchStart);
        _onDrawerKeyDown = EventCallback.Factory.Create<KeyboardEventArgs>(new object(), HandleOnDrawerKeyDown);
        _onToggleKeyDown = EventCallback.Factory.Create<KeyboardEventArgs>(new object(), HandleOnToggleKeyDown);
    }

    // A breakpoint that changed (or went away) asks the browser again, so the shape falls back on the stylesheet's
    // until the media query has reported the new one.
    private void SyncDrawerBreakpoint()
    {
        if (DrawerBreakpoint == _checkedBreakpoint) return;

        _checkedBreakpoint = DrawerBreakpoint;
        _screenChecked = false;
        _isDrawer = false;

        ClassBuilder.Reset();
    }

    private async Task OnAfterRenderDrawer()
    {
        // Everything the open drawer holds while it covers the page is taken and handed back here rather than at the
        // moment a single parameter changes: whether it covers the page at all is decided by the screen, the open
        // state and the disabled state together, and each step does nothing unless it is the one that actually changed.
        await UpdateModalState();

        if (IsDisposed) return;

        // A drawer taken away (the DrawerBreakpoint unset) stops being watched, so the one a later breakpoint renders
        // is watched afresh.
        if (_escapeWatched && DrawerBreakpoint is null)
        {
            _escapeWatched = false;

            if (_escapeWatch is not null)
            {
                await _escapeWatch.DisposeAsync();
                _escapeWatch = null;
            }
        }

        // The drawer hears every key pressed inside it, so the browser tells it which Escapes a part of it claimed
        // first (see _OnEscapeVerdict, Utils.watchEscapeClaim).
        if (_escapeWatched is false && DrawerBreakpoint is not null && _drawerRef.Context is not null)
        {
            _escapeWatched = true;

            await EscapeWatch.WatchAsync(_drawerRef);
        }

        // The drawer that has just opened takes the focus onto itself, so its dialog role and name are announced and
        // the keyboard lands in the drawer that covers the page rather than on the page behind it. The move waits for
        // the browser to report the shape of the navbar, since a bar of a wide screen opened nothing to move into.
        var takesFocus = _IsModalDrawer;
        var tookFocus = _focusTaken;

        _focusTaken = takesFocus;

        if (takesFocus && tookFocus is false && _drawerRef.Context is not null)
        {
            await _drawerRef.FocusSafelyAsync(preventScroll: true);
        }
    }

    // Reports the screen the navbar renders on: on the screens the DrawerBreakpoint matches it is folded into its
    // drawer, on the others it is a bar. A render is only asked for when the answer actually changes.
    private async Task HandleScreenChange(bool isDrawer)
    {
        // The first report is acted on whatever it says: it is the render that follows it which the shape class is
        // swapped in.
        var reported = _screenChecked;

        _screenChecked = true;

        if (reported && _isDrawer == isDrawer) return;

        _isDrawer = isDrawer;

        // A screen that grew out of the drawer closes it, so the drawer does not open again on its own the moment the
        // screen shrinks back.
        if (isDrawer is false && DrawerOpen)
        {
            await AssignDrawerOpen(false);
        }

        ClassBuilder.Reset();

        await UpdateModalState();

        StateHasChanged();
    }

    private async Task HandleOnToggleClick()
    {
        if (Disabled) return;

        await SetDrawerOpen(DrawerOpen is false);
    }

    // The toggle button is outside the drawer, so a press on it is no part of the drawer's claim watch; it only acts on
    // the plain Escape that closes the open drawer.
    private async Task HandleOnToggleKeyDown(KeyboardEventArgs e)
    {
        if (e.IsPlainEscape() is false) return;
        if (_ClosesOnEscape is false) return;

        await SetDrawerOpen(false);
    }

    private async Task HandleOnDrawerKeyDown(KeyboardEventArgs e)
    {
        // Only the plain key, the one the drawer claims (_EscapeClaim): an Escape with a modifier is left to the
        // surface the navbar sits in.
        if (e.IsPlainEscape() is false) return;

        // One Escape does one thing: a press a part of the drawer claimed first is that part's, so it does not close
        // the drawer as well. The verdict is read once, so it never outlives the press it was given for.
        if (EscapeWatch.TakeForeign()) return;

        if (_ClosesOnEscape is false) return;

        await SetDrawerOpen(false);
    }

    private async Task HandleOnOverlayClick()
    {
        if (Disabled) return;

        await SetDrawerOpen(false);
    }

    private void HandleOnOverlayTouchStart(TouchEventArgs e)
    {
        _overlayTouch = e.Touches.Length == 1 && e.ChangedTouches.Length > 0
            ? (e.ChangedTouches[0].ClientX, e.ChangedTouches[0].ClientY)
            : null;
    }

    // Only a touch that is a tap closes the drawer, which is all that ever made a click before: one finger, lifted
    // where it came down. A touchend also ends a drag, a scroll attempt or a pinch begun on the overlay, none of which did.
    private async Task HandleOnOverlayTouchEnd(TouchEventArgs e)
    {
        if (_overlayTouch is not { } start) return;

        _overlayTouch = null;

        // A finger still down is a gesture of more than one, which no lift of one of them turns into a tap.
        if (e.Touches.Length > 0 || e.ChangedTouches.Length == 0) return;

        var end = e.ChangedTouches[0];

        if (Math.Abs(end.ClientX - start.X) > OverlayTapSlop || Math.Abs(end.ClientY - start.Y) > OverlayTapSlop) return;

        await HandleOnOverlayClick();
    }

    // Closes the open drawer once an item of it was clicked: the item navigated (or, in the manual mode, selected a
    // destination), which is what the drawer was opened for.
    private async Task CloseDrawerOnItemClick()
    {
        if (DrawerOpen is false || _isDrawer is false) return;

        await SetDrawerOpen(false);
    }

    private async Task SetDrawerOpen(bool open)
    {
        if (DrawerOpen == open) return;

        if (await AssignDrawerOpen(open) is false) return;

        ClassBuilder.Reset();

        StateHasChanged();
    }

    private async Task UpdateModalState()
    {
        if (_IsModalDrawer)
        {
            await StoreFocusOrigin();
            await LockScroll();
            await SetupFocusTrap();
        }
        else
        {
            await DisposeFocusTrap();
            await UnlockScroll();
            await RestoreFocusOrigin();
        }
    }

    private async Task SetupFocusTrap()
    {
        if (_focusTrapped || IsDisposed || IsRendered is false) return;

        _focusTrapped = true;

        try
        {
            await _js.BitUtilsSetupFocusTrap(_drawerId!);
        }
        catch (JSDisconnectedException) { } // the circuit is gone, and the drawer with it
    }

    private async Task DisposeFocusTrap()
    {
        // Only what was taken is handed back, and the hold is given up whether or not the call goes through, so the
        // drawer can never end up holding a focus it has already let go of.
        if (_focusTrapped is false) return;

        _focusTrapped = false;

        try
        {
            await _js.BitUtilsDisposeFocusTrap(_drawerId!);
        }
        catch (JSDisconnectedException) { } // the circuit is gone, and the drawer with it
    }

    private async Task StoreFocusOrigin()
    {
        if (_focusOriginStored || IsDisposed || IsRendered is false) return;

        _focusOriginStored = true;

        try
        {
            await _js.BitUtilsStoreFocus(_drawerId!, scopeId: _drawerId);
        }
        catch (JSDisconnectedException) { } // the circuit is gone, and the drawer with it
    }

    // Hands the focus back to what had it as the drawer opened - the toggle button, as a rule - once it closes. The
    // drawer stays in the page while it closes, so a focus still inside it counts as lost too.
    private async Task RestoreFocusOrigin()
    {
        if (_focusOriginStored is false) return;

        _focusOriginStored = false;

        if (IsDisposed) return;

        try
        {
            await _js.BitUtilsRestoreFocus(_drawerId!, onlyWhenLost: true, scopeId: _drawerId);
        }
        catch (JSDisconnectedException) { } // the circuit is gone, and the drawer with it
    }

    private async Task LockScroll()
    {
        if (_scrollLocked || IsDisposed || IsRendered is false) return;

        _scrollLocked = true;

        try
        {
            if (AppShellContainer.HasValue)
            {
                await _js.BitUtilsLockScroll(_drawerId!, AppShellContainer.Value);
            }
            else
            {
                await _js.BitUtilsLockScroll(_drawerId!);
            }
        }
        catch (JSDisconnectedException) { } // the circuit is gone, and the drawer with it
    }

    private async Task UnlockScroll()
    {
        if (_scrollLocked is false) return;

        _scrollLocked = false;

        try
        {
            await _js.BitUtilsUnlockScroll(_drawerId!);
        }
        catch (JSDisconnectedException) { } // the circuit is gone, and the drawer with it
    }

    // The page and the focus are handed back before the component goes: a drawer disposed while it is open would
    // otherwise leave the page held by a key nothing will ever release again.
    private async Task DisposeDrawer()
    {
        await DisposeFocusTrap();
        await UnlockScroll();

        try
        {
            if (_focusOriginStored)
            {
                _focusOriginStored = false;

                await _js.BitUtilsForgetFocus(_drawerId!);
            }
        }
        catch (JSDisconnectedException) { } // the circuit is gone, and the drawer with it

        // Disposed even when it never started, so a first render still on its way does not start it afterwards.
        if (_escapeWatch is not null)
        {
            await _escapeWatch.DisposeAsync();
        }
    }
}
