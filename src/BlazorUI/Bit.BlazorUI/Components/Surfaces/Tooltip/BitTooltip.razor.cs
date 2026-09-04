namespace Bit.BlazorUI;

/// <summary>
/// Tooltip briefly describes an unlabeled control or adds a bit of information to a labeled one, in a
/// small surface that is shown next to what it belongs to for as long as the pointer or the keyboard
/// stays on it.
/// </summary>
/// <remarks>
/// The tooltip is shown on hover and on focus by default, is dismissed by the Escape key, and can be
/// made hoverable so that the pointer may travel into it - the three things WCAG 1.4.13 asks of content
/// shown on hover or focus. It names or describes its anchor according to its
/// <see cref="Relationship"/>, and a row of them wrapped in a <see cref="BitTooltipGroup"/> shares its
/// delays and shows one tooltip at a time. It is laid out purely in CSS, next to the anchor inside the
/// flow of the page, so it needs no positioning pass and nothing of JavaScript beyond copying the
/// relationship onto the anchor; a surface that has to escape an overflow, flip to the side with room, or
/// hold interactive content of its own is what BitCallout is for.
/// </remarks>
public partial class BitTooltip : BitComponentBase
{
    // The pending show and hide, so that a trigger arriving while one of them waits out its delay takes
    // over from it instead of both landing. Each waiter keeps its own reference to the source it made,
    // so a waiter that is cancelled never clears the field a newer one has already claimed.
    private CancellationTokenSource? _showDelayTokenSource;
    private CancellationTokenSource? _hideDelayTokenSource;

    // Set the first time the tooltip is shown, which is what a lazily rendered content waits for. It is
    // never unset: a content that has been rendered once stays rendered, so showing the tooltip again is
    // a class change rather than a render of everything in it.
    private bool _contentRendered;

    // The two reasons a tooltip is on the screen, kept apart so that one of them ending does not take the
    // tooltip away while the other still holds it: a pointer leaving an anchor the keyboard is still on
    // would otherwise hide a tooltip that the focus is asking for, and the other way round.
    private bool _isPointerOver;
    private bool _isFocusWithin;

    // Whether the focus inside the anchor arrived there by being pressed on rather than tabbed to. A
    // pointer that presses a control focuses it, and answering that focus would show the tooltip a second
    // time for the same pointer and then keep it on the screen after the pointer has gone; this is what
    // :focus-visible does for CSS, done by hand because the focus events carry no such flag.
    private bool _isFocusFromPointer;

    // Whether the tooltip on the screen is the one the tap that is still under way put there. A touch
    // sends the enter, the down and the up of a single tap one after another, so without this the tap
    // that shows the tooltip would be taken for a click on it as well and undo itself.
    private bool _isShownByTouch;

    // Whether the tooltip on the screen is one a click or a keyboard press of the anchor opened. Such a
    // tooltip is not held by a pointer or a focus that can end, so what dismisses it is the next thing the
    // user does somewhere else on the page - which the focus leaving the anchor is what both a click
    // outside it and a Tab away from it come down to.
    private bool _isShownByClick;

    // The relationship last mirrored onto the anchor, so that the round trip to the DOM is made when what
    // it would write has actually changed rather than once per render.
    private string _syncedAria = string.Empty;



    private string _tooltipId => $"{_Id}-ttp";

    // The delays the group above fills in for a tooltip that was left without one of its own. A tooltip
    // that names its delay keeps it, even where the value it names is the same as the default.
    private int _ShowDelay => HasNotBeenSet(nameof(ShowDelay)) ? Group?.ShowDelay ?? ShowDelay : ShowDelay;

    private int _HideDelay => HasNotBeenSet(nameof(HideDelay)) ? Group?.HideDelay ?? HideDelay : HideDelay;

    private bool HasContent => Template is not null || Text.HasValue();

    // A tooltip that cannot be shown describes nothing, so nothing is pointed at it either.
    private bool HasAccessibleContent => HasContent && IsEnabled;

    // A tooltip whose shown state is handed to it and never handed back cannot be driven by anything
    // that happens on the page: the page owns it, and the triggers below leave it alone.
    private bool IsControlledExternally => IsShownHasBeenSet && IsShownChanged.HasDelegate is false;

    // The one attribute the relationship comes down to, which the markup declares on the root element and
    // the anchor inside it is given a copy of. An empty one is a tooltip that declares nothing.
    private string AriaAttribute => HasAccessibleContent
        ? Relationship switch
        {
            BitTooltipRelationship.Description => "aria-describedby",
            BitTooltipRelationship.Label => "aria-labelledby",
            _ => string.Empty
        }
        : string.Empty;



