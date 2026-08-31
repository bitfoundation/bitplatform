namespace Bit.BlazorUI;

/// <summary>
/// Panel is an overlay surface that slides in from an edge of the screen to host supplementary content -
/// a form, a filter, a set of details, a navigation menu - without taking the user away from the page
/// behind it. It can slide in from any of the four edges, be sized along the axis it slides on, scroll
/// whatever it cannot fit, dim the page or leave it usable, hold the page still while it is open, take the
/// keyboard over and hand it back, and be dismissed by a click on the overlay, the Escape key or a swipe of
/// the finger - each of which it can be asked to refuse.
/// </summary>
/// <remarks>
/// A panel given a <see cref="Header"/>, a <see cref="Footer"/> or a <see cref="ShowCloseButton"/> builds
/// the chrome that goes with them - a header row with the close button in it, a body that takes the
/// scrolling, and a footer - around its content. A panel given none of them is the plain surface it has
/// always been, and its content fills it however it likes.
/// </remarks>
public partial class BitPanel : BitComponentBase
{
    private bool _internalIsOpen;
    private bool _contentRendered;
    private bool _focusTrapped;
    private bool _scrollLocked;
    // What the current hold was taken on. The hold is registered against the element the parameters
    // resolved to and not against the parameters themselves, so a panel pointed somewhere else while it is
    // open has to let the old one go before taking the new one.
    private string? _lockedScrollerSelector;
    private ElementReference? _lockedScrollerElement;
    // Whether the gestures that land on the panel are being handed to the scroller behind it, and what they
    // are being handed to - recorded for the same reason the hold above records what it took: a panel aimed
    // somewhere else while it is open has to take the registration back and make it again.
    private bool _scrollForwarded;
    private string? _forwardedScrollerSelector;
    private ElementReference? _forwardedScrollerElement;
    // The room the scroller gave back when its overflow was toggled off, which an absolutely positioned
    // panel is pushed down by so that it stays where the eye left it rather than jumping to the top of the
    // scroller it is laid out in.
    private float _offsetTop;
    // Whether the overflow of a scroller was actually toggled during the open sequence, so the close
    // sequence hands it back if and only if it was taken, regardless of later changes to AutoToggleScroll.
    // The scroller is snapshotted with it, so the close restores the same one.
    private bool _scrollToggledOnOpen;
    private ElementReference? _scrollerElementOnToggle;
    private string? _scrollerSelectorOnToggle;
    private string? _swipesKey;
    private string _headerId = default!;
    private string _containerId = default!;
    private MouseEventArgs? _dismissArgs;
    private DotNetObjectReference<BitPanel>? _dotnetObj;
    private DotNetObjectReference<BitPanel>? _swipesDotnetObj;



    [Inject] private IJSRuntime _js { get; set; } = default!;



    // The scroller of the application shell the panel was declared inside of, cascaded by BitAppShell under
    // this name. Taken by name rather than off BitAppShell.Container because the shell lives in
    // Bit.BlazorUI.Extras, which this assembly cannot reference.
    [CascadingParameter(Name = "BitAppShell.Container")]
    private ElementReference? AppShellContainer { get; set; }



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
    /// Enables the auto scrollbar toggle behavior of the panel, which takes the overflow off the scroller
    /// while the panel is open and hands it back once it closes.
    /// </summary>
    /// <remarks>
    /// This is the panel holding the scroller itself, so the hold it would otherwise take on the page
    /// through <see cref="NoScrollLock"/> is stood down for it - the two would else both be holding the same
    /// page. The scroller is named by <see cref="ScrollerElement"/> or <see cref="ScrollerSelector"/>; when
    /// neither is set it is the scroller of the application shell the panel is inside of, and the page when
    /// it is inside none.
    /// <br />
    /// The room the scrollbar gave back is what an <see cref="AbsolutePosition"/> panel is pushed down by,
    /// so that it stays where the eye left it rather than jumping to the top of the scroller it is laid out
    /// in.
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
    /// Alias for <see cref="ChildContent"/>, named for the body it becomes on a panel that was given a
    /// header or a footer to lay out around it.
    /// </summary>
    [Parameter] public RenderFragment? Body { get; set; }

    /// <summary>
    /// The content of the panel.
    /// </summary>
    [Parameter] public RenderFragment? ChildContent { get; set; }

