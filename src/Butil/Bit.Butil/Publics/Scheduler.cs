using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using Microsoft.JSInterop;

namespace Bit.Butil;

/// <summary>
/// Wraps the browser's scheduling primitives:
/// <see href="https://developer.mozilla.org/en-US/docs/Web/API/Window/requestAnimationFrame">requestAnimationFrame</see>,
/// <see href="https://developer.mozilla.org/en-US/docs/Web/API/Window/requestIdleCallback">requestIdleCallback</see>,
/// and the <see href="https://developer.mozilla.org/en-US/docs/Web/API/Prioritized_Task_Scheduling_API">Prioritized Task Scheduling API</see>
/// (<c>scheduler.postTask</c>, <c>scheduler.yield</c>, <c>navigator.scheduling.isInputPending</c>).
/// </summary>
/// <remarks>
/// These are the browser's answer to "when should this run?", and C# had no way to ask. A frame loop
/// runs in step with the compositor rather than on a timer; idle work runs in the gaps rather than
/// competing with rendering; and a posted task can say how urgent it is instead of joining one queue
/// with everything else.
/// <br/>
/// <b>Do not use a frame loop as a timer.</b> Frames stop entirely in a background tab, and their
/// spacing follows the display - 16.7ms at 60Hz, 8.3ms at 120Hz. Drive animation from the timestamp
/// you are handed, not from a count of frames.
/// </remarks>
[ButilService(typeof(Scheduler))]
public class Scheduler(IJSRuntime js) : IAsyncDisposable
{
    internal const string FrameMethodName = nameof(InvokeAnimationFrame);
    internal const string IdleMethodName = nameof(InvokeIdleCallback);
    internal const string TaskMethodName = nameof(InvokeScheduledTask);

    // A frame handler carries whether it repeats, because a one-shot request and a loop share this
    // dictionary and the same JS callback - and only the one-shot's handler is finished after it runs.
    private readonly ConcurrentDictionary<Guid, (Action<double> OnFrame, bool Repeats)> _frameHandlers = new();
    private readonly ConcurrentDictionary<Guid, Action<IdleDeadline>> _idleHandlers = new();
    private readonly ConcurrentDictionary<Guid, Action> _taskHandlers = new();

    // Per-instance callback reference (see Keyboard): callbacks are isolated per circuit / WASM app
    // and cancelled on disposal - no static state, no cross-circuit leak.
    private DotNetObjectReference<Scheduler>? _dotNetRef;
    private DotNetObjectReference<Scheduler> DotNetRef => DotNetObjectReferenceHelper.GetOrCreate(ref _dotNetRef, this);

    /// <summary>True when the runtime exposes <c>requestAnimationFrame</c>, which is everywhere.</summary>
    /// <remarks>
    /// During prerender/SSR (no JS runtime) this returns <c>default</c> (<c>false</c>) rather than
    /// throwing, so the result can't be distinguished from a genuine value.
    /// </remarks>
    public ValueTask<bool> IsSupported() => js.Invoke<bool>("BitButil.scheduler.isSupported");

    /// <summary>True when the runtime exposes <c>requestIdleCallback</c>. Safari was the last holdout.</summary>
    public ValueTask<bool> IsIdleCallbackSupported() => js.Invoke<bool>("BitButil.scheduler.isIdleCallbackSupported");

    /// <summary>
    /// True when the runtime exposes <c>scheduler.postTask</c>. <see cref="PostTask"/> still works
    /// where it does not - it falls back to a timeout, where the priority is ignored because a
    /// timeout queue has none.
    /// </summary>
    public ValueTask<bool> IsPostTaskSupported() => js.Invoke<bool>("BitButil.scheduler.isPostTaskSupported");

    /// <summary>
    /// True when the runtime exposes <c>scheduler.yield</c>. <see cref="Yield"/> falls back to a
    /// macrotask, which also lets the browser breathe but sends the continuation to the back of the
    /// queue rather than keeping its priority.
    /// </summary>
    public ValueTask<bool> IsYieldSupported() => js.Invoke<bool>("BitButil.scheduler.isYieldSupported");

    /// <summary>True when the runtime exposes <c>navigator.scheduling.isInputPending</c> - Chromium only, for now.</summary>
    public ValueTask<bool> IsInputPendingSupported() => js.Invoke<bool>("BitButil.scheduler.isInputPendingSupported");

    /// <summary>
    /// Invoked from JS on each animation frame. Public + <see cref="JSInvokableAttribute"/> so it
    /// can be dispatched through the per-instance <see cref="DotNetObjectReference{T}"/>.
    /// </summary>
    [JSInvokable(FrameMethodName)]
    public void InvokeAnimationFrame(Guid id, double timestamp)
    {
        if (_frameHandlers.TryGetValue(id, out var handler) is false) return;

        // A single frame does not repeat, so the handler goes with it - a loop keeps its own until
        // the subscription is disposed.
        if (handler.Repeats is false) _frameHandlers.TryRemove(id, out _);

        handler.OnFrame(timestamp);
    }

