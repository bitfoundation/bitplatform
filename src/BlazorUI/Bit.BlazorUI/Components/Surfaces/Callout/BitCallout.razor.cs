namespace Bit.BlazorUI;

/// <summary>
/// A callout is an anchored tip that can be used to teach people or guide them through the app without
/// blocking them. It hosts any content next to an anchor of its own, next to an element elsewhere on the
/// page, or at a point a right-click happened; flips to the side with the most room, takes a side and an
/// alignment of its own where they fit, can be sized in every direction, points at its anchor with an
/// optional arrow, nests inside another callout, and closes on an outside click or the Escape key.
/// </summary>
public partial class BitCallout : BitComponentBase
{
    private string _anchorId = default!;
    private string _arrowId = default!;
    private string _bodyId = default!;
    private string _pointId = default!;
    private string _contentId = default!;
    private string _footerId = default!;
    private string _headerId = default!;
    private string _overlayId = default!;
    private double? _pointX;
    private double? _pointY;
    private bool _openAfterRender;
    private bool _placeAfterRender;
    private bool _contentRendered;
    private bool _selfDrivenIsOpen;
    private bool _focusTrapped;
    private bool _scrollLocked;
    private bool _hoverInside;
    private bool? _isHoverDevice;
    private (bool IsOpen, string? HasPopup)? _syncedAria;
    private string? _swipesKey;
    private CancellationTokenSource? _hoverCts;
    private DotNetObjectReference<BitCallout>? _dotnetObj;
    private DotNetObjectReference<BitCallout>? _swipesDotnetObj;



    [Inject] private IJSRuntime _js { get; set; } = default!;



    /// <summary>
    /// How the callout is lined up with its anchor along the axis it is not placed on: a callout above or
    /// below the anchor is aligned horizontally, and one beside the anchor is aligned vertically. It
    /// defaults to Start, which lines the callout up with the edge the anchor starts at.
    /// </summary>
    /// <remarks>
    /// The alignment is applied before the callout is kept within the screen, so a callout that would hang
    /// off an edge is still slid back onto it, and the arrow keeps pointing at the anchor either way.
    /// </remarks>
    [Parameter] public BitSideAlignment? Alignment { get; set; }

    /// <summary>
    /// The distance in pixels the callout is slid along the axis it is aligned on, off the edge of the
    /// anchor it was lined up with. It runs inwards from whichever edge <see cref="Alignment"/> picked,
    /// so the same value moves a Start-aligned callout and an End-aligned one towards each other, and it
    /// has no edge to slide a centered callout away from. It defaults to zero.
    /// </summary>
    /// <remarks>
    /// It is applied before the callout is kept within the screen, so a callout slid off an edge is still
    /// brought back onto it, and the arrow keeps pointing at the anchor either way.
    /// </remarks>
    [Parameter] public int AlignmentOffset { get; set; }

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
    /// The distance in pixels the arrow drawn by <see cref="ShowArrow"/> is kept away from the corners of
    /// the callout, so that it never lands on a rounded corner, where the radius would cut half of it away.
    /// It defaults to 16, and never drops below the size of the arrow itself.
    /// </summary>
    /// <remarks>
    /// It is what a callout aligned with an edge of a wide anchor needs: the arrow is centered on the
    /// anchor, so a wider padding pulls it back towards the middle of the callout, and a narrower one lets
    /// it sit closer to the corner and stay nearer to what it points at.
    /// </remarks>
    [Parameter] public int? ArrowPadding { get; set; }

    /// <summary>
    /// The size in pixels of the arrow drawn by <see cref="ShowArrow"/>, which is the length of the side of
    /// the square the beak is cut out of. It defaults to 12.
    /// </summary>
    [Parameter] public int? ArrowSize { get; set; }

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
    /// <remarks>
    /// An element in the content marked with a <c>data-autofocus</c> attribute takes the focus instead,
    /// for the callouts whose first focusable element is not the one worth starting at.
    /// </remarks>
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
    /// The distance in pixels the callout keeps from the edges of the screen when it is placed and when it
    /// is slid back onto it, so that a callout at the edge of the page is not pressed flat against it, and
    /// one under a fixed header or over a fixed toolbar can be kept clear of it. It defaults to zero.
    /// </summary>
    /// <remarks>
    /// The padding is taken off the room every side is measured against, so it also decides which side has
    /// enough room for the callout, not only where the callout comes to rest.
    /// </remarks>
    [Parameter] public int CollisionPadding { get; set; }

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
    /// Holds the callout to the width of its anchor, so that a content wider than the anchor wraps inside
    /// it instead of stretching it. A callout whose content is narrower stays as narrow as it is.
    /// </summary>
    [Parameter] public bool FixedCalloutWidth { get; set; }

