namespace Bit.BlazorUI;

/// <summary>
/// A callout is an anchored tip that can be used to teach people or guide them through the app without
/// blocking them. It hosts any content next to an anchor of its own or an element elsewhere on the page,
/// flips to the side with the most room, can be sized in every direction, points at its anchor with an
/// optional arrow, and closes on an outside click or the Escape key.
/// </summary>
public partial class BitCallout : BitComponentBase
{
    private string _anchorId = default!;
    private string _arrowId = default!;
    private string _contentId = default!;
    private string _overlayId = default!;
    private bool _openOnFirstRender;
    private bool _selfDrivenIsOpen;
    private bool _focusTrapped;
    private bool _hoverInside;
    private bool? _isHoverDevice;
    private string? _swipesKey;
    private CancellationTokenSource? _hoverCts;
    private DotNetObjectReference<BitCallout>? _dotnetObj;
    private DotNetObjectReference<BitCallout>? _swipesDotnetObj;



    [Inject] private IJSRuntime _js { get; set; } = default!;



    /// <summary>
    /// The content of the anchor element of the callout.
    /// </summary>
    /// <remarks>
    /// The anchor is rendered as a plain container around the content given here, so that content should
    /// hold the focusable element the user activates - a button, most of the time. The container carries
    /// the aria-haspopup, aria-controls and aria-expanded relationship of the callout, and a click
    /// anywhere in it toggles the callout.
    /// </remarks>
    [Parameter] public RenderFragment? Anchor { get; set; }

    /// <summary>
    /// The setter function for element reference to the external anchor element.
    /// </summary>
    [Parameter] public Func<ElementReference>? AnchorEl { get; set; }

    /// <summary>
    /// The id of the external anchor element.
    /// </summary>
    [Parameter] public string? AnchorId { get; set; }

    /// <summary>
    /// Closes the callout as soon as a click lands anywhere inside it, which is what an action list is
    /// expected to do: picking an item completes the interaction. It is off by default, since a callout
    /// hosting a form or a filter panel is meant to stay open while it is being used.
    /// </summary>
    [Parameter] public bool AutoClose { get; set; }

    /// <summary>
    /// Moves the focus into the callout as soon as it opens, to its first focusable element,
    /// or to the callout itself when it holds none.
    /// </summary>
    [Parameter] public bool AutoFocus { get; set; }

    /// <summary>
    /// The color kind of the background of the callout.
    /// </summary>
    [Parameter] public BitColorKind? Background { get; set; }

    /// <summary>
    /// The color kind of the border of the callout.
    /// </summary>
    [Parameter] public BitColorKind? Border { get; set; }

    /// <summary>
    /// The content of the callout.
    /// </summary>
    [Parameter] public RenderFragment? ChildContent { get; set; }

    /// <summary>
    /// Custom CSS classes for different parts of the callout.
    /// </summary>
    [Parameter] public BitCalloutClassStyles? Classes { get; set; }

    /// <summary>
    /// Alias for ChildContent.
    /// </summary>
    [Parameter] public RenderFragment? Content { get; set; }

    /// <summary>
    /// The initial opening state of the callout in the uncontrolled mode, which is when the IsOpen
    /// parameter is not set.
    /// </summary>
    [Parameter] public bool? DefaultIsOpen { get; set; }

    /// <summary>
    /// Determines the allowed directions in which the callout should decide to be opened.
    /// </summary>
    [Parameter] public BitDropDirection? Direction { get; set; }

    /// <summary>
    /// Forces the callout to preserve its component's original width.
    /// </summary>
    [Parameter] public bool FixedCalloutWidth { get; set; }

    /// <summary>
    /// The id of the footer element that renders at the end of the scrolling container of the callout content.
    /// </summary>
    [Parameter] public string? FooterId { get; set; }

    /// <summary>
    /// The distance in pixels between the anchor and the callout. It defaults to zero, which tucks the
    /// callout against its anchor, and applies to whichever side the callout ends up being placed on.
    /// </summary>
    [Parameter] public int Gap { get; set; }

    /// <summary>
    /// The id of the header element that renders at the top of the scrolling container of the callout content.
    /// </summary>
    [Parameter] public string? HeaderId { get; set; }

    /// <summary>
    /// The delay in milliseconds before the callout closes once the pointer leaves the callout and its
    /// anchor in the <see cref="OpenOnHover"/> mode. It bridges the gap between the two, so moving the
    /// pointer from one to the other does not close what the pointer is on its way to. Defaults to 150.
    /// </summary>
    [Parameter] public int HoverCloseDelay { get; set; } = 150;

