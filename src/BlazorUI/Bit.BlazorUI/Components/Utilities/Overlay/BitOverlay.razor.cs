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
/// what the Overlay itself offers is the layer, the click handling and the scroll handling - the scrollbar
/// toggle, and the gestures it hands on to the scroller it was told to leave scrolling.
/// <br/>
/// A click dismisses the Overlay only where it is the layer that was clicked: the content it hosts is what
/// the user is reaching past the layer for, so neither a click on it nor a press that began on it and
/// ended on the layer takes the Overlay away. Every click is reported through <see cref="OnClick"/> all
/// the same.
/// </remarks>
public partial class BitOverlay : BitComponentBase
{
    private float _offsetTop;
    private bool _internalIsOpen;
    // Whether the press the click on the way belongs to started on the content rather than on the layer
    // around it. A press that starts inside the content and ends on the layer - the last stretch of a text
    // selection dragged past the edge of a box - reports its click on the Overlay, since that is where the
    // two ends of it meet, and a dismissal on that click is the one dismissal the user never asked for.
    // It stays false while nothing has been pressed at all, so a click raised on its own still dismisses.
    private bool _pressedOnContent;
    // Whether the overflow of a scroller was actually toggled during the open sequence, so the close
    // sequence hands it back if and only if it was taken, regardless of later changes to AutoToggleScroll.
    // The scroller is snapshotted with it, so the close restores the same one even when ScrollerElement or
    // ScrollerSelector changed while the Overlay was open.
    private bool _scrollToggledOnOpen;
    private ElementReference? _scrollerElementOnToggle;
    private string? _scrollerSelectorOnToggle;
    // Whether the gestures that land on the Overlay are being handed to a scroller behind it, and which
    // scroller they were aimed at when the forwarding was registered, so an Overlay pointed somewhere else
    // while it is open takes the forwarding back and makes it again.
    private bool _scrollForwarded;
    private string? _forwardedScrollerSelector;
    private ElementReference? _forwardedScrollerElement;



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
    /// (<c>body</c>) when it is inside none. The room the scrollbar took is added back as padding for as
    /// long as the overflow is off, so that taking it away shifts nothing sideways in the frame the Overlay
    /// appears in, and the holds are counted: two Overlays holding the same scroller both hold it, and it
    /// is only handed back once the last of them closes.
    /// <br/>
    /// The offset the scroller was left at is what an <see cref="AbsolutePosition"/> Overlay is pushed down
    /// by, so that it stays where the eye left it rather than jumping to the top of the scroller it is laid
    /// out in.
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
    /// react to a click the Overlay refuses to be closed by. Only the layer itself dismisses the Overlay
    /// either way - a click on the content it hosts never does - so this is for the Overlay that has to
    /// stay up until whatever it is showing is dealt with.
    /// </remarks>
    [Parameter] public bool NoAutoClose { get; set; }

    /// <summary>
    /// Callback that is called when the overlay is clicked.
    /// </summary>
    /// <remarks>
    /// Invoked for every click on an open Overlay - the ones on its content included, and the ones a
    /// <see cref="NoAutoClose"/> Overlay refuses to be closed by - and invoked before the Overlay closes.
    /// Since a click on the content no longer closes the Overlay by itself, this is also what closes one on
    /// terms of the consumer's own: an <c>OnClick</c> that sets <see cref="IsOpen"/> to false brings back
    /// the dismissal from anywhere the Overlay used to do without being asked.
    /// </remarks>
    [Parameter] public EventCallback<MouseEventArgs> OnClick { get; set; }

    /// <summary>
    /// Callback that is called when the Overlay has closed.
    /// </summary>
    /// <remarks>
    /// Reports the state change rather than the gesture behind it, so it is invoked however the Overlay was
    /// closed: a click on it, the <see cref="IsOpen"/> binding, <see cref="Close"/> and <see cref="Toggle"/>
    /// alike. It is invoked after the scroller the Overlay was holding has been handed back, which is what
    /// makes it the place to do whatever has to happen once the page is the user's again.
    /// </remarks>
    [Parameter] public EventCallback OnClose { get; set; }

    /// <summary>
    /// Callback that is called when the Overlay has opened.
    /// </summary>
    /// <remarks>
    /// Reports the state change rather than the gesture behind it, so it is invoked however the Overlay was
    /// opened - the <see cref="IsOpen"/> binding, <see cref="Open"/>, <see cref="Toggle"/>, and the first
    /// render of one that starts open through <see cref="DefaultIsOpen"/> - and after the scroller it holds
    /// has been taken.
    /// </remarks>
    [Parameter] public EventCallback OnOpen { get; set; }

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
    /// <br/>
    /// The scroller named here is also the one an Overlay that is <em>not</em> holding it hands the wheel
    /// and the touch drag it catches to. A fixed layer chains those gestures to the document, which in
    /// such a layout is not the thing that scrolls, so the page would otherwise read as held by an Overlay
    /// that holds nothing.
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

        // The forwarding is settled on every render rather than only on an opening, since it is what the
        // parameters say it is: an Overlay told to hold its scroller, or aimed at another one, while it is
        // open has to have it taken back or made again there and then.
        await SyncScrollForward();

        if (_internalIsOpen == IsOpen) return;

        // The state this pass is settling, held on to rather than read again: the scroller is asked for
        // over the wire, and an Overlay closed while that call was still out would otherwise be reported
        // as opened. The change that overtook this one is a render of its own, which this same method sees
        // through the next time it runs.
        var isOpen = IsOpen;

        _internalIsOpen = isOpen;