    /// <summary>
    /// The content of a footer that stays at the bottom of the callout while the rest of it scrolls.
    /// </summary>
    /// <remarks>
    /// Setting a header or a footer lays the callout out as a column of header, scrolling body and footer,
    /// and wires the three of them up on its own, which is the id-free version of pointing
    /// <see cref="HeaderId"/>, <see cref="ScrollContainerId"/> and <see cref="FooterId"/> at elements of the
    /// consumer's own. Those parameters still win where they are set.
    /// </remarks>
    [Parameter] public RenderFragment? Footer { get; set; }

    /// <summary>
    /// The id of the footer element that renders at the end of the scrolling container of the callout content.
    /// </summary>
    /// <remarks>
    /// It is the manual version of the <see cref="Footer"/> parameter, for a footer the consumer renders in
    /// the content itself, and it wins over the footer the callout renders when both are given.
    /// </remarks>
    [Parameter] public string? FooterId { get; set; }

    /// <summary>
    /// The distance in pixels between the anchor and the callout. It defaults to zero, which tucks the
    /// callout against its anchor, and applies to whichever side the callout ends up being placed on.
    /// </summary>
    [Parameter] public int Gap { get; set; }

    /// <summary>
    /// The content of a header that stays at the top of the callout while the rest of it scrolls.
    /// </summary>
    /// <remarks>
    /// Setting a header or a footer lays the callout out as a column of header, scrolling body and footer,
    /// and wires the three of them up on its own, which is the id-free version of pointing
    /// <see cref="HeaderId"/>, <see cref="ScrollContainerId"/> and <see cref="FooterId"/> at elements of the
    /// consumer's own. Those parameters still win where they are set.
    /// </remarks>
    [Parameter] public RenderFragment? Header { get; set; }

    /// <summary>
    /// The id of the header element that renders at the top of the scrolling container of the callout content.
    /// </summary>
    /// <remarks>
    /// It is the manual version of the <see cref="Header"/> parameter, for a header the consumer renders in
    /// the content itself, and it wins over the header the callout renders when both are given.
    /// </remarks>
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
    /// Keeps the content of the callout out of the page until the callout is opened for the first time, for
    /// the callouts whose content is expensive enough that rendering it behind a closed callout is a cost of
    /// its own. Once rendered it stays, so whatever state the content holds survives the callout closing.
    /// </summary>
    /// <remarks>
    /// The placement of the callout is measured against its content, so the first opening of a lazy callout
    /// waits for the render that puts the content in it before the callout is placed and shown.
    /// </remarks>
    [Parameter] public bool LazyRender { get; set; }

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
    /// The window width in pixels below which the callout is allowed to hang off the end of the screen
    /// rather than being slid back onto it, for the layouts that scroll sideways on the narrow screens and
    /// would otherwise have the callout pulled away from the anchor it belongs to. Leaving it unset keeps
    /// the callout within the screen at every width.
    /// </summary>
    [Parameter] public int? MaxWindowWidth { get; set; }

    /// <summary>
    /// The minimum width of the callout as a CSS value (e.g. "20rem"), so that a narrow content does not
    /// end up in a cramped callout.
    /// </summary>
    [Parameter] public string? MinWidth { get; set; }

    /// <summary>
    /// Dims the page behind the callout and holds it still while the callout is open, so that the callout
    /// reads as the only thing in play. The overlay still dismisses the callout on a click unless
    /// <see cref="NoDismissOnOutsideClick"/> says otherwise.
    /// </summary>
    /// <remarks>
    /// The page is what would otherwise take the wheel and the touch away from the callout, and scrolling
    /// it is also what dismisses a callout, so a modal one keeps the page from scrolling underneath it.
    /// </remarks>
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
    /// Keeps the callout on the <see cref="Side"/> it was asked for even when there is not enough room for
    /// it there, instead of flipping it to the opposite side. It has nothing to hold in place for a callout
    /// that was not given a side, whose placement is the automatic one to begin with.
    /// </summary>
    /// <remarks>
    /// The callout is still kept within the screen, so one that is forced onto a side without the room for
    /// it ends up overlapping its anchor rather than running off the edge of the page.
    /// </remarks>
    [Parameter] public bool NoFlip { get; set; }

