namespace Bit.BlazorUI;

/// <summary>
/// The Overlay component is used to provide emphasis on a particular element or parts of it. It signals to
/// the user of a state change within the application and can be used for creating loaders, dialogs and more.
/// </summary>
/// <remarks>
/// The Overlay is the low-level layer the dialog surfaces of the library (Modal, Panel, Dialog) are built
/// on: a single element that covers the screen - or, with <see cref="AbsolutePosition"/>, the container it
/// was declared inside of - catches the clicks meant for what it covers, and shows whatever content it was
/// given. The dialog behaviors (focus trapping, Escape dismissal, page holding) belong to those surfaces;
/// what the Overlay itself offers is the layer, the click handling and the scrollbar toggle.
/// </remarks>
public partial class BitOverlay : BitComponentBase
{
    private float _offsetTop;
    private bool _internalIsOpen;
    // Whether the overflow of a scroller was actually toggled during the open sequence, so the close
    // sequence hands it back if and only if it was taken, regardless of later changes to AutoToggleScroll.
    // The scroller is snapshotted with it, so the close restores the same one even when ScrollerElement or
    // ScrollerSelector changed while the Overlay was open.
    private bool _scrollToggledOnOpen;
    private ElementReference? _scrollerElementOnToggle;
    private string? _scrollerSelectorOnToggle;



    [Inject] private IJSRuntime _js { get; set; } = default!;



    // The scroller of the application shell the Overlay was declared inside of, cascaded by BitAppShell
    // under this name. A shell scrolls a region of its own rather than the page, so the body of such an app
    // never scrolls and toggling its overflow takes nothing away: this is the element to toggle instead, for
    // an Overlay that has not been pointed at a scroller of its own. Taken by name rather than off
    // BitAppShell.Container because the shell lives in Bit.BlazorUI.Extras, which this assembly cannot
    // reference.
    [CascadingParameter(Name = "BitAppShell.Container")]
    private ElementReference? AppShellContainer { get; set; }



    /// <summary>
    /// When true, the Overlay will be positioned absolute instead of fixed, so that it covers the element
    /// it was declared inside of rather than the screen.
    /// </summary>
    /// <remarks>
    /// The element it is declared inside of has to establish a containing block of its own
    /// (<c>position: relative</c>) for this to place the Overlay over it rather than over the page.
    /// </remarks>
    [Parameter, ResetClassBuilder, ResetStyleBuilder]
    public bool AbsolutePosition { get; set; }

    /// <summary>
    /// When true, the scroll behavior of the scroller element behind the overlay will be disabled while the
    /// Overlay is open and handed back once it closes.
    /// </summary>
    /// <remarks>
    /// The scroller is named by <see cref="ScrollerElement"/> or <see cref="ScrollerSelector"/>; when
    /// neither is set it is the scroller of the application shell the Overlay is inside of, and the page
    /// (<c>body</c>) when it is inside none. The room the scrollbar gave back is what an
    /// <see cref="AbsolutePosition"/> Overlay is pushed down by, so that it stays where the eye left it
    /// rather than jumping to the top of the scroller it is laid out in.
    /// </remarks>
    [Parameter] public bool AutoToggleScroll { get; set; }

    /// <summary>
    /// Centers the content of the Overlay horizontally and vertically.
    /// </summary>
    /// <remarks>
    /// The Overlay lays its content out as a flex container, which stretches it over the whole layer when
    /// this is not set - the layout a surface of the consumer's own wants. Centering is what a loader or a
    /// message wants, and setting it here saves the stylesheet that would otherwise carry nothing else.
    /// </remarks>
    [Parameter, ResetClassBuilder]
    public bool Center { get; set; }

    /// <summary>
    /// The content of the Overlay.
    /// </summary>
    [Parameter] public RenderFragment? ChildContent { get; set; }

    /// <summary>
    /// The initial opening state of the Overlay in the uncontrolled mode, which is when the
    /// <see cref="IsOpen"/> parameter is not set.
    /// </summary>
    [Parameter] public bool? DefaultIsOpen { get; set; }

