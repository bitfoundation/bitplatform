namespace Bit.BlazorUI;

/// <summary>
/// Modals are temporary pop-ups that take focus from the page or app and require people to interact with them.
/// </summary>
/// <remarks>
/// The dialog behaviors every modal owes its user are handled here: the focus moves into the Modal when it opens,
/// Tab keeps cycling inside it while it is open, Escape dismisses it, and the focus goes back to whatever opened it
/// once it closes. Each of those can be turned off on its own.
/// <br/>
/// The Modal is a surface first: what goes in it is the content of the consumer. It also offers a header / body /
/// footer chrome of its own - a title, a close button, a footer for the actions - which is rendered only for the
/// Modals that ask for it, so a Modal given nothing but its content still renders nothing but that content.
/// </remarks>
public partial class BitModal : BitComponentBase
{
    /// <summary>
    /// The default title (and aria-label) used for the close button when none is provided.
    /// </summary>
    internal const string DefaultCloseButtonTitle = "Close";

    private bool _internalIsOpen;
    private bool _focusTrapped;
    private bool _focusStored;
    private bool _scrollLocked;
    // The selector the current hold was taken with, so that a selector changed while the Modal is open is
    // noticed: the hold is registered against the element the selector resolved to, not against the selector.
    private string? _lockedScrollerSelector;
    // The element the current hold was taken on, for the same reason and for the holds taken on an element
    // rather than on a selector - the scroller of an application shell, first of all.
    private ElementReference? _lockedScrollerElement;
    // Whether the gestures that land on the Modal are being handed to the scroller behind it, and what they
    // are being handed to - recorded for the same reason the hold above records what it took: a Modal aimed
    // somewhere else while it is open has to take the registration back and make it again.
    private bool _scrollForwarded;
    private string? _forwardedScrollerSelector;
    private ElementReference? _forwardedScrollerElement;
    private bool _hasBeenOpened;
    private bool _contentFocused;
    private string _containerId = default!;

    // The room the scroller gave back when its overflow was toggled off, which an absolutely positioned Modal
    // is pushed down by so that it stays where the eye left it rather than jumping to the top of the scroller.
    private float _offsetTop;
    // Whether the overflow of a scroller was actually toggled during the open sequence, so the close sequence
    // toggles it back if and only if it was toggled, regardless of later changes to AutoToggleScroll.
    private bool _scrollToggledOnOpen;
    // Snapshots of the scroller target captured during open, so the close sequence restores the exact same
    // scroller even if ScrollerElement/ScrollerSelector changed since the Modal was opened.
    private ElementReference? _scrollerElementOnToggle;
    private string? _scrollerSelectorOnToggle;
    // Snapshots the drag element selector the drag handlers were registered with, so teardown unregisters the
    // exact same selector even if DragElementSelector changed since the Modal was opened.
    private string? _dragElementSelectorOnSetup;
    private bool _dragSetup;

    // Which of the two interchangeable "refused" animations the content is carrying, 0 for none. Two
    // classes carrying the same movement under different names are alternated rather than one being added
    // and taken away again: an animation only restarts when the animation-name it resolves to changes, and
    // taking the class off would resolve back to the entry animation and replay that instead.
    private int _bounce;

    // Stable EventCallback wrappers created once (in OnInitialized) instead of on every
    // BuildParameters call. These are only invoked internally (not passed to a child), so
    // re-creating them per render did not defeat change detection, but it did allocate two
    // closures each OnParametersSet. Their bodies read the current property / cascaded
    // parameter values at invoke time, so they remain correct while avoiding the allocations.
    private EventCallback<MouseEventArgs> _onDismiss;
    private EventCallback<MouseEventArgs> _onOverlayClick;
    private EventCallback<KeyboardEventArgs> _onEscapeKeyDown;
    private EventCallback _onOpen;

    // Memoizes the merged HtmlAttributes dictionary so BuildParameters doesn't re-run the
    // Concat/GroupBy/ToDictionary allocation on every OnParametersSet when neither the own nor the
    // cascaded HtmlAttributes content changed. The last-seen sources are stored as content snapshots
    // (copies) rather than references so that in-place mutations of the live dictionaries are detected.
    private Dictionary<string, object>? _mergedHtmlAttributes;
    private Dictionary<string, object>? _lastOwnHtmlAttributes;
    private Dictionary<string, object>? _lastCascadedHtmlAttributes;

    // Snapshots of the scalar values the class/style builders consume. The Classes/Styles inputs are
    // mutable: their members can change without the instance reference changing (e.g. via
    // BitModalService.Refresh), so we compare against these value snapshots rather than references.
    private string? _lastClassesRoot;
    private string? _lastParamsClassesRoot;
    private string? _lastStylesRoot;
    private string? _lastParamsStylesRoot;



    [Inject] private IJSRuntime _js { get; set; } = default!;



    /// <summary>
    /// When true, the Modal is positioned absolute instead of fixed, so that it covers the element it was
    /// declared inside of rather than the screen.
    /// </summary>
    /// <remarks>
    /// The element it is declared inside of has to establish a containing block of its own
    /// (<c>position: relative</c>) for this to place the Modal over it rather than over the page.
    /// </remarks>
    [Parameter, ResetClassBuilder]
    public bool AbsolutePosition { get; set; }

    /// <summary>
    /// Whether the Modal should be announced as modal to assistive technologies.
    /// </summary>
    /// <remarks>
    /// This is also what decides whether the Modal behaves as a modal dialog: a Modal announced as one keeps
    /// the keyboard inside itself while it is open, and one that is not (a modeless Modal) leaves the page
    /// behind it reachable with the keyboard the way it is reachable with the pointer.
    /// </remarks>
    [Parameter] public bool AriaModal { get; set; } = true;

    /// <summary>
    /// Enables the auto scrollbar toggle behavior of the Modal, which takes the overflow off the scroller
    /// while the Modal is open and hands it back once it closes.
    /// </summary>
    /// <remarks>
    /// This is the Modal holding the scroller itself, so the hold it would otherwise take on the page through
    /// <see cref="NoScrollLock"/> is stood down for it - the two would else both be holding the same page.
    /// The scroller is named by <see cref="ScrollerElement"/> or <see cref="ScrollerSelector"/>; when neither
    /// is set it is the scroller of the application shell the Modal is inside of, and the page when it is
    /// inside none.
    /// </remarks>
    [Parameter] public bool AutoToggleScroll { get; set; }

    /// <summary>
    /// When enabled, prevents the Modal from being light dismissed by clicking outside the Modal (on the overlay).
    /// </summary>
    /// <remarks>
    /// Only the overlay click is blocked. Escape still dismisses the Modal unless <see cref="NoDismissOnEscape"/>
    /// is also set, so that a Modal which blocks the pointer can still choose whether it blocks the keyboard.
    /// </remarks>
    [Parameter] public bool Blocking { get; set; }

    /// <summary>
    /// The content of the body section of the Modal, the alias of <see cref="ChildContent"/>.
    /// </summary>
    /// <remarks>
    /// Takes precedence over <see cref="ChildContent"/>, which is what a Modal that also declares a
    /// <see cref="Header"/> or a <see cref="Footer"/> uses to keep the three of them side by side.
    /// </remarks>
    [Parameter] public RenderFragment? Body { get; set; }