    [Inject] private IJSRuntime _js { get; set; } = default!;



    /// <summary>
    /// The group this tooltip belongs to, which fills in the delays it was left without and keeps it the
    /// only tooltip of the group on the screen.
    /// </summary>
    [CascadingParameter] private BitTooltipGroup? Group { get; set; }



    /// <summary>
    /// Alias of ChildContent.
    /// </summary>
    [Parameter] public RenderFragment? Anchor { get; set; }

    /// <summary>
    /// The size in pixels of the arrow that points at the anchor, which is the length of the side of the
    /// square it is drawn from. Leaving it unset keeps the size the theme gives it.
    /// </summary>
    [Parameter, ResetStyleBuilder] public int? ArrowSize { get; set; }

    /// <summary>
    /// The content inside of tooltip tag, It can be Any custom tag or a text.
    /// </summary>
    [Parameter] public RenderFragment? ChildContent { get; set; }

    /// <summary>
    /// Custom CSS classes for different parts of the BitTooltip.
    /// </summary>
    [Parameter] public BitTooltipClassStyles? Classes { get; set; }

    /// <summary>
    /// The general color of the tooltip, which colors its surface and the arrow along with it.
    /// </summary>
    [Parameter, ResetClassBuilder] public BitColor? Color { get; set; }

    /// <summary>
    /// Default value of the IsShown.
    /// </summary>
    [Parameter] public bool? DefaultIsShown { get; set; }

    /// <summary>
    /// Expands the tooltip's own element to 100% of the available width, so that the anchor inside it
    /// keeps the width it would have had without a tooltip around it.
    /// </summary>
    /// <remarks>
    /// The tooltip wraps its anchor in an element of its own, which is laid out inline and is therefore
    /// only as wide as what it holds. A block-level anchor - a text field, a button meant to fill a
    /// column - would be shrunk to its content by that, and this is what keeps it stretched.
    /// </remarks>
    [Parameter, ResetClassBuilder] public bool FullWidth { get; set; }

    /// <summary>
    /// Hides the arrow of tooltip.
    /// </summary>
    [Parameter] public bool HideArrow { get; set; }

    /// <summary>
    /// Delay (in milliseconds) before hiding the tooltip.
    /// </summary>
    /// <remarks>
    /// It is the grace an <see cref="Interactive"/> tooltip needs while the pointer crosses the gap
    /// between the anchor and the tooltip, and the pause that keeps a tooltip from flickering while the
    /// pointer skims across a row of anchors. Leaving it alone inside a <see cref="BitTooltipGroup"/>
    /// takes the delay the group sets for all of its tooltips.
    /// </remarks>
    [Parameter] public int HideDelay { get; set; } = 0;

    /// <summary>
    /// Hides the tooltip when the anchor is clicked, which is what a tooltip on a control that does
    /// something when pressed - opening a dialog, submitting a form - owes the reader: its own text has
    /// been read by then, and leaving it over whatever the click brought up only gets in the way.
    /// </summary>
    /// <remarks>
    /// It is the plain hide of a tooltip shown by the hover or the focus, and it answers Enter and Space
    /// on the anchor the way it answers the pointer. A tooltip the press is meant to open and close
    /// instead is <see cref="ShowOnClick"/>, which takes the press over when it is on.
    /// </remarks>
    [Parameter] public bool HideOnClick { get; set; }

    /// <summary>
    /// Lets the pointer travel into the tooltip and stay there without it being hidden, which is what
    /// WCAG 1.4.13 asks of content shown on hover, and what a tooltip whose text has to be read across,
    /// magnified or selected needs.
    /// </summary>
    /// <remarks>
    /// The gap between the anchor and the tooltip is bridged by an invisible margin around the tooltip,
    /// so the pointer never leaves the component on its way over. A tooltip that holds something to
    /// click or to type in is a callout rather than a tooltip: it has to take the focus, which a tooltip
    /// never does.
    /// </remarks>
    [Parameter, ResetClassBuilder] public bool Interactive { get; set; }

    /// <summary>
    /// The visibility state of the tooltip.
    /// </summary>
    [Parameter, TwoWayBound]
    public bool IsShown { get; set; }

