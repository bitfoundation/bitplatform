using System.Diagnostics.CodeAnalysis;

namespace Bit.BlazorUI;

/// <summary>
/// The PullToRefresh component is used to add the pull down to refresh feature to a page or a specific element.
/// </summary>
public partial class BitPullToRefresh : BitComponentBase
{
    /// <summary>
    /// The diameter, in pixels, the indicator's disc is drawn at once the pull has reached the trigger, and the
    /// glyph inside it. Everything below the trigger is drawn as the same fraction of these, so the indicator
    /// grows into place along with the pull rather than appearing at full size.
    /// </summary>
    private const decimal SpinnerWrapperSize = 35;
    private const decimal SpinnerSize = 24;

    private decimal _diff;
    private bool _completed;
    private bool _refreshing;
    private int _lastTrigger;
    private int _lastMargin;
    private int _lastThreshold;
    private int _lastMaxPull;
    private decimal _lastFactor;
    private bool _lastIsEnabled;
    private string? _lastScrollerSelector;
    private ElementReference? _lastScrollerElement;
    private ElementReference _loadingRef = default!;
    private DotNetObjectReference<BitPullToRefresh>? _dotnetObj;



    /// <summary>
    /// The anchor element that the pull to refresh component adheres to (alias of ChildContent).
    /// </summary>
    [Parameter] public RenderFragment? Anchor { get; set; }

    /// <summary>
    /// The anchor element that the pull to refresh component adheres to.
    /// </summary>
    [Parameter] public RenderFragment? ChildContent { get; set; }

    /// <summary>
    /// Custom CSS classes for different parts of the BitPullToRefresh.
    /// </summary>
    [Parameter] public BitPullToRefreshClassStyles? Classes { get; set; }

    /// <summary>
    /// The general color of the pull indicator.
    /// </summary>
    /// <remarks>
    /// It colors the glyph inside the indicator's disc, which is what the pull, the refresh and the complete
    /// states all draw. Leave it unset to take the theme's primary foreground color, or to let
    /// <see cref="CustomColor"/> apply - a theme role always wins over a literal color.
    /// </remarks>
    [Parameter, ResetStyleBuilder] public BitColor? Color { get; set; }

    /// <summary>
    /// The custom template to replace the default checkmark svg shown while the complete state is visible.
    /// </summary>
    [Parameter] public RenderFragment? Complete { get; set; }

    /// <summary>
    /// The duration in milliseconds to keep the complete indicator visible after a successful refresh before snapping back (0 disables the complete state).
    /// </summary>
    [Parameter] public int CompleteDelay { get; set; }

    /// <summary>
    /// The text that gets announced to screen readers while the complete state is visible after a successful refresh.
    /// </summary>
    [Parameter] public string CompleteLabel { get; set; } = "Refresh complete";

    /// <summary>
    /// The custom css color of the pull indicator.
    /// </summary>
    /// <remarks>
    /// Any valid CSS color works here, <c>currentColor</c> included, which is what lets the indicator take the
    /// color of the content it sits over. It only applies while <see cref="Color"/> is left unset.
    /// </remarks>
    [Parameter, ResetStyleBuilder] public string? CustomColor { get; set; }

    /// <summary>
    /// The factor to balance the pull height out. The pull-down distance gets divided by it, so higher values make the pull feel heavier.
    /// </summary>
    /// <remarks>
    /// Values below 0.1 are treated as 0.1: a factor of zero would divide the travelled distance by nothing.
    /// </remarks>
    [Parameter] public decimal Factor { get; set; } = 1.5m;

    /// <summary>
    /// Gets or sets a value indicating whether the component takes the whole width of its container.
    /// <br />
    /// The default value is <strong>false</strong>.
    /// </summary>
    /// <remarks>
    /// The component shrink-wraps its anchor by default, which is what keeps it out of the way of an anchor
    /// that sizes itself. An anchor that is meant to fill a page or a layout region - the usual case on a
    /// phone - needs the component around it to fill it too, which is what this does.
    /// </remarks>
    [Parameter, ResetClassBuilder] public bool FullWidth { get; set; }

