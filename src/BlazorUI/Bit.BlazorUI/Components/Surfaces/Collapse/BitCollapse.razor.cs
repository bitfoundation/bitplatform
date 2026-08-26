namespace Bit.BlazorUI;

/// <summary>
/// The Collapse component shows and hides a section of related content with an animated transition,
/// leaving the trigger that toggles it to the page around it.
/// </summary>
public partial class BitCollapse : BitComponentBase
{
    // The duration the stylesheet gives the transition when nothing overrides it, which is what a delayed
    // unmount has to outlast. It mirrors --bit-mot-duration-long-full rather than reading it: the value only
    // has to be long enough for the content to have finished leaving the screen before it leaves the DOM.
    private const int DefaultDurationInMs = 300;

    // Whether the content has ever been expanded, which is the whole of what LazyRender waits for and what
    // keeps a collapse that was never opened from scheduling an unmount of something that was never mounted.
    private bool _everExpanded;

    // Whether UnmountOnCollapse has taken the content back out of the DOM. It is set once the collapse
    // transition has had time to run, and cleared the moment the component is expanded again.
    private bool _unmounted;

    // Whether the expand transition has finished, which is the only moment at which it is safe to stop
    // clipping the content: while the track is still growing, anything the wrapper does not clip spills
    // out of a collapse that is not yet its full size.
    private bool _entered;

    private CancellationTokenSource? _transitionCts;



    // A collapse holding a peek of its content is partly on the screen even while it is closed, so it is
    // neither hidden from assistive technology nor taken out of the tab order; a fully closed one is both.
    private bool _visible => Expanded || _keepsPeek;

    // Whether a CollapsedSize keeps part of the content on the screen while the collapse is closed, which
    // is what makes the two rendering parameters inapplicable: there would be nothing left in the peek.
    private bool _keepsPeek => CollapsedSize.HasValue();

    // inert takes the closed content out of the tab order, out of hit testing and out of the accessibility
    // tree in one attribute. The stylesheet hides it as well - a zero-height box with overflow:hidden still
    // holds focusable children - but the attribute applies the instant the state flips rather than at the
    // end of the transition, which is what keeps a Tab pressed mid-collapse from landing inside it.
    private bool _inert => _visible is false;

    // An unnamed region is dropped by assistive technology rather than announced, so the role is worth
    // keeping only while the consumer can name it; it stays the default for the markup this component has
    // always rendered, and an explicitly empty Role takes it off.
    private string? _role => Role is null ? "region" : (Role.HasValue() ? Role : null);

    // Content that has never been expanded is not rendered at all while LazyRender is on, and content that
    // has been collapsed long enough for the transition to finish is dropped again while UnmountOnCollapse is.
    // Neither applies to a collapse that keeps a peek, which has to have something in it to show.
    private bool _renderContent => _keepsPeek || ((LazyRender is false || _everExpanded) && _unmounted is false);



    /// <summary>
    /// The id of the content element of the collapse, so a trigger elsewhere on the page can point its
    /// <c>aria-controls</c> at the section it opens.
    /// </summary>
    /// <remarks>
    /// It is derived from the id of the root element, so setting <see cref="BitComponentBase.Id"/> makes it
    /// predictable and leaving it unset still gives a stable value for the lifetime of the component.
    /// </remarks>
    public string ContentId => $"{_Id}-content";



    /// <summary>
    /// The color kind of the background of the collapse.
    /// </summary>
    /// <remarks>
    /// The collapse paints the primary background by default so its content never shows the page through it.
    /// <see cref="BitColorKind.Transparent"/> takes that away, which is what a collapse sitting on a surface
    /// that already has a background of its own - a card, a panel, a colored section - wants.
    /// </remarks>
    [Parameter, ResetClassBuilder]
    public BitColorKind? Background { get; set; }

    /// <summary>
    /// Alias for the ChildContent parameter.
    /// </summary>
    [Parameter] public RenderFragment? Body { get; set; }

    /// <summary>
    /// The content of the collapse.
    /// </summary>
    [Parameter] public RenderFragment? ChildContent { get; set; }