    /// <summary>
    /// When true, the Overlay and its content will be shown.
    /// </summary>
    [Parameter, ResetClassBuilder, TwoWayBound]
    public bool IsOpen { get; set; }

    /// <summary>
    /// Renders the Overlay in full mode that gives it an opaque background using the theme's overlay
    /// background color.
    /// </summary>
    /// <remarks>
    /// The Overlay catches the clicks meant for what it covers either way; this is what makes it dim it as
    /// well, without a stylesheet of the consumer's own. It is transparent otherwise, for the overlays that
    /// are a click catcher rather than a backdrop.
    /// </remarks>
    [Parameter, ResetClassBuilder]
    public bool ModeFull { get; set; }

    /// <summary>
    /// When true, the Overlay will not be closed by clicking on it.
    /// </summary>
    /// <remarks>
    /// The click is still reported through <see cref="OnClick"/>, which is what makes that the place to
    /// react to a click the Overlay refuses to be closed by.
    /// </remarks>
    [Parameter] public bool NoAutoClose { get; set; }

    /// <summary>
    /// Callback that is called when the overlay is clicked.
    /// </summary>
    /// <remarks>
    /// Invoked for every click on an open Overlay, including the ones a <see cref="NoAutoClose"/> Overlay
    /// refuses to be closed by, and invoked before the Overlay closes.
    /// </remarks>
    [Parameter] public EventCallback<MouseEventArgs> OnClick { get; set; }

    /// <summary>
    /// The element reference of the scroller whose scrolling is taken away while the Overlay is open, for
    /// the layouts whose scroller is not the page and cannot be named by a selector.
    /// </summary>
    /// <remarks>
    /// Takes precedence over <see cref="ScrollerSelector"/> when both are set, and over the scroller a
    /// <c>BitAppShell</c> cascades. Read only by <see cref="AutoToggleScroll"/>, which is what takes the
    /// scrollbar away.
    /// </remarks>
    [Parameter] public ElementReference? ScrollerElement { get; set; }

    /// <summary>
    /// The CSS selector of the scroller element whose scrolling is taken away while the Overlay is open,
    /// for <see cref="AutoToggleScroll"/>.
    /// </summary>
    /// <remarks>
    /// An Overlay inside a <c>BitAppShell</c> holds the shell's scroller without being told to, since the
    /// shell cascades it; the page (<c>body</c>) is what is held when there is no shell and this is not
    /// set, which is the scroller of an ordinary page. Any other layout that scrolls a region of its own
    /// names that region here, since holding a page that never scrolls holds nothing.
    /// </remarks>
    [Parameter] public string? ScrollerSelector { get; set; }

    /// <summary>
    /// The layer the Overlay is stacked at, which takes over from the one the whole library shares.
    /// </summary>
    /// <remarks>
    /// It is what an Overlay that has to sit above (or below) another surface of the page needs: a fixed
    /// Overlay sits at the library's shared overlay layer otherwise, and an <see cref="AbsolutePosition"/>
    /// one carries no z-index of its own at all, covering nothing but its own earlier siblings.
    /// </remarks>
    [Parameter, ResetStyleBuilder]
    public int? ZIndex { get; set; }



    /// <summary>
    /// Opens the Overlay, unless it is disabled.
    /// </summary>
    public async Task Open()
    {
        if (IsEnabled is false) return;

        if (IsOpen) return;

        if (await AssignIsOpen(true) is false) return;

        StateHasChanged();
    }

    /// <summary>
    /// Closes the Overlay. It closes whether or not the Overlay is enabled, so that an Overlay disabled
    /// while it was open can still be taken off the screen by the code that owns it.
    /// </summary>
    public async Task Close()
    {
        if (IsOpen is false) return;

        if (await AssignIsOpen(false) is false) return;

        StateHasChanged();
    }

    /// <summary>
    /// Opens the Overlay when it is closed, and closes it when it is open.
    /// </summary>
    public Task Toggle() => IsOpen ? Close() : Open();