    /// <summary>
    /// The custom loading template to replace the default loading svg.
    /// </summary>
    /// <remarks>
    /// It is what the indicator shows while the pull is under way and while the refresh is running, so it
    /// covers every state that <see cref="Release"/> and <see cref="Complete"/> do not take over.
    /// </remarks>
    [Parameter] public RenderFragment? Loading { get; set; }

    /// <summary>
    /// The value in pixel to add to the top of pull element as a margin for the pull height.
    /// </summary>
    [Parameter] public int Margin { get; set; } = 30;

    /// <summary>
    /// The furthest the pull can travel, in pixels, past which it stops following the finger.
    /// <br />
    /// The default value is <strong>0</strong>, which stops the pull at <see cref="Trigger"/>.
    /// </summary>
    /// <remarks>
    /// A pull that stops dead the moment it has done its job feels like the gesture broke, so letting it
    /// carry on a little past the trigger - a third of the trigger again is a good starting point - is what
    /// makes the release feel deliberate rather than accidental. The indicator holds its full size over that
    /// stretch; only the strip keeps growing.
    /// <br />
    /// It is measured on the same damped scale as <see cref="Trigger"/> - the finger travels
    /// <see cref="Factor"/> times as far - and a value at or below the trigger leaves the pull stopping there,
    /// which is what it does by default.
    /// </remarks>
    [Parameter] public int MaxPull { get; set; }

    /// <summary>
    /// The callback for when the trigger condition of the pull-down happens.
    /// </summary>
    [Parameter] public EventCallback OnRefresh { get; set; }

    /// <summary>
    /// The callback for the starting of the pull-down.
    /// </summary>
    [Parameter] public EventCallback<BitPullToRefreshPullStartArgs> OnPullStart { get; set; }

    /// <summary>
    /// The callback for when the pull-down is in progress.
    /// </summary>
    /// <remarks>
    /// It reports the pull height in pixels, which is capped at <see cref="Trigger"/> - or at
    /// <see cref="MaxPull"/> where the pull is allowed past it. Use <see cref="PullProgress"/> where the
    /// fraction of the way to the trigger is what matters rather than the distance itself.
    /// <br />
    /// The gesture produces far more move events than the browser paints frames, so the reports are coalesced
    /// to at most one per frame and never repeat a whole pixel; a handler still runs often enough that it
    /// should stay cheap.
    /// </remarks>
    [Parameter] public EventCallback<decimal> OnPullMove { get; set; }

    /// <summary>
    /// The callback for the ending of the pull-down.
    /// </summary>
    [Parameter] public EventCallback<decimal> OnPullEnd { get; set; }

    /// <summary>
    /// The callback for when the pull-down gets canceled before release, providing the last pull height.
    /// </summary>
    [Parameter] public EventCallback<decimal> OnPullCancel { get; set; }

    /// <summary>
    /// The text that gets announced to screen readers while the refresh is in progress.
    /// </summary>
    [Parameter] public string RefreshingLabel { get; set; } = "Refreshing";

    /// <summary>
    /// The custom template to replace the default svg while the pull has passed the trigger and releasing starts the refresh.
    /// </summary>
    /// <remarks>
    /// The release state is the moment the gesture becomes a commitment, and showing something different for
    /// it is what tells the user that letting go now will refresh. Without this the state is still there - the
    /// indicator's disc changes color through the SpinnerWrapperCanRelease part - only the glyph inside it
    /// stays the one <see cref="Loading"/> draws.
    /// </remarks>
    [Parameter] public RenderFragment? Release { get; set; }

    /// <summary>
    /// The text that gets announced to screen readers while the pull has passed the trigger and releasing starts the refresh.
    /// </summary>
    /// <remarks>
    /// Set it to an empty string to leave the release state unannounced, which is worth doing where the pull
    /// is a shortcut for a refresh the page also offers as a control.
    /// </remarks>
    [Parameter] public string ReleaseLabel { get; set; } = "Release to refresh";

