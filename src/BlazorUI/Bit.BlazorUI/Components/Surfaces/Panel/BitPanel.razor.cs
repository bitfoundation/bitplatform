namespace Bit.BlazorUI;

/// <summary>
/// Panel is an overlay surface that slides in from an edge of the screen to host supplementary content -
/// a form, a filter, a set of details, a navigation menu - without taking the user away from the page
/// behind it. It can slide in from any of the four edges, be sized along the axis it slides on, dim the
/// page or leave it usable, hold the page still while it is open, and be dismissed by a click on the
/// overlay, the Escape key or a swipe of the finger.
/// </summary>
/// <remarks>
/// There are two panel components available for different purposes: BitPanel is the plain surface described
/// here, while BitProPanel (in the Bit.BlazorUI.Extras package) builds a header, a close button, a scrolling
/// body and a footer on top of it. Use BitProPanel where that chrome is what you would otherwise write by hand.
/// </remarks>
public partial class BitPanel : BitComponentBase
{
    private bool _internalIsOpen;
    private bool _contentRendered;
    private bool _focusTrapped;
    private bool _scrollLocked;
    private string? _lockedScroller;
    private string? _swipesKey;
    private string _containerId = default!;
    private MouseEventArgs? _dismissArgs;
    private DotNetObjectReference<BitPanel>? _swipesDotnetObj;



    [Inject] private IJSRuntime _js { get; set; } = default!;



    /// <summary>
    /// Lays the panel out against the nearest positioned ancestor instead of against the screen, so that
    /// the panel - and the overlay that comes with it - stay inside a container of the page rather than
    /// covering all of it.
    /// </summary>
    /// <remarks>
    /// The container it is laid out in has to establish a containing block of its own (a position other
    /// than static), and the panel is clipped by it where it hides its overflow. The panel takes no clicks
    /// away from that container while it is closed.
    /// </remarks>
    [Parameter, ResetClassBuilder]
    public bool AbsolutePosition { get; set; }

    /// <summary>
    /// Holds the page still while the panel is open, by taking the scrollbar off the element named by
    /// <see cref="ScrollerSelector"/> - the body of the document by default - and giving it back when the
    /// panel closes.
    /// </summary>
    /// <remarks>
    /// The page underneath is what would otherwise take the wheel and the touch away from the panel, and it
    /// scrolls out from under an open panel that is anchored to the screen rather than to the page.
    /// </remarks>
    [Parameter] public bool AutoToggleScroll { get; set; }

    /// <summary>
    /// Keeps a click on the overlay from dismissing the panel, for the panels whose content has to be
    /// completed or cancelled through the panel itself.
    /// </summary>
    /// <remarks>
    /// It says nothing about the Escape key, which dismisses the panel unless
    /// <see cref="NoDismissOnEscape"/> says otherwise, and nothing about the swipe gesture, which is turned
    /// off with <see cref="NoSwipe"/>.
    /// </remarks>
    [Parameter] public bool Blocking { get; set; }

    /// <summary>
    /// The content of the panel.
    /// </summary>
    [Parameter] public RenderFragment? ChildContent { get; set; }

    /// <summary>
    /// Custom CSS classes for different parts of the panel.
    /// </summary>
    [Parameter] public BitPanelClassStyles? Classes { get; set; }

    /// <summary>
    /// Alias for ChildContent.
    /// </summary>
    [Parameter] public RenderFragment? Content { get; set; }

    /// <summary>
    /// Stretches the panel to the full size of the screen along the axis it is sized on, which takes over
    /// from <see cref="Size"/> and from the cap that otherwise leaves a strip of the page showing beside it.
    /// </summary>
    [Parameter] public bool FullSize { get; set; }

    /// <summary>
    /// Reports the panel to assistive technologies as an alert dialog rather than a plain one, for the
    /// panels that carry an urgent message the user is expected to deal with before carrying on.
    /// </summary>
    [Parameter] public bool IsAlert { get; set; }

    /// <summary>
    /// Determines the openness of the panel.
    /// </summary>
    [Parameter, TwoWayBound]
    public bool IsOpen { get; set; }