    /// <summary>
    /// The content of the Modal, it can be any custom tag or text.
    /// </summary>
    [Parameter] public RenderFragment? ChildContent { get; set; }

    /// <summary>
    /// Custom CSS classes for different parts of the BitModal component.
    /// </summary>
    [Parameter] public BitModalClassStyles? Classes { get; set; }

    /// <summary>
    /// The title (and aria-label) of the close button for accessibility and localization.
    /// Defaults to "Close" when not set.
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
    /// The initial opening state of the Modal in the uncontrolled mode, which is when the
    /// <see cref="IsOpen"/> parameter is not set.
    /// </summary>
    [Parameter] public bool? DefaultIsOpen { get; set; }

    private BitModalParameters _modalParameters = new();
    [CascadingParameter]
    private BitModalParameters? ModalParameters
    {
        // Tolerate a null cascading value (e.g. ModalParameters="null"): fall back to a fresh
        // instance so downstream consumers never NRE.
        get => _modalParameters;
        set => _modalParameters = value ?? new();
    }

    // The scroller of the application shell the Modal was declared inside of, cascaded by BitAppShell under
    // this well-known name. A shell scrolls a region of its own rather than the page, so the body of such an
    // app never scrolls and a hold taken on it would hold nothing: this is the element to hold instead, for a
    // Modal that has not been pointed at a scroller of its own. The name is written out rather than taken
    // from BitAppShell.Container because the shell lives in Bit.BlazorUI.Extras, which this assembly cannot
    // reference - the string is the contract between the two.
    [CascadingParameter(Name = "BitAppShell.Container")]
    private ElementReference? AppShellContainer { get; set; }

    // The effective parameters: this component's own parameters merged with the cascaded
    // BitModalParameters (the latter supplied by the BitModalService). The component's own
    // parameters take precedence. Rebuilt in OnParametersSet whenever either source changes.
    private BitModalParameters _params = new();


    /// <summary>
    /// The CSS selector of the drag element, which is the content of the Modal by default.
    /// </summary>
    /// <remarks>
    /// Ignored by a Modal that is not <see cref="Draggable"/>. Naming a part of the content - a header bar -
    /// is what keeps the rest of the Modal usable while it is draggable: a drag started anywhere on the
    /// content is a drag the selection and the scrolling inside it never get.
    /// </remarks>
    [Parameter] public string? DragElementSelector { get; set; }

    /// <summary>
    /// Whether the Modal can be dragged around.
    /// </summary>
    [Parameter] public bool Draggable { get; set; }

    /// <summary>
    /// The template used to render the footer section of the Modal.
    /// </summary>
    [Parameter] public RenderFragment? Footer { get; set; }

    /// <summary>
    /// The text of the footer section of the Modal.
    /// </summary>
    [Parameter] public string? FooterText { get; set; }

    /// <summary>
    /// Makes the Modal height 100% of its parent container.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public bool FullHeight { get; set; }

    /// <summary>
    /// Makes the Modal width and height 100% of its parent container.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public bool FullSize { get; set; }

    /// <summary>
    /// Makes the Modal width 100% of its parent container.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public bool FullWidth { get; set; }

    /// <summary>
    /// The template used to render the header section of the Modal.
    /// </summary>
    /// <remarks>
    /// Takes precedence over <see cref="HeaderText"/> when both are set. Point <see cref="TitleAriaId"/> at
    /// the title inside it so that the Modal is announced by the name it shows.
    /// </remarks>
    [Parameter] public RenderFragment? Header { get; set; }

    /// <summary>
    /// The text of the header section of the Modal.
    /// </summary>
    [Parameter] public string? HeaderText { get; set; }

    /// <summary>
    /// The CSS height of the Modal, for the Modals whose height is not the one their content happens to have.
    /// </summary>
    /// <remarks>
    /// Any CSS length (<c>24rem</c>, <c>60vh</c>, <c>480px</c>). A Modal is as tall as what is inside it when
    /// this is not set, capped to the room the screen has - which is what a dialog usually wants. Set it where
    /// the height has to stand still whatever is inside it: a step of a flow whose panes are of different
    /// lengths, a surface with a scrolling list in it.
    /// <br/>
    /// It is written as an inline style on the content box, so it takes precedence over
    /// <see cref="FullHeight"/> and over the height a stylesheet gives the Modal, and it is capped by
    /// <see cref="MaxHeight"/> - or, when that is not set either, by the height of the screen.
    /// </remarks>
    [Parameter] public string? Height { get; set; }

    /// <summary>
    /// Determines the ARIA role of the Modal (alertdialog/dialog).
    /// </summary>
    /// <remarks>
    /// A <see cref="Blocking"/> Modal that is not <see cref="Modeless"/> announces itself as an
    /// <c>alertdialog</c> when this is not set, since a surface that refuses to be dismissed by a click
    /// outside of it is one that is waiting to be answered.
    /// </remarks>
    [Parameter] public bool? IsAlert { get; set; }

    /// <summary>
    /// Whether the Modal is displayed.
    /// </summary>
    [Parameter, TwoWayBound]
    [CallOnSet(nameof(OnSetIsOpen))]
    public bool IsOpen { get; set; }

    /// <summary>
    /// Keeps the Modal in the page while it is closed instead of taking it out and building it again the
    /// next time it opens.
    /// </summary>
    /// <remarks>
    /// A Modal is built when it opens and taken away when it closes, which is what keeps a page that declares
    /// many of them cheap - and what makes each of them start over: a half-filled form inside one is gone by
    /// the time it is opened again. Keeping it mounted hides it instead, so its content, and whatever state
    /// that content holds, is still there the next time it opens. A kept Modal is <c>inert</c> and hidden
    /// from assistive technologies while it is closed, and nothing of it is rendered until the first time it
    /// opens, so a Modal that is never opened still costs nothing.
    /// </remarks>
    [Parameter] public bool KeepMounted { get; set; }

    /// <summary>
    /// The CSS height the Modal is not to grow past, however long its content is.
    /// </summary>
    /// <remarks>
    /// Any CSS length (<c>32rem</c>, <c>80vh</c>, <c>640px</c>). The height of the screen is the cap when this
    /// is not set, which is what keeps a Modal longer than the screen reachable: it scrolls inside itself
    /// rather than running off both ends of a page whose own scrolling never brings it back. A smaller cap
    /// leaves the Modal short of the screen edge and starts its scrolling sooner.
    /// </remarks>
    [Parameter] public string? MaxHeight { get; set; }

    /// <summary>
    /// The CSS width the Modal is not to grow past, however wide its content is.
    /// </summary>
    /// <remarks>
    /// Any CSS length (<c>40rem</c>, <c>90vw</c>, <c>600px</c>). The width of the screen is the cap when this
    /// is not set, which leaves a Modal as wide as its content - and on a wide screen that can be a line of
    /// text too long to read comfortably. This is the parameter that gives a Modal the measure a dialog wants,
    /// and the one the <c>small</c> / <c>large</c> sizes of other dialog components amount to.
    /// </remarks>
    [Parameter] public string? MaxWidth { get; set; }

    /// <summary>
    /// Renders the overlay in full mode that gives it an opaque background.
    /// </summary>
    /// <remarks>
    /// The overlay catches the clicks meant for the page behind it either way; this is what makes it dim that
    /// page as well, for the Modals that are to be the only thing being read.
    /// </remarks>
    [Parameter, ResetClassBuilder]
    public bool ModeFull { get; set; }

