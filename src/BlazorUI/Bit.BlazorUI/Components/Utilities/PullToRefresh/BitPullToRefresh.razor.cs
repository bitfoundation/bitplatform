using System.Diagnostics.CodeAnalysis;

namespace Bit.BlazorUI;

public partial class BitPullToRefresh : BitComponentBase, IAsyncDisposable
{
    private bool _disposed;



    /// <summary>
    /// The CSS selector of the anchor element that the pull to refresh adheres to.
    /// </summary>
    [Parameter] public string Anchor { get; set; } = "body";

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
    [Parameter] public int Threshold { get; set; } = 80;



    [Inject] private IJSRuntime _js { get; set; } = default!;


    [JSInvokable("Refresh")]
    public async Task Refresh()
    {
        await OnRefresh.InvokeAsync();
    }

    [JSInvokable("TouchStart")]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(BitPullToRefreshPullStartArgs))]
    public async Task TouchStart(decimal top, decimal left, decimal width)
    {
        await OnPullStart.InvokeAsync(new BitPullToRefreshPullStartArgs(top, left, width));
    }

    [JSInvokable("TouchMove")]
    public async Task TouchMove(decimal diff)
    {
        await OnPullMove.InvokeAsync(diff);
    }

    [JSInvokable("TouchEnd")]
    public async Task TouchEnd(decimal diff)
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
            await _js.BitPullToRefreshSetup(UniqueId, RootElement, Anchor, Threshold, dotnetObj);
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