    /// <summary>
    /// Keeps the content of the panel out of the page until the panel is opened for the first time, for the
    /// panels whose content is expensive enough that rendering it behind a closed panel is a cost of its own.
    /// Once rendered it stays, so whatever state the content holds survives the panel closing.
    /// </summary>
    [Parameter] public bool LazyRender { get; set; }

    /// <summary>
    /// Leaves the page its own clicks while the panel is open, by not rendering the overlay that otherwise
    /// covers it. Whatever is underneath the panel stays usable, and the panel is closed through its own
    /// content, the Escape key, a swipe or the code that opened it.
    /// </summary>
    /// <remarks>
    /// A modeless panel is not a modal dialog, so it does not report itself as one and does not keep the
    /// keyboard inside itself - the page behind it is meant to be reached. It still moves the focus into
    /// itself when it opens, unless <see cref="NoAutoFocus"/> says otherwise.
    /// </remarks>
    [Parameter] public bool Modeless { get; set; }

    /// <summary>
    /// Leaves the focus where it is when the panel opens, instead of moving it into the panel.
    /// </summary>
    /// <remarks>
    /// Moving the focus into a panel is what puts the keyboard and the screen reader where the panel is, and
    /// it is what the focus trap of a modal panel needs to have anything to hold on to, so only opt out of it
    /// for a panel that is shown alongside the work the user is doing rather than instead of it.
    /// <br />
    /// An element in the content marked with a <c>data-autofocus</c> attribute takes the focus instead of the
    /// first focusable one, for the panels whose first focusable element is not the one worth starting at.
    /// </remarks>
    [Parameter] public bool NoAutoFocus { get; set; }

    /// <summary>
    /// Keeps the Escape key from dismissing the panel, for the panels that are only meant to be closed
    /// through their own content.
    /// </summary>
    [Parameter] public bool NoDismissOnEscape { get; set; }

    /// <summary>
    /// Lets the keyboard leave the panel while it is open, instead of cycling Tab and Shift+Tab inside it.
    /// </summary>
    /// <remarks>
    /// The tab order runs on into the page behind the panel otherwise, where an overlay swallows every click
    /// that could bring the keyboard back. A <see cref="Modeless"/> panel never traps the focus, since the
    /// page behind it is meant to be reached.
    /// </remarks>
    [Parameter] public bool NoFocusTrap { get; set; }

    /// <summary>
    /// Turns off the swipe gesture that otherwise dismisses the panel when it is dragged towards the edge it
    /// slid in from.
    /// </summary>
    /// <remarks>
    /// It is what a panel hosting something that is itself dragged needs - a slider, a canvas, a table that
    /// scrolls sideways - since the gesture would otherwise be taken by the panel before it reaches them.
    /// </remarks>
    [Parameter] public bool NoSwipe { get; set; }

    /// <summary>
    /// A callback function for when the panel is dismissed.
    /// </summary>
    /// <remarks>
    /// It is called for every closing of the panel, however it happened: a click on the overlay, the Escape
    /// key, a swipe, the Close and Toggle methods, and the IsOpen parameter being set to false from the
    /// outside. The event arguments carry the click that dismissed the panel where there was one.
    /// </remarks>
    [Parameter] public EventCallback<MouseEventArgs> OnDismiss { get; set; }

    /// <summary>
    /// A callback function for when the panel is opened.
    /// </summary>
    [Parameter] public EventCallback OnOpen { get; set; }

    /// <summary>
    /// A callback function for when a click lands on the overlay of the panel.
    /// </summary>
    /// <remarks>
    /// It is called before the panel is dismissed, and it is called for a <see cref="Blocking"/> panel too,
    /// which is what a panel that wants to draw attention to itself rather than close needs.
    /// </remarks>
    [Parameter] public EventCallback<MouseEventArgs> OnOverlayClick { get; set; }

    /// <summary>
    /// The event callback for when the swipe action starts on the container of the panel.
    /// </summary>
    [Parameter] public EventCallback<decimal> OnSwipeStart { get; set; }