    /// <summary>
    /// The delay in milliseconds before the callout opens once the pointer enters the anchor in the
    /// <see cref="OpenOnHover"/> mode, so that passing over it on the way somewhere else does not open
    /// the callout. Defaults to 0, which opens it as soon as the pointer arrives.
    /// </summary>
    [Parameter] public int HoverOpenDelay { get; set; }

    /// <summary>
    /// Determines the opening state of the callout.
    /// </summary>
    [Parameter]
    [CallOnSet(nameof(OnSetIsOpen))]
    [ResetClassBuilder, ResetStyleBuilder, TwoWayBound]
    public bool IsOpen { get; set; }

    /// <summary>
    /// The maximum height of the callout as a CSS value (e.g. "20rem"), beyond which its content scrolls.
    /// It takes over from the automatic cap that otherwise keeps the callout within the room the viewport
    /// leaves, so it should stay within what the shortest screen the callout is used on can show.
    /// </summary>
    [Parameter] public string? MaxHeight { get; set; }

    /// <summary>
    /// The maximum width of the callout as a CSS value (e.g. "20rem"), beyond which its content wraps.
    /// </summary>
    [Parameter] public string? MaxWidth { get; set; }

    /// <summary>
    /// The max window width to consider when calculating the position of the callout before opening.
    /// </summary>
    [Parameter] public int? MaxWindowWidth { get; set; }

    /// <summary>
    /// The minimum width of the callout as a CSS value (e.g. "20rem"), so that a narrow content does not
    /// end up in a cramped callout.
    /// </summary>
    [Parameter] public string? MinWidth { get; set; }

    /// <summary>
    /// Dims the page behind the callout, so that the callout reads as the only thing in play. The overlay
    /// still dismisses the callout on a click unless <see cref="NoDismissOnOutsideClick"/> says otherwise.
    /// </summary>
    [Parameter] public bool Modal { get; set; }

    /// <summary>
    /// Keeps the Escape key from dismissing the callout, for the callouts that are only meant to be closed
    /// through their own content.
    /// </summary>
    [Parameter] public bool NoDismissOnEscape { get; set; }

    /// <summary>
    /// Keeps the callout open when a click lands outside of it, and when the page is scrolled or resized
    /// under it - the callout is re-anchored to its anchor instead of being dismissed. Such a callout is
    /// closed programmatically, by its own content, or by another callout opening.
    /// </summary>
    [Parameter] public bool NoDismissOnOutsideClick { get; set; }

    /// <summary>
    /// Removes the box-shadow from the callout.
    /// </summary>
    [Parameter] public bool NoShadow { get; set; }

    /// <summary>
    /// The callback that is called when the callout is dismissed.
    /// </summary>
    [Parameter] public EventCallback OnDismiss { get; set; }

    /// <summary>
    /// The callback that is called when the callout is opened.
    /// </summary>
    [Parameter] public EventCallback OnOpen { get; set; }

    /// <summary>
    /// The callback that is called when the callout opens or closes.
    /// </summary>
    [Parameter] public EventCallback<bool> OnToggle { get; set; }

    /// <summary>
    /// Opens the callout when the pointer enters the anchor and closes it when the pointer leaves both the
    /// anchor and the callout, which is what a hover card is expected to do. The anchor keeps toggling the
    /// callout on a click, so the keyboard and the touch screens - where hovering does not exist and this
    /// mode turns itself off - are left with a way to reach it.
    /// </summary>
    [Parameter] public bool OpenOnHover { get; set; }

    /// <summary>
    /// The edge of the screen the responsive panel slides in from, for a <see cref="ResponsiveMode"/> of
    /// Panel. It defaults to End.
    /// </summary>
    [Parameter] public BitPanelPosition? PanelPosition { get; set; }

    /// <summary>
    /// Configures the responsive mode of the callout for the small screens.
    /// </summary>
    [Parameter] public BitResponsiveMode? ResponsiveMode { get; set; }

    /// <summary>
    /// The ARIA role of the callout. It defaults to dialog for a callout that traps the focus, and to
    /// nothing for the others, which leaves the callout as the plain group of content it is.
    /// </summary>
    [Parameter] public string? Role { get; set; }

    /// <summary>
    /// The id of the element which needs to be scrollable in the content of the callout.
    /// </summary>
    [Parameter] public string? ScrollContainerId { get; set; }

    /// <summary>
    /// The vertical offset of the scroll container to consider in the positioning and height calculation of the callout.
    /// </summary>
    [Parameter] public int? ScrollOffset { get; set; }