    /// <summary>
    /// Invoked from JS when an idle period begins. Public + <see cref="JSInvokableAttribute"/> so it
    /// can be dispatched through the per-instance <see cref="DotNetObjectReference{T}"/>.
    /// </summary>
    [JSInvokable(IdleMethodName)]
    public void InvokeIdleCallback(Guid id, bool didTimeout, double timeRemaining)
    {
        // One-shot: the callback does not repeat, so the handler goes with it.
        if (_idleHandlers.TryRemove(id, out var handler)) handler(new IdleDeadline(didTimeout, timeRemaining));
    }

    /// <summary>
    /// Invoked from JS when a posted task runs. Public + <see cref="JSInvokableAttribute"/> so it
    /// can be dispatched through the per-instance <see cref="DotNetObjectReference{T}"/>.
    /// </summary>
    [JSInvokable(TaskMethodName)]
    public void InvokeScheduledTask(Guid id)
    {
        if (_taskHandlers.TryRemove(id, out var handler)) handler();
    }

    /// <summary>
    /// Runs <paramref name="onFrame"/> once, just before the browser's next paint.
    /// </summary>
    /// <param name="onFrame">
    /// Called with the frame's timestamp in milliseconds - the same value every callback in that
    /// frame receives, which is what keeps animations driven from different places in step.
    /// </param>
    /// <returns>A subscription; disposing it before the frame arrives cancels the callback.</returns>
    public async ValueTask<ButilSubscription> RequestAnimationFrame(Action<double> onFrame)
    {
        ArgumentNullException.ThrowIfNull(onFrame);

        var id = Guid.NewGuid();
        _frameHandlers[id] = (onFrame, Repeats: false);

        // False means nothing was scheduled - during prerender/SSR, most often - and the handler
        // would otherwise sit in the dictionary for the life of the service with no frame to run it.
        var requested = await js.Invoke<bool>("BitButil.scheduler.requestFrame", DotNetRef, id);
        if (requested is false) _frameHandlers.TryRemove(id, out _);

        return new ButilSubscription(id, async () =>
        {
            _frameHandlers.TryRemove(id, out _);
            await js.InvokeVoid("BitButil.scheduler.cancelFrame", id);
        });
    }

    /// <summary>
    /// Runs <paramref name="onFrame"/> on every frame until the subscription is disposed.
    /// </summary>
    /// <param name="onFrame">Called with each frame's timestamp in milliseconds.</param>
    /// <returns>A subscription; disposing it stops the loop.</returns>
    /// <remarks>
    /// The next frame is requested before your callback is told about this one, so the loop runs at
    /// the browser's cadence rather than at the speed of the interop round trip. A callback that
    /// takes longer than a frame causes frames to be <em>skipped</em> rather than queued - which is
    /// what an animation wants, and why you should drive it from the timestamp rather than from a
    /// count of calls.
    /// <br/>
    /// Frames stop entirely while the tab is in the background, so a loop is not a way to keep time.
    /// </remarks>
    public async ValueTask<ButilSubscription> OnAnimationFrame(Action<double> onFrame)
    {
        ArgumentNullException.ThrowIfNull(onFrame);

        var id = Guid.NewGuid();
        _frameHandlers[id] = (onFrame, Repeats: true);

        // As for a single frame: a loop that was never started leaves no handler behind.
        var started = await js.Invoke<bool>("BitButil.scheduler.startFrameLoop", DotNetRef, id);
        if (started is false) _frameHandlers.TryRemove(id, out _);

        return new ButilSubscription(id, async () =>
        {
            _frameHandlers.TryRemove(id, out _);
            await js.InvokeVoid("BitButil.scheduler.cancelFrame", id);
        });
    }

    /// <summary>
    /// Runs <paramref name="onIdle"/> when the browser has time to spare.
    /// </summary>
    /// <param name="onIdle">
    /// Called once, with how much of the idle period is left. Check
    /// <see cref="IdleDeadline.TimeRemaining"/> and stop when it runs out rather than working
    /// through - the point of idle work is that it yields.
    /// </param>
    /// <param name="timeout">
    /// Run anyway after this long, even if the browser never goes idle. Without it, work scheduled
    /// on a page that is never idle never runs at all.
    /// </param>
    /// <returns>A subscription to cancel it, or null where the runtime has no <c>requestIdleCallback</c>.</returns>
    /// <remarks>
    /// One shot. Re-request from inside the callback to keep going, which is also the point at which
    /// you get to decide whether there is still work worth doing.
    /// </remarks>
    public async ValueTask<ButilSubscription?> RequestIdleCallback(Action<IdleDeadline> onIdle, TimeSpan? timeout = null)
    {
        ArgumentNullException.ThrowIfNull(onIdle);

        var id = Guid.NewGuid();
        _idleHandlers[id] = onIdle;

        var requested = await js.Invoke<bool>("BitButil.scheduler.requestIdle",
            DotNetRef, id, (long)(timeout?.TotalMilliseconds ?? 0));

        if (requested is false)
        {
            _idleHandlers.TryRemove(id, out _);
            return null;
        }

        return new ButilSubscription(id, async () =>
        {
            _idleHandlers.TryRemove(id, out _);
            await js.InvokeVoid("BitButil.scheduler.cancelIdle", id);
        });
    }

