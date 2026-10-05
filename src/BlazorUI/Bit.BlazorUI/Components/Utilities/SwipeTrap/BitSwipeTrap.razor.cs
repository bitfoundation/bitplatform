using System.Diagnostics.CodeAnalysis;

namespace Bit.BlazorUI;

/// <summary>
/// A SwipeTrap is a component that traps swipe actions and triggers corresponding events.
/// </summary>
/// <remarks>
/// A swipe is a path-based gesture, which WCAG 2.2 asks to be paired with an alternative that needs no path
/// (SC 2.5.1, 2.5.7) and with one the keyboard can operate (SC 2.1.1). <see cref="KeyboardTrigger"/> is the
/// keyboard's: it makes the trap a tab stop whose arrow keys raise <see cref="OnTrigger"/> in their direction,
/// and names those keys to assistive technologies. Escape cancels a swipe in progress, which reports it through
/// <see cref="OnEnd"/> as canceled, the way a native drag-and-drop is put back.
/// </remarks>
public partial class BitSwipeTrap : BitComponentBase
{
    private decimal _appliedTrigger;
    private decimal _appliedTriggerVelocity;
    private decimal _appliedThreshold;
    private int _appliedThrottle;
    private BitSwipeOrientation _appliedOrientationLock;
    private bool _appliedTouchOnly;
    private string? _appliedSkipSelector;
    private bool _appliedKeyboardTrigger;
    private bool _cascadeChanged;



    [Inject] private IJSRuntime _js { get; set; } = default!;



    /// <summary>
    /// Gets or sets the cascading parameters for the swipe trap component.
    /// </summary>
    /// <remarks>
    /// This property receives its value from an ancestor component via Blazor's cascading parameter mechanism.
    /// <br />
    /// The intended use is to allow shared configuration or settings to be applied to multiple swipe trap components through the <see cref="BitParams"/> component.
    /// </remarks>
    [CascadingParameter(Name = BitSwipeTrapParams.ParamName)]
    public BitSwipeTrapParams? CascadingParameters { get; set; }



    /// <summary>
    /// The content of the swipe trap.
    /// </summary>
    [Parameter] public RenderFragment? ChildContent { get; set; }

    /// <summary>
    /// Lets the arrow keys raise the OnTrigger event, in their own direction, while the swipe trap itself has the focus.
    /// </summary>
    /// <remarks>
    /// This is the keyboard alternative to the swipe. The trap becomes a tab stop (unless a <see cref="BitComponentBase.TabIndex"/>
    /// says otherwise), the keys it answers to are named in <c>aria-keyshortcuts</c>, and an <see cref="OrientationLock"/>
    /// of Horizontal or Vertical limits them to the locked axis. A key press raises <see cref="OnTrigger"/> alone, with
    /// zero distances and a <c>PointerType</c> of "keyboard"; a held key does not repeat it, a key combined with a modifier
    /// is left to the browser, and a key pressed on a descendant (an input, a button) is that descendant's.
    /// Give the trap an <see cref="BitComponentBase.AriaLabel"/> that says what the keys do.
    /// The keys complement a control that needs no dragging (a button) rather than replace it: a screen reader in
    /// browse mode keeps the arrow keys for itself, so that control is what its users reach.
    /// </remarks>
    [Parameter] public bool KeyboardTrigger { get; set; }

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
    /// <remarks>
    /// A swipe that is called off rather than released - the browser took it over, the pointer left the trap before the
    /// swipe was trapped, or Escape was pressed - ends with <see cref="BitSwipeTrapEventArgs.IsCanceled"/> set, and
    /// triggers nothing.
    /// </remarks>
    [Parameter] public EventCallback<BitSwipeTrapEventArgs> OnEnd { get; set; }

    /// <summary>
    /// The event callback for when the swipe action triggers based on the Trigger or TriggerVelocity constraints.
    /// </summary>
    /// <remarks>
    /// Also raised for an arrow key when <see cref="KeyboardTrigger"/> is on, with zero distances and a
    /// <see cref="BitSwipeTrapTriggerArgs.PointerType"/> of "keyboard".
    /// </remarks>
    [Parameter] public EventCallback<BitSwipeTrapTriggerArgs> OnTrigger { get; set; }