    /// <summary>
    /// Custom CSS classes for different parts of the collapse.
    /// </summary>
    [Parameter] public BitCollapseClassStyles? Classes { get; set; }

    /// <summary>
    /// The size the collapse keeps while it is collapsed, as any CSS length, which leaves a peek of the
    /// content on the page instead of closing it all the way.
    /// </summary>
    /// <remarks>
    /// This is the height of the closed collapse, or its width while <see cref="Horizontal"/> is on. It is
    /// what a "show more" clamp is made of: the first few lines stay readable and the rest of them animate in.
    /// <br />
    /// A collapse that keeps a peek is still partly on the screen, so it neither fades out nor hides itself
    /// from assistive technology while it is closed, and it ignores <see cref="LazyRender"/> and
    /// <see cref="UnmountOnCollapse"/>: the peek has to have something in it to show.
    /// <br />
    /// The value is the size of the closed section rather than of the content inside its padding, so it means
    /// the same thing whether or not <see cref="NoPadding"/> is on.
    /// </remarks>
    [Parameter, ResetClassBuilder, ResetStyleBuilder]
    public string? CollapsedSize { get; set; }

    /// <summary>
    /// The default value of the Expanded parameter, for a collapse that is left to manage its own state.
    /// </summary>
    /// <remarks>
    /// It is applied once, at initialization, and only while <c>Expanded</c> itself has not been set.
    /// </remarks>
    [Parameter] public bool? DefaultExpanded { get; set; }

    /// <summary>
    /// The delay of the expand/collapse transition in ms.
    /// </summary>
    /// <remarks>
    /// This postpones the start of the transition; it does not make it longer. Negative values are clamped away.
    /// </remarks>
    [Parameter, ResetStyleBuilder]
    public int? Delay { get; set; }

    /// <summary>
    /// The duration of the expand/collapse transition in ms.
    /// </summary>
    /// <remarks>
    /// Leaving it unset keeps the duration the motion theme gives a transition of this length, which is also
    /// what collapses to nothing when the reader asks for reduced motion. A value set here is a fixed
    /// duration that the reduced motion preference no longer shortens, so prefer the theme unless the pace
    /// of this particular collapse carries meaning. Negative values are clamped away.
    /// <br />
    /// It is also what <see cref="OnExpanded"/>, <see cref="OnCollapsed"/>, <see cref="NoClip"/> and
    /// <see cref="UnmountOnCollapse"/> wait for, so a collapse whose transition is retuned in CSS rather than
    /// here is better off setting it to the same value.
    /// </remarks>
    [Parameter, ResetStyleBuilder]
    public int? Duration { get; set; }

    /// <summary>
    /// The timing function of the expand/collapse transition, as any CSS easing value.
    /// </summary>
    /// <remarks>
    /// Leaving it unset keeps the easing of the motion theme, which is what every other transition in the
    /// library is drawn with.
    /// </remarks>
    [Parameter, ResetStyleBuilder]
    public string? Easing { get; set; }

    /// <summary>
    /// Determines whether the collapse is expanded or collapsed.
    /// <br />
    /// The default value is <strong>false</strong>.
    /// </summary>
    /// <remarks>
    /// The collapse renders no trigger of its own: whatever toggles it lives on the page around it, which is
    /// what lets one button drive several collapses, or several buttons drive one. Bind it with
    /// <c>@bind-Expanded</c> to let <see cref="ToggleAsync"/> and the methods beside it write back to it.
    /// </remarks>
    [Parameter, ResetClassBuilder, ResetStyleBuilder, TwoWayBound, CallOnSet(nameof(HandleExpandedChanged))]
    public bool Expanded { get; set; }

    /// <summary>
    /// Collapses the content along the inline axis instead of the block one, so it opens sideways.
    /// <br />
    /// The default value is <strong>false</strong>.
    /// </summary>
    /// <remarks>
    /// A horizontal collapse takes the width of its content rather than the full width of its container, and
    /// opens from the start edge, which follows the direction of the page.
    /// </remarks>
    [Parameter, ResetClassBuilder]
    public bool Horizontal { get; set; }