    /// <summary>
    /// Custom CSS classes for different parts of the panel.
    /// </summary>
    [Parameter] public BitPanelClassStyles? Classes { get; set; }

    /// <summary>
    /// The accessible name of the close button, which is what a screen reader reads out for it and what the
    /// pointer shows as its tooltip. It defaults to "Close".
    /// </summary>
    [Parameter] public string? CloseButtonTitle { get; set; }

    /// <summary>
    /// Gets or sets the icon to display in the close button using custom CSS classes for external icon libraries.
    /// Takes precedence over <see cref="CloseIconName"/> when both are set.
    /// </summary>
    /// <remarks>
    /// Use this property to render icons from external libraries like FontAwesome, Material Icons, or Bootstrap Icons.
    /// For built-in Fluent UI icons, use <see cref="CloseIconName"/> instead.
    /// </remarks>
    [Parameter] public BitIconInfo? CloseIcon { get; set; }

    /// <summary>
    /// Gets or sets the name of the icon to display in the close button from the built-in Fluent UI icons.
    /// </summary>
    /// <remarks>
    /// The icon name should be from the Fluent UI icon set (e.g., <c>BitIconName.Cancel</c>).
    /// <br />
    /// For external icon libraries, use <see cref="CloseIcon"/> instead.
    /// </remarks>
    [Parameter] public string? CloseIconName { get; set; }

    /// <summary>
    /// The footer of the panel, which stays put at the far edge of it while the content between it and the
    /// header scrolls.
    /// </summary>
    /// <remarks>
    /// It is where the actions that finish what the panel was opened for belong, so that they are still
    /// reachable however far the content has been scrolled.
    /// </remarks>
    [Parameter] public RenderFragment? Footer { get; set; }

    /// <summary>
    /// The text of the footer of the panel, for the footer that is nothing but a line of text.
    /// <see cref="Footer"/> takes precedence over it.
    /// </summary>
    [Parameter] public string? FooterText { get; set; }

    /// <summary>
    /// Stretches the panel over the whole of the screen, which takes over from <see cref="Size"/> and from
    /// the cap that otherwise leaves a strip of the page showing beside it.
    /// </summary>
    [Parameter] public bool FullSize { get; set; }

    /// <summary>
    /// The header of the panel, which stays put at the edge the panel slid in from while the content below
    /// it scrolls.
    /// </summary>
    /// <remarks>
    /// A panel that renders a header of its own is already showing the name it should be announced under, so
    /// the header is what the dialog points its <c>aria-labelledby</c> at unless
    /// <see cref="TitleAriaId"/> or <see cref="BitComponentBase.AriaLabel"/> names it instead.
    /// </remarks>
    [Parameter] public RenderFragment? Header { get; set; }

    /// <summary>
    /// The text of the header of the panel, for the header that is nothing but a title.
    /// <see cref="Header"/> takes precedence over it.
    /// </summary>
    [Parameter] public string? HeaderText { get; set; }

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
    /// Keeps the content of the panel in the page once it has been opened, instead of taking it back out
    /// every time the panel closes.
    /// </summary>
    /// <remarks>
    /// A panel builds its content when it opens and takes it away once it has finished sliding out, which is
    /// what keeps a page that declares many of them cheap - and what makes each of them start over: a
    /// half-filled form inside one is gone by the time it is opened again. Keeping it mounted hides it
    /// instead, so its content, and whatever state that content holds, is still there the next time it
    /// opens. Nothing of it is rendered until the first time it opens either way, so a panel that is never
    /// opened still costs nothing.
    /// </remarks>
    [Parameter] public bool KeepMounted { get; set; }

    /// <summary>
    /// Renders the overlay in full mode that gives it an opaque background.
    /// </summary>
    /// <remarks>
    /// The overlay catches the clicks meant for the page behind it either way; this is what makes it dim the
    /// page as well, for the panels that take the work over rather than sitting beside it. A
    /// <see cref="Modeless"/> panel renders no overlay at all, so there is nothing there to give a background
    /// to.
    /// </remarks>
    [Parameter] public bool ModeFull { get; set; }

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
    /// <br />
    /// The Escape key reaches the panel from wherever the keyboard is inside it, so a panel that never took
    /// the keyboard over is also a panel Escape does not reach until the user has tabbed or clicked into it.
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
    /// Leaves the focus wherever it is when the panel closes, instead of handing it back to the element that
    /// had it before the panel opened.
    /// </summary>
    /// <remarks>
    /// A panel that takes the keyboard over and then takes its content away drops the focus on the body,
    /// which starts the next Tab at the top of the page the user never navigated to - so the focus is handed
    /// back by default. It is only handed back while nothing else has taken it in the meantime, so a close
    /// handler that deliberately places the focus somewhere keeps it there whether or not this is set.
    /// <br />
    /// Nothing is recorded for a panel that hands nothing back, so this also keeps the element the focus was
    /// on out of the page's memory for as long as the panel is open.
    /// </remarks>
    [Parameter] public bool NoRestoreFocus { get; set; }