    /// <summary>
    /// Specifies the orientation lock in which the swipe trap allows to trap the swipe actions.
    /// A Horizontal or Vertical lock is fixed for the whole gesture, whichever direction it starts in: the locked
    /// axis is the only one trapped and the only one reported, while the other axis keeps its default browser
    /// behavior (via a matching touch-action) and always reports zero.
    /// Auto instead locks to the first axis the gesture moves along.
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
    /// <remarks>
    /// The latest move of each window is still delivered when the window closes, so a pointer that comes to rest is
    /// reported where it rests. A move still held when the gesture ends is dropped: OnEnd carries the final position.
    /// </remarks>
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

    [JSInvokable("OnKeyTrigger")]
    public async Task _OnKeyTrigger(BitSwipeDirection direction)
    {
        if (Disabled || KeyboardTrigger is false) return;

        await OnTrigger.InvokeAsync(new(direction, 0, 0, 0, 0, "keyboard", 0));
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

    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(BitSwipeTrapParams))]
    protected override void OnParametersSet()
    {
        ApplyCascade();

        base.OnParametersSet();
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
        var keyboardTrigger = KeyboardTrigger;

        if (firstRender ||
            _appliedTrigger != trigger ||
            _appliedTriggerVelocity != triggerVelocity ||
            _appliedThreshold != threshold ||
            _appliedThrottle != throttle ||
            _appliedOrientationLock != orientationLock ||
            _appliedTouchOnly != touchOnly ||
            _appliedSkipSelector != skipSelector ||
            _appliedKeyboardTrigger != keyboardTrigger)
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
                        keyboardTrigger,
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
                _appliedKeyboardTrigger = keyboardTrigger;
            }
            catch (JSDisconnectedException) { } // we can ignore this exception here
        }

        await base.OnAfterRenderAsync(firstRender);
    }



    // ARIA prohibits naming an element with no role, so a named trap is a group - the generic container a name can
    // be given to - unless the page gave it a role of its own. A trap the keyboard can reach is one too: a tab stop
    // with no role is announced as nothing at all.
    private string? _GetRole(string? ariaLabel)
    {
        var role = GetSplattedAttribute("role");
        if (role is not null) return role;

        return ariaLabel.HasValue() || GetSplattedAttribute("aria-labelledby").HasValue() || _IsKeyboardReachable ? "group" : null;
    }

    // The trap is only put in the tab order for the keys it answers to, and only while it answers to them.
    private string? _GetTabIndex()
    {
        return TabIndex ?? GetSplattedAttribute("tabindex") ?? (_IsKeyboardReachable ? "0" : null);
    }

    private string? _GetKeyShortcuts()
    {
        var keyShortcuts = GetSplattedAttribute("aria-keyshortcuts");
        if (keyShortcuts is not null || _IsKeyboardReachable is false) return keyShortcuts;

        return OrientationLock switch
        {
            BitSwipeOrientation.Horizontal => "ArrowLeft ArrowRight",
            BitSwipeOrientation.Vertical => "ArrowUp ArrowDown",
            _ => "ArrowLeft ArrowRight ArrowUp ArrowDown"
        };
    }

    private bool _IsKeyboardReachable => KeyboardTrigger && Disabled is false;



    /// <summary>
    /// Supplies a parameter from the cascade, unless the markup has set it. BitComponentBase remembers the value it
    /// held before the cascade first supplied it, and puts it back once the cascade stops giving one.
    /// </summary>
    internal void TakeFromCascade<T>(string name, T value, Func<BitSwipeTrap, T> get, Action<BitSwipeTrap, T> set)
    {
        if (IsSetByMarkup(name)) return;

        // A value the cascade supplies again unchanged is no change: the class and style strings built from it the
        // last time still hold, so they are only rebuilt when something the cascade gives actually moves.
        if (EqualityComparer<T>.Default.Equals(get(this), value)) return;

        set(this, value);

        _cascadeChanged = true;
    }

    private void ApplyCascade()
    {
        if (CascadingParameters is null) return;

        _cascadeChanged = false;

        CascadingParameters.UpdateParameters(this);

        if (_cascadeChanged is false) return;

        ClassBuilder.Reset();
        StyleBuilder.Reset();
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