    /// <summary>
    /// The event callback for when the swipe action moves on the container of the panel.
    /// </summary>
    [Parameter] public EventCallback<decimal> OnSwipeMove { get; set; }

    /// <summary>
    /// The event callback for when the swipe action ends on the container of the panel.
    /// </summary>
    [Parameter] public EventCallback<decimal> OnSwipeEnd { get; set; }

    /// <summary>
    /// A callback function for when the panel opens or closes, called with the new open state.
    /// </summary>
    [Parameter] public EventCallback<bool> OnToggle { get; set; }

    /// <summary>
    /// The edge of the screen the panel slides in from. Start and End are the logical edges, so they follow
    /// the direction of the panel. It defaults to End.
    /// </summary>
    [Parameter] public BitPanelPosition? Position { get; set; }

    /// <summary>
    /// The size of the panel in pixels along the axis it slides on: the width of a panel at the start or the
    /// end of the screen, and the height of one at the top or the bottom. It is capped so that a strip of
    /// the page stays visible beside the panel, unless <see cref="FullSize"/> takes the whole screen.
    /// </summary>
    /// <remarks>
    /// Leaving it unset sizes the panel to its own content. A size that is not a pixel value - a percentage,
    /// a rem, a viewport unit - is given through the Container member of <see cref="Styles"/>.
    /// </remarks>
    [Parameter] public double? Size { get; set; }

    /// <summary>
    /// The CSS selector of the element whose scrolling is taken away while the panel is open, for
    /// <see cref="AutoToggleScroll"/>. It defaults to the body of the document.
    /// </summary>
    [Parameter] public string? ScrollerSelector { get; set; }

    /// <summary>
    /// Custom CSS styles for different parts of the panel component.
    /// </summary>
    [Parameter] public BitPanelClassStyles? Styles { get; set; }

    /// <summary>
    /// The ARIA id of the element that describes the panel, which is what a screen reader reads out after
    /// the name of the panel when it opens.
    /// </summary>
    [Parameter] public string? SubtitleAriaId { get; set; }

    /// <summary>
    /// How far the panel has to be dragged towards the edge it slid in from before it is dismissed, as a
    /// fraction of its own size (default is 0.25). Values outside of the range it can mean - greater than
    /// zero and no more than one - fall back to the default.
    /// </summary>
    [Parameter] public decimal? SwipeTrigger { get; set; }

    /// <summary>
    /// The ARIA id of the element that names the panel, which is what a screen reader reads out when the
    /// panel opens. <see cref="BitComponentBase.AriaLabel"/> names the panel where there is no such element.
    /// </summary>
    [Parameter] public string? TitleAriaId { get; set; }



    /// <summary>
    /// Opens the panel, unless it is disabled.
    /// </summary>
    public async Task Open()
    {
        if (IsEnabled is false) return;

        if (IsOpen) return;

        if (await AssignIsOpen(true) is false) return;

        StateHasChanged();
    }

    /// <summary>
    /// Closes the panel. A panel that is already closed is left alone.
    /// </summary>
    public async Task Close()
    {
        if (IsOpen is false) return;

        if (await AssignIsOpen(false) is false) return;

        StateHasChanged();
    }

    /// <summary>
    /// Opens the panel when it is closed, and closes it when it is open.
    /// </summary>
    public Task Toggle() => IsOpen ? Close() : Open();



    [JSInvokable("OnStart")]
    public async Task _OnStart(decimal startX, decimal startY)
    {
        var start = IsHorizontal ? startX : startY;
        await OnSwipeStart.InvokeAsync(start);
    }

    [JSInvokable("OnMove")]
    public async Task _OnMove(decimal diffX, decimal diffY)
    {
        var diff = IsHorizontal ? diffX : diffY;
        await OnSwipeMove.InvokeAsync(diff);
    }

    [JSInvokable("OnEnd")]
    public async Task _OnEnd(decimal diffX, decimal diffY)
    {
        var diff = IsHorizontal ? diffX : diffY;
        await OnSwipeEnd.InvokeAsync(diff);
    }

    [JSInvokable("OnClose")]
    public async Task _OnClose()
    {
        await ClosePanel(new());

        await InvokeAsync(StateHasChanged);
    }