    /// <summary>
    /// Prevents the panel from holding the page still while it is open.
    /// </summary>
    /// <remarks>
    /// A panel that covers the page takes it over, and a page that carries on scrolling behind it moves what
    /// the user is coming back to out from under them, so the page is held for as long as the panel is open.
    /// The room the scrollbar took is added back as padding while it is held, so that taking the scrollbar
    /// away shifts nothing sideways, and the locks are counted: two panels open at once both hold the page
    /// and it is only handed back once the last of them closes.
    /// <br />
    /// A <see cref="Modeless"/> panel never holds the page in the first place, since it is meant to leave
    /// what is behind it usable, and a panel that does its own scroll handling through
    /// <see cref="AutoToggleScroll"/> holds its scroller itself, so this hold is stood down for it whether or
    /// not this is set.
    /// <br />
    /// The layer the panel is drawn in is fixed to the viewport, so the wheel and the touch drag that land on
    /// it are chained to the document rather than to whatever region the app scrolls: they are handed to that
    /// region - the one <see cref="ScrollerElement"/> or <see cref="ScrollerSelector"/> names, or the scroller
    /// of the application shell the panel is inside of - for as long as a panel that leaves the page
    /// scrolling is open, so that the page moves under the gesture the way it would with no panel over it.
    /// Anything inside the panel that scrolls itself takes its own gestures first.
    /// </remarks>
    [Parameter] public bool NoScrollLock { get; set; }

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
    /// It is called for every closing of the panel, however it happened: the close button, a click on the
    /// overlay, the Escape key, a swipe, the Close and Toggle methods, and the IsOpen parameter being set to
    /// false from the outside. The event arguments carry the click that dismissed the panel where there was one.
    /// </remarks>
    [Parameter] public EventCallback<MouseEventArgs> OnDismiss { get; set; }

    /// <summary>
    /// A callback function invoked before the panel closes, which lets the closing be refused.
    /// </summary>
    /// <remarks>
    /// Set <c>Cancel</c> on the provided <see cref="BitPanelDismissArgs"/> to leave the panel open, and read
    /// its <c>Reason</c> to tell a click on the overlay, the Escape key, a swipe, the close button and a
    /// <see cref="Close"/> call apart - refusing to let a stray swipe throw away a half-filled form is not
    /// the same as refusing the Close the form itself asked for. Since the callback is awaited, it can also
    /// run asynchronous work like a confirmation prompt.
    /// <br />
    /// It only gets its say over the closings the panel performs itself. The <see cref="IsOpen"/> parameter
    /// being set to false from the outside has already happened by the time the panel sees it, so it is
    /// reported through <see cref="OnDismiss"/> without passing through here.
    /// </remarks>
    [Parameter] public EventCallback<BitPanelDismissArgs> OnDismissing { get; set; }

    /// <summary>
    /// A callback function for when the Escape key is pressed inside the panel.
    /// </summary>
    /// <remarks>
    /// It is called for every Escape, including the ones a panel with <see cref="NoDismissOnEscape"/> refuses
    /// to be dismissed by, which makes it the counterpart of <see cref="OnOverlayClick"/> for the keyboard:
    /// the place to react to a dismissal that was turned down, or to close a panel on terms of its own.
    /// <br />
    /// A dismissal the panel does perform is reported through <see cref="OnDismissing"/> - which can still
    /// refuse it - and then through <see cref="OnDismiss"/>.
    /// </remarks>
    [Parameter] public EventCallback<KeyboardEventArgs> OnEscapeKeyDown { get; set; }

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
    /// A callback function for when the panel has finished sliding in or out, called with the state it
    /// settled in.
    /// </summary>
    /// <remarks>
    /// <see cref="OnOpen"/>, <see cref="OnDismiss"/> and <see cref="OnToggle"/> are called on the frame the
    /// panel changed state on, which is the start of the movement rather than the end of it. This is the one
    /// to wait for where the panel has to have arrived: measuring it, scrolling something into view inside
    /// it, or starting the work its arrival was the cue for.
    /// </remarks>
    [Parameter] public EventCallback<bool> OnTransitionEnd { get; set; }