    /// <summary>
    /// Holds the content of the tooltip out of the DOM until the tooltip is first shown, and keeps it
    /// rendered from then on. The element the relationship points at is still on the page from the
    /// start, but it is empty until then, so the accessible name or description the tooltip provides
    /// is only there once the tooltip has been shown for the first time.
    /// </summary>
    [Parameter] public bool LazyRender { get; set; }

    /// <summary>
    /// The maximum width of the tooltip as a CSS value (e.g. "20rem"), beyond which its text wraps onto
    /// another line instead of the tooltip growing wider. Leaving it unset keeps the cap the theme gives
    /// it, and a value of "none" takes the cap off.
    /// </summary>
    [Parameter, ResetStyleBuilder] public string? MaxWidth { get; set; }

    /// <summary>
    /// Mirrors the position of the tooltip along the horizontal axis while the direction is right to
    /// left, so that a position named for one side of the anchor lands on the side the reader starts at.
    /// </summary>
    /// <remarks>
    /// The twelve positions are named for the sides of the screen rather than for the reading order, so
    /// Left is the left of the anchor in either direction unless this is turned on. Turn it on for a
    /// tooltip that follows the text - a hint beside a field, say - and leave it off for one that has to
    /// stay where it is put, such as a tooltip aimed at the edge of a fixed layout.
    /// </remarks>
    [Parameter] public bool MirrorInRtl { get; set; }

    /// <summary>
    /// Removes the fade the tooltip is shown and hidden with, so that it simply appears.
    /// </summary>
    [Parameter, ResetClassBuilder] public bool NoAnimation { get; set; }

    /// <summary>
    /// Keeps the Escape key from dismissing the tooltip.
    /// </summary>
    /// <remarks>
    /// Dismissing content shown on hover or focus without moving either of them is what WCAG 1.4.13 asks
    /// for, so only turn it off for a tooltip that obscures nothing.
    /// </remarks>
    [Parameter] public bool NoDismissOnEscape { get; set; }

    /// <summary>
    /// Keeps a touch or a pen from showing the tooltip at all, leaving the anchor to answer the tap alone.
    /// </summary>
    /// <remarks>
    /// A touch screen has no pointer that hovers, so a tap both presses the anchor and shows the tooltip
    /// over whatever the press brought up. Turn it off for a tooltip that only repeats what a touch user
    /// can already read; leave it on where the tooltip is the only place the text is.
    /// </remarks>
    [Parameter] public bool NoTouch { get; set; }

    /// <summary>
    /// The distance in pixels between the anchor and the tooltip, which is also the room the arrow is
    /// drawn in. An arrow that reaches further than this is given the room it needs anyway, so this is
    /// the smallest distance rather than the exact one. Leaving it unset keeps the distance the theme
    /// gives it.
    /// </summary>
    [Parameter, ResetStyleBuilder] public int? Offset { get; set; }

    /// <summary>
    /// The callback that is called when the tooltip is hidden.
    /// </summary>
    [Parameter] public EventCallback OnHide { get; set; }

    /// <summary>
    /// The callback that is called when the tooltip is shown.
    /// </summary>
    [Parameter] public EventCallback OnShow { get; set; }

    /// <summary>
    /// The callback that is called when the tooltip is shown or hidden, with the new state.
    /// </summary>
    [Parameter] public EventCallback<bool> OnToggle { get; set; }

    /// <summary>
    /// The position of tooltip around its anchor.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public BitTooltipPosition Position { get; set; }

    /// <summary>
    /// What the tooltip is to the anchor it belongs to: the description of a control that has a name of
    /// its own, the name of one that has none, or nothing at all.
    /// </summary>
    /// <remarks>
    /// It decides which of aria-describedby and aria-labelledby the anchor is given, and is declared for
    /// as long as there is a tooltip to declare it with rather than only while it is on the screen. The
    /// tooltip declares it on the element it wraps the anchor in and copies it onto the first focusable
    /// control inside that element, since a name or a description is computed on the element that has the
    /// focus; an anchor that names one of its own keeps it.
    /// </remarks>
    [Parameter] public BitTooltipRelationship Relationship { get; set; }

    /// <summary>
    /// Delay (in milliseconds) before showing the tooltip.
    /// </summary>
    /// <remarks>
    /// It applies to the pointer only: a tooltip reached with the keyboard or opened by a click is shown
    /// at once, since the user asked for it rather than merely passed over it. Leaving it alone inside a
    /// <see cref="BitTooltipGroup"/> takes the delay the group sets for all of its tooltips, and the
    /// group also drops the delay entirely while another of its tooltips is still fresh in mind.
    /// </remarks>
    [Parameter] public int ShowDelay { get; set; } = 0;