    /// <summary>
    /// The id of the element that names the content region of the collapse, rendered as <c>aria-labelledby</c>.
    /// </summary>
    /// <remarks>
    /// A region is only announced as one while it has a name, so pointing this at the trigger that opens the
    /// collapse - or at the heading above it - is what turns the section into something a screen reader can
    /// jump to. See also <see cref="Role"/>.
    /// </remarks>
    [Parameter] public string? LabelledBy { get; set; }

    /// <summary>
    /// Keeps the content out of the DOM until the collapse is expanded for the first time.
    /// <br />
    /// The default value is <strong>false</strong>.
    /// </summary>
    /// <remarks>
    /// This is what a page carrying many collapses of expensive content wants: nothing inside a closed one is
    /// built, queried or measured until it is opened. Once it has been opened the content stays, unless
    /// <see cref="UnmountOnCollapse"/> says otherwise.
    /// <br />
    /// A collapse that keeps a <see cref="CollapsedSize"/> ignores it: the peek has to have something in it
    /// to show before the section is ever opened.
    /// </remarks>
    [Parameter] public bool LazyRender { get; set; }

    /// <summary>
    /// Removes the expand/collapse transition, so the content appears and disappears at once.
    /// <br />
    /// The default value is <strong>false</strong>.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public bool NoAnimation { get; set; }

    /// <summary>
    /// Removes the fade of the content, leaving the size on its own to open and close the collapse.
    /// <br />
    /// The default value is <strong>false</strong>.
    /// </summary>
    /// <remarks>
    /// A collapse that keeps a <see cref="CollapsedSize"/> never fades either way: the peek has to stay readable.
    /// </remarks>
    [Parameter, ResetClassBuilder]
    public bool NoFade { get; set; }

    /// <summary>
    /// Stops clipping the content once the collapse has finished opening, so anything that reaches past the
    /// edges of the section - a focus ring, a shadow, a menu that drops out of a control inside it - is drawn
    /// in full instead of being cut off.
    /// <br />
    /// The default value is <strong>false</strong>.
    /// </summary>
    /// <remarks>
    /// The clipping is what makes the transition look like an opening section rather than content sliding
    /// over the page, so it is only taken off at the end of the expand transition and put back the moment the
    /// collapse starts closing again.
    /// </remarks>
    [Parameter, ResetClassBuilder]
    public bool NoClip { get; set; }

    /// <summary>
    /// Removes the padding the collapse puts around its content.
    /// <br />
    /// The default value is <strong>false</strong>.
    /// </summary>
    /// <remarks>
    /// The padding is what keeps text off the edges of the section; content that draws its own insets - a
    /// list, a card, an image that meets the container edge to edge - is better off without it.
    /// </remarks>
    [Parameter, ResetClassBuilder]
    public bool NoPadding { get; set; }

    /// <summary>
    /// Callback that is called when the Expanded value has changed.
    /// </summary>
    /// <remarks>
    /// It reports the changes the component itself makes - <see cref="ToggleAsync"/>,
    /// <see cref="ExpandAsync"/> and <see cref="CollapseAsync"/> - rather than the ones the page makes by
    /// assigning to <see cref="Expanded"/>.
    /// </remarks>
    [Parameter] public EventCallback<bool> OnChange { get; set; }

    /// <summary>
    /// Callback that is called once the collapse has finished closing.
    /// </summary>
    /// <remarks>
    /// It fires at the end of the collapse transition rather than at the start of it, which is the moment
    /// the content is off the screen: this is where a page unloads what the section was holding, or moves
    /// focus, or scrolls to what the closed section pushed back up. A collapse that is not animated at all
    /// reports it as soon as it closes.
    /// <br />
    /// It reports every close, whether the page made it by assigning to <see cref="Expanded"/> or the
    /// component made it itself, and never fires for a collapse that was closed to begin with.
    /// </remarks>
    [Parameter] public EventCallback OnCollapsed { get; set; }

    /// <summary>
    /// Callback that is called once the collapse has finished opening.
    /// </summary>
    /// <remarks>
    /// It fires at the end of the expand transition rather than at the start of it, which is the moment the
    /// content is at its full size: this is where a page scrolls the section into view, or measures it, or
    /// moves focus into it. A collapse that is not animated at all reports it as soon as it opens.
    /// <br />
    /// It reports every open, whether the page made it by assigning to <see cref="Expanded"/> or the
    /// component made it itself, and never fires for a collapse that was open to begin with.
    /// </remarks>
    [Parameter] public EventCallback OnExpanded { get; set; }