    /// <summary>
    /// Forces the callout to set its content container width while opening based on the available space and actual content.
    /// </summary>
    [Parameter] public bool SetCalloutWidth { get; set; }

    /// <summary>
    /// Draws an arrow on the edge of the callout that faces the anchor, pointing at it. The arrow follows
    /// the callout wherever it is placed and is left out on the screens where a responsive callout becomes
    /// a panel, which is sized against the screen rather than placed against the anchor.
    /// </summary>
    [Parameter] public bool ShowArrow { get; set; }

    /// <summary>
    /// The side of the anchor the callout is placed on when there is room for it there. It is a preference
    /// rather than a demand: a callout that does not fit on the side asked for is placed on the opposite
    /// one, and when neither has room the placement falls back to <see cref="Direction"/>, which weighs
    /// every side it allows. Leaving it unset leaves the choice to Direction alone.
    /// </summary>
    [Parameter] public BitCalloutSide? Side { get; set; }

    /// <summary>
    /// Custom CSS styles for different parts of the callout.
    /// </summary>
    [Parameter] public BitCalloutClassStyles? Styles { get; set; }

    /// <summary>
    /// Keeps the keyboard inside the callout while it is open: the focus moves into it as it opens, Tab and
    /// Shift+Tab cycle within it instead of running on into the page behind it, and the callout reports
    /// itself as a modal dialog to the screen readers. It is what the callouts that host a form or a filter
    /// panel need, and it implies <see cref="AutoFocus"/>.
    /// </summary>
    [Parameter] public bool TrapFocus { get; set; }

    /// <summary>
    /// The width of the callout as a CSS value (e.g. "20rem"). By default the callout is only as wide as
    /// its content needs. <see cref="SetCalloutWidth"/> and <see cref="FixedCalloutWidth"/> are applied
    /// after the callout is measured, so they take precedence over it.
    /// </summary>
    [Parameter] public string? Width { get; set; }



    /// <summary>
    /// Opens the callout programmatically, unless it is disabled.
    /// </summary>
    public async Task Open()
    {
        await OpenCallout();

        await InvokeAsync(StateHasChanged);
    }

    /// <summary>
    /// Closes the callout programmatically.
    /// </summary>
    public async Task Close()
    {
        // A callout that is already closed has nothing to close, and going through with it would reach
        // the JS side to reposition a callout that is not shown.
        if (IsOpen)
        {
            await CloseCallout();
        }

        await InvokeAsync(StateHasChanged);
    }

    /// <summary>
    /// Toggles the callout to open/close it.
    /// </summary>
    public async Task Toggle()
    {
        if (IsOpen)
        {
            await CloseCallout();
        }
        else
        {
            await OpenCallout();
        }

        await InvokeAsync(StateHasChanged);
    }



    [JSInvokable("CloseCallout")]
    public async Task CloseCalloutBeforeAnotherCalloutIsOpened()
    {
        // The callout has already been hidden by the JS side, which is why nothing is toggled here: the
        // state is all that is left to correct, and going back through the positioning code would only
        // hide a callout that is already hidden - and restore one that is already back where it came
        // from. Assigning the state is what would otherwise take that path, so it is suppressed for it.
        // The focus is deliberately left where it is: whatever took over from this callout is about to
        // take it.
        await DisposeFocusTrap();

        _selfDrivenIsOpen = true;
        try
        {
            await DismissCallout();
        }
        finally
        {
            _selfDrivenIsOpen = false;
        }

        StateHasChanged();
    }

    [JSInvokable("OnStart")]
    public Task _OnStart(decimal startX, decimal startY) => Task.CompletedTask;

    [JSInvokable("OnMove")]
    public Task _OnMove(decimal diffX, decimal diffY) => Task.CompletedTask;

    [JSInvokable("OnEnd")]
    public Task _OnEnd(decimal diffX, decimal diffY) => Task.CompletedTask;

    [JSInvokable("OnClose")]
    public async Task _OnClose()
    {
        await CloseCallout();

        await InvokeAsync(StateHasChanged);
    }



    protected override string RootElementClass => "bit-clo";

    protected override void RegisterCssClasses()
    {
        ClassBuilder.Register(() => Classes?.Root);

        ClassBuilder.Register(() => IsOpen ? "bit-clo-opn" : string.Empty);

        ClassBuilder.Register(() => IsOpen ? Classes?.Opened : string.Empty);
    }

    protected override void RegisterCssStyles()
    {
        StyleBuilder.Register(() => Styles?.Root);

        StyleBuilder.Register(() => IsOpen ? Styles?.Opened : string.Empty);
    }