    protected override string RootElementClass => "bit-pnl";

    protected override void RegisterCssClasses()
    {
        ClassBuilder.Register(() => Classes?.Root);

        ClassBuilder.Register(() => AbsolutePosition ? "bit-pnl-abs" : string.Empty);
    }

    protected override void RegisterCssStyles()
    {
        StyleBuilder.Register(() => Styles?.Root);
    }

    protected override void OnInitialized()
    {
        _containerId = $"BitPanel-{UniqueId}-container";

        _contentRendered = IsOpen;

        base.OnInitialized();
    }

    protected override async Task OnParametersSetAsync()
    {
        await base.OnParametersSetAsync();

        // A lazy panel keeps its content out of the page until it is first opened, and keeps it from then on.
        if (IsOpen)
        {
            _contentRendered = true;
        }

        if (IsRendered is false) return;

        // The gesture is registered against the panel with the geometry it was set up with, and every input
        // of that geometry is a parameter that can change at runtime - the direction is even cascaded in - so
        // it is registered again whenever any of them does.
        if (GetSwipesKey() != _swipesKey)
        {
            await DisposeSwipes();
            await SetupSwipes();
        }

        if (IsOpen is false) return;

        // The focus trap and the scroll lock are registered against the open panel, so turning either of them
        // on or off while the panel is open has to reach the already registered one rather than wait for the
        // next time the panel opens.
        if (ShouldTrapFocus)
        {
            await SetupFocusTrap();
        }
        else
        {
            await DisposeFocusTrap();
        }

        if (AutoToggleScroll)
        {
            await SetupScrollLock();
        }
        else
        {
            await DisposeScrollLock();
        }
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        await base.OnAfterRenderAsync(firstRender);

        if (firstRender)
        {
            await SetupSwipes();
        }

        if (_internalIsOpen == IsOpen) return;

        _internalIsOpen = IsOpen;

        // Every way the panel can be opened or closed - the overlay, the Escape key, a swipe, the Open, Close
        // and Toggle methods, and the IsOpen parameter being set from the outside - ends up here, on the
        // render that has already put the new state in the page. So the keyboard, the page scrolling and the
        // consumer's callbacks are all dealt with once, in one place, and against a DOM that is up to date.
        if (IsOpen)
        {
            // Remembered before anything else is done, so that what is captured is the element the focus was
            // on when the panel opened rather than wherever it may have gone while the rest of this ran.
            await CaptureFocusOrigin();

            await SetupScrollLock();

            await SetupFocusTrap();

            await FocusPanel();

            await OnToggle.InvokeAsync(true);

            await OnOpen.InvokeAsync();
        }
        else
        {
            await DisposeFocusTrap();

            await DisposeScrollLock();

            await RestoreFocusOrigin();

            var args = _dismissArgs ?? new();
            _dismissArgs = null;

            await OnToggle.InvokeAsync(false);

            await OnDismiss.InvokeAsync(args);
        }
    }



    private async Task ClosePanel(MouseEventArgs e)
    {
        if (IsEnabled is false || IsOpen is false) return;

        // Kept for the dismissal callback, which is invoked from the render that closes the panel rather than
        // from here, so that it is called for every way the panel can be closed and never before the page
        // shows it closed.
        _dismissArgs = e;

        if (await AssignIsOpen(false) is false)
        {
            _dismissArgs = null;
        }
    }

    private async Task HandleOnOverlayClick(MouseEventArgs e)
    {
        if (IsEnabled is false || IsOpen is false) return;

        await OnOverlayClick.InvokeAsync(e);

        if (Blocking) return;

        await ClosePanel(e);
    }

    private async Task HandleOnKeyDown(KeyboardEventArgs e)
    {
        if (DismissesOnEscape is false) return;

        if (e.Key is not "Escape") return;

        await ClosePanel(new());

        StateHasChanged();
    }

    // Whether the Escape key is the panel's to act on, which is also whether it stops at the panel rather
    // than carrying on up to whatever the panel was opened from.
    private bool DismissesOnEscape => IsOpen && IsEnabled && NoDismissOnEscape is false;