    /// <summary>
    /// The element that is the scroller in the anchor to control the behavior of the pull to refresh.
    /// </summary>
    [Parameter] public ElementReference? ScrollerElement { get; set; }

    /// <summary>
    /// The CSS selector of the element that is the scroller in the anchor to control the behavior of the pull to refresh.
    /// </summary>
    /// <remarks>
    /// It is looked up inside the anchor first and in the document afterwards, so a scroller that lives outside
    /// the anchor can still be named. Point it at "body" to hang the gesture off the page's own scrolling.
    /// <br />
    /// Left unset, the first element of the anchor is taken as the scroller.
    /// </remarks>
    [Parameter] public string? ScrollerSelector { get; set; }

    /// <summary>
    /// Custom CSS styles for different parts of the BitPullToRefresh.
    /// </summary>
    [Parameter] public BitPullToRefreshClassStyles? Styles { get; set; }

    /// <summary>
    /// The dead-zone distance in pixel that the pull-down must travel before the pull to refresh process starts and the indicator appears.
    /// </summary>
    [Parameter] public int Threshold { get; set; } = 0;

    /// <summary>
    /// The pulling height in pixel that triggers the refresh.
    /// </summary>
    /// <remarks>
    /// It is also the distance the indicator grows to its full size over, so it doubles as the scale of the
    /// whole gesture. Values below 1 are treated as 1.
    /// </remarks>
    [Parameter] public int Trigger { get; set; } = 80;



    [Inject] private IJSRuntime _js { get; set; } = default!;



    /// <summary>
    /// Whether a refresh is currently running - the pull was released past the trigger, or
    /// <see cref="RefreshAsync"/> was called, and the <see cref="OnRefresh"/> callback has not returned yet.
    /// </summary>
    public bool IsRefreshing => _refreshing;

    /// <summary>
    /// How far the current pull has come as a fraction of <see cref="Trigger"/>: 0 while nothing is being
    /// pulled, and 1 once releasing would start a refresh.
    /// </summary>
    /// <remarks>
    /// It reads 1 for the whole of a refresh, since the indicator is held at its triggered height there. A
    /// handler of <see cref="OnPullMove"/> reading this sees the value the move it was given produced.
    /// </remarks>
    public decimal PullProgress => Math.Min(_diff / _Trigger, 1);

    /// <summary>
    /// Starts the refresh process programmatically, showing the loading indicator and invoking the OnRefresh callback.
    /// It has no effect while the component is disabled, a refresh is already in progress or the complete state is visible.
    /// </summary>
    public async Task RefreshAsync()
    {
        if (_refreshing || _completed || IsEnabled is false || IsRendered is false || IsDisposed) return;

        await _js.BitPullToRefreshRefresh(UniqueId);
    }



    [JSInvokable("Refresh")]
    public async Task _Refresh()
    {
        _diff = _Trigger;
        _refreshing = true;
        await InvokeAsync(StateHasChanged);
        try
        {
            await OnRefresh.InvokeAsync();

            if (CompleteDelay > 0)
            {
                _completed = true;
                _refreshing = false;
                await InvokeAsync(StateHasChanged);
                await Task.Delay(CompleteDelay);
            }
        }
        finally
        {
            _diff = 0;
            _completed = false;
            _refreshing = false;
            await InvokeAsync(StateHasChanged);
        }
    }