    protected override void OnInitialized()
    {
        _anchorId = $"BitCallout-{UniqueId}-anchor";
        _arrowId = $"BitCallout-{UniqueId}-arrow";
        _contentId = $"BitCallout-{UniqueId}-content";
        _overlayId = $"BitCallout-{UniqueId}-overlay";

        // The uncontrolled starting state. The callout itself can only be shown once the DOM exists,
        // so the actual opening is deferred to the first render like an initially set IsOpen is.
        if (IsOpenHasBeenSet is false && DefaultIsOpen.HasValue)
        {
            IsOpen = DefaultIsOpen.Value;
        }

        _openOnFirstRender = IsOpen;

        base.OnInitialized();
    }

    protected override async Task OnParametersSetAsync()
    {
        await base.OnParametersSetAsync();

        await CloseWhenUnavailable();

        // The swipe gestures are registered against the callout with the geometry they were set up with,
        // and all of the inputs of that geometry are parameters that can change at runtime (the responsive
        // mode itself can be bound to a media query), so re-register whenever any of them does.
        if (IsRendered && GetSwipesKey() != _swipesKey)
        {
            await DisposeSwipes();
            await SetupSwipes();
        }

        // The focus trap is registered against the open callout, so turning it on or off while the callout
        // is open has to reach the already registered one rather than wait for the next time it opens.
        if (IsRendered && IsOpen)
        {
            if (TrapFocus)
            {
                await SetupFocusTrap();
            }
            else
            {
                await DisposeFocusTrap();
            }
        }
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        await base.OnAfterRenderAsync(firstRender);

        // Whether the pointer of the device can hover at all decides both whether the hover mode applies
        // and whether the overlay may stop taking the clicks, so it is resolved before the callout is
        // interacted with rather than on the first hover, and only for the callouts that ask for it.
        if (OpenOnHover && _isHoverDevice is null)
        {
            _isHoverDevice = await GetIsHoverDevice();

            StateHasChanged();
        }

        if (firstRender is false) return;

        _dotnetObj = DotNetObjectReference.Create(this);

        await SetupSwipes();

        // An IsOpen (or DefaultIsOpen) that starts out true reaches OnSetIsOpen before the first render,
        // when neither the callout element nor the .NET object reference the JS side needs exist yet.
        if (_openOnFirstRender)
        {
            _openOnFirstRender = false;

            await ToggleCallout();

            await SetupFocusTrap();

            await FocusCalloutIfNeeded();

            await OnToggle.InvokeAsync(true);

            await OnOpen.InvokeAsync();
        }
    }



    private async Task HandleOnAnchorClick()
    {
        if (IsEnabled is false) return;

        // A click on the anchor while the callout is open usually lands on the overlay above it, but it
        // still arrives here when an ancestor stacking context lifts the anchor over the overlay, and it
        // always does in the hover mode, where the overlay takes no pointer events. Toggling is what an
        // anchor is expected to do - except for the pointer that opened the callout by hovering and is
        // still on the anchor: closing there would take away what the user has only just been shown, and
        // moving the pointer off closes it anyway.
        if (IsOpen is false)
        {
            await OpenCallout();
        }
        else if (HoverDriven is false || _hoverInside is false)
        {
            await CloseCallout();
        }
    }

    private async Task HandleOnOverlayClick()
    {
        if (IsEnabled is false || IsOpen is false) return;

        if (NoDismissOnOutsideClick) return;

        await CloseCallout();
    }

    private async Task HandleOnCalloutClick()
    {
        if (AutoClose is false || IsEnabled is false || IsOpen is false) return;

        await CloseCallout();

        // The close runs on the callout's own event, which does not re-render the anchor, so refresh the
        // open-state classes and aria-expanded here.
        StateHasChanged();
    }

    private async Task HandleOnCalloutKeyDown(KeyboardEventArgs e)
    {
        if (IsEnabled is false || IsOpen is false) return;

        if (e.Key is not "Escape" || NoDismissOnEscape) return;

        // The focus is inside the callout, so closing it hands the focus back to the anchor on its own.
        await CloseCallout();

        // The close runs on the callout's own event, which does not re-render the anchor, so refresh the
        // open-state classes and aria-expanded here.
        StateHasChanged();
    }

    private async Task HandleOnMouseEnter()
    {
        if (HoverDriven is false) return;

        _hoverInside = true;

        // Whichever of the two is pending: entering the callout cancels the close the pointer leaving the
        // anchor scheduled, and coming back to the anchor cancels the close leaving the callout scheduled.
        CancelHover();

        if (IsEnabled is false || IsOpen) return;

        if (await DelayHover(HoverOpenDelay) is false) return;

        await OpenCallout();

        StateHasChanged();
    }

