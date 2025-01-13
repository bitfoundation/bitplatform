using System.Diagnostics.CodeAnalysis;

namespace Bit.BlazorUI;

/// <summary>
/// A SwipeTrap is a component that traps swipe actions and triggers corresponding events.
/// </summary>
public partial class BitSwipeTrap : BitComponentBase, IAsyncDisposable
{
    private bool _disposed;



    [Inject] private IJSRuntime _js { get; set; } = default!;



    /// <summary>
    /// The content of the swipe trap.
    /// </summary>
    [Parameter] public RenderFragment? ChildContent { get; set; }

    /// <summary>
    /// The event callback for when the swipe action starts on the container of the swipe trap.
    /// </summary>
    [Parameter] public EventCallback<BitSwipeTrapEventArgs> OnStart { get; set; }

    /// <summary>
    /// The event callback for when the swipe action moves on the container of the swipe trap.
    /// </summary>
    [Parameter] public EventCallback<BitSwipeTrapEventArgs> OnMove { get; set; }

    /// <summary>
    /// The event callback for when the swipe action ends on the container of the swipe trap.
    /// </summary>
    [Parameter] public EventCallback<BitSwipeTrapEventArgs> OnEnd { get; set; }

    /// <summary>
    /// The event callback for when the swipe action triggers based on the Trigger constraint.
    /// </summary>
    [Parameter] public EventCallback<BitSwipeTrapTriggerArgs> OnTrigger { get; set; }

    /// <summary>
    /// Specifies the orientation lock in which the swipe trap allows to trap the swipe actions.
    /// </summary>
    [Parameter] public BitSwipeOrientation? OrientationLock { get; set; }

    /// <summary>
    /// The threshold in pixel for swiping distance that starts the swipe process process which stops the default behavior.
    /// </summary>
    [Parameter] public decimal? Threshold { get; set; }

    /// <summary>
    /// The throttle time in milliseconds to apply a delay between periodic calls to raise the events (default is 10).
    /// </summary>
    [Parameter] public int? Throttle { get; set; }

    /// <summary>
    /// The swiping point (fraction of element's width or an absolute value) to trigger and call the OnTrigger event (default is 0.25m).
    /// </summary>
    [Parameter] public decimal? Trigger { get; set; }



    [JSInvokable("OnStart")]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(BitSwipeTrapEventArgs))]
    public async Task _OnStart(decimal startX, decimal startY)
    {
        await OnStart.InvokeAsync(new(startX, startY, 0, 0));
    }

    [JSInvokable("OnMove")]
    public async Task _OnMove(decimal startX, decimal startY, decimal diffX, decimal diffY)
    {
        await OnMove.InvokeAsync(new(startX, startY, diffX, diffY));
    }

    [JSInvokable("OnEnd")]
    public async Task _OnEnd(decimal startX, decimal startY, decimal diffX, decimal diffY)
    {
        await OnEnd.InvokeAsync(new(startX, startY, diffX, diffY));
    }

    [JSInvokable("OnTrigger")]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(BitSwipeTrapTriggerArgs))]
    public async Task _OnTrigger(decimal diffX, decimal diffY)
    {
        var direction = Math.Abs(diffX) > Math.Abs(diffY)
            ? diffX > 0 ? BitSwipeDirection.Right : BitSwipeDirection.Left
            : diffY > 0 ? BitSwipeDirection.Bottom : BitSwipeDirection.Top;

        await OnTrigger.InvokeAsync(new(direction, diffX, diffY));
    }



    protected override string RootElementClass => "bit-stp";

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            var dotnetObj = DotNetObjectReference.Create(this);
            await _js.BitSwipeTrapSetup(
                UniqueId, 
                RootElement, 
                Trigger ?? 0.25m, 
                Threshold ?? 0, 
                Throttle ?? 0, 
                OrientationLock ?? BitSwipeOrientation.None, 
                dotnetObj);
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
            await _js.BitSwipeTrapDispose(UniqueId);
        }
        catch (JSDisconnectedException) { } // we can ignore this exception here

        _disposed = true;
    }
}