    /// <summary>
    /// The ARIA role of the content region of the collapse.
    /// </summary>
    /// <remarks>
    /// It is <c>region</c> by default, which becomes a landmark a screen reader can jump to as soon as
    /// <see cref="LabelledBy"/> names it. Set it to an empty string to render no role at all, which is what a
    /// collapse holding something that already carries semantics of its own - a list, a table, a form - wants.
    /// </remarks>
    [Parameter] public string? Role { get; set; }

    /// <summary>
    /// Custom CSS styles for different parts of the collapse.
    /// </summary>
    [Parameter] public BitCollapseClassStyles? Styles { get; set; }

    /// <summary>
    /// Takes the content back out of the DOM once the collapse has closed.
    /// <br />
    /// The default value is <strong>false</strong>.
    /// </summary>
    /// <remarks>
    /// The content is removed after the collapse transition has had time to finish, so the close still
    /// animates; it is put back the moment the collapse is expanded again, which means anything the content
    /// was holding - the position of a scroll, the text in a field, the frame of a video - starts over.
    /// <br />
    /// A collapse that keeps a <see cref="CollapsedSize"/> ignores it: the peek would have nothing left in it
    /// to show.
    /// </remarks>
    [Parameter] public bool UnmountOnCollapse { get; set; }



    /// <summary>
    /// Expands the collapse, reporting the change through <c>ExpandedChanged</c> and <see cref="OnChange"/>.
    /// </summary>
    public Task ExpandAsync() => SetExpandedAsync(true);

    /// <summary>
    /// Collapses the collapse, reporting the change through <c>ExpandedChanged</c> and <see cref="OnChange"/>.
    /// </summary>
    public Task CollapseAsync() => SetExpandedAsync(false);

    /// <summary>
    /// Flips the collapse between expanded and collapsed, reporting the change through <c>ExpandedChanged</c>
    /// and <see cref="OnChange"/>.
    /// </summary>
    public Task ToggleAsync() => SetExpandedAsync(Expanded is false);



    protected override string RootElementClass => "bit-col";

    protected override void RegisterCssClasses()
    {
        ClassBuilder.Register(() => Classes?.Root);

        ClassBuilder.Register(() => Expanded ? "bit-col-exp" : "bit-col-col");

        ClassBuilder.Register(() => Expanded ? Classes?.Expanded : Classes?.Collapsed);

        ClassBuilder.Register(() => Horizontal ? "bit-col-hor" : string.Empty);

        ClassBuilder.Register(() => NoAnimation ? "bit-col-nan" : string.Empty);

        ClassBuilder.Register(() => (NoFade || _keepsPeek) ? "bit-col-nfd" : string.Empty);

        ClassBuilder.Register(() => NoPadding ? "bit-col-npd" : string.Empty);

        // The clipping is only taken off once the expand transition has finished, so the class carries both
        // halves of that condition rather than leaving the stylesheet to guess at the second one.
        ClassBuilder.Register(() => (NoClip && _entered) ? "bit-col-ncl" : string.Empty);

        ClassBuilder.Register(() => _keepsPeek ? "bit-col-pek" : string.Empty);

        ClassBuilder.Register(() => Background switch
        {
            BitColorKind.Primary => "bit-col-pbg",
            BitColorKind.Secondary => "bit-col-sbg",
            BitColorKind.Tertiary => "bit-col-tbg",
            BitColorKind.Transparent => "bit-col-rbg",
            _ => string.Empty
        });
    }

