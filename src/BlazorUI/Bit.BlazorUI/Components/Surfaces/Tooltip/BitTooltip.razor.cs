namespace Bit.BlazorUI;

/// <summary>
/// Tooltip briefly describes an unlabeled control or adds a bit of information to a labeled one, in a
/// small surface that is shown next to what it belongs to for as long as the pointer or the keyboard
/// stays on it.
/// </summary>
/// <remarks>
/// The tooltip is shown on hover and on focus by default, is dismissed by the Escape key, and can be
/// made hoverable so that the pointer may travel into it - the three things WCAG 1.4.13 asks of content
/// shown on hover or focus. It is laid out purely in CSS, next to the anchor inside the flow of the
/// page, so it needs no positioning pass and no JavaScript; a surface that has to escape an overflow,
/// flip to the side with room, or hold interactive content of its own is what BitCallout is for.
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



    private string _tooltipId => $"{_Id}-ttp";

    private bool HasContent => Template is not null || Text.HasValue();

    // A tooltip whose shown state is handed to it and never handed back cannot be driven by anything
    // that happens on the page: the page owns it, and the triggers below leave it alone.
    private bool IsControlledExternally => IsShownHasBeenSet && IsShownChanged.HasDelegate is false;



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
    /// Hides the arrow of tooltip.
    /// </summary>
    [Parameter] public bool HideArrow { get; set; }

    /// <summary>
    /// Delay (in milliseconds) before hiding the tooltip.
    /// </summary>
    /// <remarks>
    /// It is the grace an <see cref="Interactive"/> tooltip needs while the pointer crosses the gap
    /// between the anchor and the tooltip, and the pause that keeps a tooltip from flickering while the
    /// pointer skims across a row of anchors.
    /// </remarks>
    [Parameter] public int HideDelay { get; set; } = 0;

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
    /// rendered from then on.
    /// </summary>
    [Parameter] public bool LazyRender { get; set; }

    /// <summary>
    /// The maximum width of the tooltip as a CSS value (e.g. "20rem"), beyond which its text wraps onto
    /// another line instead of the tooltip growing wider. Leaving it unset keeps the cap the theme gives
    /// it, and a value of "none" takes the cap off.
    /// </summary>
    [Parameter, ResetStyleBuilder] public string? MaxWidth { get; set; }

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
    /// The distance in pixels between the anchor and the tooltip, which is also the room the arrow is
    /// drawn in. Leaving it unset keeps the distance the theme gives it.
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
    /// Delay (in milliseconds) before showing the tooltip.
    /// </summary>
    /// <remarks>
    /// It applies to the pointer only: a tooltip reached with the keyboard or opened by a click is shown
    /// at once, since the user asked for it rather than merely passed over it.
    /// </remarks>
    [Parameter] public int ShowDelay { get; set; } = 0;

    /// <summary>
    /// Determines shows tooltip on click.
    /// </summary>
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
    }

    protected override async Task OnInitializedAsync()
    {
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
        // instead of leaving behind a surface that nothing on the page can dismiss any more.
        if (IsEnabled is false && IsShown)
        {
            CancelPendingDelays();

            await SetIsShown(false);
        }

        if (IsShown)
        {
            _contentRendered = true;
        }

        await base.OnParametersSetAsync();
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

            await OnShow.InvokeAsync();
        }
        else
        {
            await OnHide.InvokeAsync();
        }

        await OnToggle.InvokeAsync(value);

        // The state can be reached from a delay that has outlived the event handler which started it,
        // and so from past the render Blazor does of its own accord when a handler returns.
        await InvokeAsync(StateHasChanged);
    }

    private async Task HandlePointerEnter(PointerEventArgs e)
    {
        if (IsControlledExternally) return;
        if (ShowOnHover is false) return;

        // A touch has no pointer that hovers: the enter and the leave arrive back to back around the
        // tap, so the tooltip is shown at once and then hides itself after a while instead.
        if (IsTouch(e))
        {
            await ShowAfterDelay(0);

            if (TouchHideDelay > 0)
            {
                await HideAfterDelay(TouchHideDelay);
            }

            return;
        }

        await ShowAfterDelay(ShowDelay);
    }

    private async Task HandlePointerLeave(PointerEventArgs e)
    {
        if (IsControlledExternally) return;
        if (ShowOnHover is false) return;

        // The leave that follows a tap would take the tooltip away the instant it was shown; the touch
        // timer the enter above started is what hides that one.
        if (IsTouch(e)) return;

        await HideAfterDelay(HideDelay);
    }

    private async Task HandleFocusIn()
    {
        if (IsControlledExternally) return;
        if (ShowOnFocus is false) return;

        // Reaching a control with the keyboard is asking for the tooltip rather than passing over it, so
        // the hover delay - which is there to keep a pointer crossing the page quiet - does not apply.
        await ShowAfterDelay(0);
    }

    private async Task HandleFocusOut()
    {
        if (IsControlledExternally) return;
        if (ShowOnFocus is false) return;

        await HideAfterDelay(HideDelay);
    }

    private async Task HandlePointerUp(PointerEventArgs e)
    {
        if (IsControlledExternally) return;
        if (ShowOnClick is false) return;

        // Only the primary button toggles: the secondary one belongs to the context menu of the page.
        if (e.Button != 0) return;

        if (IsShown)
        {
            await HideAfterDelay(0);
        }
        else
        {
            await ShowAfterDelay(0);
        }
    }

    private async Task HandleKeyDown(KeyboardEventArgs e)
    {
        if (IsControlledExternally) return;
        if (NoDismissOnEscape) return;
        if (IsShown is false) return;
        if (e.Key is not "Escape") return;

        await HideAfterDelay(0);
    }

    private static bool IsTouch(PointerEventArgs e) => e.PointerType is "touch" or "pen";

    protected override ValueTask DisposeAsync(bool disposing)
    {
        if (IsDisposed || disposing is false) return ValueTask.CompletedTask;

        CancelPendingDelays();

        return base.DisposeAsync(disposing);
    }

    private string GetTooltipClasses()
    {
        var visibility = IsShown ? "bit-ttp-vis " : string.Empty;

        var position = Position switch
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