    /// <summary>
    /// Turns the anchor into a toggle for the tooltip, which is shown by a press of it and taken away by
    /// the next one.
    /// </summary>
    /// <remarks>
    /// Enter and Space are a press of the anchor as much as the pointer is, so a tooltip only the click
    /// shows can still be opened from the keyboard. What dismisses it, besides a second press, is the
    /// Escape key and the focus leaving the anchor - which a click elsewhere on the page and a Tab away
    /// from it both come down to. It takes the press over from <see cref="HideOnClick"/>.
    /// </remarks>
    [Parameter] public bool ShowOnClick { get; set; }

    /// <summary>
    /// Determines shows tooltip on focus. It defaults to true, so that a tooltip reached with the
    /// keyboard is shown the way it is to a pointer.
    /// </summary>
    [Parameter] public bool ShowOnFocus { get; set; } = true;

    /// <summary>
    /// Determines shows tooltip on hover.
    /// </summary>
    [Parameter] public bool ShowOnHover { get; set; } = true;

    /// <summary>
    /// The size of the tooltip, which sets the size of its text and the padding around it.
    /// </summary>
    [Parameter, ResetClassBuilder] public BitSize? Size { get; set; }

    /// <summary>
    /// Custom CSS styles for different parts of the BitTooltip.
    /// </summary>
    [Parameter] public BitTooltipClassStyles? Styles { get; set; }

    /// <summary>
    /// The content you want inside the tooltip.
    /// </summary>
    [Parameter] public RenderFragment? Template { get; set; }

    /// <summary>
    /// The text of tooltip to show.
    /// </summary>
    [Parameter] public string? Text { get; set; }

    /// <summary>
    /// The time in milliseconds a tooltip shown by a touch stays before it hides itself. A touch leaves
    /// no pointer behind that can leave the anchor again, so without it the tooltip would stay for good.
    /// Zero leaves it shown until something else hides it.
    /// </summary>
    [Parameter] public int TouchHideDelay { get; set; } = 1500;

    /// <summary>
    /// The time in milliseconds a touch has to rest on the anchor before the tooltip is shown, which turns
    /// a tap that only meant to press the anchor into a press that leaves the tooltip out of it. Zero shows
    /// the tooltip on the tap itself.
    /// </summary>
    /// <remarks>
    /// A tap both presses the anchor and asks for the tooltip, so a tooltip shown at once lands over
    /// whatever the press brought up; a delay here is what makes the tooltip a long press of its own.
    /// <see cref="NoTouch"/> is the way to leave the touch to the anchor altogether.
    /// </remarks>
    [Parameter] public int TouchShowDelay { get; set; }

    /// <summary>
    /// The stacking order of the tooltip surface and its arrow. Leaving it unset keeps the one the theme
    /// gives every popup surface in the library.
    /// </summary>
    /// <remarks>
    /// The tooltip is laid out inside the flow of the page rather than at the end of the body, so it is
    /// stacked against whatever the page puts around its anchor. This is the way past a neighbour that
    /// would otherwise be painted over it.
    /// </remarks>
    [Parameter, ResetStyleBuilder] public int? ZIndex { get; set; }



    /// <summary>
    /// The id of the element the text of the tooltip is rendered in, which is what an anchor of your own
    /// points its aria-describedby or aria-labelledby at.
    /// </summary>
    /// <remarks>
    /// The relationship the tooltip declares of its own accord is on the element wrapping the anchor. A
    /// screen reader computes the description of the control that has the focus, so an anchor that is a
    /// plain HTML element of yours is better off carrying the reference itself; give the tooltip an
    /// <see cref="BitComponentBase.Id"/> and this is the id to point at.
    /// </remarks>
    public string TooltipId => _tooltipId;



    /// <summary>
    /// Shows the tooltip programmatically, at once and regardless of the triggers it is configured with,
    /// unless it is disabled.
    /// </summary>
    public async Task Show()
    {
        if (IsEnabled is false) return;

        CancelPendingDelays();

        await SetIsShown(true);
    }

    /// <summary>
    /// Hides the tooltip programmatically, at once and regardless of the delays it is configured with.
    /// </summary>
    public async Task Hide()
    {
        CancelPendingDelays();

        await SetIsShown(false);
    }

