using System.Diagnostics.CodeAnalysis;

namespace Bit.BlazorUI;

/// <summary>
/// A SwipeTrap is a component that traps swipe actions and triggers corresponding events.
/// </summary>
public partial class BitSwipeTrap : BitComponentBase
{
    private decimal _appliedTrigger;
    private decimal _appliedTriggerVelocity;
    private decimal _appliedThreshold;
    private int _appliedThrottle;
    private BitSwipeOrientation _appliedOrientationLock;
    private bool _appliedTouchOnly;
    private string? _appliedSkipSelector;



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
    /// The event callback for when the swipe action triggers based on the Trigger or TriggerVelocity constraints.
    /// </summary>
    [Parameter] public EventCallback<BitSwipeTrapTriggerArgs> OnTrigger { get; set; }

    /// <summary>
    /// Specifies the orientation lock in which the swipe trap allows to trap the swipe actions.
    /// The locked axis is trapped and the other axis keeps its default browser behavior (via a matching touch-action).
    /// </summary>
    [Parameter, ResetClassBuilder]
    public BitSwipeOrientation? OrientationLock { get; set; }

    /// <summary>
    /// A CSS selector of descendant elements on which starting a swipe is ignored (e.g. inputs or nested interactive elements).
    /// </summary>
    [Parameter] public string? SkipSelector { get; set; }

    /// <summary>
    /// The distance in pixels a gesture must cover before the swipe trap takes it over and stops the
    /// default behavior. It is also what resolves the axis a diagonal gesture is moving along (default is 0).
    /// </summary>
    [Parameter] public decimal? Threshold { get; set; }

    /// <summary>
    /// The throttle time in milliseconds to apply a delay between periodic calls to raise the OnMove event (default is 0, meaning no throttling).
    /// </summary>
    [Parameter] public int? Throttle { get; set; }

    /// <summary>
    /// Ignores mouse swipes, trapping only touch (and pen) gestures.
    /// </summary>
    [Parameter] public bool TouchOnly { get; set; }

    /// <summary>
    /// The swiping point to trigger and call the OnTrigger event: either a fraction of the element's width/height
    /// (values less than 1) or an absolute value in pixels (default is 0.25m).
    /// </summary>
    [Parameter] public decimal? Trigger { get; set; }

    /// <summary>
    /// The swiping velocity in pixels per millisecond that triggers and calls the OnTrigger event on release (a flick),
    /// even if the swiping distance has not reached the Trigger point (default is 0, meaning disabled).
    /// </summary>
    [Parameter] public decimal? TriggerVelocity { get; set; }



    [JSInvokable("OnStart")]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(BitSwipeTrapEventArgs))]
    public async Task _OnStart(decimal startX, decimal startY, string? pointerType = null)
    {
        await OnStart.InvokeAsync(new(startX, startY, 0, 0, 0, 0, pointerType));
    }

    [JSInvokable("OnMove")]
    public async Task _OnMove(decimal startX, decimal startY, decimal diffX, decimal diffY, decimal velocityX, decimal velocityY, string? pointerType = null, decimal duration = 0)
    {
        await OnMove.InvokeAsync(new(startX, startY, diffX, diffY, velocityX, velocityY, pointerType, false, duration));
    }

    [JSInvokable("OnEnd")]
    public async Task _OnEnd(decimal startX, decimal startY, decimal diffX, decimal diffY, decimal velocityX, decimal velocityY, string? pointerType = null, bool isCanceled = false, decimal duration = 0)
    {
        await OnEnd.InvokeAsync(new(startX, startY, diffX, diffY, velocityX, velocityY, pointerType, isCanceled, duration));
    }

    [JSInvokable("OnTrigger")]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(BitSwipeTrapTriggerArgs))]
    public async Task _OnTrigger(decimal diffX, decimal diffY, decimal velocityX, decimal velocityY, string? pointerType = null, decimal duration = 0)
    {
        // A dead heat goes to the horizontal axis, the same way the JS side resolves the axis a gesture
        // moves along: a perfect diagonal must not be reported as one axis here and locked to the other there.
        var direction = Math.Abs(diffX) >= Math.Abs(diffY)
            ? diffX > 0 ? BitSwipeDirection.Right : BitSwipeDirection.Left
            : diffY > 0 ? BitSwipeDirection.Bottom : BitSwipeDirection.Top;

        await OnTrigger.InvokeAsync(new(direction, diffX, diffY, velocityX, velocityY, pointerType, duration));
    }



    protected override string RootElementClass => "bit-stp";

    protected override void RegisterCssClasses()
    {
        // The orientation lock also declares itself to the browser as a touch-action: without it, a
        // scroll the browser has already started stops sending cancelable events, and trapping the
        // locked axis becomes a race the trap can lose.
        ClassBuilder.Register(() => OrientationLock switch
        {
            BitSwipeOrientation.Horizontal => "bit-stp-hrz",
            BitSwipeOrientation.Vertical => "bit-stp-vrt",
            BitSwipeOrientation.Auto => "bit-stp-lck",
            _ => string.Empty
        });
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        var trigger = Trigger ?? 0.25m;
        var triggerVelocity = TriggerVelocity ?? 0;
        var threshold = Threshold ?? 0;
        var throttle = Throttle ?? 0;
        var orientationLock = OrientationLock ?? BitSwipeOrientation.None;
        var touchOnly = TouchOnly;
        var skipSelector = SkipSelector;

        if (firstRender ||
            _appliedTrigger != trigger ||
            _appliedTriggerVelocity != triggerVelocity ||
            _appliedThreshold != threshold ||
            _appliedThrottle != throttle ||
            _appliedOrientationLock != orientationLock ||
            _appliedTouchOnly != touchOnly ||
            _appliedSkipSelector != skipSelector)
        {
            try
            {
                if (firstRender is false)
                {
                    await _js.BitSwipeTrapDispose(UniqueId);
                }

                // The JS side disposes the .NET reference it is handed when the trap is disposed or
                // re-setup, so each setup gets a fresh one instead of a field kept for the component's life.
                // Until the setup call has actually handed it over, disposing it is still this side's job.
                var dotnetObj = DotNetObjectReference.Create(this);
                try
                {
                    await _js.BitSwipeTrapSetup(
                        UniqueId,
                        RootElement,
                        trigger,
                        triggerVelocity,
                        threshold,
                        throttle,
                        orientationLock,
                        touchOnly,
                        skipSelector,
                        dotnetObj);
                }
                catch
                {
                    dotnetObj.Dispose();
                    throw;
                }

                // What is remembered is what the trap was actually set up with, so a setup that failed
                // leaves the previous values in place and the next render tries again.
                _appliedTrigger = trigger;
                _appliedTriggerVelocity = triggerVelocity;
                _appliedThreshold = threshold;
                _appliedThrottle = throttle;
                _appliedOrientationLock = orientationLock;
                _appliedTouchOnly = touchOnly;
                _appliedSkipSelector = skipSelector;
            }
            catch (JSDisconnectedException) { } // we can ignore this exception here
        }

        await base.OnAfterRenderAsync(firstRender);
    }



    protected override async ValueTask DisposeAsync(bool disposing)
    {
        if (IsDisposed || disposing is false) return;

        try
        {
            await _js.BitSwipeTrapDispose(UniqueId);
        }
        catch (JSDisconnectedException) { } // we can ignore this exception here

        await base.DisposeAsync(disposing);
    }
}