    /// <summary>
    /// Leaves the page its own clicks while the callout is open, by not rendering the overlay that
    /// otherwise covers it. Whatever is underneath the callout can be interacted with, and an interaction
    /// outside of the callout dismisses it unless <see cref="NoDismissOnOutsideClick"/> says otherwise.
    /// </summary>
    /// <remarks>
    /// This is what a context menu needs: the overlay would take the right-click that is meant to move the
    /// menu to a new point, leaving the browser to show its own menu on top of it instead. A
    /// <see cref="Modal"/> callout keeps its overlay, since dimming the page and holding it still is the
    /// whole of what it is for.
    /// </remarks>
    [Parameter] public bool NoOverlay { get; set; }

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
    /// <remarks>
    /// Only Top, Bottom, Start and End are meaningful here; the physical pair and the two combined values
    /// fall back to the default.
    /// </remarks>
    [Parameter] public BitSide? PanelPosition { get; set; }

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
    /// Widens the callout to at least the width of its anchor, so that a callout with little in it still
    /// reads as belonging to what it was opened from. A wider content keeps its own width.
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
    /// every side it allows. Leaving it unset leaves the choice to Direction alone, and
    /// <see cref="NoFlip"/> turns the preference into a demand.
    /// </summary>
    /// <remarks>
    /// Only Top, Bottom, Start and End are meaningful here; the physical pair and the two combined values
    /// leave the choice to Direction, exactly as leaving this unset does.
    /// </remarks>
    [Parameter] public BitSide? Side { get; set; }

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
    /// The width of the callout as a CSS value (e.g. "20rem"), which a content wider than it wraps inside
    /// rather than stretching. By default the callout is only as wide as its content needs.
    /// <see cref="SetCalloutWidth"/> and <see cref="FixedCalloutWidth"/> are applied after the callout is
    /// measured, so they take precedence over it.
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
    /// Opens the callout at a point on the screen rather than against an anchor, which is what a context
    /// menu needs: the callout is placed against the point the way it would be against an anchor, so it
    /// still flips to the side with the most room and is still kept within the screen.
    /// </summary>
    /// <param name="x">The horizontal distance in pixels from the left edge of the visible page.</param>
    /// <param name="y">The vertical distance in pixels from the top edge of the visible page.</param>
    /// <remarks>
    /// Calling it again while the callout is open moves it to the new point instead of reopening it, so a
    /// second right-click somewhere else brings the menu along without it closing and opening again. The
    /// anchor takes over again the next time the callout is opened by anything else.
    /// </remarks>
    public async Task OpenAt(double x, double y)
    {
        if (IsEnabled is false) return;

        _pointX = x;
        _pointY = y;

        // The element the callout is placed against is rendered from these coordinates, so both the
        // opening and a move to a new point wait for the render that puts it there.
        if (IsOpen)
        {
            _placeAfterRender = true;
        }
        else
        {
            _selfDrivenIsOpen = true;
            try
            {
                if (await AssignIsOpen(true) is false) return;
            }
            finally
            {
                _selfDrivenIsOpen = false;
            }

            _contentRendered = true;
            _openAfterRender = true;
        }

        await InvokeAsync(StateHasChanged);
    }

    /// <summary>
    /// Opens the callout at the point a mouse event happened, which is what a context menu needs. It is the
    /// <see cref="OpenAt(double, double)"/> above, handed the coordinates of the event.
    /// </summary>
    public Task OpenAt(MouseEventArgs e) => OpenAt(e.ClientX, e.ClientY);

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
    /// Lays the open callout out again against what it is placed on, without reopening it: the side, the
    /// alignment and the room the content is given are all decided anew, and the entry animation is not
    /// replayed. It does nothing to a callout that is closed.
    /// </summary>
    /// <remarks>
    /// The callout already follows the page scrolling and resizing under it, and an anchor that changes
    /// size while it is open, so this is for what it cannot see: a content of its own that has grown or
    /// shrunk, or an anchor moved by something other than a resize of it.
    /// </remarks>
    public Task Reposition() => IsOpen ? RepositionCallout() : Task.CompletedTask;

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