    /// <summary>
    /// Toggles the tooltip to show/hide it.
    /// </summary>
    public Task Toggle() => IsShown ? Hide() : Show();



    protected override string RootElementClass => "bit-ttp";

    protected override void RegisterCssClasses()
    {
        ClassBuilder.Register(() => Classes?.Root);

        ClassBuilder.Register(() => FullWidth ? "bit-ttp-flw" : string.Empty);

        ClassBuilder.Register(() => Interactive ? "bit-ttp-itr" : string.Empty);

        ClassBuilder.Register(() => NoAnimation ? "bit-ttp-nan" : string.Empty);

        ClassBuilder.Register(() => Size switch
        {
            BitSize.Small => "bit-ttp-sm",
            BitSize.Medium => "bit-ttp-md",
            BitSize.Large => "bit-ttp-lg",
            _ => string.Empty
        });

        ClassBuilder.Register(() => Color switch
        {
            BitColor.Primary => "bit-ttp-pri",
            BitColor.Secondary => "bit-ttp-sec",
            BitColor.Tertiary => "bit-ttp-ter",
            BitColor.Info => "bit-ttp-inf",
            BitColor.Success => "bit-ttp-suc",
            BitColor.Warning => "bit-ttp-wrn",
            BitColor.SevereWarning => "bit-ttp-swr",
            BitColor.Error => "bit-ttp-err",
            BitColor.PrimaryBackground => "bit-ttp-pbg",
            BitColor.SecondaryBackground => "bit-ttp-sbg",
            BitColor.TertiaryBackground => "bit-ttp-tbg",
            BitColor.PrimaryForeground => "bit-ttp-pfg",
            BitColor.SecondaryForeground => "bit-ttp-sfg",
            BitColor.TertiaryForeground => "bit-ttp-tfg",
            BitColor.PrimaryBorder => "bit-ttp-pbr",
            BitColor.SecondaryBorder => "bit-ttp-sbr",
            BitColor.TertiaryBorder => "bit-ttp-tbr",
            _ => string.Empty
        });
    }

    protected override void RegisterCssStyles()
    {
        StyleBuilder.Register(() => Styles?.Root);

        // The three measurements the placement rules read. They are declared on the root element, so a
        // value handed over here beats the default the stylesheet leaves on that same element.
        StyleBuilder.Register(() => Offset.HasValue ? $"--bit-ttp-offset:{Offset.Value}px" : string.Empty);

        StyleBuilder.Register(() => ArrowSize.HasValue ? $"--bit-ttp-arrow-size:{ArrowSize.Value}px" : string.Empty);

        StyleBuilder.Register(() => MaxWidth.HasValue() ? $"--bit-ttp-max-width:{MaxWidth}" : string.Empty);

        StyleBuilder.Register(() => ZIndex.HasValue ? $"--bit-ttp-zindex:{ZIndex.Value}" : string.Empty);
    }

    protected override async Task OnInitializedAsync()
    {
        Group?.Register(this);

        if (IsShownHasBeenSet is false && DefaultIsShown.HasValue)
        {
            await AssignIsShown(DefaultIsShown.Value);
        }

        _contentRendered = LazyRender is false || IsShown;

        await base.OnInitializedAsync();
    }

    protected override async Task OnParametersSetAsync()
    {
        // A tooltip that is turned off while it is shown takes its content off the screen with it,
        // instead of leaving behind a surface that nothing on the page can dismiss any more. The reasons
        // it was on the screen for go with it, so turning it back on does not bring it straight back.
        if (IsEnabled is false)
        {
            _isPointerOver = false;
            _isFocusWithin = false;
            _isFocusFromPointer = false;
            _isShownByClick = false;

            if (IsShown)
            {
                CancelPendingDelays();

                await SetIsShown(false);
            }
        }

        if (IsShown)
        {
            _contentRendered = true;
        }

        await base.OnParametersSetAsync();
    }



    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        await base.OnAfterRenderAsync(firstRender);