    /// <summary>
    /// Whether the Modal should be modeless (e.g. not dismiss when focusing/clicking outside of the Modal).
    /// If true: <see cref="Blocking"/> is ignored, and there will be no overlay.
    /// </summary>
    /// <remarks>
    /// A modeless Modal leaves the page behind it usable, so it neither reports itself modal (see
    /// <see cref="AriaModal"/>), nor holds the keyboard, nor holds the page still.
    /// </remarks>
    [Parameter, ResetClassBuilder]
    public bool Modeless { get; set; }

    /// <summary>
    /// Prevents the Modal from moving the focus into itself when it opens, for the cases where the focus is
    /// placed by the consumer instead.
    /// </summary>
    /// <remarks>
    /// By default the focus moves to the first focusable element of the content, or to the content itself
    /// when it holds none, so the keyboard is where the Modal is. An element inside the content marked with
    /// the <c>data-autofocus</c> attribute takes the focus instead of the first one, which is how a Modal
    /// whose first focusable element is not the one worth starting at (a close button ahead of the field the
    /// Modal was opened to fill in) names the one that is.
    /// <br/>
    /// Setting this leaves the focus wherever it was, which is only worth doing when the consumer places it
    /// itself: the focus trap and the Escape dismissal both work off the keys pressed inside the Modal, so
    /// neither of them applies for as long as the focus stays outside of it.
    /// </remarks>
    [Parameter] public bool NoAutoFocus { get; set; }

    /// <summary>
    /// Removes the default top border of the Modal.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public bool NoBorder { get; set; }

    /// <summary>
    /// Prevents the Modal from being dismissed by pressing the Escape key.
    /// </summary>
    [Parameter] public bool NoDismissOnEscape { get; set; }

    /// <summary>
    /// Prevents the Modal from keeping the keyboard focus inside itself while it is open.
    /// </summary>
    /// <remarks>
    /// A modal dialog holds the tab sequence: without the trap, Tab walks out of the Modal and into the page
    /// behind it, where the overlay swallows every click that could bring the focus back. The trap is only
    /// set up for a Modal that reports itself modal (see <see cref="AriaModal"/>) in the first place.
    /// </remarks>
    [Parameter] public bool NoFocusTrap { get; set; }

    /// <summary>
    /// Prevents the Modal from handing the focus back to the element that had it before the Modal opened.
    /// </summary>
    /// <remarks>
    /// The focus is only handed back when nothing else has taken it in the meantime, so a close handler that
    /// deliberately moves the focus somewhere keeps it there whether or not this is set.
    /// </remarks>
    [Parameter] public bool NoRestoreFocus { get; set; }

    /// <summary>
    /// Prevents the Modal from holding the page still while it is open.
    /// </summary>
    /// <remarks>
    /// A modal surface takes the page over, and a page that carries on scrolling behind it moves what the
    /// user is coming back to out from under them, so the page is held for as long as the Modal is open. The
    /// room the scrollbar took is added back as padding while it is held, so that taking the scrollbar away
    /// shifts nothing sideways, and the locks are counted: two Modals open at once both hold the page and it
    /// is only handed back once the last of them closes.
    /// <br/>
    /// A Modal that reports itself modeless (see <see cref="Modeless"/>) never holds the page in the first
    /// place, since it is meant to leave what is behind it usable, and a Modal that does its own scroll
    /// handling through <see cref="AutoToggleScroll"/> holds its scroller itself, so this hold is stood down
    /// for it whether or not this is set.
    /// <br/>
    /// The layer the Modal is drawn in is fixed to the viewport, so the wheel and the touch drag that
    /// land on it are chained to the document rather than to whatever region the app scrolls: they are
    /// handed to that region - the one <see cref="ScrollerElement"/> or <see cref="ScrollerSelector"/>
    /// names, or the scroller of the application shell the Modal is inside of - for as long as a Modal
    /// that leaves the page scrolling is open, so that the page moves under the gesture the way it would
    /// with no Modal over it. Anything inside the Modal that scrolls itself takes its own gestures first.
    /// </remarks>
    [Parameter] public bool NoScrollLock { get; set; }

    /// <summary>
    /// A callback function for when the Modal is dismissed.
    /// </summary>
    [Parameter] public EventCallback<MouseEventArgs> OnDismiss { get; set; }

    /// <summary>
    /// A callback function for when the Escape key is pressed inside the Modal.
    /// </summary>
    /// <remarks>
    /// Invoked for every Escape, including the ones a Modal with <see cref="NoDismissOnEscape"/> refuses to
    /// be dismissed by, which makes it the counterpart of <see cref="OnOverlayClick"/> for the keyboard: the
    /// place to react to a dismissal that was turned down, or to close a Modal on terms of its own.
    /// </remarks>
    [Parameter] public EventCallback<KeyboardEventArgs> OnEscapeKeyDown { get; set; }

    /// <summary>
    /// A callback function for when the Modal is opened.
    /// </summary>
    /// <remarks>
    /// Invoked after the Modal has rendered and its focus handling has run, so the content is in the page
    /// and can be measured or scripted from here.
    /// </remarks>
    [Parameter] public EventCallback OnOpen { get; set; }

    /// <summary>
    /// A callback function for when somewhere on the overlay element of the Modal is clicked.
    /// </summary>
    /// <remarks>
    /// Invoked for every overlay click, including the ones a <see cref="Blocking"/> Modal refuses to be
    /// dismissed by, which is what makes it the place to react to a click that was turned down.
    /// </remarks>
    [Parameter] public EventCallback<MouseEventArgs> OnOverlayClick { get; set; }

    /// <summary>
    /// Position of the Modal on the screen.
    /// </summary>
    /// <remarks>
    /// The Modal sits in the middle of the screen when this is not set. The <c>Start</c> / <c>End</c> members
    /// are the direction-aware counterparts of the <c>Left</c> / <c>Right</c> ones.
    /// </remarks>
    [Parameter, ResetClassBuilder]
    public BitPosition? Position { get; set; }

    /// <summary>
    /// The element reference of the scroller the Modal toggles the overflow of while it is open.
    /// </summary>
    /// <remarks>
    /// Takes precedence over <see cref="ScrollerSelector"/> when both are set, and over the scroller a
    /// <c>BitAppShell</c> cascades. Read by both scroll holds: the one the Modal takes on the page by
    /// default and the overflow toggle of <see cref="AutoToggleScroll"/>.
    /// </remarks>
    [Parameter] public ElementReference? ScrollerElement { get; set; }

    /// <summary>
    /// The CSS selector of the element whose scrolling the Modal holds while it is open, for the layouts
    /// whose scroller is not the page itself.
    /// </summary>
    /// <remarks>
    /// A Modal inside a <c>BitAppShell</c> holds the shell's scroller without being told to, since the shell
    /// cascades it; the page (<c>body</c>) is what is held when there is no shell and this is not set, which
    /// is the scroller of an ordinary page. Any other layout that scrolls a region of its own - a fixed
    /// header over a scrolling main area - names that region here, since holding a page that never scrolls
    /// holds nothing. Ignored by a Modal that holds nothing in the first place (see <see cref="NoScrollLock"/>).
    /// </remarks>
    [Parameter] public string? ScrollerSelector { get; set; }