    [JSInvokable("OnStart")]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(BitPullToRefreshPullStartArgs))]
    public async Task _OnStart(decimal top, decimal left, decimal width)
    {
        await OnPullStart.InvokeAsync(new BitPullToRefreshPullStartArgs(top, left, width));
    }

    [JSInvokable("OnMove")]
    public async Task _OnMove(decimal diff)
    {
        // Only what the indicator is actually drawn from decides whether a re-render is worth it. A move that
        // lands on the same whole pixel and the same release state renders identically, and re-rendering the
        // component means re-rendering the whole anchor with it.
        var changed = Math.Round(diff) != Math.Round(_diff) || CanReleaseAt(diff) != CanReleaseAt(_diff);

        _diff = diff;

        if (changed)
        {
            await InvokeAsync(StateHasChanged);
        }

        await OnPullMove.InvokeAsync(diff);
    }

    [JSInvokable("OnEnd")]
    public async Task _OnEnd(decimal diff)
    {
        // A pull that fell short is dropped; one that made it is settled at the trigger, which is where the
        // refresh about to be asked for holds it. Settling it here rather than leaving it standing is what
        // keeps an overpull - see MaxPull - from being drawn for the round trip in between.
        var settled = diff < _Trigger ? 0 : _Trigger;

        if (_diff != settled)
        {
            _diff = settled;
            await InvokeAsync(StateHasChanged);
        }

        await OnPullEnd.InvokeAsync(diff);
    }

    [JSInvokable("OnCancel")]
    public async Task _OnCancel(decimal diff)
    {
        _diff = 0;
        await InvokeAsync(StateHasChanged);
        await OnPullCancel.InvokeAsync(diff);
    }



    protected override string RootElementClass => "bit-ptr";

    protected override void RegisterCssClasses()
    {
        ClassBuilder.Register(() => Classes?.Root);

        ClassBuilder.Register(() => FullWidth ? "bit-ptr-flw" : string.Empty);
    }

    protected override void RegisterCssStyles()
    {
        // Registered before Styles.Root so that a --bit-ptr-color written there still wins: in an inline style
        // the last declaration of a custom property is the one that takes.
        StyleBuilder.Register(() => Color switch
        {
            BitColor.Primary => "--bit-ptr-color:var(--bit-clr-pri)",
            BitColor.Secondary => "--bit-ptr-color:var(--bit-clr-sec)",
            BitColor.Tertiary => "--bit-ptr-color:var(--bit-clr-ter)",
            BitColor.Info => "--bit-ptr-color:var(--bit-clr-inf)",
            BitColor.Success => "--bit-ptr-color:var(--bit-clr-suc)",
            BitColor.Warning => "--bit-ptr-color:var(--bit-clr-wrn)",
            BitColor.SevereWarning => "--bit-ptr-color:var(--bit-clr-swr)",
            BitColor.Error => "--bit-ptr-color:var(--bit-clr-err)",
            BitColor.PrimaryBackground => "--bit-ptr-color:var(--bit-clr-bg-pri)",
            BitColor.SecondaryBackground => "--bit-ptr-color:var(--bit-clr-bg-sec)",
            BitColor.TertiaryBackground => "--bit-ptr-color:var(--bit-clr-bg-ter)",
            BitColor.PrimaryForeground => "--bit-ptr-color:var(--bit-clr-fg-pri)",
            BitColor.SecondaryForeground => "--bit-ptr-color:var(--bit-clr-fg-sec)",
            BitColor.TertiaryForeground => "--bit-ptr-color:var(--bit-clr-fg-ter)",
            BitColor.PrimaryBorder => "--bit-ptr-color:var(--bit-clr-brd-pri)",
            BitColor.SecondaryBorder => "--bit-ptr-color:var(--bit-clr-brd-sec)",
            BitColor.TertiaryBorder => "--bit-ptr-color:var(--bit-clr-brd-ter)",
            // Color is nullable, so this also covers the unset case, where CustomColor applies.
            _ => CustomColor.HasValue() ? $"--bit-ptr-color:{CustomColor}" : null
        });

        StyleBuilder.Register(() => Styles?.Root);
    }

    protected override async Task OnParametersSetAsync()
    {
        await base.OnParametersSetAsync();

        if (IsRendered is false) return;

        if (_lastTrigger != Trigger || _lastFactor != Factor || _lastMargin != Margin || _lastThreshold != Threshold ||
            _lastMaxPull != MaxPull || _lastIsEnabled != IsEnabled || _lastScrollerSelector != ScrollerSelector ||
            !Nullable.Equals(_lastScrollerElement, ScrollerElement))
        {
            // js drops the pull height of an idle component when it gets disabled, so the managed
            // side does the same, otherwise the indicator keeps rendering at the height it had.
            if (IsEnabled is false && _refreshing is false && _completed is false)
            {
                _diff = 0;
            }

            CacheJsParameters();
            await _js.BitPullToRefreshUpdate(UniqueId, ScrollerElement, ScrollerSelector, _Trigger, _Factor, _Margin, _Threshold, _MaxPull, IsEnabled);
        }
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            CacheJsParameters();

            _dotnetObj = DotNetObjectReference.Create(this);

            try
            {
                await _js.BitPullToRefreshSetup(UniqueId, RootElement, _loadingRef, ScrollerElement, ScrollerSelector, _Trigger, _Factor, _Margin, _Threshold, _MaxPull, IsEnabled, _dotnetObj);
            }
            catch
            {
                // The setup didn't complete, so JS never registered this id and never took ownership of the
                // reference - and the JS dispose silently no-ops for an unknown id, so DisposeAsync can't
                // release it either. Release it here, then rethrow so the original failure still surfaces.
                _dotnetObj.Dispose();
                _dotnetObj = null;
                throw;
            }
        }

        await base.OnAfterRenderAsync(firstRender);
    }

    private void CacheJsParameters()
    {
        _lastTrigger = Trigger;
        _lastFactor = Factor;
        _lastMargin = Margin;
        _lastThreshold = Threshold;
        _lastMaxPull = MaxPull;
        _lastIsEnabled = IsEnabled;
        _lastScrollerSelector = ScrollerSelector;
        _lastScrollerElement = ScrollerElement;
    }

    // The four numbers that drive the gesture, held inside the range it can actually be drawn from. The same
    // clamps are applied in js, so the height it draws and the size the indicator renders at never disagree.
    private int _Trigger => Trigger < 1 ? 1 : Trigger;
    private decimal _Factor => Factor < 0.1m ? 0.1m : Factor;
    private int _Margin => Margin < 0 ? 0 : Margin;
    private int _Threshold => Threshold < 0 ? 0 : Threshold;
    private int _MaxPull => MaxPull < 0 ? 0 : MaxPull;

    // The pull height the indicator is drawn from, which stops at the trigger even where the pull itself is
    // allowed past it: over that stretch the indicator holds its full size and the strip alone keeps growing.
    private decimal _VisualDiff => _diff > _Trigger ? _Trigger : _diff;

    private bool CanRelease => CanReleaseAt(_diff);

    private bool CanReleaseAt(decimal diff) => _refreshing is false && _completed is false && diff > 0 && diff >= _Trigger;

    // The live region's whole content, which is what a screen reader announces every time it changes. Only one
    // state speaks at a time, and the idle state says nothing so that the region falls silent between pulls.
    private string GetScreenReaderText()
    {
        if (_refreshing) return RefreshingLabel;
        if (_completed) return CompleteLabel;
        if (CanRelease) return ReleaseLabel;

        return string.Empty;
    }

    private string? GetSpinnerWrapperCssClasses()
    {
        List<string> classes = ["bit-ptr-spw"];

        if (Classes?.SpinnerWrapper?.HasValue() ?? false)
        {
            classes.Add(Classes.SpinnerWrapper.Trim());
        }

        if (CanRelease)
        {
            classes.Add("bit-ptr-crl");

            if (Classes?.SpinnerWrapperCanRelease?.HasValue() ?? false)
            {
                classes.Add(Classes.SpinnerWrapperCanRelease.Trim());
            }
        }

        if (_refreshing)
        {
            classes.Add("bit-ptr-swr");

            if (Classes?.SpinnerWrapperRefreshing?.HasValue() ?? false)
            {
                classes.Add(Classes.SpinnerWrapperRefreshing.Trim());
            }
        }

        if (_completed)
        {
            classes.Add("bit-ptr-cmp");

            if (Classes?.SpinnerWrapperComplete?.HasValue() ?? false)
            {
                classes.Add(Classes.SpinnerWrapperComplete.Trim());
            }
        }

        return string.Join(' ', classes);
    }

    private string? GetSpinnerWrapperCssStyles()
    {
        var size = SpinnerWrapperSize * _VisualDiff / _Trigger;

        List<string> styles = [FormattableString.Invariant($"margin-top:{(_refreshing || _completed ? 0 : _diff / 2)}px;width:{size}px;height:{size}px")];

        if (Styles?.SpinnerWrapper?.HasValue() ?? false)
        {
            styles.Add(Styles.SpinnerWrapper.Trim(';'));
        }

        if (CanRelease && (Styles?.SpinnerWrapperCanRelease?.HasValue() ?? false))
        {
            styles.Add(Styles.SpinnerWrapperCanRelease.Trim(';'));
        }

        if (_refreshing && (Styles?.SpinnerWrapperRefreshing?.HasValue() ?? false))
        {
            styles.Add(Styles.SpinnerWrapperRefreshing.Trim(';'));
        }

        if (_completed && (Styles?.SpinnerWrapperComplete?.HasValue() ?? false))
        {
            styles.Add(Styles.SpinnerWrapperComplete.Trim(';'));
        }

        return string.Join(';', styles);
    }

    private string? GetSpinnerCssClasses()
    {
        List<string> classes = ["bit-ptr-spn"];

        if (Classes?.Spinner?.HasValue() ?? false)
        {
            classes.Add(Classes.Spinner.Trim());
        }

        if (CanRelease && (Classes?.SpinnerCanRelease?.HasValue() ?? false))
        {
            classes.Add(Classes.SpinnerCanRelease.Trim());
        }

        if (_refreshing)
        {
            classes.Add("bit-ptr-spin");

            if (Classes?.SpinnerRefreshing?.HasValue() ?? false)
            {
                classes.Add(Classes.SpinnerRefreshing.Trim());
            }
        }

        if (_completed && (Classes?.SpinnerComplete?.HasValue() ?? false))
        {
            classes.Add(Classes.SpinnerComplete.Trim());
        }

        return string.Join(' ', classes);
    }

    private string? GetSpinnerCssStyles()
    {
        var trigger = _Trigger;
        var diff = _VisualDiff;
        var size = SpinnerSize * diff / trigger;

        List<string> styles = [FormattableString.Invariant($"transform:rotate({(diff - trigger) * 2}deg);width:{size}px;height:{size}px")];

        if (Styles?.Spinner?.HasValue() ?? false)
        {
            styles.Add(Styles.Spinner.Trim(';'));
        }

        if (CanRelease && (Styles?.SpinnerCanRelease?.HasValue() ?? false))
        {
            styles.Add(Styles.SpinnerCanRelease.Trim(';'));
        }

        if (_refreshing && (Styles?.SpinnerRefreshing?.HasValue() ?? false))
        {
            styles.Add(Styles.SpinnerRefreshing.Trim(';'));
        }

        if (_completed && (Styles?.SpinnerComplete?.HasValue() ?? false))
        {
            styles.Add(Styles.SpinnerComplete.Trim(';'));
        }

        return string.Join(';', styles);
    }



    protected override async ValueTask DisposeAsync(bool disposing)
    {
        if (IsDisposed || disposing is false) return;

        // Ownership of _dotnetObj is single-sourced to the JS dispose path: BitPullToRefresh.ts disposes
        // the .NET reference in its dispose(). Disposing it here too would double-dispose the same object.
        try
        {
            await _js.BitPullToRefreshDispose(UniqueId);
        }
        catch (JSDisconnectedException)
        {
            // The circuit/browser is gone, so the JS dispose that normally owns _dotnetObj can't run.
            // Release the managed reference here so it doesn't leak.
            _dotnetObj?.Dispose();
            _dotnetObj = null;
        }
        catch
        {
            // Any other failure means the JS dispose didn't complete its ownership handoff, so release the
            // managed reference here to avoid leaking it, then rethrow so the original error still surfaces.
            _dotnetObj?.Dispose();
            _dotnetObj = null;
            throw;
        }
        finally
        {
            // Base cleanup must always run, even when the JS dispose failed and rethrew above.
            await base.DisposeAsync(disposing);
        }
    }
}