    // Whether the panel slides in along the horizontal axis, which is what decides both the axis the swipe
    // gesture is locked to and which of the two coordinates the swipe callbacks are given.
    private bool IsHorizontal => (Position ?? BitPanelPosition.End) is BitPanelPosition.Start or BitPanelPosition.End;

    // Whether the content of the panel is in the page. A lazy panel leaves it out until the panel is opened
    // for the first time, and keeps it from then on, so the state the content holds survives a close.
    private bool ContentRendered => LazyRender is false || _contentRendered;

    // A modeless panel leaves the page usable, so the keyboard is meant to reach it: trapping the focus in a
    // panel the user can still click out of would leave them unable to tab back to what they clicked on.
    private bool ShouldTrapFocus => NoFocusTrap is false && Modeless is false;

    private string GetRole() => IsAlert ? "alertdialog" : "dialog";

    // Only a panel that actually holds the page back is a modal one, and only while it is open: a panel that
    // reports itself as modal while the page behind it is still usable is telling a screen reader something
    // the user can prove wrong by clicking.
    private string? GetAriaModal() => (IsOpen && Modeless is false) ? "true" : null;

    private string GetOverlayCssClasses()
    {
        List<string> classes = ["bit-pnl-ovl"];

        if (IsOpen)
        {
            classes.Add("bit-pnl-ovl-opn");
        }

        if (Classes?.Overlay is string overlayClass && overlayClass.HasValue())
        {
            classes.Add(overlayClass);
        }

        return string.Join(' ', classes);
    }

    private string GetContainerCssStyles()
    {
        List<string> styles = [];

        // A full-size panel is sized by its own class, so a size given as well is left out rather than
        // fought with.
        if (Size is not null && FullSize is false)
        {
            var prop = IsHorizontal ? "width" : "height";
            styles.Add(FormattableString.Invariant($"{prop}:{Size}px"));
        }

        if (Styles?.Container is string containerStyle && containerStyle.HasValue())
        {
            styles.Add(containerStyle);
        }

        return string.Join(';', styles);
    }

    private string GetContainerCssClasses()
    {
        List<string> classes = ["bit-pnl-cnt"];

        classes.Add(Position switch
        {
            BitPanelPosition.Start => "bit-pnl-start",
            BitPanelPosition.End => "bit-pnl-end",
            BitPanelPosition.Top => "bit-pnl-top",
            BitPanelPosition.Bottom => "bit-pnl-bottom",
            _ => "bit-pnl-end"
        });

        if (FullSize)
        {
            classes.Add("bit-pnl-fsz");
        }

        if (IsOpen)
        {
            classes.Add("bit-pnl-opn");
        }

        if (Classes?.Container is string containerClass && containerClass.HasValue())
        {
            classes.Add(containerClass);
        }

        return string.Join(' ', classes);
    }



    // The geometry the swipe gesture was registered with, or null when there is no gesture to register.
    private string? GetSwipesKey()
    {
        return NoSwipe ? null : $"{Position ?? BitPanelPosition.End}|{Dir}|{GetSwipeTrigger()}";
    }

    // A trigger outside of the range it can mean - a fraction of the size of the panel, so greater than zero
    // and no more than one - would dismiss the panel on the first pixel of a drag, or never at all.
    private decimal GetSwipeTrigger()
    {
        return SwipeTrigger is > 0 and <= 1 ? SwipeTrigger.Value : 0.25m;
    }

    private async Task SetupSwipes()
    {
        if (NoSwipe || IsDisposed) return;

        _swipesKey = GetSwipesKey();

        var position = Position ?? BitPanelPosition.End;

        // Swipes.dispose releases the .NET reference it was handed, so a re-registration gets one of its own
        // rather than reusing a reference that has already been released.
        _swipesDotnetObj = DotNetObjectReference.Create(this);

        try
        {
            await _js.BitSwipesSetup(
                id: _containerId,
                trigger: GetSwipeTrigger(),
                position: position,
                isRtl: Dir is BitDir.Rtl,
                // The axis the panel is swiped away along is the one it slid in on, and the lock is what takes
                // that axis from the page: a top or bottom panel dragged with the wrong lock follows the finger
                // while the page scrolls out from under it at the same time.
                orientationLock: IsHorizontal ? BitSwipeOrientation.Horizontal : BitSwipeOrientation.Vertical,
                dotnetObj: _swipesDotnetObj,
                isResponsive: false);
        }
        catch (JSDisconnectedException) { } // we can ignore this exception here
    }