        // A press whose click never came - one released outside the window - is not carried into the next
        // opening, where it would refuse the first dismissal for no reason the user could see.
        _pressedOnContent = false;

        var hadOffset = _offsetTop > 0;

        _offsetTop = 0;

        await ToggleScroll(isOpen);

        // The top-offset means nothing to a closed Overlay, whatever the release call reported back.
        if (isOpen is false)
        {
            _offsetTop = 0;
        }

        // Only re-rendered when the offset the style reads actually changed, and only where the style
        // reads it at all, so an Overlay anchored to the screen renders nothing twice for an offset it
        // would never carry.
        if (AbsolutePosition && hadOffset != _offsetTop > 0)
        {
            StyleBuilder.Reset();
            StateHasChanged();
        }

        // The state change is reported here rather than from the places that cause it, so that one report
        // covers every way the Overlay is opened and closed - the binding, the methods, a click on the
        // layer - and lands once the scroller has been taken or handed back rather than before.
        if (isOpen)
        {
            await OnOpen.InvokeAsync();
        }
        else
        {
            await OnClose.InvokeAsync();
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
            // The room the scrollbar took is added back as padding while the overflow is off, so that
            // taking it away shifts nothing sideways in the frame the Overlay appears in - the same
            // compensation the scroll lock of the dialog surfaces has always made.
            _offsetTop = _scrollerElementOnToggle.HasValue
                ? await _js.BitUtilsToggleOverflow(UniqueId, _scrollerElementOnToggle.Value, isOpen, true)
                : await _js.BitUtilsToggleOverflow(UniqueId, _scrollerSelectorOnToggle ?? "body", isOpen, true);
        }
        catch (JSDisconnectedException) { } // we can ignore this exception here
    }

    // Whether the wheel and the touch drag that land on the Overlay are to be handed to the scroller behind
    // it. The layer is fixed to the viewport, so the browser chains those gestures to the document, which
    // in an application shell - or in any layout that scrolls a region of its own - is not the thing that
    // scrolls: the gesture reaches nothing at all, and a page the Overlay was never told to hold reads as
    // held. Only the Overlay holding nothing wants it - one that took the overflow off its scroller means
    // that scroller to stay still, so moving it from here would undo what it did. Only the one anchored to
    // the screen needs it: an absolutely positioned Overlay is laid out inside the scroller already, so the
    // browser chains to it on its own. And only the one aimed at a scroller of its own can use it, since
    // the page is what the browser already chains to.
    private bool ShouldForwardScroll => IsOpen
                                        && IsRendered
                                        && IsDisposed is false
                                        && AutoToggleScroll is false
                                        && AbsolutePosition is false
                                        && (ScrollerElementTarget.HasValue || ScrollerSelector.HasValue());

    private async Task SyncScrollForward()
    {
        // The forwarding is registered against the scroller it was aimed at when it was made, so an Overlay
        // pointed somewhere else while it is open takes it back and makes it again.
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

    private async Task ForwardScroll()
    {
        if (_scrollForwarded) return;

        _scrollForwarded = true;
        _forwardedScrollerSelector = ScrollerSelector;
        var element = ScrollerElementTarget;
        _forwardedScrollerElement = element;

        try
        {
            if (element.HasValue)
            {
                await _js.BitUtilsForwardScroll(UniqueId, _Id, element.Value);
            }
            else
            {
                await _js.BitUtilsForwardScroll(UniqueId, _Id, _forwardedScrollerSelector);
            }
        }
        catch (JSDisconnectedException) { } // we can ignore this exception here
    }

    // Takes the forwarding back, and only what was registered, so an Overlay never ends up handing gestures
    // to a scroller it has already let go of.
    private async Task StopForwardScroll()
    {
        if (_scrollForwarded is false) return;

        _scrollForwarded = false;
        _forwardedScrollerSelector = null;
        _forwardedScrollerElement = null;

        try
        {
            await _js.BitUtilsStopForwardScroll(UniqueId);
        }
        catch (JSDisconnectedException) { } // we can ignore this exception here
    }

    // A press on the layer is the start of a dismissal; a press on the content is not, and the content
    // stops its own press from reaching here, so this only ever runs for the one on the layer.
    private void HandleOnMouseDown()
    {
        _pressedOnContent = false;
    }

    private void HandleOnContentMouseDown()
    {
        _pressedOnContent = true;
    }

    // The click on the content is reported like any other, but it never dismisses: the layer is what the
    // user reaches for to put the Overlay away, and the surface it is showing is the thing they are
    // reaching past it for.
    private async Task HandleOnContentClick(MouseEventArgs e)
    {
        _pressedOnContent = false;

        if (IsEnabled is false || IsOpen is false) return;

        await OnClick.InvokeAsync(e);
    }

    private async Task HandleOnClick(MouseEventArgs e)
    {
        var pressedOnContent = _pressedOnContent;
        _pressedOnContent = false;

        if (IsEnabled is false || IsOpen is false) return;

        await OnClick.InvokeAsync(e);

        if (NoAutoClose) return;

        // The click of a press that began on the content: the pointer only reached the layer on its way
        // back up, which is the last stretch of a selection dragged past the edge of a box rather than the
        // user asking for the Overlay to go away.
        if (pressedOnContent) return;

        await AssignIsOpen(false);
    }



    protected override async ValueTask DisposeAsync(bool disposing)
    {
        if (IsDisposed || disposing is false) return;

        // An Overlay taken off the page while it was open would otherwise leave the scroller it held
        // without its scrollbar for good, and the listeners it registered on its root element behind on a
        // registry that lives as long as the page does.
        await ToggleScroll(false);

        await StopForwardScroll();

        await base.DisposeAsync(disposing);
    }
}