        await SyncAnchorAria();
    }

    // The root element carries the relationship the tooltip declares, but the element the reader actually
    // lands on is the control the consumer put inside it, so the relationship is copied onto that one as
    // well - a describedby or a labelledby on a container that is neither focusable nor interactive is one
    // no screen reader ever reads. Only when what it would write has changed: the attribute is written from
    // JavaScript, so a call per render would be a round trip per render for something that changes with the
    // relationship alone.
    private async Task SyncAnchorAria()
    {
        if (IsDisposed) return;

        var attribute = AriaAttribute;

        if (_syncedAria == attribute) return;

        _syncedAria = attribute;

        try
        {
            await _js.BitUtilsSyncAriaDescription(_Id, _tooltipId, attribute);
        }
        catch (JSDisconnectedException) { } // we can ignore this exception here
    }

    private async Task ShowAfterDelay(int delay)
    {
        if (IsEnabled is false) return;

        CancelPendingDelays();

        if (delay > 0)
        {
            var tokenSource = _showDelayTokenSource = new CancellationTokenSource();

            try
            {
                await Task.Delay(delay, tokenSource.Token);
            }
            catch (OperationCanceledException) { return; }
            finally
            {
                // Only the waiter that still owns the field clears it: a newer show has already put its
                // own source there, and clearing that one would leave it impossible to cancel.
                if (ReferenceEquals(_showDelayTokenSource, tokenSource)) _showDelayTokenSource = null;
                tokenSource.Dispose();
            }

            if (IsDisposed || IsEnabled is false) return;
        }

        await SetIsShown(true);
    }

    private async Task HideAfterDelay(int delay)
    {
        CancelPendingDelays();

        if (delay > 0)
        {
            var tokenSource = _hideDelayTokenSource = new CancellationTokenSource();

            try
            {
                await Task.Delay(delay, tokenSource.Token);
            }
            catch (OperationCanceledException) { return; }
            finally
            {
                if (ReferenceEquals(_hideDelayTokenSource, tokenSource)) _hideDelayTokenSource = null;
                tokenSource.Dispose();
            }

            if (IsDisposed) return;
        }

        await SetIsShown(false);
    }

    private void CancelPendingDelays()
    {
        var showTokenSource = _showDelayTokenSource;
        _showDelayTokenSource = null;
        showTokenSource?.Cancel();
        showTokenSource?.Dispose();

        var hideTokenSource = _hideDelayTokenSource;
        _hideDelayTokenSource = null;
        hideTokenSource?.Cancel();
        hideTokenSource?.Dispose();
    }

    private async Task SetIsShown(bool value)
    {
        if (IsShown == value) return;

        if (await AssignIsShown(value) is false) return;

        if (value)
        {
            _contentRendered = true;

            // A group holds one tooltip at a time, so the one that has just been shown takes the place of
            // whichever of its siblings was on the screen before it.
            if (Group is not null)
            {
                await Group.NotifyShown(this);
            }

            await OnShow.InvokeAsync();
        }
        else
        {
            _isShownByClick = false;

            Group?.NotifyHidden();

            await OnHide.InvokeAsync();
        }

        await OnToggle.InvokeAsync(value);

        // The state can be reached from a delay that has outlived the event handler which started it,
        // and so from past the render Blazor does of its own accord when a handler returns.
        await InvokeAsync(StateHasChanged);
    }

    // Whether one of the two reasons a tooltip stays on the screen is still standing. It is what keeps a
    // pointer leaving an anchor from taking away the tooltip the keyboard on it is still asking for.
    private bool IsHeldByHover => ShowOnHover && _isPointerOver;

    private bool IsHeldByFocus => ShowOnFocus && _isFocusWithin && _isFocusFromPointer is false;

    private async Task HandlePointerEnter(PointerEventArgs e)
    {
        if (IsControlledExternally) return;

        if (IsTouch(e) && NoTouch) return;

        _isPointerOver = true;

        if (ShowOnHover is false) return;

        // A touch has no pointer that hovers: the enter and the leave arrive back to back around the
        // tap, so the tooltip is shown for as long as the press lasts - at once, or after the press has
        // lasted long enough to be a request of its own - and then hides itself after a while instead.
        if (IsTouch(e))
        {
            await ShowAfterDelay(TouchShowDelay);

            // The press ended before the tooltip was due, so the leave that ended it took the waiting show
            // with it: there is nothing on the screen to hide again, and the tap that ended it is a tap
            // like any other - the up it ends with is not the one that has to be kept from undoing a
            // tooltip nothing put on the screen.
            if (IsShown is false) return;

            _isShownByTouch = true;

            if (TouchHideDelay > 0)
            {
                await HideAfterDelay(TouchHideDelay);
            }

            return;
        }

        // The group hands the tooltip its delay when it has none of its own, and takes the delay away
        // altogether while another tooltip of the group is still fresh in mind.
        await ShowAfterDelay(Group?.ShouldSkipShowDelay() is true ? 0 : _ShowDelay);
    }

    private async Task HandlePointerLeave(PointerEventArgs e)
    {
        if (IsControlledExternally) return;

        if (IsTouch(e) && NoTouch) return;

        var wasHeldByHover = IsHeldByHover;

        _isPointerOver = false;

        // A press that has not been followed by a focus of its own leaves nothing behind once the pointer
        // is gone, so the next keyboard arrival is answered as the keyboard rather than as that press.
        if (_isFocusWithin is false) _isFocusFromPointer = false;

        // A tap the pointer up of which never arrived - one that was cancelled, or taken by something
        // else on the page - leaves nothing for the next tap to be measured against.
        _isShownByTouch = false;

        if (wasHeldByHover is false) return;

        // The leave that follows a tap would take the tooltip away the instant it was shown; the touch
        // timer the enter above started is what hides that one. A press that ended before the tooltip was
        // due is a tap that only meant to press the anchor, and the show still waiting on it goes with it.
        if (IsTouch(e))
        {
            if (IsShown is false) CancelPendingDelays();

            return;
        }

        // The keyboard is still on the anchor, so the tooltip it asked for stays where it is.
        if (IsHeldByFocus) return;

        await HideAfterDelay(_HideDelay);
    }

    private async Task HandleFocusIn()
    {
        if (IsControlledExternally) return;

        _isFocusWithin = true;

        if (IsHeldByFocus is false) return;

        // Reaching a control with the keyboard is asking for the tooltip rather than passing over it, so
        // the hover delay - which is there to keep a pointer crossing the page quiet - does not apply.
        await ShowAfterDelay(0);
    }

    private async Task HandleFocusOut()
    {
        if (IsControlledExternally) return;

        var wasHeldByFocus = IsHeldByFocus;
        var wasShownByClick = _isShownByClick && IsShown;

        _isFocusWithin = false;
        _isFocusFromPointer = false;

        // A tooltip a press of the anchor opened is held by nothing that can end on its own, so what
        // dismisses it is the next thing the user does elsewhere: a click somewhere else on the page and a
        // Tab away from the anchor both take the focus off it, which is the one signal both leave behind.
        // A pointer still resting on the anchor is asking for the tooltip in its own right, so it stays.
        if (wasShownByClick)
        {
            _isShownByClick = false;

            if (IsHeldByHover is false)
            {
                await HideAfterDelay(0);

                return;
            }
        }

        if (wasHeldByFocus is false) return;

        // The pointer is still on the anchor, so the tooltip it is hovering stays where it is.
        if (IsHeldByHover) return;

        await HideAfterDelay(_HideDelay);
    }

    private void HandlePointerDown(PointerEventArgs e)
    {
        // The focus that follows a press is the press, not the keyboard, and the tooltip is already being
        // shown by the pointer that made it. Recording it here is what lets the focus in the moment after
        // be told apart from a tab that reached the same control.
        if (e.Button != 0) return;

        _isFocusFromPointer = true;
    }

    private async Task HandlePointerUp(PointerEventArgs e)
    {
        if (IsControlledExternally) return;

        if (IsTouch(e) && NoTouch) return;

        // Only the primary button acts: the secondary one belongs to the context menu of the page.
        if (e.Button != 0) return;

        // The tap that has just put the tooltip on the screen is not also a click on it, or the same tap
        // would show the tooltip and take it away again in the one gesture.
        if (IsTouch(e) && _isShownByTouch)
        {
            _isShownByTouch = false;

            return;
        }

        if (ShowOnClick)
        {
            if (IsShown)
            {
                await HideAfterDelay(0);
            }
            else
            {
                _isShownByClick = true;

                await ShowAfterDelay(0);
            }

            return;
        }

        if (HideOnClick is false) return;

        // The pointer that pressed the anchor is still on it, so the hover that showed the tooltip is
        // given up along with it: nothing is left to put the tooltip back until the pointer leaves and
        // comes again.
        _isPointerOver = false;

        await HideAfterDelay(0);
    }

    private async Task HandleKeyDown(KeyboardEventArgs e)
    {
        if (IsControlledExternally) return;

        if (e.Key is "Escape")
        {
            if (NoDismissOnEscape) return;
            if (IsShown is false) return;

            // The dismissal has to outlive the pointer and the focus that are still on the anchor, or the
            // tooltip would be back the moment anything asked the triggers again.
            _isPointerOver = false;
            _isFocusFromPointer = true;
            _isShownByClick = false;

            await HideAfterDelay(0);

            return;
        }

        // Enter and Space are how a keyboard presses the anchor, so they are answered the way the pointer
        // pressing it is: a click-driven tooltip is toggled, and one that steps aside for a click steps
        // aside for these as well. Without it a tooltip that only the click shows would be one the keyboard
        // could never open. The key is left to travel on to the anchor, which is what is being pressed.
        if (e.Key is not ("Enter" or " ")) return;

        if (ShowOnClick)
        {
            if (IsShown)
            {
                _isShownByClick = false;

                await HideAfterDelay(0);
            }
            else
            {
                _isShownByClick = true;

                await ShowAfterDelay(0);
            }

            return;
        }

        if (HideOnClick is false) return;
        if (IsShown is false) return;

        // As with the pointer, what showed the tooltip is given up along with it, so that nothing puts it
        // back until the anchor is left and reached again.
        _isPointerOver = false;
        _isFocusFromPointer = true;

        await HideAfterDelay(0);
    }

    private static bool IsTouch(PointerEventArgs e) => e.PointerType is "touch" or "pen";

    // Nothing is done with the event: it is bound only so that its propagation can be stopped, which is
    // what keeps a pointer pressed or released inside the tooltip from reaching the handlers on the root.
    private static void SwallowPointerEvent(PointerEventArgs e) { }

    // The way the group takes a sibling off the screen. It leaves a tooltip the page itself is driving
    // alone, since a state that was handed over is not the group's to change, and it gives up the reasons
    // the tooltip was shown for so that nothing puts it straight back.
    internal async Task HideFromGroup()
    {
        if (IsControlledExternally) return;
        if (IsShown is false) return;

        _isPointerOver = false;
        _isFocusWithin = false;
        _isFocusFromPointer = false;
        _isShownByClick = false;

        await Hide();
    }

    protected override ValueTask DisposeAsync(bool disposing)
    {
        if (IsDisposed || disposing is false) return ValueTask.CompletedTask;

        Group?.Unregister(this);

        CancelPendingDelays();

        return base.DisposeAsync(disposing);
    }

    // The horizontal half of a position swapped for its opposite. The vertical ones are left alone: Top
    // is the top of the anchor in either direction, and a centred position has no side to swap.
    private static BitTooltipPosition Mirror(BitTooltipPosition position) => position switch
    {
        BitTooltipPosition.TopLeft => BitTooltipPosition.TopRight,
        BitTooltipPosition.TopRight => BitTooltipPosition.TopLeft,
        BitTooltipPosition.RightTop => BitTooltipPosition.LeftTop,
        BitTooltipPosition.Right => BitTooltipPosition.Left,
        BitTooltipPosition.RightBottom => BitTooltipPosition.LeftBottom,
        BitTooltipPosition.BottomRight => BitTooltipPosition.BottomLeft,
        BitTooltipPosition.BottomLeft => BitTooltipPosition.BottomRight,
        BitTooltipPosition.LeftBottom => BitTooltipPosition.RightBottom,
        BitTooltipPosition.Left => BitTooltipPosition.Right,
        BitTooltipPosition.LeftTop => BitTooltipPosition.RightTop,
        _ => position
    };

    private string GetTooltipClasses()
    {
        var visibility = IsShown ? "bit-ttp-vis " : string.Empty;

        var placement = MirrorInRtl && Dir == BitDir.Rtl ? Mirror(Position) : Position;

        var position = placement switch
        {
            BitTooltipPosition.Top => "bit-ttp-top",
            BitTooltipPosition.TopLeft => "bit-ttp-tlf",
            BitTooltipPosition.TopRight => "bit-ttp-trg",
            BitTooltipPosition.RightTop => "bit-ttp-rtp",
            BitTooltipPosition.Right => "bit-ttp-rgt",
            BitTooltipPosition.RightBottom => "bit-ttp-rbm",
            BitTooltipPosition.BottomRight => "bit-ttp-brg",
            BitTooltipPosition.Bottom => "bit-ttp-btm",
            BitTooltipPosition.BottomLeft => "bit-ttp-blf",
            BitTooltipPosition.LeftBottom => "bit-ttp-lbm",
            BitTooltipPosition.Left => "bit-ttp-lft",
            BitTooltipPosition.LeftTop => "bit-ttp-ltp",
            _ => "bit-ttp-top"
        };

        return visibility + position;
    }
}