    /// <summary>
    /// The edge of the screen the panel slides in from. Start and End are the logical edges, so they follow
    /// the direction of the panel. It defaults to End.
    /// </summary>
    [Parameter] public BitPanelPosition? Position { get; set; }

    /// <summary>
    /// The ARIA role the panel reports itself under, which takes over from the <c>dialog</c> it is announced
    /// as by default (or the <c>alertdialog</c> of an <see cref="IsAlert"/> panel).
    /// </summary>
    /// <remarks>
    /// It is for the panel that is not a dialog at all: a <see cref="Modeless"/> panel that sits alongside
    /// the page rather than over it is better announced as a <c>complementary</c> or a <c>region</c>, since
    /// a screen reader tells the user a dialog is something to deal with and leave.
    /// </remarks>
    [Parameter] public string? Role { get; set; }

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
    /// The element reference of the scroller whose scrolling is taken away while the panel is open, for the
    /// layouts whose scroller is not the page and cannot be named by a selector.
    /// </summary>
    /// <remarks>
    /// Takes precedence over <see cref="ScrollerSelector"/> when both are set, and over the scroller a
    /// <c>BitAppShell</c> cascades. Read only by <see cref="AutoToggleScroll"/>, which is what takes the hold.
    /// </remarks>
    [Parameter] public ElementReference? ScrollerElement { get; set; }

    /// <summary>
    /// The CSS selector of the element whose scrolling is taken away while the panel is open, for
    /// <see cref="AutoToggleScroll"/>.
    /// </summary>
    /// <remarks>
    /// A panel inside a <c>BitAppShell</c> holds the shell's scroller without being told to, since the shell
    /// cascades it; the page (<c>body</c>) is what is held when there is no shell and this is not set, which
    /// is the scroller of an ordinary page. Any other layout that scrolls a region of its own - a fixed
    /// header over a scrolling main area - names that region here, since holding a page that never scrolls
    /// holds nothing.
    /// </remarks>
    [Parameter] public string? ScrollerSelector { get; set; }

    /// <summary>
    /// Shows the close button of the panel, at the end of the header row.
    /// </summary>
    /// <remarks>
    /// It is what a panel that cannot be dismissed by a click on the overlay - a <see cref="Blocking"/> or a
    /// <see cref="Modeless"/> one - needs to be closable with the pointer at all. The dismissal it performs
    /// is reported through <see cref="OnDismissing"/> as <see cref="BitPanelDismissReason.CloseButton"/>,
    /// which is what tells it apart from the gestures that could be a slip.
    /// </remarks>
    [Parameter] public bool ShowCloseButton { get; set; }

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
    /// panel opens. It defaults to the <see cref="Header"/> of the panel, which is the name the panel is
    /// already showing, and <see cref="BitComponentBase.AriaLabel"/> takes precedence over both.
    /// </summary>
    [Parameter] public string? TitleAriaId { get; set; }

