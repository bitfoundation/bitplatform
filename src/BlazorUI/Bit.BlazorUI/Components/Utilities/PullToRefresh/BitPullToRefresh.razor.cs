using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace Bit.BlazorUI;

public partial class BitPullToRefresh : BitComponentBase, IAsyncDisposable
{
    private decimal _diff;
    private bool _disposed;
    private bool _refreshing;
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
    /// The factor to balance the pull height out.
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
    /// The callback for when the threshold of the pull-down happens.
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
    /// The threshold in pixel for pulling height that starts the pull to refresh process.
    /// </summary>
    [Parameter] public int Threshold { get; set; } = 0;

    /// <summary>
    /// The pulling height in pixel that triggers the refresh.
    /// </summary>
    [Parameter] public int Trigger { get; set; } = 80;



    [Inject] private IJSRuntime _js { get; set; } = default!;



    [JSInvokable("Refresh")]
    public async Task _Refresh()
    {
        _refreshing = true;
        await InvokeAsync(StateHasChanged);
        await OnRefresh.InvokeAsync();
        _diff = 0;
        _refreshing = false;
        await InvokeAsync(StateHasChanged);
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
        await OnPullEnd.InvokeAsync(diff);
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

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            var dotnetObj = DotNetObjectReference.Create(this);
            await _js.BitPullToRefreshSetup(UniqueId, RootElement, _loadingRef, ScrollerElement, ScrollerSelector, Trigger, Factor, Margin, Threshold, dotnetObj);
        }

        await base.OnAfterRenderAsync(firstRender);
    }

    private string? GetSpinnerWrapperCssClasses()
    {
        var classes = new StringBuilder("bit-ptr-swr");

        if (Classes?.SpinnerWrapper?.HasValue() ?? false)
        {
            classes.Append(' ').Append(Classes.SpinnerWrapper.Trim());
        }

        if (_refreshing && (Classes?.SpinnerWrapperRefreshing?.HasValue() ?? false))
        {
            classes.Append(' ').Append(Classes.SpinnerWrapperRefreshing.Trim());
        }

        return classes.ToString();
    }

    private string? GetSpinnerWrapperCssStyles()
    {
        var wrpSize = (35 * _diff) / Trigger;
        var styles = new StringBuilder();

        if (_refreshing)
        {
            styles.Append("transition:margin-top 100ms linear;background-color:var(--bit-clr-bg-ter);");
        }

        styles.Append($"margin-top:{(_refreshing ? 0 : _diff / 2)}px;width:{wrpSize}px;height:{wrpSize}px;");

        if (Styles?.SpinnerWrapper?.HasValue() ?? false)
        {
            styles.Append(Styles.SpinnerWrapper.Trim(';')).Append(';');
        }

        if (_refreshing && (Styles?.SpinnerWrapperRefreshing?.HasValue() ?? false))
        {
            styles.Append(Styles.SpinnerWrapperRefreshing.Trim(';')).Append(';');
        }

        return styles.ToString();
    }

    private string? GetSpinnerCssClasses()
    {
        var classes = new StringBuilder("bit-ptr-spn");

        if (Classes?.Spinner?.HasValue() ?? false)
        {
            classes.Append(' ').Append(Classes.Spinner.Trim());
        }

        if (_refreshing)
        {
            classes.Append(" bit-ptr-spin");

            if (Classes?.SpinnerRefreshing?.HasValue() ?? false)
            {
                classes.Append(' ').Append(Classes.SpinnerRefreshing.Trim());
            }
        }

        return classes.ToString();
    }

    private string? GetSpinnerCssStyles()
    {
        var svgSize = (24 * _diff) / Trigger;
        var styles = new StringBuilder();

        styles.Append($"transform:rotate({(_diff - Trigger) * 2}deg);width:{svgSize}px;height:{svgSize}px;");

        if (Styles?.Spinner?.HasValue() ?? false)
        {
            styles.Append(Styles.Spinner.Trim(';')).Append(';');
        }

        if (_refreshing && (Styles?.SpinnerRefreshing?.HasValue() ?? false))
        {
            styles.Append(Styles.SpinnerRefreshing.Trim(';')).Append(';');
        }

        return styles.ToString();
    }

    public async ValueTask DisposeAsync()
    {
        await DisposeAsync(true);
        GC.SuppressFinalize(this);
    }

    protected virtual async ValueTask DisposeAsync(bool disposing)
    {
        if (_disposed || disposing is false) return;

        try
        {
            await _js.BitPullToRefreshDispose(UniqueId);
        }
        catch (JSDisconnectedException) { } // we can ignore this exception here

        _disposed = true;
    }
}