    protected override string RootElementClass => "bit-ovl";

    protected override void OnInitialized()
    {
        // The uncontrolled starting state, which only applies while the consumer is not driving IsOpen
        // itself. It is read once here rather than every time the parameters are set, so that closing an
        // uncontrolled Overlay is not undone by the next render.
        if (IsOpenHasBeenSet is false && DefaultIsOpen.HasValue)
        {
            IsOpen = DefaultIsOpen.Value;
        }

        base.OnInitialized();
    }

    protected override void RegisterCssStyles()
    {
        StyleBuilder.Register(() => ZIndex is null
            ? string.Empty
            : FormattableString.Invariant($"z-index:{ZIndex}"));

        // Only an absolutely positioned Overlay is laid out inside the scroller AutoToggleScroll takes the
        // overflow off, so only that one is pushed down by the room it gave back. An Overlay anchored to
        // the screen is positioned against the viewport, which never moved.
        StyleBuilder.Register(() => AbsolutePosition && _offsetTop > 0
            ? FormattableString.Invariant($"top:{_offsetTop}px")
            : string.Empty);
    }

    protected override void RegisterCssClasses()
    {
        ClassBuilder.Register(() => IsOpen ? "bit-ovl-opn" : string.Empty);
        ClassBuilder.Register(() => Center ? "bit-ovl-ctr" : string.Empty);
        ClassBuilder.Register(() => ModeFull ? "bit-ovl-mfl" : string.Empty);
        ClassBuilder.Register(() => AbsolutePosition ? "bit-ovl-abs" : string.Empty);
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        await base.OnAfterRenderAsync(firstRender);

        if (_internalIsOpen == IsOpen) return;

        _internalIsOpen = IsOpen;

        var hadOffset = _offsetTop > 0;

        _offsetTop = 0;

        await ToggleScroll(IsOpen);

        // The top-offset means nothing to a closed Overlay, whatever the release call reported back.
        if (IsOpen is false)
        {
            _offsetTop = 0;
        }

        // Only re-rendered when the offset the style reads actually changed, so an Overlay that toggles
        // nothing renders nothing twice.
        if (hadOffset != _offsetTop > 0)
        {
            StyleBuilder.Reset();
            StateHasChanged();
        }
    }



    // What the overflow toggle acts on, in the order the consumer's intent is expressed: the element it
    // named, then the selector it named, then the scroller of the application shell the Overlay is inside
    // of, and the page when it is inside none.
    private ElementReference? ScrollerElementTarget => ScrollerElement
                                                       ?? (ScrollerSelector.HasValue() ? null : AppShellContainer);

    // The scroll handling the Overlay does itself: the overflow of the scroller is taken away while the
    // Overlay is open and handed back once it closes, and the room that gave back is what an absolutely
    // positioned Overlay is pushed down by.
    private async Task ToggleScroll(bool isOpen)
    {
        if (isOpen)
        {
            // The decision is taken at open time; the close reuses it instead of re-reading
            // AutoToggleScroll, which may have changed since the Overlay was opened.
            _scrollToggledOnOpen = AutoToggleScroll;
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
                ? await _js.BitUtilsToggleOverflow(UniqueId, _scrollerElementOnToggle.Value, isOpen)
                : await _js.BitUtilsToggleOverflow(UniqueId, _scrollerSelectorOnToggle ?? "body", isOpen);
        }
        catch (JSDisconnectedException) { } // we can ignore this exception here
    }

    private async Task CloseOverlay(MouseEventArgs e)
    {
        if (IsEnabled is false || IsOpen is false) return;

        await OnClick.InvokeAsync(e);

        if (NoAutoClose) return;

        await AssignIsOpen(false);
    }



    protected override async ValueTask DisposeAsync(bool disposing)
    {
        if (IsDisposed || disposing is false) return;

        // An Overlay taken off the page while it was open would otherwise leave the scroller it held
        // without its scrollbar for good.
        await ToggleScroll(false);

        await base.DisposeAsync(disposing);
    }
}