    /// <summary>
    /// The layer the panel and its overlay are stacked at, which takes over from the one the whole library
    /// shares. The overlay takes this value and the panel itself sits one above it.
    /// </summary>
    /// <remarks>
    /// It is what a panel opened from inside another one needs: the two panels sit at the same layer
    /// otherwise, where the overlay of the inner one lands underneath the panel it was opened from and a
    /// click there reaches that panel rather than dismissing the inner one.
    /// </remarks>
    [Parameter, ResetStyleBuilder]
    public int? ZIndex { get; set; }



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
    /// Closes the panel. A panel that is already closed is left alone, and one whose
    /// <see cref="OnDismissing"/> refuses the closing stays open.
    /// </summary>
    public async Task Close()
    {
        if (IsOpen is false) return;

        if (await ClosePanel(BitPanelDismissReason.Programmatic) is false) return;

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

    [JSInvokable("OnTransitionEnd")]
    public async Task _OnTransitionEnd()
    {
        if (IsDisposed) return;

        // The content of a closed panel is only taken out of the page once the panel has finished sliding
        // away with it, so the closing is still seen with something in it.
        if (IsOpen is false && KeepMounted is false && _contentRendered)
        {
            _contentRendered = false;

            await InvokeAsync(StateHasChanged);
        }

        await OnTransitionEnd.InvokeAsync(IsOpen);
    }

    [JSInvokable("OnClose")]
    public async Task _OnClose()
    {
        await ClosePanel(BitPanelDismissReason.Swipe);

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

        // The two layers are written as variables on the root rather than onto the two elements, so that the
        // panel keeps the one thing the layering contract of the library asks of it - the surface above the
        // overlay it comes with - wherever the pair of them is moved to.
        StyleBuilder.Register(() => ZIndex is null
            ? string.Empty
            : FormattableString.Invariant($"--bit-pnl-zin-ovl:{ZIndex};--bit-pnl-zin-cnt:{ZIndex + 1}"));

        // Only an absolutely positioned panel is laid out inside the scroller AutoToggleScroll takes the
        // overflow off, so only that one is pushed down by the room it gave back. A panel anchored to the
        // screen is positioned against the viewport, which never moved.
        StyleBuilder.Register(() => AbsolutePosition && _offsetTop > 0
            ? FormattableString.Invariant($"top:{_offsetTop}px")
            : string.Empty);
    }

    protected override void OnInitialized()
    {
        _headerId = $"BitPanel-{UniqueId}-header";
        _containerId = $"BitPanel-{UniqueId}-container";

        _contentRendered = IsOpen;

        base.OnInitialized();
    }

    protected override async Task OnParametersSetAsync()
    {
        await base.OnParametersSetAsync();

        // The content goes into the page on the first opening; whether it stays once the panel closes is
        // what KeepMounted decides, and the transition-end callback is where it goes back out.
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

        // The hold is taken on the element the parameters resolved to at the time it was taken, so
        // parameters that change while the panel is open have to be let go of and taken again - the panel
        // would otherwise be holding the element it was pointed at before while the one it is pointed at now
        // carries on scrolling.
        if (_scrollLocked && (_lockedScrollerSelector != ScrollerSelector ||
                              Nullable.Equals(_lockedScrollerElement, ScrollerElementTarget) is false))
        {
            await UnlockScroll();
        }

        if (ShouldLockScroll)
        {
            await LockScroll();
        }
        else
        {
            await UnlockScroll();
        }

        // As with the hold, the forwarding is registered against the scroller it was aimed at when it was
        // made, so a panel aimed somewhere else while it is open takes it back and makes it again.
        if (_scrollForwarded && (_forwardedScrollerSelector != ScrollerSelector ||
                                 Nullable.Equals(_forwardedScrollerElement, ScrollerElementTarget) is false))
        {
            await StopForwardScroll();
        }

        if (ShouldForwardScroll)
        {
            await ForwardScroll();
        }
        else
        {
            await StopForwardScroll();
        }
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        await base.OnAfterRenderAsync(firstRender);

        if (firstRender)
        {
            await SetupTransitionEnd();

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

            await LockScroll();

            await ForwardScroll();

            await SetupFocusTrap();

            // Reset before ToggleScroll: a panel that no longer toggles the scroll returns early from it and
            // would else be left with a stale top-offset from a previous opening.
            _offsetTop = 0;

            await ToggleScroll(true);

            // The top-offset only means anything to an absolutely positioned panel, so only that one is
            // asked to render again for the style ToggleScroll may just have changed.
            if (AbsolutePosition)
            {
                StyleBuilder.Reset();
                StateHasChanged();
            }

            await FocusPanel();

            await OnToggle.InvokeAsync(true);

            await OnOpen.InvokeAsync();
        }
        else
        {
            await DisposeFocusTrap();

            await UnlockScroll();

            await StopForwardScroll();

            await ToggleScroll(false);

            await RestoreFocusOrigin();

            var args = _dismissArgs ?? new();
            _dismissArgs = null;

            await OnToggle.InvokeAsync(false);

            await OnDismiss.InvokeAsync(args);
        }
    }



    // Every closing the panel performs itself comes through here, so that the one callback that can refuse a
    // closing is asked about all of them and the reason it is given is the real one. It reports whether the
    // panel was actually closed, which is what the callers that have to re-render on it go by.
    private async Task<bool> ClosePanel(BitPanelDismissReason reason, MouseEventArgs? e = null)
    {
        if (IsOpen is false) return false;

        // A disabled panel takes nothing from the user, but the code that owns it can always close it: a
        // panel disabled while it was open would otherwise be left on the screen with no way off it.
        if (IsEnabled is false && reason is not BitPanelDismissReason.Programmatic) return false;

        if (OnDismissing.HasDelegate)
        {
            var args = new BitPanelDismissArgs(reason, e);

            await OnDismissing.InvokeAsync(args);

            // The panel may have been closed - or taken off the page - while the callback was awaited, in
            // which case there is nothing left here to close.
            if (args.Cancel || IsDisposed || IsOpen is false) return false;
        }

        // Kept for the dismissal callback, which is invoked from the render that closes the panel rather than
        // from here, so that it is called for every way the panel can be closed and never before the page
        // shows it closed.
        _dismissArgs = e ?? new();

        if (await AssignIsOpen(false) is false)
        {
            _dismissArgs = null;
            return false;
        }

        return true;
    }

    private async Task HandleOnOverlayClick(MouseEventArgs e)
    {
        if (IsEnabled is false || IsOpen is false) return;

        await OnOverlayClick.InvokeAsync(e);

        if (Blocking) return;

        await ClosePanel(BitPanelDismissReason.Overlay, e);
    }

    private async Task HandleOnCloseClick(MouseEventArgs e)
    {
        await ClosePanel(BitPanelDismissReason.CloseButton, e);

        StateHasChanged();
    }

    private async Task HandleOnKeyDown(KeyboardEventArgs e)
    {
        if (IsOpen is false || IsEnabled is false) return;

        if (e.Key is not "Escape") return;

        // Reported before the dismissal is attempted, and reported whether or not there is going to be one,
        // so that a panel which refuses the key still hears it.
        await OnEscapeKeyDown.InvokeAsync(e);

        if (NoDismissOnEscape) return;

        await ClosePanel(BitPanelDismissReason.Escape);

        StateHasChanged();
    }

    // Whether the Escape key is the panel's to act on, which is also whether it stops at the panel rather
    // than carrying on up to whatever the panel was opened from.
    private bool DismissesOnEscape => IsOpen && IsEnabled && NoDismissOnEscape is false;

    // The scroller the panel holds while it is open, as an element where one is to be had. A selector the
    // consumer named beats the shell's scroller, since a panel inside a shell that names a region of its own
    // means that region; naming neither inside a shell means the shell, whose scroller is the only thing on
    // such a page that scrolls at all.
    private ElementReference? ScrollerElementTarget => ScrollerElement
                                                       ?? (ScrollerSelector.HasValue() ? null : AppShellContainer);

    // A panel that was taken out of view carries none of the behaviors that only make sense for one the user
    // can actually see, the hold on the page first of all.
    private bool IsShown => Visibility == BitVisibility.Visible;

    // Whether the page behind the panel is the panel's to hold. A modeless panel leaves it usable on
    // purpose, and a panel doing its own scroll handling holds its scroller itself.
    private bool ShouldLockScroll => NoScrollLock is false
                                     && AutoToggleScroll is false
                                     && Modeless is false
                                     && IsOpen
                                     && IsShown;

    // Whether the gestures that land on the panel are the page's rather than nobody's. A panel that leaves
    // the page scrolling still covers it with an overlay, and the layer that overlay sits in is fixed to the
    // viewport: the wheel and the touch drag that land on it are chained to the document, which in an
    // application shell - or in any layout that scrolls a region of its own - is not the thing that scrolls,
    // so the gesture reaches nothing at all and the page reads as held by a panel that holds nothing. It is
    // handed to that region by hand instead, for as long as such a panel is open.
    // Only the panel showing the overlay needs it: a modeless one lets the pointer through to the page,
    // which takes its own gestures. Only the panel holding nothing wants it: one holding the page has
    // nothing to forward, and one that took the overflow off its scroller (AutoToggleScroll) means that
    // scroller to stay still, so moving it from here would undo what it did. And only the panel aimed at a
    // scroller of its own can use it: the page is what the browser already chains to.
    private bool ShouldForwardScroll => Modeless is false
                                        && IsOpen
                                        && IsShown
                                        && ShouldLockScroll is false
                                        && AutoToggleScroll is false
                                        && (ScrollerElementTarget.HasValue || ScrollerSelector.HasValue());

    // Whether the panel slides in along the horizontal axis, which is what decides both the axis the swipe
    // gesture is locked to and which of the two coordinates the swipe callbacks are given.
    private bool IsHorizontal => (Position ?? BitPanelPosition.End) is BitPanelPosition.Start or BitPanelPosition.End;

    // Whether the content of the panel is in the page. It goes in on the first opening and comes back out
    // once the panel has finished sliding away, so every opening starts over; a KeepMounted panel keeps it
    // from the first opening on, so the state the content holds survives a close.
    private bool ContentRendered => _contentRendered;

    // A modeless panel leaves the page usable, so the keyboard is meant to reach it: trapping the focus in a
    // panel the user can still click out of would leave them unable to tab back to what they clicked on.
    private bool ShouldTrapFocus => NoFocusTrap is false && Modeless is false;

    // Whether the panel builds the chrome that goes around a body of its own. The close button counts: it is
    // rendered in the header row, so a panel that shows one has that row whether or not it was given a title
    // to put in it.
    private bool HasSections => Header is not null
                             || HeaderText.HasValue()
                             || Footer is not null
                             || FooterText.HasValue()
                             || ShowCloseButton;

    private string GetRole() => IsAlert ? "alertdialog" : "dialog";

    // A dialog needs an accessible name, and a panel that renders a header of its own is already showing the
    // name it should be given. A name set by hand wins over it, and so does an AriaLabel, which the panel
    // renders instead of pointing at an element.
    private string? GetTitleAriaId()
    {
        if (TitleAriaId.HasValue()) return TitleAriaId;

        if (AriaLabel.HasValue()) return null;

        return (Header is not null || HeaderText.HasValue()) ? _headerId : null;
    }

    // Only a panel that actually holds the page back is a modal one, and only while it is open: a panel that
    // reports itself as modal while the page behind it is still usable is telling a screen reader something
    // the user can prove wrong by clicking. The property belongs to the two dialog roles and to nothing else,
    // so a panel announced as something the user can walk past never carries it either.
    private string? GetAriaModal()
    {
        if (IsOpen is false || Modeless) return null;

        return (Role ?? GetRole()) is "dialog" or "alertdialog" ? "true" : null;
    }

    private string GetOverlayCssClasses()
    {
        List<string> classes = ["bit-pnl-ovl"];

        if (ModeFull)
        {
            classes.Add("bit-pnl-ovl-mfl");
        }

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

        // The scrolling moves from the panel to the body between the header and the footer, which is what
        // keeps those two put while the content underneath them moves.
        if (HasSections)
        {
            classes.Add("bit-pnl-sec");
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

    private async Task SetupTransitionEnd()
    {
        if (IsDisposed) return;

        _dotnetObj = DotNetObjectReference.Create(this);

        try
        {
            await _js.BitUtilsSetupTransitionEnd(_containerId, _dotnetObj);
        }
        catch (JSDisconnectedException) { } // we can ignore this exception here
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
        // Nothing is handed back by a panel that was told not to hand anything back, so nothing is recorded
        // for it either - the map would otherwise keep the element alive until the panel closes.
        if (NoAutoFocus || NoRestoreFocus || IsDisposed || IsRendered is false) return;

        try
        {
            await _js.BitUtilsCaptureFocusOrigin(_containerId);
        }
        catch (JSDisconnectedException) { } // we can ignore this exception here
    }

    private async Task RestoreFocusOrigin()
    {
        if (NoAutoFocus || NoRestoreFocus || IsDisposed) return;

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

    private async Task LockScroll()
    {
        if (ShouldLockScroll is false || _scrollLocked || IsDisposed || IsRendered is false) return;

        _scrollLocked = true;
        // What the scrollbar was taken from, so that it is given back to the same element even when the
        // parameters have changed in the meantime.
        _lockedScrollerSelector = ScrollerSelector;
        var element = ScrollerElementTarget;
        _lockedScrollerElement = element;

        try
        {
            if (element.HasValue)
            {
                await _js.BitUtilsLockScroll(_containerId, element.Value);
            }
            else
            {
                await _js.BitUtilsLockScroll(_containerId, _lockedScrollerSelector);
            }
        }
        catch (JSDisconnectedException) { } // we can ignore this exception here
    }

    private async Task UnlockScroll()
    {
        // Only what was taken is handed back, and the hold is given up whether or not the call goes through,
        // so a panel can never end up holding a page it has already let go of.
        if (_scrollLocked is false) return;

        _scrollLocked = false;
        _lockedScrollerSelector = null;
        _lockedScrollerElement = null;

        try
        {
            await _js.BitUtilsUnlockScroll(_containerId);
        }
        catch (JSDisconnectedException) { } // we can ignore this exception here
    }

    // Hands the gestures that land on the panel to the scroller behind it, for the panel that covers a page
    // it was told not to hold: what the layer catches would else be chained to a document that does not
    // scroll. Only the gestures nothing inside the panel took first are forwarded, which the script decides.
    private async Task ForwardScroll()
    {
        if (ShouldForwardScroll is false || _scrollForwarded || IsDisposed || IsRendered is false) return;

        _scrollForwarded = true;
        _forwardedScrollerSelector = ScrollerSelector;
        var element = ScrollerElementTarget;
        _forwardedScrollerElement = element;

        try
        {
            if (element.HasValue)
            {
                await _js.BitUtilsForwardScroll(_containerId, _Id, element.Value);
            }
            else
            {
                await _js.BitUtilsForwardScroll(_containerId, _Id, _forwardedScrollerSelector);
            }
        }
        catch (JSDisconnectedException) { } // we can ignore this exception here
    }

    // Takes the forwarding back, and only what was registered, so a panel never ends up handing gestures to
    // a scroller it has already let go of.
    private async Task StopForwardScroll()
    {
        if (_scrollForwarded is false) return;

        _scrollForwarded = false;
        _forwardedScrollerSelector = null;
        _forwardedScrollerElement = null;

        try
        {
            await _js.BitUtilsStopForwardScroll(_containerId);
        }
        catch (JSDisconnectedException) { } // we can ignore this exception here
    }

    // The scroll handling a panel does itself, as opposed to the hold it takes on the page through
    // LockScroll: the overflow of the scroller is taken away while the panel is open and handed back once it
    // closes, and the room that gave back is what an absolutely positioned panel is pushed down by.
    private async Task ToggleScroll(bool isOpen)
    {
        if (isOpen)
        {
            // The decision is taken at open time; the close reuses it instead of re-reading
            // AutoToggleScroll, which may have changed since the panel was opened.
            _scrollToggledOnOpen = AutoToggleScroll && IsDisposed is false && IsRendered;
            if (_scrollToggledOnOpen is false) return;

            // The scroller is snapshotted with it, so the close hands back the same one even if
            // ScrollerElement / ScrollerSelector changed in the meantime.
            _scrollerElementOnToggle = ScrollerElementTarget;
            _scrollerSelectorOnToggle = ScrollerSelector;
        }
        else
        {
            // Only hand the overflow back if it was actually taken away, regardless of the current value.
            if (_scrollToggledOnOpen is false) return;

            _scrollToggledOnOpen = false;
        }

        try
        {
            _offsetTop = _scrollerElementOnToggle.HasValue
                ? await _js.BitUtilsToggleOverflow(_containerId, _scrollerElementOnToggle.Value, isOpen)
                : await _js.BitUtilsToggleOverflow(_containerId, _scrollerSelectorOnToggle ?? "body", isOpen);
        }
        catch (JSDisconnectedException) { } // we can ignore this exception here
    }



    protected override async ValueTask DisposeAsync(bool disposing)
    {
        if (IsDisposed || disposing is false) return;

        // A panel taken off the page while it was open would otherwise leave the page without its scrollbar
        // and with a focus trap registered on an element that no longer exists.
        await UnlockScroll();

        await StopForwardScroll();

        await ToggleScroll(false);

        await DisposeFocusTrap();

        try
        {
            await _js.BitUtilsDisposeFocusOrigin(_containerId);
        }
        catch (JSDisconnectedException) { } // we can ignore this exception here

        await DisposeSwipes();

        try
        {
            await _js.BitUtilsDisposeTransitionEnd(_containerId);
        }
        catch (JSDisconnectedException) { } // we can ignore this exception here

        _dotnetObj?.Dispose();
        _dotnetObj = null;

        await base.DisposeAsync(disposing);
    }
}