    private async Task HandleOnMouseLeave()
    {
        if (HoverDriven is false) return;

        _hoverInside = false;

        CancelHover();

        if (IsEnabled is false || IsOpen is false) return;

        if (await DelayHover(HoverCloseDelay) is false) return;

        // The pointer came back before the delay was up, onto the anchor or into the callout.
        if (_hoverInside) return;

        await CloseCallout();

        StateHasChanged();
    }

    private async Task OpenCallout()
    {
        // A callout the user cannot reach must not be opened by the Open and Toggle methods either, since
        // it would then hang over the page with a disabled anchor under it. An IsOpen the parent sets
        // itself is left alone: the state is the parent's to own there.
        if (IsOpen || IsEnabled is false) return;

        // Assigning IsOpen runs OnSetIsOpen, which is the entry point for the open state changing from the
        // outside and toggles the callout on its own. Here the toggling is done below instead, once the
        // assignment is known to have gone through, so it is suppressed for the assignment itself.
        _selfDrivenIsOpen = true;
        try
        {
            if (await AssignIsOpen(true) is false) return;
        }
        finally
        {
            _selfDrivenIsOpen = false;
        }

        // Before the first render there is no callout element to show, only the state to record.
        if (IsRendered is false)
        {
            _openOnFirstRender = true;
            return;
        }

        await ToggleCallout();

        await SetupFocusTrap();

        await FocusCalloutIfNeeded();

        await OnToggle.InvokeAsync(true);

        await OnOpen.InvokeAsync();
    }

    private async Task CloseCallout()
    {
        var wasOpen = IsOpen;

        // Whether the focus is the callout's to hand back has to be known before the callout is hidden,
        // since hiding the element the focus sits in is what drops the focus to the body.
        var restoreFocus = wasOpen && await CalloutContainsFocus();

        _selfDrivenIsOpen = true;
        try
        {
            await DismissCallout();
        }
        finally
        {
            _selfDrivenIsOpen = false;
        }

        // An IsOpen the parent holds at true without a change callback stays open: toggling the callout
        // here would only replay the entry animation of a callout that is not going anywhere.
        if (wasOpen && IsOpen) return;

        await DisposeFocusTrap();

        await ToggleCallout();

        // The element the focus was on is gone with the callout, which would leave the focus on the body
        // and the keyboard back at the top of the page, so it goes back to the anchor it came from.
        if (restoreFocus)
        {
            await FocusAnchor();
        }
    }

    // A callout that is turned off while it is open would leave it hanging over the page with a disabled
    // anchor under it, and in the hover mode it would be stuck there: a disabled root takes no pointer
    // events, so the pointer leaving it never closes it again.
    private async Task CloseWhenUnavailable()
    {
        if (IsOpen is false || IsEnabled) return;

        if (IsRendered)
        {
            await CloseCallout();
            return;
        }

        // Before the first render there is no callout to hide, only the state to correct.
        _openOnFirstRender = false;

        _selfDrivenIsOpen = true;
        try
        {
            await AssignIsOpen(false);
        }
        finally
        {
            _selfDrivenIsOpen = false;
        }
    }

    private async Task DismissCallout()
    {
        // AssignIsOpen reports success for a value it did not have to change, so the already-closed case
        // is filtered out here to keep the callbacks from firing for a dismissal that never happened.
        if (IsOpen is false) return;

        if (await AssignIsOpen(false) is false) return;

        await OnToggle.InvokeAsync(false);

        await OnDismiss.InvokeAsync();
    }