    protected override void RegisterCssStyles()
    {
        StyleBuilder.Register(() => Styles?.Root);

        StyleBuilder.Register(() => Expanded ? Styles?.Expanded : Styles?.Collapsed);

        // The pace of the transition is a value rather than a class, so it lands on the root element as a
        // custom property the stylesheet reads with the motion theme token as its fallback. Negative
        // milliseconds are clamped away: a negative duration is not one a browser accepts, and a negative
        // delay would start the transition part-way through.
        StyleBuilder.Register(() => Duration.HasValue ? $"--bit-col-dur:{Math.Max(0, Duration.Value)}ms" : string.Empty);

        StyleBuilder.Register(() => Delay.HasValue ? $"--bit-col-del:{Math.Max(0, Delay.Value)}ms" : string.Empty);

        StyleBuilder.Register(() => Easing.HasValue() ? $"--bit-col-eas:{Easing}" : string.Empty);

        StyleBuilder.Register(() => CollapsedSize.HasValue() ? $"--bit-col-csz:{CollapsedSize}" : string.Empty);
    }

    protected override async Task OnInitializedAsync()
    {
        if (ExpandedHasBeenSet is false && DefaultExpanded.HasValue)
        {
            await AssignExpanded(DefaultExpanded.Value);
        }

        await base.OnInitializedAsync();
    }

    protected override ValueTask DisposeAsync(bool disposing)
    {
        CancelPendingTransition();

        return base.DisposeAsync(disposing);
    }



    private async Task SetExpandedAsync(bool value)
    {
        if (IsEnabled is false) return;

        if (Expanded == value) return;

        if (await AssignExpanded(value) is false) return;

        await OnChange.InvokeAsync(value);

        StateHasChanged();
    }

    private void HandleExpandedChanged()
    {
        CancelPendingTransition();

        if (Expanded)
        {
            _everExpanded = true;
            _unmounted = false;
        }

        // Nothing has been drawn yet, so the state the component starts in is where it begins rather than
        // something it transitioned into: the end of that transition is reached at once and reported to no one.
        if (IsRendered is false)
        {
            SetEntered(Expanded);
            return;
        }

        // The clipping goes back on the moment either transition starts, since the content is the wrong size
        // for the whole of both of them.
        SetEntered(false);

        // A collapse that was closed to begin with never played a close, so there is nothing to report and
        // nothing to take out of the DOM that was ever put in it.
        if (Expanded is false && _everExpanded is false) return;

        var needsEnd = Expanded
            ? (NoClip || OnExpanded.HasDelegate)
            : ((UnmountOnCollapse && _keepsPeek is false) || OnCollapsed.HasDelegate);

        if (needsEnd is false) return;

        // The end of the transition is reached by the clock rather than by an event from the browser, so that
        // it is still reached when the transition was collapsed to nothing - by NoAnimation here, or by the
        // reduced motion preference in the stylesheet, which C# cannot see.
        var wait = NoAnimation ? 0 : Math.Max(0, Delay ?? 0) + Math.Max(0, Duration ?? DefaultDurationInMs);

        _transitionCts = new CancellationTokenSource();

        _ = CompleteTransitionAsync(wait, Expanded, _transitionCts.Token);
    }

    private async Task CompleteTransitionAsync(int wait, bool expanded, CancellationToken token)
    {
        try
        {
            if (wait > 0)
            {
                await Task.Delay(wait, token);
            }

            if (token.IsCancellationRequested || IsDisposed || Expanded != expanded) return;

            var render = false;

            if (expanded)
            {
                if (NoClip && _entered is false)
                {
                    SetEntered(true);
                    render = true;
                }
            }
            // The content leaves the DOM only after the close has had time to play, so the collapse still
            // animates shut rather than blinking out of existence.
            else if (UnmountOnCollapse && _keepsPeek is false && _unmounted is false)
            {
                _unmounted = true;
                render = true;
            }

            if (render)
            {
                await InvokeAsync(StateHasChanged);
            }

            var callback = expanded ? OnExpanded : OnCollapsed;

            if (callback.HasDelegate)
            {
                await InvokeAsync(() => callback.InvokeAsync());
            }
        }
        catch (OperationCanceledException) { }
        catch (ObjectDisposedException) { }
    }

    private void SetEntered(bool value)
    {
        if (_entered == value) return;

        _entered = value;

        ClassBuilder.Reset();
    }

    private void CancelPendingTransition()
    {
        if (_transitionCts is null) return;

        _transitionCts.Cancel();
        _transitionCts.Dispose();
        _transitionCts = null;
    }
}