        await DisposeScrollLock();

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
        _bodyId = $"BitCallout-{UniqueId}-body";
        _contentId = $"BitCallout-{UniqueId}-content";
        _footerId = $"BitCallout-{UniqueId}-footer";
        _headerId = $"BitCallout-{UniqueId}-header";
        _overlayId = $"BitCallout-{UniqueId}-overlay";
        _pointId = $"BitCallout-{UniqueId}-point";

        // The uncontrolled starting state. The callout itself can only be shown once the DOM exists,
        // so the actual opening is deferred to the first render like an initially set IsOpen is.
        if (IsOpenHasBeenSet is false && DefaultIsOpen.HasValue)
        {
            IsOpen = DefaultIsOpen.Value;
        }

        _openAfterRender = IsOpen;
        _contentRendered = IsOpen;

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

        // The focus trap and the scroll lock are registered against the open callout, so turning either of
        // them on or off while the callout is open has to reach the already registered one rather than
        // wait for the next time it opens.
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

            if (Modal)
            {
                await SetupScrollLock();
            }
            else
            {
                await DisposeScrollLock();
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

        if (firstRender)
        {
            _dotnetObj = DotNetObjectReference.Create(this);

            await SetupSwipes();
        }

        await SyncAnchorAria();

        // The opening that had to wait for a render: an IsOpen (or DefaultIsOpen) that starts out true
        // reaches OnSetIsOpen before the first render, when neither the callout element nor the .NET object
        // reference the JS side needs exist yet, and a lazily rendered content is only put in the callout
        // by the render the opening itself asks for, which the placement then has to be measured against.
        if (_openAfterRender)
        {
            _openAfterRender = false;

            // The callout may have been closed again between the opening being deferred and the render it
            // was waiting for, in which case there is nothing left to open.
            if (IsOpen is false) return;

            await ToggleCallout();

            await SetupFocusTrap();

            await SetupScrollLock();

            await FocusCalloutIfNeeded();

            await OnToggle.InvokeAsync(true);

            await OnOpen.InvokeAsync();
        }
        else if (_placeAfterRender)
        {
            _placeAfterRender = false;

            // The callout is already open and was only moved to a new point, so it is laid out again
            // rather than opened again: nothing about its state changed for the consumer to hear about,
            // and going back through the toggle would replay the entry animation of a callout that never
            // went anywhere - a second right-click somewhere else would flash the menu it only moved.
            if (IsOpen)
            {
                await RepositionCallout();
            }
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

        // The key can come from the callout or from the anchor, since the focus stays on the trigger unless
        // the callout was asked to take it. Closing hands the focus back to the anchor when it was in the
        // callout, and leaves it where it is when it was already on the anchor.
        await CloseCallout();

        // The close runs on an event of the callout or of the root, neither of which re-renders the other,
        // so refresh the open-state classes and aria-expanded here.
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

        // The pointer came back before the delay was up, onto the anchor or into the callout - or the
        // callout was closed by something else while the delay ran out.
        if (_hoverInside || IsOpen is false) return;

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

        // Opened by something other than OpenAt, so the anchor takes the placement back from the point the
        // callout was last opened at.
        ForgetPoint();

        // Before the first render there is no callout element to show, and a lazy content is not in the
        // callout the placement is measured against yet, so the opening waits for the render either way.
        if (IsRendered is false || NeedsContentRender)
        {
            _contentRendered = true;
            _openAfterRender = true;
            return;
        }

        await ToggleCallout();

        await SetupFocusTrap();

        await SetupScrollLock();

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

        await DisposeScrollLock();

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
        _openAfterRender = false;
        _placeAfterRender = false;

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

        // An opening, or a move to a new point, that was waiting for a render is called off: the callout is
        // closed before the render it was deferred to ever arrives.
        _openAfterRender = false;
        _placeAfterRender = false;

        if (await AssignIsOpen(false) is false) return;

        await OnToggle.InvokeAsync(false);

        await OnDismiss.InvokeAsync();
    }

    private async Task ToggleCallout()
    {
        if (IsDisposed) return;

        // The reference is created on the first render, so before it there is nothing to position either.
        if (_dotnetObj is null) return;

        // A callout opened at a point is placed against the zero-sized element rendered there, so the
        // placement code needs to know nothing about points: it measures an element as it always does.
        var id = HasPoint ? _pointId : Anchor is not null ? _anchorId : AnchorId ?? _Id;

        try
        {
            await _js.BitCalloutToggleCallout(
                dotnetObj: _dotnetObj,
                componentId: id,
                component: (HasPoint || AnchorEl is null) ? null : AnchorEl(),
                calloutId: _contentId,
                callout: null,
                // No overlay to hide (and, on the JS side, no overlay to take the outside clicks for the
                // callout) when the page was left its own clicks.
                overlayId: HasOverlay ? _overlayId : "",
                isCalloutOpen: IsOpen,
                responsiveMode: ResponsiveMode ?? BitResponsiveMode.None,
                dropDirection: Direction ?? BitDropDirection.TopAndBottom,
                isRtl: Dir is BitDir.Rtl,
                // Whatever is named as the scrollable part of the content is what the positioning code caps
                // to the room the viewport leaves. With nothing named, the callout itself takes that role,
                // so that content taller than the screen scrolls inside the callout instead of running off
                // the bottom of it, where a fixed-positioned element is out of reach of the page's own
                // scrolling.
                scrollContainerId: ScrollContainerId.HasValue()
                                    ? ScrollContainerId!
                                    : HasSections ? _bodyId : (FitsToViewport ? _contentId : ""),
                scrollOffset: ScrollOffset ?? 0,
                headerId: HeaderId.HasValue() ? HeaderId! : (Header is not null ? _headerId : ""),
                footerId: FooterId.HasValue() ? FooterId! : (Footer is not null ? _footerId : ""),
                setCalloutWidth: SetCalloutWidth,
                fixedCalloutWidth: FixedCalloutWidth,
                maxWindowWidth: MaxWindowWidth ?? 0,
                maxHeight: 0,
                arrowId: ShowArrow ? _arrowId : "",
                gap: Gap,
                noDismiss: NoDismissOnOutsideClick,
                preferredSide: Side switch
                {
                    BitSide.Top => "top",
                    BitSide.Bottom => "bottom",
                    BitSide.Start => "start",
                    BitSide.End => "end",
                    _ => ""
                },
                alignment: Alignment switch
                {
                    BitSideAlignment.Center => "center",
                    BitSideAlignment.End => "end",
                    _ => ""
                },
                noFlip: NoFlip,
                collisionPadding: CollisionPadding,
                alignmentOffset: AlignmentOffset,
                arrowPadding: ArrowPadding ?? 0);
        }
        catch (JSDisconnectedException) { } // we can ignore this exception here
    }

    private async Task RepositionCallout()
    {
        if (IsDisposed || _dotnetObj is null) return;

        try
        {
            await _js.BitCalloutReposition();
        }
        catch (JSDisconnectedException) { } // we can ignore this exception here
    }

    private void OnSetIsOpen()
    {
        // The open/close path of the component toggles the callout itself, right after the assignment.
        if (_selfDrivenIsOpen) return;

        // Opened by the parent rather than by OpenAt, so the anchor takes the placement back from the point
        // the callout was last opened at.
        if (IsOpen)
        {
            ForgetPoint();
        }

        // Before the first render the callout element does not exist yet, and a lazy content is not in the
        // callout the placement is measured against; OnAfterRenderAsync opens it once the render is in.
        if (IsRendered is false || (IsOpen && NeedsContentRender))
        {
            _contentRendered = _contentRendered || IsOpen;
            _openAfterRender = IsOpen;
            return;
        }

        _ = ToggleCalloutFromOutside();
    }

    // The open state changing from the outside goes through the same steps the component's own open and
    // close path does, so that a callout driven by its IsOpen parameter alone still hands the keyboard
    // over to its content and still keeps it there.
    private async Task ToggleCalloutFromOutside()
    {
        try
        {
            await ToggleCalloutFromOutsideCore();
        }
        catch (JSDisconnectedException) { } // we can ignore this exception here
        catch (ObjectDisposedException) { } // we can ignore this exception here
        catch (Exception ex)
        {
            // The setter that starts this does not await it, so an exception thrown by one of the consumer
            // callbacks it invokes - OnToggle, OnOpen, OnDismiss - has no caller to surface on and would be
            // left unobserved. It is handed to the component's error boundary instead, the way an exception
            // out of a click handler is - unless the component is already gone, where there is no longer a
            // boundary to hand it to.
            if (IsDisposed is false) await DispatchExceptionAsync(ex);
        }
    }

    private async Task ToggleCalloutFromOutsideCore()
    {
        if (IsOpen)
        {
            await ToggleCallout();

            await SetupFocusTrap();

            await SetupScrollLock();

            await FocusCalloutIfNeeded();

            await OnToggle.InvokeAsync(true);

            await OnOpen.InvokeAsync();
        }
        else
        {
            await DisposeFocusTrap();

            await DisposeScrollLock();

            await ToggleCallout();

            await OnToggle.InvokeAsync(false);

            await OnDismiss.InvokeAsync();
        }
    }

    private async Task FocusCalloutIfNeeded()
    {
        // A trapped callout has to hold the focus to trap it: leaving it on the anchor would let the very
        // first Tab out of the callout, since the trap only ever sees the keys pressed inside of it.
        if ((AutoFocus || TrapFocus) is false || IsOpen is false || IsDisposed) return;

        if (_dotnetObj is null) return;

        try
        {
            await _js.BitUtilsFocusFirstElement(_contentId);
        }
        catch (JSDisconnectedException) { } // we can ignore this exception here
    }

    // The anchor container carries the popup relationship, but the element the user actually lands on is
    // the trigger the consumer put inside it, so the relationship is copied onto that one as well. Only
    // when what it would report has actually changed: the attributes are written from JS, so a call per
    // render would be a round trip per render for something that only changes when the callout is toggled
    // or the kind of popup it holds is.
    private async Task SyncAnchorAria()
    {
        if (Anchor is null || IsDisposed || _dotnetObj is null) return;

        var aria = (IsOpen, GetAriaHasPopup());

        if (_syncedAria == aria) return;

        _syncedAria = aria;

        try
        {
            await _js.BitUtilsSyncAriaPopup(_anchorId, _contentId, aria.Item1, aria.Item2);
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

    // Whether the callout was opened at a point on the screen rather than against an anchor.
    private bool HasPoint => _pointX.HasValue && _pointY.HasValue;

    private void ForgetPoint()
    {
        _pointX = null;
        _pointY = null;
    }

    // Whether the content of the callout is in the page. A lazy callout leaves it out until it is opened
    // for the first time, and keeps it from then on, so the state the content holds survives a close.
    private bool ContentRendered => LazyRender is false || _contentRendered;

    // Whether an opening has to wait for a render to put the content in the callout first, since the
    // placement of the callout is measured against what is in it.
    private bool NeedsContentRender => LazyRender && _contentRendered is false;

    // Whether the callout lays itself out as a header, a scrolling body and a footer rather than as one
    // block of content. A footer alone is enough: what makes the layout is the body taking the scrolling.
    private bool HasSections => Header is not null || Footer is not null;

    // Whether the callout is the one that has to be kept within the viewport. A named scroll container is
    // the consumer taking that over, a header or a footer hands it to the body between them, a max height
    // is the consumer capping it by hand, and a responsive callout is a panel sized against the screen on
    // exactly the screens where the callout would not fit.
    private bool FitsToViewport => IsResponsive is false
                                && HasSections is false
                                && MaxHeight.HasValue() is false
                                && ScrollContainerId.HasValue() is false;

    // Whether the overlay that covers the page while the callout is open is rendered at all. Without it
    // the page keeps its own clicks - which is the point of NoOverlay - and the dismissal that the overlay
    // would have taken care of falls to the page-level handler on the JS side. A modal callout keeps its
    // overlay whatever else was asked for: the overlay is what dims the page and holds it still, so a
    // modal callout without one would be modal in name only.
    private bool HasOverlay => NoOverlay is false || Modal;

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

    // A modal callout holds the page still underneath it: the wheel and the touch are the callout's while
    // it is open, and scrolling the page is also what dismisses a callout, so a modal one would otherwise
    // be scrolled away by the very gesture it is meant to be the only thing in play for.
    private async Task SetupScrollLock()
    {
        if (Modal is false || _scrollLocked || IsDisposed || _dotnetObj is null) return;

        _scrollLocked = true;

        try
        {
            await _js.BitUtilsToggleOverflow(UniqueId, "body", true);
        }
        catch (JSDisconnectedException) { } // we can ignore this exception here
    }

    private async Task DisposeScrollLock()
    {
        if (_scrollLocked is false) return;

        _scrollLocked = false;

        try
        {
            await _js.BitUtilsToggleOverflow(UniqueId, "body", false);
        }
        catch (JSDisconnectedException) { } // we can ignore this exception here
    }

    // The edge a responsive callout slides in from, which is the panel position for the Panel mode and
    // the mode itself for the two that name an edge of their own. BitSide carries sides a panel has no styles
    // for - the physical pair and the two combined values - so they are resolved to the default here, which
    // keeps the class the callout draws and the value the swipe is registered with from disagreeing.
    private BitSide ResponsivePosition => ResponsiveMode switch
    {
        BitResponsiveMode.Top => BitSide.Top,
        BitResponsiveMode.Bottom => BitSide.Bottom,
        _ => PanelPosition switch
        {
            BitSide.Start or BitSide.Top or BitSide.Bottom => PanelPosition.Value,
            _ => BitSide.End
        }
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
                orientationLock: ResponsivePosition is BitSide.Top or BitSide.Bottom
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

    // What the anchor tells the screen readers is behind it. The token has to name what the popup actually
    // is - the role of the element that holds it has to match it - and the `true` this used to carry is
    // read as `menu` by every screen reader, which is a promise a callout of plain content does not keep.
    // So a callout that is a dialog says dialog, one given a popup role of its own is named by it, and the
    // rest carry nothing at all rather than announce a menu that is not there; aria-expanded is what tells
    // the user there is something to open either way.
    private string? GetAriaHasPopup()
    {
        var role = GetRole();

        return role is "dialog" or "menu" or "listbox" or "tree" or "grid" ? role : null;
    }

    // A callout that reports itself as a dialog needs an accessible name (WAI-ARIA APG), and one that
    // renders a header of its own is already showing the name it should be given. AriaLabel wins where it
    // is set, so naming the callout by hand still takes precedence over the header it happens to have.
    private string? GetAriaLabelledBy()
    {
        return (AriaLabel.HasValue() is false && Header is not null) ? _headerId : null;
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

    // The coordinates are written with the invariant culture, since a comma decimal separator would leave
    // the browser with a length it cannot read.
    private string GetPointStyles()
    {
        var x = _pointX!.Value.ToString(System.Globalization.CultureInfo.InvariantCulture);
        var y = _pointY!.Value.ToString(System.Globalization.CultureInfo.InvariantCulture);

        return $"left:{x}px;top:{y}px";
    }

    private string? GetArrowStyles()
    {
        // The size travels as a custom property the stylesheet reads, so that the arrow keeps whatever the
        // theme sizes it to when nothing is asked for, and the consumer's own styles still win over both.
        var size = ArrowSize > 0 ? $"--bit-clo-arw-siz:{ArrowSize.Value}px;" : null;

        var result = $"{size}{Styles?.Arrow}";

        return result.HasValue() ? result : null;
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
                BitSide.Start => "bit-clo-sta",
                BitSide.Top => "bit-clo-top",
                BitSide.Bottom => "bit-clo-btm",
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

        if (Width.HasValue())
        {
            classes.Add("bit-clo-wid");
        }

        if (FitsToViewport)
        {
            classes.Add("bit-clo-fit");
        }

        if (HasSections)
        {
            classes.Add("bit-clo-sec");
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

                // A modal callout disposed while it is open would otherwise leave the page it was holding
                // still unable to scroll again, with nothing left on the page to release it.
                if (_scrollLocked)
                {
                    await _js.BitUtilsToggleOverflow(UniqueId, "body", false);
                }
            }
            catch (JSDisconnectedException) { } // we can ignore this exception here
        }

        await DisposeSwipes();

        _dotnetObj?.Dispose();
    }
}