    private async Task ToggleCallout()
    {
        if (IsDisposed) return;

        // The reference is created on the first render, so before it there is nothing to position either.
        if (_dotnetObj is null) return;

        var id = Anchor is not null ? _anchorId : AnchorId ?? _Id;

        try
        {
            await _js.BitCalloutToggleCallout(
                dotnetObj: _dotnetObj,
                componentId: id,
                component: AnchorEl is null ? null : AnchorEl(),
                calloutId: _contentId,
                callout: null,
                overlayId: _overlayId,
                isCalloutOpen: IsOpen,
                responsiveMode: ResponsiveMode ?? BitResponsiveMode.None,
                dropDirection: Direction ?? BitDropDirection.TopAndBottom,
                isRtl: Dir is BitDir.Rtl,
                // Whatever is named as the scrollable part of the content is what the positioning code caps
                // to the room the viewport leaves. With nothing named, the callout itself takes that role,
                // so that content taller than the screen scrolls inside the callout instead of running off
                // the bottom of it, where a fixed-positioned element is out of reach of the page's own
                // scrolling.
                scrollContainerId: ScrollContainerId.HasValue() ? ScrollContainerId! : (FitsToViewport ? _contentId : ""),
                scrollOffset: ScrollOffset ?? 0,
                headerId: HeaderId ?? "",
                footerId: FooterId ?? "",
                setCalloutWidth: SetCalloutWidth,
                fixedCalloutWidth: FixedCalloutWidth,
                maxWindowWidth: MaxWindowWidth ?? 0,
                maxHeight: 0,
                arrowId: ShowArrow ? _arrowId : "",
                gap: Gap,
                noDismiss: NoDismissOnOutsideClick,
                preferredSide: Side switch
                {
                    BitCalloutSide.Top => "top",
                    BitCalloutSide.Bottom => "bottom",
                    BitCalloutSide.Start => "start",
                    BitCalloutSide.End => "end",
                    _ => ""
                });
        }
        catch (JSDisconnectedException) { } // we can ignore this exception here
    }

    private void OnSetIsOpen()
    {
        // The open/close path of the component toggles the callout itself, right after the assignment.
        if (_selfDrivenIsOpen) return;

        // Before the first render the callout element does not exist yet; OnAfterRenderAsync opens it.
        if (IsRendered is false)
        {
            _openOnFirstRender = IsOpen;
            return;
        }

        _ = ToggleCalloutFromOutside();
    }

    // The open state changing from the outside goes through the same steps the component's own open and
    // close path does, so that a callout driven by its IsOpen parameter alone still hands the keyboard
    // over to its content and still keeps it there.
    private async Task ToggleCalloutFromOutside()
    {
        if (IsOpen)
        {
            await ToggleCallout();

            await SetupFocusTrap();

            await FocusCalloutIfNeeded();

            await OnToggle.InvokeAsync(true);

            await OnOpen.InvokeAsync();
        }
        else
        {
            await DisposeFocusTrap();

            await ToggleCallout();

            await OnToggle.InvokeAsync(false);

            await OnDismiss.InvokeAsync();
        }
    }

    private async Task FocusCalloutIfNeeded(bool force = false)
    {
        // A trapped callout has to hold the focus to trap it: leaving it on the anchor would let the very
        // first Tab out of the callout, since the trap only ever sees the keys pressed inside of it.
        if ((force || AutoFocus || TrapFocus) is false || IsOpen is false || IsDisposed) return;

        if (_dotnetObj is null) return;

        try
        {
            await _js.BitUtilsFocusFirstElement(_contentId);
        }
        catch (JSDisconnectedException) { } // we can ignore this exception here
    }

    private async Task FocusAnchor()
    {
        // Only the anchor the component renders itself is one it can hand the focus back to; an external
        // anchor belongs to the consumer, who is the one that knows what in it should take the focus.
        if (IsDisposed || Anchor is null || _dotnetObj is null) return;

        try
        {
            await _js.BitUtilsFocusFirstElement(_anchorId);
        }
        catch (JSDisconnectedException) { } // we can ignore this exception here
    }

    private async Task<bool> CalloutContainsFocus()
    {
        // Before the first render there is neither a callout nor a JS side to ask about it.
        if (IsDisposed || _dotnetObj is null) return false;

        try
        {
            return await _js.BitUtilsContainsActiveElement(_contentId);
        }
        catch (JSDisconnectedException) { return false; } // we can ignore this exception here
    }

    private async Task<bool> GetIsHoverDevice()
    {
        try
        {
            return await _js.BitUtilsIsHoverDevice();
        }
        catch (JSDisconnectedException) { return false; } // we can ignore this exception here
    }

    // The hover mode only applies to the devices that have a pointer to hover with: a tap on a touch
    // screen reports a mouseover of its own, which would fight the click that is meant to toggle it.
    private bool HoverDriven => OpenOnHover && _isHoverDevice is true;

    // Whether the callout is the one that has to be kept within the viewport. A named scroll container is
    // the consumer taking that over, a max height is the consumer capping it by hand, and a responsive
    // callout is a panel sized against the screen on exactly the screens where the callout would not fit.
    private bool FitsToViewport => IsResponsive is false && MaxHeight.HasValue() is false && ScrollContainerId.HasValue() is false;

    private bool IsResponsive => ResponsiveMode is not null && ResponsiveMode != BitResponsiveMode.None;

    private void CancelHover()
    {
        var cts = _hoverCts;
        if (cts is null) return;

        _hoverCts = null;
        cts.Cancel();
        cts.Dispose();
    }

