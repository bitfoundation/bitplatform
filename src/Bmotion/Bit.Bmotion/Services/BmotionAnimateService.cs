using Microsoft.AspNetCore.Components;

namespace Bit.Bmotion;
/// <summary>
/// Provides a method-based animation API analogous to the <c>animate()</c> function in
/// <see href="https://motion.dev/docs/quick-start">motion.dev</see>.
/// <para>
/// Elements are identified by a CSS selector string or a Blazor <see cref="ElementReference"/>.
/// They do <em>not</em> need to be wrapped in a <c>&lt;Bmotion&gt;</c> component.
/// </para>
/// </summary>
/// <example>
/// <code>
/// // By CSS selector
/// var controls = await Motion.AnimateAsync(".box", new BmProps { X = 100, Opacity = 1 });
/// await controls; // wait for completion
///
/// // By ElementReference captured via @ref
/// var controls = await Motion.AnimateAsync(myRef, new BmProps { Scale = 1.2 },
///                                          BmotionTransitionConfig.Spring());
/// controls.Stop(); // cancel early
/// </code>
/// </example>
public sealed class BmotionAnimateService
{
    private readonly BmotionAnimationEngine _engine;
    private readonly IBmotionInterop _interop;

    public BmotionAnimateService(BmotionAnimationEngine engine, IBmotionInterop interop)
    {
        ArgumentNullException.ThrowIfNull(engine);
        ArgumentNullException.ThrowIfNull(interop);
        _engine = engine;
        _interop = interop;
    }

