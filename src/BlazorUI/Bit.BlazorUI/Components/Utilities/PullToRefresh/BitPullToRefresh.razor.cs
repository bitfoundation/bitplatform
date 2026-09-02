using System.Diagnostics.CodeAnalysis;

namespace Bit.BlazorUI;

/// <summary>
/// The PullToRefresh component is used to add the pull down to refresh feature to a page or a specific element.
/// </summary>
public partial class BitPullToRefresh : BitComponentBase
{
    private decimal _diff;
    private bool _completed;
    private bool _refreshing;
    private int _lastTrigger;
    private int _lastMargin;
    private int _lastThreshold;
    private decimal _lastFactor;
    private bool _lastIsEnabled;
    private ElementReference _loadingRef = default!;



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
    /// The factor to balance the pull height out. The pull-down distance gets divided by it, so higher values make the pull feel heavier.
    /// </summary>
    [Parameter] public decimal Factor { get; set; } = 1.5m;

    /// <summary>
    /// The custom loading template to replace the default loading svg.
    /// </summary>
    [Parameter] public RenderFragment? Loading { get; set; }

    /// <summary>
    /// The value in pixel to add to the top of pull element as a margin for the pull height.
    /// </summary>
    [Parameter] public int Margin { get; set; } = 30;

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
    /// The element that is the scroller in the anchor to control the behavior of the pull to refresh.
    /// </summary>
    [Parameter] public ElementReference? ScrollerElement { get; set; }

    /// <summary>
    /// The CSS selector of the element that is the scroller in the anchor to control the behavior of the pull to refresh.
    /// </summary>
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
    [Parameter] public int Trigger { get; set; } = 80;



    [Inject] private IJSRuntime _js { get; set; } = default!;



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
        _diff = Trigger;
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
        _diff = diff;
        await InvokeAsync(StateHasChanged);
        await OnPullMove.InvokeAsync(diff);
    }

    [JSInvokable("OnEnd")]
    public async Task _OnEnd(decimal diff)
    {
        if (diff < Trigger)
        {
            _diff = 0;
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
    }

    protected override void RegisterCssStyles()
    {
        StyleBuilder.Register(() => Styles?.Root);
    }

    protected override async Task OnParametersSetAsync()
    {
        await base.OnParametersSetAsync();

        if (IsRendered is false) return;

        if (_lastTrigger != Trigger || _lastFactor != Factor || _lastMargin != Margin || _lastThreshold != Threshold || _lastIsEnabled != IsEnabled)
        {
            // js drops the pull height of an idle component when it gets disabled, so the managed
            // side does the same, otherwise the indicator keeps rendering at the height it had.
            if (IsEnabled is false && _refreshing is false && _completed is false)
            {
                _diff = 0;
            }

            CacheJsParameters();
            await _js.BitPullToRefreshUpdate(UniqueId, Trigger, Factor, Margin, Threshold, IsEnabled);
        }
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            CacheJsParameters();
            var dotnetObj = DotNetObjectReference.Create(this);
            await _js.BitPullToRefreshSetup(UniqueId, RootElement, _loadingRef, ScrollerElement, ScrollerSelector, Trigger, Factor, Margin, Threshold, IsEnabled, dotnetObj);
        }

        await base.OnAfterRenderAsync(firstRender);
    }

    private void CacheJsParameters()
    {
        _lastTrigger = Trigger;
        _lastFactor = Factor;
        _lastMargin = Margin;
        _lastThreshold = Threshold;
        _lastIsEnabled = IsEnabled;
    }

    private bool CanRelease => _refreshing is false && _completed is false && _diff > 0 && _diff >= Trigger;

    private string? GetSpinnerWrapperCssClasses()
    {
        List<string> classes = ["bit-ptr-spw"];

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

        if (Classes?.SpinnerWrapper?.HasValue() ?? false)
        {
            classes.Add(Classes.SpinnerWrapper.Trim());
        }

        return string.Join(' ', classes).Trim();
    }

    private string? GetSpinnerWrapperCssStyles()
    {
        List<string> styles = [];
        var trigger = Trigger < 1 ? 1 : Trigger;
        decimal size = 35 * _diff / trigger;

        styles.Add(FormattableString.Invariant($"margin-top:{(_refreshing || _completed ? 0 : _diff / 2)}px;width:{size}px;height:{size}px"));

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

        return string.Join(' ', classes).Trim();
    }

    private string? GetSpinnerCssStyles()
    {
        List<string> styles = [];
        var trigger = Trigger < 1 ? 1 : Trigger;
        decimal size = 24 * _diff / trigger;

        styles.Add(FormattableString.Invariant($"transform:rotate({(_diff - trigger) * 2}deg);width:{size}px;height:{size}px"));

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

        try
        {
            await _js.BitPullToRefreshDispose(UniqueId);
        }
        catch (JSDisconnectedException) { } // we can ignore this exception here

        await base.DisposeAsync(disposing);
    }
}
