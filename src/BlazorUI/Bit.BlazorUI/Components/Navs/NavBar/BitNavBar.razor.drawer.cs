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
    // Set once a DrawerBreakpoint is taken away, so a drawer left open then is closed (see CloseDrawerOffScreen).
    private bool _drawerTakenAway;
    // Whether the browser has been asked to report the screen (see SyncScreenListener).
    private bool _screenListened;
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
    // The receiver of the reports of the screen (_OnMatchChange). Made on first use, so a navbar disposed before it
    // rendered leaves no reference behind.
    private DotNetObjectReference<BitNavBar<TItem>>? _screenDotnetObj;



    // The scroller of the application shell the navbar was declared inside of, cascaded by BitAppShell under this name.
    // What the open drawer holds while it covers the page is whatever actually scrolls behind it, which in a shell is
    // the region the shell scrolls rather than the document.
    [CascadingParameter(Name = "BitAppShell.Container")]
    private ElementReference? AppShellContainer { get; set; }

    // The theme of an enclosing BitThemeProvider, whose breakpoints the screen query of the DrawerBreakpoint is built
    // from ahead of the --bit-bp-* variables of the document, the way BitMediaQuery builds it.
    [CascadingParameter] private BitTheme? CascadingTheme { get; set; }



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

    /// <summary>
    /// Sent by the browser with whether the screen matches the <see cref="DrawerBreakpoint"/>: the first answer once
    /// it is asked, and every change of it after that (the listener of BitMediaQuery.ts reports it).
    /// <br />
    /// <strong>This method is intended for internal use and should not be called directly.</strong>
    /// </summary>
    [JSInvokable("OnMatchChange")]
    public async Task _OnMatchChange(bool isDrawer)
    {
        if (IsDisposing) return;

        try
        {
            await InvokeAsync(() => HandleScreenChange(isDrawer));
        }
        catch (Exception ex) when (ex is not ObjectDisposedException)
        {
            // This method is called from JavaScript, which reads a rejected call as the .NET side being gone and stops
            // listening for good; an exception of a handler of the page is handed to Blazor's error handling instead.
            await DispatchExceptionAsync(ex);
        }
    }



    // The open drawer is a modal surface while it covers the page: only on the screens the navbar is folded on, only
    // while it is open, and never while the navbar is disabled, which leaves nothing of it to dismiss it with.
    private bool _IsModalDrawer => DrawerBreakpoint is not null && _isDrawer && DrawerOpen && Disabled is false;

    // Whether an Escape closes the drawer, read by the key handlers and _EscapeClaim alike. The bar of a wide screen
    // is not a surface over the page, and closing it there would flip the open state - and report it - with nothing
    // on screen to show for it.
    private bool _ClosesOnEscape => _IsModalDrawer;

    // Whether the drawer is open as far as the page can see: a disabled navbar keeps the open state it was given, but
    // shows no open drawer it would leave nothing to dismiss with (see bit-nbr-dop).
    private bool _DrawerShown => DrawerOpen && Disabled is false;

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

    // A breakpoint set afresh (or taken away) asks the browser again, so the shape falls back on the stylesheet's until
    // it has reported the screen. One changed for another keeps the shape last reported: the listener is rebuilt for
    // the new query and carries its last answer over, so it only reports again when the new query answers otherwise
    // (see BitMediaQuery.ts) - a shape reset here would wait for a report that never comes.
    private void SyncDrawerBreakpoint()
    {
        if (DrawerBreakpoint == _checkedBreakpoint) return;

        var previous = _checkedBreakpoint;

        _checkedBreakpoint = DrawerBreakpoint;

        if (previous is not null && DrawerBreakpoint is not null) return;

        if (DrawerBreakpoint is null) _drawerTakenAway = true;

        _screenChecked = false;
        _isDrawer = false;

        ClassBuilder.Reset();
    }

    // A drawer that is not on screen is not open either, so it does not slide open on its own the moment a drawer shape
    // applies again: the one left open as the DrawerBreakpoint is taken away, and the one opened (bound) on a screen the
    // browser has reported to be a bar.
    private async Task CloseDrawerOffScreen()
    {
        var takenAway = _drawerTakenAway;

        _drawerTakenAway = false;

        if (DrawerOpen is false) return;

        var offScreen = DrawerBreakpoint is null ? takenAway : _screenChecked && _isDrawer is false;

        if (offScreen is false) return;

        if (await AssignDrawerOpen(false) is false) return;

        ClassBuilder.Reset();
    }

    // Asks the browser to report the screen the navbar renders on, on every render: the query is built from the live
    // breakpoints, and the script keeps its listener while the query it resolves to is the same one. A drawer taken
    // away stops listening.
    private async Task SyncScreenListener()
    {
        if (IsDisposed) return;

        if (DrawerBreakpoint is { } query)
        {
            _screenDotnetObj ??= DotNetObjectReference.Create(this);
            _screenListened = true;

            try
            {
                await _js.BitMediaQuerySetup(_drawerId!, null, null, query.ToString(), BitMediaQuery.ToScriptBreakpoints(CascadingTheme), _screenDotnetObj);
            }
            catch (JSDisconnectedException)
            {
                _screenListened = false; // the circuit is gone, and the listener was never made
            }
        }
        else if (_screenListened)
        {
            _screenListened = false;

            try
            {
                await _js.BitMediaQueryDispose(_drawerId!);
            }
            catch (JSDisconnectedException) { } // the circuit is gone, and the listener with it
        }
    }

    private async Task OnAfterRenderDrawer()
    {
        // Everything the open drawer holds while it covers the page is taken and handed back here rather than at the
        // moment a single parameter changes: whether it covers the page at all is decided by the screen, the open
        // state and the disabled state together, and each step does nothing unless it is the one that actually changed.
        await UpdateModalState();

        await SyncScreenListener();

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
        // screen shrinks back. The focus stays where it is: the items it can be on are in the bar now, and the toggle
        // button it would be handed back to is hidden there, which would drop it onto the body.
        if (isDrawer is false && DrawerOpen)
        {
            await ForgetFocusOrigin();

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

    // Each step awaits the browser, and another pass (a render, a report of the screen) can open or close the drawer
    // while it does: a closing pass then finds nothing taken yet and releases nothing, and the opening one goes on to
    // take the page for a drawer that is already closed. So the state is read again once the steps are done, and they
    // are run again for it until it holds still.
    private async Task UpdateModalState()
    {
        while (true)
        {
            var modal = _IsModalDrawer;

            if (modal)
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

            if (_IsModalDrawer == modal) return;
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

    // Lets go of the focus stored as the drawer opened, without handing it back.
    private async Task ForgetFocusOrigin()
    {
        if (_focusOriginStored is false) return;

        _focusOriginStored = false;

        try
        {
            await _js.BitUtilsForgetFocus(_drawerId!);
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
        await ForgetFocusOrigin();

        // The listener is taken down before the reference it reports to is released, so a change of the screen in
        // between cannot reach a disposed object.
        try
        {
            if (_screenListened)
            {
                _screenListened = false;

                await _js.BitMediaQueryDispose(_drawerId!);
            }
        }
        catch (JSDisconnectedException) { } // the circuit is gone, and the listener with it
        finally
        {
            _screenDotnetObj?.Dispose();
        }

        // Disposed even when it never started, so a first render still on its way does not start it afterwards.
        if (_escapeWatch is not null)
        {
            await _escapeWatch.DisposeAsync();
        }
    }
}