    /// <summary>
    /// Animate all DOM elements matching <paramref name="selector"/> to
    /// <paramref name="keyframes"/>.
    /// </summary>
    /// <param name="selector">
    /// A CSS selector string, e.g. <c>".card"</c>, <c>"#hero"</c>, or <c>"div.item"</c>.
    /// Multiple matching elements are animated simultaneously.
    /// </param>
    /// <param name="keyframes">Target animation properties.</param>
    /// <param name="transition">
    /// Optional transition configuration (easing, duration, spring parameters, etc.).
    /// Falls back to the global <see cref="BmotionConfig"/> default when omitted.
    /// </param>
    /// <param name="stagger">
    /// Optional per-element start-delay generator (see <see cref="BmStagger"/>) applied in
    /// document order across the matched elements.
    /// </param>
    /// <param name="cancellationToken">Cancelling stops the animation and resolves the controls.</param>
    /// <returns>
    /// An <see cref="BmAnimationControls"/> that can be <c>await</c>ed or stopped early.
    /// </returns>
    public async ValueTask<BmAnimationControls> AnimateAsync(
        string selector,
        BmProps keyframes,
        BmTransition? transition = null,
        BmStagger? stagger = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(selector))
            throw new ArgumentException("Selector must not be null or whitespace.", nameof(selector));
        ArgumentNullException.ThrowIfNull(keyframes);
        // Skip interop + engine registration when already cancelled (see Bmotion.AnimateAsync).
        if (cancellationToken.IsCancellationRequested) return CancelledControls();
        var ids = await _interop.ResolveOrRegisterBySelectorAsync(selector);
        return WithCancellation(StartAnimations(ids, keyframes, transition, stagger), cancellationToken);
    }

    // A resolved, no-op controls for a pre-cancelled call: no elements registered, already settled.
    private BmAnimationControls CancelledControls()
        => new(Array.Empty<string>(), _engine, Task.CompletedTask, static () => { });

    // Wires a CancellationToken to an in-flight animation: cancelling stops it (and resolves the
    // controls). The registration is disposed once the animation settles so it doesn't leak.
    private static BmAnimationControls WithCancellation(BmAnimationControls controls, CancellationToken cancellationToken)
    {
        if (!cancellationToken.CanBeCanceled) return controls;
        if (cancellationToken.IsCancellationRequested) { controls.Stop(); return controls; }
        var registration = cancellationToken.Register(controls.Stop);
        controls.WhenCompleteAsync().ContinueWith(_ => registration.Dispose(), TaskScheduler.Default);
        return controls;
    }

    /// <summary>
    /// Animate the element captured by <paramref name="elementReference"/> to
    /// <paramref name="keyframes"/>.
    /// </summary>
    /// <param name="elementReference">
    /// A Blazor <see cref="ElementReference"/> obtained via <c>@ref</c> on any HTML element.
    /// </param>
    /// <param name="keyframes">Target animation properties.</param>
    /// <param name="transition">Optional transition configuration.</param>
    /// <param name="cancellationToken">Cancelling stops the animation and resolves the controls.</param>
    /// <returns>
    /// An <see cref="BmAnimationControls"/> that can be <c>await</c>ed or stopped early.
    /// </returns>
    public async ValueTask<BmAnimationControls> AnimateAsync(
        ElementReference elementReference,
        BmProps keyframes,
        BmTransition? transition = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(keyframes);
        if (cancellationToken.IsCancellationRequested) return CancelledControls();
        var id = await _interop.ResolveOrRegisterByRefAsync(elementReference);
        return WithCancellation(StartAnimations([id], keyframes, transition, stagger: null), cancellationToken);
    }

    /// <summary>
    /// Animates a motion value to a target, like motion.dev's <c>animate(x, 200)</c>.
    /// A spring transition inherits the value's current velocity for seamless interruption.
    /// The returned task completes when the animation settles (false result semantics: it
    /// resolves even when superseded by a newer animation on the same value).
    /// </summary>
    public async Task AnimateAsync(BmValue<double> value, double to, BmTransition? transition = null)
    {
        ArgumentNullException.ThrowIfNull(value);

        // Motion values are driven by the per-frame loop, which needs synchronous interop
        // (Blazor WebAssembly). Elsewhere (Blazor Server) they settle instantly.
        if (!_engine.SupportsFrameLoop)
        {
            value.SetSync(to);
            return;
        }

        var config = transition?.ToConfig() ?? new BmotionTransitionConfig();
        // Springs pick up the value's live velocity so interrupting an in-flight animation
        // continues smoothly instead of visibly restarting from rest.
        if (config.Type == BmotionTransitionType.Spring && config.Velocity == 0)
            config.Velocity = value.GetVelocity();

        var tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        _engine.StartDetached(value, value.Value, to, config, v => value.SetSync(v), tcs);
        await _engine.EnsureLoopRunningAsync();
        await tcs.Task;
    }

    /// <summary>
    /// Animates a raw number from <paramref name="from"/> to <paramref name="to"/>, invoking
    /// <paramref name="onUpdate"/> with every interpolated value - motion.dev's
    /// <c>animate(0, 100, { onUpdate })</c>. Useful for counters, canvas drawing, or any
    /// consumer outside the DOM. Without a frame loop (Blazor Server) it settles instantly.
    /// </summary>
    public async Task AnimateAsync(double from, double to, Action<double> onUpdate, BmTransition? transition = null)
    {
        ArgumentNullException.ThrowIfNull(onUpdate);
        onUpdate(from);
        var value = Bm.Value(from);
        using var subscription = value.Subscribe(onUpdate);
        await AnimateAsync(value, to, transition);
    }

    /// <summary>
    /// Creates a derived motion value that spring-follows <paramref name="source"/> -
    /// motion.dev's <c>useSpring</c>. Dispose the returned value to detach it.
    /// </summary>
    public BmValue<double> Spring(BmValue<double> source, BmSpring? spring = null)
    {
        ArgumentNullException.ThrowIfNull(source);
        var template = spring ?? new BmSpring();
        var follower = Bm.Value(source.Value);

        var subscription = source.Subscribe(target =>
        {
            // No frame loop (Blazor Server): the follower tracks the source directly.
            if (!_engine.SupportsFrameLoop)
            {
                follower.SetSync(target);
                return;
            }

            var config = template.ToConfig();
            config.Velocity = follower.GetVelocity(); // carry momentum across retargets
            _engine.StartDetached(follower, follower.Value, target, config, v => follower.SetSync(v));
            // Fire-and-forget loop start: a failure here surfaces on the next awaited animation.
            _ = EnsureLoopSafeAsync();
        });
        follower.AttachUpstream(subscription);
        return follower;

        async Task EnsureLoopSafeAsync()
        {
            try { await _engine.EnsureLoopRunningAsync(); }
            catch { /* best-effort; awaited entry points surface real errors */ }
        }
    }

    /// <summary>
    /// Runs a multi-step <see cref="BmSequence"/> timeline. Segments start at their computed
    /// timeline positions; the returned controls can await, stop, pause or speed up the whole run.
    /// </summary>
    public async ValueTask<BmAnimationControls> RunAsync(BmSequence sequence, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(sequence);
        if (cancellationToken.IsCancellationRequested) return CancelledControls();

        // Resolve every segment's targets up front so registration/refcounting is symmetric.
        var starts = new List<(string[] ids, Dictionary<string, object?> values, BmotionTransitionConfig config, double start)>();
        foreach (var segment in sequence.Segments)
        {
            var ids = await _interop.ResolveOrRegisterBySelectorAsync(segment.Selector);
            if (ids.Length == 0) continue;
            var config = (segment.Transition ?? new BmTween()).ToConfig();
            starts.Add((ids, segment.Target.ToJsDictionary(), config, segment.Start));
        }

        // One refcount per (segment, element) occurrence, released exactly once by the controls.
        var allIds = starts.SelectMany(s => s.ids).ToArray();
        foreach (var id in allIds)
            _engine.RegisterElement(id);

        // Segment starts are scheduled with a timer rather than driver delays: a delayed driver
        // would immediately supersede an earlier segment animating the same property (drivers for
        // a key cancel their predecessor at creation), which would break sequential steps.
        var cts = new CancellationTokenSource();

        async Task RunSegmentAsync(string id, Dictionary<string, object?> values, BmotionTransitionConfig config, double start)
        {
            try
            {
                if (start > 0) await Task.Delay(TimeSpan.FromSeconds(start), cts.Token);
            }
            catch (OperationCanceledException)
            {
                return; // sequence stopped/completed before this segment started
            }
            await _engine.AnimateToAwaitAsync(id, values, config);
        }

        var completionTasks = starts
            .SelectMany(s => s.ids.Select(id => RunSegmentAsync(id, s.values, s.config, s.start)))
            .ToArray();
        var completion = Task.WhenAll(completionTasks);

        var released = false;
        void Release()
        {
            if (released) return;
            released = true;
            // Cancel not-yet-started segments first so a Stop() can't race a segment into starting.
            cts.Cancel();
            cts.Dispose();
            foreach (var id in allIds)
                _engine.UnregisterElement(id);
        }

        var controls = new BmAnimationControls(allIds, _engine, completion, Release);

        _ = completion.ContinueWith(
            _ => controls.OnCompletionSettled(),
            TaskScheduler.Default);

        return WithCancellation(controls, cancellationToken);
    }

    // ────────────────────────────────────────────────────────────────────────────

    private BmAnimationControls StartAnimations(
        string[] elementIds,
        BmProps keyframes,
        BmTransition? transition,
        BmStagger? stagger)
    {
        var values = keyframes.ToJsDictionary();
        // A transition embedded in the target wins over the explicit argument, matching the
        // component's resolution order. Lower once and share across all target elements
        // (drivers never mutate the config).
        var config = (keyframes.Transition ?? transition)?.ToConfig();

        // A stagger needs a per-element config so each element gets its own delay.
        BmotionTransitionConfig ConfigFor(int index)
        {
            if (stagger is null) return config ?? new BmotionTransitionConfig();
            var c = config?.Clone() ?? new BmotionTransitionConfig();
            c.Delay += stagger.DelayFor(index, elementIds.Length);
            return c;
        }

        // Reference-count every target for the lifetime of this call. Overlapping AnimateAsync
        // invocations (and any wrapping <Bmotion>) each hold a count, so an element is only torn
        // down once the last animation settles - a single completing call can't unregister an
        // element out from under another still-running animation.
        foreach (var id in elementIds)
            _engine.RegisterElement(id);

        // Start all animations concurrently; collect their completion tasks. If task creation
        // throws synchronously (e.g. a driver rejects the keyframes), release every element we
        // already registered so they don't leak a refcount before the exception propagates.
        Task[] completionTasks;
        try
        {
            completionTasks = elementIds
                .Select((id, index) => _engine.AnimateToAwaitAsync(id, values, ConfigFor(index)).AsTask())
                .ToArray();
        }
        catch
        {
            foreach (var id in elementIds)
                _engine.UnregisterElement(id);
            throw;
        }

        var completion = Task.WhenAll(completionTasks);

        // The controls own the single refcount release (idempotent). It fires whichever happens
        // first: natural completion, Stop(), or Complete(). This is what prevents an
        // infinite-repeat animation - whose completion task never resolves - from leaking refcounts.
        var released = false;
        void Release()
        {
            if (released) return;
            released = true;
            foreach (var id in elementIds)
                _engine.UnregisterElement(id);
        }

        var controls = new BmAnimationControls(elementIds, _engine, completion, Release);

        _ = completion.ContinueWith(
            _ => controls.OnCompletionSettled(),
            TaskScheduler.Default);

        return controls;
    }
}