    // Waits out the hover delay and reports whether the wait is still the one that matters: the pointer
    // moving again cancels it, and the callout may be gone by the time it is over.
    private async Task<bool> DelayHover(int delay)
    {
        if (delay <= 0) return IsDisposed is false;

        var cts = new CancellationTokenSource();
        _hoverCts = cts;

        try
        {
            await Task.Delay(delay, cts.Token);
        }
        catch (OperationCanceledException)
        {
            return false;
        }

        if (ReferenceEquals(_hoverCts, cts) is false) return false;

        _hoverCts = null;
        cts.Dispose();

        return IsDisposed is false;
    }

    private async Task SetupFocusTrap()
    {
        if (TrapFocus is false || _focusTrapped || IsDisposed || _dotnetObj is null) return;

        _focusTrapped = true;

        try
        {
            await _js.BitUtilsSetupFocusTrap(_contentId);
        }
        catch (JSDisconnectedException) { } // we can ignore this exception here
    }

    private async Task DisposeFocusTrap()
    {
        if (_focusTrapped is false) return;

        _focusTrapped = false;

        try
        {
            await _js.BitUtilsDisposeFocusTrap(_contentId);
        }
        catch (JSDisconnectedException) { } // we can ignore this exception here
    }

    // The edge a responsive callout slides in from, which is the panel position for the Panel mode and
    // the mode itself for the two that name an edge of their own.
    private BitPanelPosition ResponsivePosition => ResponsiveMode switch
    {
        BitResponsiveMode.Top => BitPanelPosition.Top,
        BitResponsiveMode.Bottom => BitPanelPosition.Bottom,
        _ => PanelPosition ?? BitPanelPosition.End
    };

    // The geometry the swipe gestures were registered with, or null when there are none to register.
    private string? GetSwipesKey()
    {
        return IsResponsive is false ? null : $"{ResponsivePosition}|{Dir}|{ScrollContainerId}";
    }

    private async Task SetupSwipes()
    {
        if (IsResponsive is false || IsDisposed) return;

        _swipesKey = GetSwipesKey();

        // Swipes.dispose releases the .NET reference it was handed, so the gestures get one of their own
        // instead of the one the callout positioning keeps using for the life of the component.
        _swipesDotnetObj = DotNetObjectReference.Create(this);

        try
        {
            await _js.BitSwipesSetup(
                id: _contentId,
                trigger: 0.25m,
                position: ResponsivePosition,
                isRtl: Dir is BitDir.Rtl,
                // The axis the panel is swiped away along is the one it slid in on, and the lock is what
                // takes that axis from the page: a top or bottom panel dragged with the wrong lock follows
                // the finger while the page scrolls out from under it at the same time.
                orientationLock: ResponsivePosition is BitPanelPosition.Top or BitPanelPosition.Bottom
                                    ? BitSwipeOrientation.Vertical
                                    : BitSwipeOrientation.Horizontal,
                dotnetObj: _swipesDotnetObj,
                isResponsive: true,
                scrollContainerId: ScrollContainerId ?? "");
        }
        catch (JSDisconnectedException) { } // we can ignore this exception here
    }

    private async Task DisposeSwipes()
    {
        if (_swipesKey is null) return;

        _swipesKey = null;

        try
        {
            await _js.BitSwipesDispose(_contentId);
        }
        catch (JSDisconnectedException) { } // we can ignore this exception here

        // Swipes.setup bails out on the screens the responsive mode does not apply to, leaving nothing for
        // Swipes.dispose to release, so the reference is also released here (disposing is idempotent).
        _swipesDotnetObj?.Dispose();
        _swipesDotnetObj = null;
    }

    private string? GetRole()
    {
        if (Role.HasValue()) return Role;

        if (TrapFocus) return "dialog";

        // A name on a generic container is a name no screen reader announces, so a callout that was given
        // one is reported as the group of content it is, which is the role that carries a name without
        // claiming anything more about what the callout holds.
        return AriaLabel.HasValue() ? "group" : null;
    }

    private string GetOverlayCssClasses()
    {
        List<string> classes = ["bit-clo-ovl"];

        // A callout that opens on hover is closed by the pointer leaving it, and the overlay would be the
        // one element the pointer could never leave it for: it covers the whole page while the callout is
        // open, so it would swallow the very mouseover events the mode is driven by.
        if (HoverDriven)
        {
            classes.Add("bit-clo-ovh");
        }

        if (Modal)
        {
            classes.Add("bit-clo-ovm");
        }

        if (Classes?.Overlay is not null)
        {
            classes.Add(Classes.Overlay);
        }

        return string.Join(' ', classes).Trim();
    }