    /// <summary>
    /// Shows the close button of the Modal.
    /// </summary>
    [Parameter] public bool ShowCloseButton { get; set; }

    /// <summary>
    /// Custom CSS styles for different parts of the BitModal component.
    /// </summary>
    [Parameter] public BitModalClassStyles? Styles { get; set; }

    /// <summary>
    /// ARIA id for the subtitle of the Modal, if any.
    /// </summary>
    [Parameter] public string? SubtitleAriaId { get; set; }

    /// <summary>
    /// ARIA id for the title of the Modal, if any.
    /// </summary>
    [Parameter] public string? TitleAriaId { get; set; }

    /// <summary>
    /// The CSS width of the Modal, for the Modals whose width is not the one their content happens to have.
    /// </summary>
    /// <remarks>
    /// Any CSS length (<c>32rem</c>, <c>50vw</c>, <c>560px</c>). A Modal is as wide as what is inside it when
    /// this is not set, capped to the width of the screen. Set it where the width has to stand still whatever
    /// is inside it - the panes of a wizard, a dialog whose content is loaded after it opens - so that the
    /// Modal does not resize under the user as its content arrives.
    /// <br/>
    /// It is written as an inline style on the content box, so it takes precedence over <see cref="FullWidth"/>
    /// and over the width a stylesheet gives the Modal, and it is capped by <see cref="MaxWidth"/> - or, when
    /// that is not set either, by the width of the screen.
    /// </remarks>
    [Parameter] public string? Width { get; set; }



    /// <summary>
    /// Opens the Modal.
    /// </summary>
    public async Task Open()
    {
        if (await AssignIsOpen(true) is false) return;

        StateHasChanged();
    }

    /// <summary>
    /// Closes the Modal.
    /// </summary>
    public async Task Close()
    {
        if (await AssignIsOpen(false) is false) return;

        StateHasChanged();
    }

    /// <summary>
    /// Toggles the Modal between its open and closed states.
    /// </summary>
    public async Task Toggle()
    {
        if (await AssignIsOpen(IsOpen is false) is false) return;

        StateHasChanged();
    }



    protected override string RootElementClass => "bit-mdl";

    protected override void RegisterCssClasses()
    {
        ClassBuilder.Register(() => Classes?.Root);
        ClassBuilder.Register(() => _params.Classes?.Root);

        ClassBuilder.Register(() => IsFullHeight ? "bit-mdl-fhe" : string.Empty);
        ClassBuilder.Register(() => IsFullWidth ? "bit-mdl-fwi" : string.Empty);
        ClassBuilder.Register(() => (_params.ModeFull ?? false) ? "bit-mdl-mfl" : string.Empty);
        ClassBuilder.Register(() => (_params.NoBorder ?? false) ? string.Empty : "bit-mdl-bdr");
        ClassBuilder.Register(() => (_params.AbsolutePosition ?? false) ? "bit-mdl-abs" : string.Empty);
        ClassBuilder.Register(() => _params.Position switch
        {
            BitPosition.TopLeft => "bit-mdl-tlf",
            BitPosition.TopCenter => "bit-mdl-tcr",
            BitPosition.TopRight => "bit-mdl-trg",
            BitPosition.TopStart => "bit-mdl-tst",
            BitPosition.TopEnd => "bit-mdl-ten",
            BitPosition.CenterLeft => "bit-mdl-clf",
            BitPosition.Center => "bit-mdl-ctr",
            BitPosition.CenterRight => "bit-mdl-crg",
            BitPosition.CenterStart => "bit-mdl-cst",
            BitPosition.CenterEnd => "bit-mdl-cen",
            BitPosition.BottomLeft => "bit-mdl-blf",
            BitPosition.BottomCenter => "bit-mdl-bcr",
            BitPosition.BottomRight => "bit-mdl-brg",
            BitPosition.BottomStart => "bit-mdl-bst",
            BitPosition.BottomEnd => "bit-mdl-ben",
            _ => string.Empty
        });
    }

    protected override void RegisterCssStyles()
    {
        StyleBuilder.Register(() => Styles?.Root);
        StyleBuilder.Register(() => _params.Styles?.Root);

        StyleBuilder.Register(() => _offsetTop > 0 ? FormattableString.Invariant($"top:{_offsetTop}px") : string.Empty);
    }

    protected override void OnInitialized()
    {
        _containerId = $"BitModal-{UniqueId}-container";

        // The uncontrolled starting state, which only applies while the consumer is not driving IsOpen
        // itself. It is read once here rather than every time the parameters are set, so that closing an
        // uncontrolled Modal is not undone by the next render.
        if (IsOpenHasBeenSet is false && DefaultIsOpen.HasValue)
        {
            IsOpen = DefaultIsOpen.Value;
        }

        // Create the event callbacks once. They read the current OnXxx properties and the
        // cascaded ModalParameters at invoke time, so they stay correct without being rebuilt
        // every render.
        _onDismiss = EventCallback.Factory.Create<MouseEventArgs>(this, async (MouseEventArgs e) =>
        {
            await OnDismiss.InvokeAsync(e);
            await ModalParameters!.OnDismiss.InvokeAsync(e);
        });
        _onOverlayClick = EventCallback.Factory.Create<MouseEventArgs>(this, async (MouseEventArgs e) =>
        {
            await OnOverlayClick.InvokeAsync(e);
            await ModalParameters!.OnOverlayClick.InvokeAsync(e);
        });
        _onEscapeKeyDown = EventCallback.Factory.Create<KeyboardEventArgs>(this, async (KeyboardEventArgs e) =>
        {
            await OnEscapeKeyDown.InvokeAsync(e);
            await ModalParameters!.OnEscapeKeyDown.InvokeAsync(e);
        });
        _onOpen = EventCallback.Factory.Create(this, async () =>
        {
            await OnOpen.InvokeAsync();
            await ModalParameters!.OnOpen.InvokeAsync();
        });

        base.OnInitialized();
    }

    protected override void OnParametersSet()
    {
        var previous = _params;

        _params = BuildParameters();

        // The [ResetClassBuilder] attribute only resets ClassBuilder when this component's own
        // parameters change. However, the registered class/style lambdas also read the (own and
        // cascaded) Classes/Styles values. Those are mutable inputs: mutating their members in place
        // (e.g. via BitModalService.Refresh) doesn't change the instance reference, so a reference
        // comparison can miss the change and leave the builders with stale cached values. Compare the
        // actual scalar values the builders consume against the previous snapshot so that any change
        // is detected, regardless of whether the instance reference changed.
        var classesRoot = Classes?.Root;
        var paramsClassesRoot = _params.Classes?.Root;
        if (previous.FullHeight != _params.FullHeight ||
            previous.FullWidth != _params.FullWidth ||
            previous.FullSize != _params.FullSize ||
            previous.ModeFull != _params.ModeFull ||
            previous.NoBorder != _params.NoBorder ||
            previous.AbsolutePosition != _params.AbsolutePosition ||
            previous.Position != _params.Position ||
            _lastClassesRoot != classesRoot ||
            _lastParamsClassesRoot != paramsClassesRoot)
        {
            ClassBuilder.Reset();
        }
        _lastClassesRoot = classesRoot;
        _lastParamsClassesRoot = paramsClassesRoot;

        var stylesRoot = Styles?.Root;
        var paramsStylesRoot = _params.Styles?.Root;
        if (_lastStylesRoot != stylesRoot ||
            _lastParamsStylesRoot != paramsStylesRoot)
        {
            StyleBuilder.Reset();
        }
        _lastStylesRoot = stylesRoot;
        _lastParamsStylesRoot = paramsStylesRoot;

        base.OnParametersSet();
    }

