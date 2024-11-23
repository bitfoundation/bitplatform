using System.Diagnostics.CodeAnalysis;

namespace Bit.BlazorUI;

public partial class BitPullToRefresh : BitComponentBase, IAsyncDisposable
{
    private bool _disposed;



    /// <summary>
    /// The element reference of the anchor element that the pull to refresh adheres to.
    /// </summary>
    [Parameter] public ElementReference? AnchorElement { get; set; }

    /// <summary>
    /// The CSS selector of the anchor element that the pull to refresh adheres to.
    /// </summary>
    [Parameter] public string? AnchorSelector { get; set; }

    /// <summary>
    /// The content of the pull to refresh element.
    /// </summary>
    [Parameter] public RenderFragment? ChildContent { get; set; }

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
    /// The pulling height that triggers the refresh.
    /// </summary>
    [Parameter] public int? Threshold { get; set; }



    [Inject] private IJSRuntime _js { get; set; } = default!;



    [JSInvokable("Refresh")]
    public async Task _Refresh()
    {
        await OnRefresh.InvokeAsync();
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
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            var dotnetObj = DotNetObjectReference.Create(this);
            await _js.BitPullToRefreshSetup(UniqueId, RootElement, AnchorElement, AnchorSelector, Threshold, dotnetObj);
        }

        await base.OnAfterRenderAsync(firstRender);
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