    /// <summary>
    /// Posts a task to the browser's own scheduler and completes when it has run.
    /// </summary>
    /// <param name="work">What to run.</param>
    /// <param name="priority">
    /// How urgent it is. This is what a plain <c>Task.Run</c> or a timeout cannot say: the browser
    /// runs <see cref="SchedulerPriority.UserBlocking"/> work ahead of rendering and
    /// <see cref="SchedulerPriority.Background"/> work behind it.
    /// </param>
    /// <param name="delay">Wait at least this long before queueing it.</param>
    /// <param name="signal">
    /// A shared <see cref="ButilAbortSignal"/> that cancels the task if it has not run yet - the
    /// same signal that cancels your requests can cancel your queued work.
    /// </param>
    /// <returns>
    /// Null when the task ran, or the reason it did not - <c>"aborted"</c>, most often, and
    /// <c>"unavailable"</c> when there is no browser to post to yet (prerendering or SSR).
    /// </returns>
    /// <remarks>
    /// Where <c>scheduler.postTask</c> is missing this falls back to a timeout, which has one queue
    /// and no priorities: the task still runs, and the priority is ignored rather than approximated.
    /// </remarks>
    public async ValueTask<string?> PostTask(Action work,
                                             SchedulerPriority priority = SchedulerPriority.UserVisible,
                                             TimeSpan? delay = null,
                                             ButilAbortSignal? signal = null)
    {
        ArgumentNullException.ThrowIfNull(work);

        // Said explicitly rather than left to Invoke's safe default, which for a string is the empty
        // string: as a "reason it did not run" that says nothing, and the work really does not run
        // here. Registering the handler after this check also keeps it out of the dictionary.
        if (js.IsJsRuntimeInvalid()) return "unavailable";

        var id = Guid.NewGuid();
        _taskHandlers[id] = work;

        try
        {
            return await js.Invoke<string?>("BitButil.scheduler.postTask",
                DotNetRef, id, ToName(priority), (long)(delay?.TotalMilliseconds ?? 0), signal?.Id);
        }
        finally
        {
            _taskHandlers.TryRemove(id, out _);
        }
    }

    /// <summary>
    /// Hands the thread back to the browser so it can paint or handle input, then continues.
    /// </summary>
    /// <remarks>
    /// For a long loop in C#: yielding every so often is the difference between a page that responds
    /// and a page that is frozen. Where <c>scheduler.yield</c> exists the continuation keeps its
    /// place in the priority queue; the fallback is a plain macrotask, which goes to the back.
    /// </remarks>
    public ValueTask Yield() => js.InvokeVoid("BitButil.scheduler.yield");

    /// <summary>
    /// Whether the user has done something the browser has not been able to handle yet - a click or
    /// a keystroke waiting behind your work.
    /// </summary>
    /// <returns>
    /// False where the API is missing, which reads the same as "nothing is waiting" on purpose: a
    /// caller uses this to decide whether to keep working, and the safe answer where it cannot be
    /// known is to keep working and yield on a schedule instead.
    /// </returns>
    public ValueTask<bool> IsInputPending() => js.Invoke<bool>("BitButil.scheduler.isInputPending");

    // The strings postTask accepts.
    private static string ToName(SchedulerPriority priority) => priority switch
    {
        SchedulerPriority.UserBlocking => "user-blocking",
        SchedulerPriority.Background => "background",
        _ => "user-visible",
    };

    /// <summary>
    /// On scope/circuit teardown, cancels every frame loop and idle callback whose subscription was
    /// never disposed, so an abandoned loop can't keep running against a component that is gone.
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        try
        {
            _frameHandlers.Clear();
            _idleHandlers.Clear();
            _taskHandlers.Clear();
            await js.InvokeVoid("BitButil.scheduler.disposeAll");
        }
        catch (Exception ex) when (ex.IsIgnorableDisposalException()) { } // teardown: circuit gone, cancelled, or already disposed
        finally
        {
            _dotNetRef?.Dispose();
            _dotNetRef = null;
        }
        GC.SuppressFinalize(this);
    }
}