    protected override async Task OnParametersSetAsync()
    {
        await base.OnParametersSetAsync();

        // The focus trap and the scroll lock are both registered against the open Modal, so turning either
        // of them on or off while the Modal is open has to reach the already registered one rather than wait
        // for the next time it opens.
        if (IsRendered is false || IsOpen is false || _internalIsOpen is false) return;

        if (ShouldTrapFocus)
        {
            await SetupFocusTrap();
        }
        else
        {
            await DisposeFocusTrap();
        }

        // The hold is taken on the element the selector named at the time it was taken, so a selector that
        // changes while the Modal is open has to be let go of and taken again - the Modal would otherwise be
        // holding the element it was pointed at before while the page it is pointed at now carries on scrolling.
        if (_scrollLocked && (_lockedScrollerSelector != _params.ScrollerSelector ||
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
        // made, so a Modal aimed somewhere else while it is open takes it back and makes it again.
        if (_scrollForwarded && (_forwardedScrollerSelector != _params.ScrollerSelector ||
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

        await FocusContent();
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        await base.OnAfterRenderAsync(firstRender);

        if (_internalIsOpen == IsOpen) return;

        _internalIsOpen = IsOpen;

        if (IsOpen)
        {
            await HandleOnOpened();
        }
        else
        {
            await HandleOnClosed();
        }
    }



    private async Task HandleOnOpened()
    {
        // Remembered from the first opening on, so that a kept-mounted Modal stays in the page from then on
        // while one that has never been opened is never rendered at all.
        _hasBeenOpened = true;

        // The focus is recorded before anything is done with it, while it is still on whatever opened the
        // Modal: this is the element it goes back to once the Modal closes.
        await StoreFocus();

        await SetupFocusTrap();

        await LockScroll();

        await ForwardScroll();

        await SetupDrag();

        // Reset before ToggleScroll: a Modal that no longer toggles the scroll returns early from it and
        // would else be left with a stale top-offset from a previous opening.
        _offsetTop = 0;

        await ToggleScroll(true);

        // The top-offset only means anything to an absolutely positioned Modal, so only that one is asked to
        // render again for the style ToggleScroll may just have changed.
        if (_params.AbsolutePosition ?? false)
        {
            StyleBuilder.Reset();
            StateHasChanged();
        }

        await FocusContent();

        await _params.OnOpen.InvokeAsync();
    }

    private async Task HandleOnClosed()
    {
        _contentFocused = false;

        await DisposeFocusTrap();

        await UnlockScroll();

        await StopForwardScroll();

        await RemoveDrag();

        await ToggleScroll(false);

        await RestoreFocus();
    }

    private async Task HandleOnOverlayClick(MouseEventArgs e)
    {
        if (_params.IsEnabled is false) return;

        await _params.OnOverlayClick.InvokeAsync(e);

        if (_params.Blocking ?? false)
        {
            Bounce();
            return;
        }

        await AssignIsOpen(false);
    }

    // Escape dismisses the Modal from anywhere inside it, as the dialog pattern requires. The key is only
    // seen while the focus is inside the Modal, which is where it is put when the Modal opens.
    private async Task HandleOnKeyDown(KeyboardEventArgs e)
    {
        if (_params.IsEnabled is false) return;

        if (e.Key is not "Escape") return;

        await _params.OnEscapeKeyDown.InvokeAsync(e);

        if (_params.NoDismissOnEscape ?? false)
        {
            Bounce();
            return;
        }

        await AssignIsOpen(false);
    }

    private async Task HandleOnCloseClick(MouseEventArgs e)
    {
        if (_params.IsEnabled is false) return;

        await AssignIsOpen(false);
    }

    // Answers a dismissal the Modal turns down with a movement rather than with nothing at all: a click on
    // the overlay of a blocking Modal, or an Escape it does not leave on, otherwise reads as a page that has
    // stopped responding rather than as a surface waiting to be answered. The movement collapses to nothing
    // under reduced motion, which the motion tokens take care of.
    private void Bounce()
    {
        _bounce = _bounce == 1 ? 2 : 1;
    }

    // A blocking Modal that is not modeless is a surface waiting to be answered rather than one that can be
    // walked away from, which is what the alertdialog role says - so that is what it reports when nothing
    // else was asked for.
    private string GetRole()
    {
        return (_params.IsAlert ?? ((_params.Blocking ?? false) && IsModeless is false)) ? "alertdialog" : "dialog";
    }

    // Null rather than an empty string when there is nothing to render, so a Modal that was given no
    // styles carries no style attribute at all instead of an empty one.
    private string? GetOverlayStyles()
    {
        return JoinStyles(Styles?.Overlay, _params.Styles?.Overlay);
    }

    private string GetOverlayClasses()
    {
        return JoinClasses("bit-mdl-ovl", Classes?.Overlay, _params.Classes?.Overlay);
    }

    // The four size parameters are written ahead of the styles the consumer gave the content, so that a
    // declaration in Styles.Content still has the last word over the parameter that named the same property:
    // within one style attribute the later declaration is the one that stands.
    private string? GetContentStyles()
    {
        return JoinStyles(GetSizeStyles(), JoinStyles(Styles?.Content, _params.Styles?.Content));
    }

    // Nothing at all for the Modal that was given no size, which is the common case: the content box then
    // carries only what the stylesheet gives it, and is as big as what is inside it.
    private string? GetSizeStyles()
    {
        var width = _params.Width;
        var height = _params.Height;
        var maxWidth = _params.MaxWidth;
        var maxHeight = _params.MaxHeight;

        if (width.HasNoValue() && height.HasNoValue() && maxWidth.HasNoValue() && maxHeight.HasNoValue()) return null;

        return string.Concat(
            width.HasValue() ? $"width:{width};" : string.Empty,
            height.HasValue() ? $"height:{height};" : string.Empty,
            maxWidth.HasValue() ? $"max-width:{maxWidth};" : string.Empty,
            maxHeight.HasValue() ? $"max-height:{maxHeight};" : string.Empty);
    }

    private string GetContentClasses()
    {
        var baseClasses = _bounce switch
        {
            1 => "bit-mdl-ctn bit-mdl-bna",
            2 => "bit-mdl-ctn bit-mdl-bnb",
            _ => "bit-mdl-ctn"
        };

        // The chrome lays the content out as a column of header, body and footer, which is only what a Modal
        // that renders any of them wants: one given nothing but its content keeps that content as it is.
        return JoinClasses(HasChrome ? $"{baseClasses} bit-mdl-chr" : baseClasses, Classes?.Content, _params.Classes?.Content);
    }

    // A kept-mounted Modal that is closed is still in the page, so it is taken out of the way of it rather
    // than left lying over it. The builder answers null for a Modal that carries no classes at all, which
    // this never is - the root class is one of them - but a null is still not something to splice a class
    // list onto.
    private string? GetRootClasses()
    {
        if (IsOpen) return ClassBuilder.Value;

        var classes = ClassBuilder.Value;

        return classes.HasNoValue() ? "bit-mdl-hid" : $"{classes} bit-mdl-hid";
    }

    // Whether a closed Modal is still to be rendered. Only one that has been open at least once is kept: a
    // Modal that has never opened has no state worth keeping, and rendering it up front would put the cost
    // of every Modal on the page onto that page's first render.
    private bool _keptMounted => (_params.KeepMounted ?? false) && _hasBeenOpened;

    // Two class lists are one attribute value while a single space stands between them, and an empty part
    // in the middle would otherwise leave a double space (or a trailing one) in the rendered attribute.
    // Written out rather than joined off a collection: this runs per part per render, and the case that
    // matters - a Modal that was given no classes of its own - then costs no allocation at all.
    private static string JoinClasses(string baseClass, string? ownClass, string? paramsClass)
    {
        if (ownClass.HasNoValue()) return paramsClass.HasNoValue() ? baseClass : $"{baseClass} {paramsClass}";

        if (paramsClass.HasNoValue()) return $"{baseClass} {ownClass}";

        return $"{baseClass} {ownClass} {paramsClass}";
    }

    private void OnSetIsOpen()
    {
        if (IsOpen)
        {
            // A refusal leaves the content marked with the movement that answered it. Opening the Modal
            // again starts from the entry animation instead.
            _bounce = 0;
            return;
        }

        if (IsRendered is false) return;

        // Fire-and-forget the dismiss callback, then re-render. Wrapped in a local async method
        // (instead of ContinueWith) so a throwing OnDismiss surfaces through Blazor's normal async
        // error handling via the renderer dispatcher rather than being swallowed on an unobserved task.
        _ = InvokeAsync(async () =>
        {
            await _params.OnDismiss.InvokeAsync(new MouseEventArgs());
            StateHasChanged();
        });
    }

    // Whether the Modal renders a chrome of its own around the content. Everything the chrome is made of is
    // opt-in, so a Modal given nothing but its content renders nothing but that content - which is what
    // keeps the surface usable for the markup that brings its own header and footer.
    private bool HasChrome => HasHeader || HasFooter || (_params.Body is not null);

    private bool HasHeader => _params.Header is not null || _params.HeaderText.HasValue() || (_params.ShowCloseButton ?? false);

    private bool HasFooter => _params.Footer is not null || _params.FooterText.HasValue();

    // A modeless Modal is one that leaves the page behind it usable, so it neither reports itself modal nor
    // renders an overlay over what it is meant to leave reachable.
    private bool IsModeless => _params.Modeless ?? false;

    private bool IsAriaModal => (_params.AriaModal ?? true) && IsModeless is false;

    // The overlay is the only thing in the layer that catches a pointer - the root is pointer-events:none -
    // so whether it is rendered is the same question as whether the Modal is modal at all. A Modal that is
    // to leave the page behind it usable says so with Modeless, which stands the overlay down along with the
    // focus trap, the scroll lock and the modality it reports: dropping the overlay on its own would leave
    // the pointer free of a surface still holding the keyboard and still announcing itself modal.
    private bool ShowsOverlay => IsModeless is false;

    private bool IsFullHeight => (_params.FullHeight ?? false) || (_params.FullSize ?? false);

    private bool IsFullWidth => (_params.FullWidth ?? false) || (_params.FullSize ?? false);

    // Whether the keyboard is the Modal's to hold. Only a Modal that reports itself modal takes the tab
    // sequence over: a modeless one is meant to leave the page behind it usable, and a trap would take the
    // keyboard half of that away while leaving the pointer half in place.
    private bool ShouldTrapFocus => (_params.NoFocusTrap ?? false) is false && IsAriaModal && IsShown;

    // Whether the page behind the Modal is the Modal's to hold. As with the focus trap, only a Modal that
    // reports itself modal takes it: a modeless one is meant to leave the page usable, and a page held still
    // behind a surface the pointer is free to leave reads as a page that broke rather than one that is
    // covered. A Modal that toggles the scroll itself already holds its scroller, so it does not take a
    // second hold on the same page here.
    // What the two scroll holds act on, in the order the consumer's intent is expressed: the element it
    // named, then the selector it named, then the scroller of the application shell the Modal is inside of,
    // and the page when it is inside none. The shell is the reason a default of "the page" is not enough on
    // its own: an app whose shell scrolls a region of its own has a body that never scrolls, so holding it
    // takes nothing away and the page carries on moving behind the Modal.
    private ElementReference? ScrollerElementTarget => _params.ScrollerElement
                                                      ?? (_params.ScrollerSelector.HasValue() ? null : AppShellContainer);

    private bool ShouldLockScroll => (_params.NoScrollLock ?? false) is false
                                     && (_params.AutoToggleScroll ?? false) is false
                                     && IsAriaModal
                                     && IsShown;

    // Whether the gestures that land on the Modal are the page's rather than nobody's. A Modal that leaves
    // the page scrolling still covers it with an overlay, and the layer that overlay sits in is fixed to the
    // viewport: the wheel and the touch drag that land on it are chained to the document, which in an
    // application shell - or in any layout that scrolls a region of its own - is not the thing that scrolls,
    // so the gesture reaches nothing at all and the page reads as held by a Modal that holds nothing. It is
    // handed to that region by hand instead, for as long as such a Modal is open.
    // Only the Modal showing the overlay needs it: a modeless one lets the pointer through to the page,
    // which takes its own gestures. Only the Modal holding nothing wants it: one holding the page has
    // nothing to forward, and one that took the overflow off its scroller (AutoToggleScroll) means that
    // scroller to stay still, so moving it from here would undo what it did. And only the Modal aimed at a
    // scroller of its own can use it: the page is what the browser already chains to.
    private bool ShouldForwardScroll => ShowsOverlay
                                        && IsShown
                                        && ShouldLockScroll is false
                                        && (_params.AutoToggleScroll ?? false) is false
                                        && (ScrollerElementTarget.HasValue || _params.ScrollerSelector.HasValue());

    // A Modal that was taken out of view carries none of the behaviors that only make sense for one the user
    // can see: it neither holds the keyboard nor the page behind it.
    private bool IsShown => Visibility == BitVisibility.Visible;

    private string _dragElementSelector => _params.DragElementSelector ?? $"#{_containerId}";

    // Moved once per opening: the call is made again whenever the Modal becomes something the user can see
    // while it is already open, so that a Modal opened out of view still starts with the keyboard in it once
    // it appears - and a Modal that already placed the focus is not made to place it a second time by an
    // unrelated parameter change.
    private async Task FocusContent()
    {
        if (_params.NoAutoFocus ?? false) return;

        if (_contentFocused || IsShown is false) return;

        if (IsDisposed) return;

        _contentFocused = true;

        try
        {
            await _js.BitUtilsFocusFirstElement(_containerId);
        }
        catch (JSDisconnectedException) { } // we can ignore this exception here
    }

    private async Task StoreFocus()
    {
        // Nothing is handed back to a Modal that was told not to hand anything back, so nothing is recorded
        // for it either - the map would otherwise keep the element alive until the Modal closes.
        if ((_params.NoRestoreFocus ?? false) || _focusStored || IsDisposed) return;

        _focusStored = true;

        try
        {
            await _js.BitUtilsStoreFocus(_containerId);
        }
        catch (JSDisconnectedException) { } // we can ignore this exception here
    }

    private async Task RestoreFocus()
    {
        // Only what was recorded is handed back, so a Modal that never took the focus over never places it
        // either - and the recording is taken back whether or not the hand-back went through.
        if (_focusStored is false) return;

        _focusStored = false;

        if (IsDisposed) return;

        try
        {
            // The Modal is out of the page by now, which drops the focus it was holding on the body: that
            // is the state the restore is for. A focus that has since moved somewhere else - a close
            // handler that placed it deliberately - belongs to whoever moved it, so it is left alone.
            await _js.BitUtilsRestoreFocus(_containerId);
        }
        catch (JSDisconnectedException) { } // we can ignore this exception here
    }

    private async Task SetupFocusTrap()
    {
        if (ShouldTrapFocus is false || _focusTrapped || IsDisposed) return;

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

    private async Task LockScroll()
    {
        if (ShouldLockScroll is false || _scrollLocked || IsDisposed) return;

        _scrollLocked = true;
        _lockedScrollerSelector = _params.ScrollerSelector;
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
        // so a Modal can never end up holding a page it has already let go of.
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

    // Hands the gestures that land on the Modal to the scroller behind it, for the Modal that covers a page
    // it was told not to hold: what the layer catches would else be chained to a document that does not
    // scroll. Only the gestures nothing inside the Modal took first are forwarded, which the script decides.
    private async Task ForwardScroll()
    {
        if (ShouldForwardScroll is false || _scrollForwarded || IsDisposed) return;

        _scrollForwarded = true;
        _forwardedScrollerSelector = _params.ScrollerSelector;
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

    // Takes the forwarding back, and only what was registered, so a Modal never ends up handing gestures to
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

    private async Task SetupDrag()
    {
        if ((_params.Draggable ?? false) is false || _dragSetup || IsDisposed) return;

        _dragSetup = true;
        // The selector is recorded as the handlers are registered against it, so that teardown reaches the
        // same element even if the parameter has changed in the meantime.
        _dragElementSelectorOnSetup = _dragElementSelector;

        try
        {
            await _js.BitDragDropSetup(_containerId, $"#{_containerId}", _dragElementSelectorOnSetup);
        }
        catch (JSDisconnectedException) { } // we can ignore this exception here
    }

    private async Task RemoveDrag()
    {
        if (_dragSetup is false) return;

        _dragSetup = false;

        var selector = _dragElementSelectorOnSetup ?? _dragElementSelector;
        _dragElementSelectorOnSetup = null;

        try
        {
            await _js.BitDragDropRemove(_containerId, selector);
        }
        catch (JSDisconnectedException) { } // we can ignore this exception here
    }

    // The scroll handling a Modal does itself, as opposed to the hold it takes on the page through
    // LockScroll: the overflow of the scroller is taken away while the Modal is open and handed back once it
    // closes, and the room that gave back is what an absolutely positioned Modal is pushed down by.
    private async Task ToggleScroll(bool isOpen)
    {
        if (isOpen)
        {
            // Snapshot the decision at open time; the close reuses it instead of re-reading
            // AutoToggleScroll, which may have changed since the Modal was opened.
            _scrollToggledOnOpen = (_params.AutoToggleScroll ?? false) && IsDisposed is false;
            if (_scrollToggledOnOpen is false) return;

            // Snapshot the scroller target at open time so the close restores the same scroller, even if
            // ScrollerElement/ScrollerSelector changed in the meantime.
            _scrollerElementOnToggle = ScrollerElementTarget;
            _scrollerSelectorOnToggle = _params.ScrollerSelector;
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
                ? await _js.BitUtilsToggleOverflow(_scrollerElementOnToggle.Value, isOpen)
                : await _js.BitUtilsToggleOverflow(_scrollerSelectorOnToggle ?? "body", isOpen);
        }
        catch (JSDisconnectedException) { } // we can ignore this exception here
    }

    /// <summary>
    /// Builds the effective parameters by merging this component's own parameters with the cascaded
    /// <see cref="BitModalParameters"/>. The component's own values take precedence, preserving the
    /// behavior previously provided by the parameters object reading back from the component.
    /// </summary>
    /// <remarks>
    /// Nullable values use a simple "own value, else cascaded" precedence (<c>Own ?? p.Own</c>).
    /// Non-nullable bools cannot distinguish "not set" from "explicitly false", so they merge
    /// asymmetrically and the component param only expresses the "stronger" intent for that flag:
    /// <list type="bullet">
    /// <item><see cref="AbsolutePosition"/>, <see cref="AutoToggleScroll"/>, <see cref="Blocking"/>,
    /// <see cref="Draggable"/>, <see cref="FullHeight"/>, <see cref="FullSize"/>, <see cref="FullWidth"/>,
    /// <see cref="KeepMounted"/>, <see cref="ModeFull"/>, <see cref="Modeless"/>, <see cref="NoAutoFocus"/>,
    /// <see cref="NoBorder"/>, <see cref="NoDismissOnEscape"/>, <see cref="NoFocusTrap"/>,
    /// <see cref="NoRestoreFocus"/>, <see cref="NoScrollLock"/>, <see cref="ShowCloseButton"/>:
    /// the component param can only force the behavior <b>on</b>
    /// (<c>X ? true : p.X</c>); it can never force it off.</item>
    /// <item><see cref="AriaModal"/>, <see cref="BitComponentBase.IsEnabled"/>:
    /// the component param can only force the behavior <b>off</b> (<c>X is false ? false : p.X</c>); it
    /// can never force it on. These default to <c>true</c>, so opting out is the meaningful override.</item>
    /// </list>
    /// To express the opposite (non-overridable) intent, set the value through the cascaded
    /// <see cref="BitModalParameters"/> (e.g. via the <see cref="BitModalService"/>) rather than the component parameter.
    /// </remarks>
    private BitModalParameters BuildParameters()
    {
        var p = ModalParameters!;

        return new BitModalParameters
        {
            // Can only force off (default is enabled): see remarks on asymmetric merge.
            IsEnabled = IsEnabled is false ? false : p.IsEnabled,
            // HtmlAttributes on both sources are externally settable (non-nullable) properties, so a
            // caller can still assign null. Coalesce to empty dictionaries so the Concat in
            // MergeHtmlAttributes (and the snapshot copies) never NRE, mirroring BitModalParameters.Merge.
            HtmlAttributes = MergeHtmlAttributes(p.HtmlAttributes ?? [], HtmlAttributes ?? []),
            // Can only force on (default is off): see remarks on asymmetric merge.
            AbsolutePosition = AbsolutePosition ? true : p.AbsolutePosition,
            Dir = Dir ?? p.Dir,
            AriaLabel = AriaLabel ?? p.AriaLabel,
            // Can only force off (default is enabled): see remarks on asymmetric merge.
            AriaModal = AriaModal is false ? false : p.AriaModal,
            // Can only force on (default is off): see remarks on asymmetric merge.
            AutoToggleScroll = AutoToggleScroll ? true : p.AutoToggleScroll,
            // Can only force on (default is off): see remarks on asymmetric merge.
            Blocking = Blocking ? true : p.Blocking,
            Body = Body ?? p.Body,
            // Service-level knobs the Modal itself has no say in: carried through the merge so that the
            // effective parameters stay a faithful picture of what the Modal was shown with.
            CanClose = p.CanClose,
            Classes = p.Classes,
            CloseButtonTitle = CloseButtonTitle ?? p.CloseButtonTitle,
            CloseIcon = CloseIcon ?? p.CloseIcon,
            CloseIconName = CloseIconName ?? p.CloseIconName,
            CloseOnNavigation = p.CloseOnNavigation,
            DragElementSelector = DragElementSelector ?? p.DragElementSelector,
            // Can only force on (default is off): see remarks on asymmetric merge.
            Draggable = Draggable ? true : p.Draggable,
            Footer = Footer ?? p.Footer,
            FooterText = FooterText ?? p.FooterText,
            // Can only force on (default is off): see remarks on asymmetric merge.
            FullHeight = FullHeight ? true : p.FullHeight,
            // Can only force on (default is off): see remarks on asymmetric merge.
            FullSize = FullSize ? true : p.FullSize,
            // Can only force on (default is off): see remarks on asymmetric merge.
            FullWidth = FullWidth ? true : p.FullWidth,
            Header = Header ?? p.Header,
            HeaderText = HeaderText ?? p.HeaderText,
            Height = Height ?? p.Height,
            IsAlert = IsAlert ?? p.IsAlert,
            // Can only force on (default is off): see remarks on asymmetric merge.
            KeepMounted = KeepMounted ? true : p.KeepMounted,
            MaxHeight = MaxHeight ?? p.MaxHeight,
            MaxWidth = MaxWidth ?? p.MaxWidth,
            // Can only force on (default is off): see remarks on asymmetric merge.
            ModeFull = ModeFull ? true : p.ModeFull,
            // Can only force on (default is off): see remarks on asymmetric merge.
            Modeless = Modeless ? true : p.Modeless,
            // Can only force on (default is off): see remarks on asymmetric merge.
            NoAutoFocus = NoAutoFocus ? true : p.NoAutoFocus,
            // Can only force on (default is off): see remarks on asymmetric merge.
            NoBorder = NoBorder ? true : p.NoBorder,
            // Can only force on (default is off): see remarks on asymmetric merge.
            NoDismissOnEscape = NoDismissOnEscape ? true : p.NoDismissOnEscape,
            // Can only force on (default is off): see remarks on asymmetric merge.
            NoFocusTrap = NoFocusTrap ? true : p.NoFocusTrap,
            // Can only force on (default is off): see remarks on asymmetric merge.
            NoRestoreFocus = NoRestoreFocus ? true : p.NoRestoreFocus,
            // Can only force on (default is off): see remarks on asymmetric merge.
            NoScrollLock = NoScrollLock ? true : p.NoScrollLock,
            OnDismiss = _onDismiss,
            OnEscapeKeyDown = _onEscapeKeyDown,
            OnOpen = _onOpen,
            OnOverlayClick = _onOverlayClick,
            Position = Position ?? p.Position,
            ScrollerElement = ScrollerElement ?? p.ScrollerElement,
            ScrollerSelector = ScrollerSelector ?? p.ScrollerSelector,
            // Can only force on (default is off): see remarks on asymmetric merge.
            ShowCloseButton = ShowCloseButton ? true : p.ShowCloseButton,
            Styles = p.Styles,
            SubtitleAriaId = SubtitleAriaId ?? p.SubtitleAriaId,
            TitleAriaId = TitleAriaId ?? p.TitleAriaId,
            // Can only force off (default is Visible): own value wins only when it is a meaningful
            // (non-default) override, otherwise the cascaded value is used.
            Visibility = Visibility != BitVisibility.Visible ? Visibility : p.Visibility,
            Width = Width ?? p.Width,
        };
    }

    /// <summary>
    /// Merges the cascaded and own HtmlAttributes (own values win), reusing the previous result when
    /// neither source dictionary changed by content to avoid a per-render allocation. Content (rather
    /// than reference) comparison is used so in-place mutations of these mutable inputs are detected.
    /// </summary>
    private Dictionary<string, object> MergeHtmlAttributes(Dictionary<string, object> cascaded, Dictionary<string, object> own)
    {
        if (_mergedHtmlAttributes is not null &&
            DictionaryContentEqual(_lastCascadedHtmlAttributes, cascaded) &&
            DictionaryContentEqual(_lastOwnHtmlAttributes, own))
        {
            return _mergedHtmlAttributes;
        }

        // Store independent content snapshots (copies) so a later in-place mutation of the live source
        // dictionaries differs from what was captured here and forces a rebuild.
        _lastCascadedHtmlAttributes = new Dictionary<string, object>(cascaded);
        _lastOwnHtmlAttributes = new Dictionary<string, object>(own);
        _mergedHtmlAttributes = cascaded.Concat(own).GroupBy(kv => kv.Key).ToDictionary(g => g.Key, g => g.Last().Value);

        return _mergedHtmlAttributes;
    }

    private static bool DictionaryContentEqual(Dictionary<string, object>? a, Dictionary<string, object>? b)
    {
        if (ReferenceEquals(a, b)) return true;
        if (a is null || b is null) return false;
        if (a.Count != b.Count) return false;

        foreach (var kv in a)
        {
            if (b.TryGetValue(kv.Key, out var value) is false) return false;
            if (Equals(kv.Value, value) is false) return false;
        }

        return true;
    }



    protected override async ValueTask DisposeAsync(bool disposing)
    {
        if (IsDisposed || disposing is false) return;

        // A Modal disposed while it is still open never reaches its close, so the registrations it made on
        // the JS side - the focus trap, the hold on the page behind it, the gestures it was handing to the
        // page, the drag handlers, the overflow it took off its scroller - are taken back here instead. The
        // stored focus is dropped rather than restored: there is no close for it to be handed back on, and
        // the map would keep the element alive without this.
        if (_focusTrapped || _focusStored || _scrollLocked || _scrollForwarded || _dragSetup || _scrollToggledOnOpen)
        {
            var trapped = _focusTrapped;
            var stored = _focusStored;
            var locked = _scrollLocked;
            var forwarded = _scrollForwarded;
            var dragged = _dragSetup;
            var toggled = _scrollToggledOnOpen;
            var dragSelector = _dragElementSelectorOnSetup ?? _dragElementSelector;
            _focusTrapped = false;
            _focusStored = false;
            _scrollLocked = false;
            _scrollForwarded = false;
            _dragSetup = false;
            _scrollToggledOnOpen = false;

            try
            {
                if (trapped)
                {
                    await _js.BitUtilsDisposeFocusTrap(_containerId);
                }

                if (locked)
                {
                    await _js.BitUtilsUnlockScroll(_containerId);
                }

                if (forwarded)
                {
                    await _js.BitUtilsStopForwardScroll(_containerId);
                }

                if (dragged)
                {
                    await _js.BitDragDropRemove(_containerId, dragSelector);
                }

                if (toggled)
                {
                    if (_scrollerElementOnToggle.HasValue)
                    {
                        await _js.BitUtilsToggleOverflow(_scrollerElementOnToggle.Value, false);
                    }
                    else
                    {
                        await _js.BitUtilsToggleOverflow(_scrollerSelectorOnToggle ?? "body", false);
                    }
                }

                if (stored)
                {
                    await _js.BitUtilsForgetFocus(_containerId);
                }
            }
            catch (JSDisconnectedException) { } // we can ignore this exception here
        }

        await base.DisposeAsync(disposing);
    }
}