    private string GetArrowCssClasses()
    {
        List<string> classes = ["bit-clo-arw"];

        classes.AddRange(GetSurfaceCssClasses());

        if (Classes?.Arrow is not null)
        {
            classes.Add(Classes.Arrow);
        }

        return string.Join(' ', classes).Trim();
    }

    private string? GetCalloutStyles()
    {
        // The positioning code clears the callout's inline sizing on every layout pass, so the caps travel
        // as custom properties the stylesheet reads instead of as declarations of their own.
        var maxHeight = MaxHeight.HasValue() ? $"--bit-clo-mxh:{MaxHeight};" : null;
        var width = Width.HasValue() ? $"--bit-clo-wid:{Width};" : null;
        var minWidth = MinWidth.HasValue() ? $"--bit-clo-mnw:{MinWidth};" : null;
        var maxWidth = MaxWidth.HasValue() ? $"--bit-clo-mxw:{MaxWidth};" : null;

        var result = $"{maxHeight}{width}{minWidth}{maxWidth}{Styles?.Content}";

        return result.HasValue() ? result : null;
    }

    private string GetCalloutCssClasses()
    {
        List<string> classes = ["bit-clo-cal"];

        if (IsOpen)
        {
            classes.Add("bit-clo-ocl");
        }

        // While open the callout is relocated to the body, which takes it out of the subtree that carries
        // the root's bit-fam class, so ForceAnimation has to be rendered on the callout itself for its
        // opening animation to opt out of reduced motion.
        if (ForceAnimation)
        {
            classes.Add("bit-fam");
        }

        if (IsResponsive)
        {
            classes.Add("bit-clo-res");

            classes.Add(ResponsivePosition switch
            {
                BitPanelPosition.Start => "bit-clo-sta",
                BitPanelPosition.Top => "bit-clo-top",
                BitPanelPosition.Bottom => "bit-clo-btm",
                _ => "bit-clo-end"
            });
        }

        if (NoShadow)
        {
            classes.Add("bit-clo-nsh");
        }

        if (MaxHeight.HasValue())
        {
            classes.Add("bit-clo-mxh");
        }

        if (MaxWidth.HasValue())
        {
            classes.Add("bit-clo-mxw");
        }

        if (FitsToViewport)
        {
            classes.Add("bit-clo-fit");
        }

        classes.AddRange(GetSurfaceCssClasses());

        // The callout is relocated to the body while it is open, so the direction of the page is what it
        // would otherwise be laid out in; the class is what carries the component's own over to it.
        if (Dir is BitDir.Rtl)
        {
            classes.Add("bit-rtl");
        }

        if (Classes?.Content is not null)
        {
            classes.Add(Classes.Content);
        }

        return string.Join(' ', classes).Trim();
    }

    // The background and border of the surface, shared by the callout and by the arrow that points out of
    // it, so that the beak is always painted in the same color as the callout it belongs to.
    private IEnumerable<string> GetSurfaceCssClasses()
    {
        var backgroundClass = Background switch
        {
            BitColorKind.Primary => "bit-clo-bpg",
            BitColorKind.Secondary => "bit-clo-bsg",
            BitColorKind.Tertiary => "bit-clo-btg",
            BitColorKind.Transparent => "bit-clo-brg",
            _ => string.Empty
        };

        if (backgroundClass.HasValue())
        {
            yield return backgroundClass;
        }

        var borderClass = Border switch
        {
            BitColorKind.Primary => "bit-clo-brd bit-clo-bpr",
            BitColorKind.Secondary => "bit-clo-brd bit-clo-bsr",
            BitColorKind.Tertiary => "bit-clo-brd bit-clo-btr",
            BitColorKind.Transparent => "bit-clo-brd bit-clo-brr",
            _ => string.Empty
        };

        if (borderClass.HasValue())
        {
            yield return borderClass;
        }
    }



    protected override async ValueTask DisposeAsync(bool disposing)
    {
        if (IsDisposed || disposing is false) return;

        await base.DisposeAsync(disposing);

        CancelHover();

        // Nothing was registered with the JS side before the first render, and reaching for it from there
        // is what a component disposed during prerendering would be doing.
        if (_dotnetObj is not null)
        {
            try
            {
                await _js.BitCalloutClearCallout(_contentId);
                await _js.BitUtilsDisposeFocusTrap(_contentId);
            }
            catch (JSDisconnectedException) { } // we can ignore this exception here
        }

        await DisposeSwipes();

        _dotnetObj?.Dispose();
    }
}