    private async Task DisposeSwipes()
    {
        if (_swipesKey is null) return;

        _swipesKey = null;

        try
        {
            await _js.BitSwipesDispose(_containerId);
        }
        catch (JSDisconnectedException) { } // we can ignore this exception here

        // Swipes.setup can bail out before it registers anything, leaving nothing for Swipes.dispose to
        // release, so the reference is also released here (disposing it is idempotent).
        _swipesDotnetObj?.Dispose();
        _swipesDotnetObj = null;
    }

    private async Task SetupFocusTrap()
    {
        if (ShouldTrapFocus is false || _focusTrapped || IsDisposed || IsRendered is false) return;

        _focusTrapped = true;

        try
        {
            await _js.BitUtilsSetupFocusTrap(_containerId);
        }
        catch (JSDisconnectedException) { } // we can ignore this exception here
    }

    private async Task DisposeFocusTrap()
    {
        if (_focusTrapped is false) return;

        _focusTrapped = false;

        try
        {
            await _js.BitUtilsDisposeFocusTrap(_containerId);
        }
        catch (JSDisconnectedException) { } // we can ignore this exception here
    }

    private async Task CaptureFocusOrigin()
    {
        if (NoAutoFocus || IsDisposed || IsRendered is false) return;

        try
        {
            await _js.BitUtilsCaptureFocusOrigin(_containerId);
        }
        catch (JSDisconnectedException) { } // we can ignore this exception here
    }

    private async Task RestoreFocusOrigin()
    {
        if (NoAutoFocus || IsDisposed) return;

        try
        {
            await _js.BitUtilsRestoreFocusOrigin(_containerId);
        }
        catch (JSDisconnectedException) { } // we can ignore this exception here
    }

    // A panel that reports itself as a modal dialog has to hold the focus to trap it: leaving it outside
    // would let the very first Tab out of the panel, since the trap only ever sees the keys pressed inside.
    private async Task FocusPanel()
    {
        if (NoAutoFocus || IsDisposed || IsRendered is false) return;

        try
        {
            await _js.BitUtilsFocusFirstElement(_containerId);
        }
        catch (JSDisconnectedException) { } // we can ignore this exception here
    }

    private async Task SetupScrollLock()
    {
        if (AutoToggleScroll is false || _scrollLocked || IsDisposed || IsRendered is false) return;

        _scrollLocked = true;
        // The selector the scrollbar was taken from, so that it is given back to the same element even when
        // the parameter has changed in the meantime.
        _lockedScroller = ScrollerSelector ?? "body";

        try
        {
            await _js.BitUtilsToggleOverflow(_lockedScroller, true);
        }
        catch (JSDisconnectedException) { } // we can ignore this exception here
    }

    private async Task DisposeScrollLock()
    {
        if (_scrollLocked is false) return;

        _scrollLocked = false;
        var scroller = _lockedScroller ?? "body";
        _lockedScroller = null;

        try
        {
            await _js.BitUtilsToggleOverflow(scroller, false);
        }
        catch (JSDisconnectedException) { } // we can ignore this exception here
    }



    protected override async ValueTask DisposeAsync(bool disposing)
    {
        if (IsDisposed || disposing is false) return;

        // A panel taken off the page while it was open would otherwise leave the page without its scrollbar
        // and with a focus trap registered on an element that no longer exists.
        await DisposeScrollLock();

        await DisposeFocusTrap();

        try
        {
            await _js.BitUtilsDisposeFocusOrigin(_containerId);
        }
        catch (JSDisconnectedException) { } // we can ignore this exception here

        await DisposeSwipes();

        await base.DisposeAsync(disposing);
    }
}
