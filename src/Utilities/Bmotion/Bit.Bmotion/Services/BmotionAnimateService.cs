using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;

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
/// var controls = await Motion.AnimateAsync(".box", new BmotionProps { X = 100, Opacity = 1 });
/// await controls; // wait for completion
///
/// // By ElementReference captured via @ref
/// var controls = await Motion.AnimateAsync(myRef, new BmotionProps { Scale = 1.2 },
///                                          BmotionTransitionConfig.Spring());
/// controls.Stop(); // cancel early
/// </code>
/// </example>
public sealed class BmotionAnimateService
{
    private readonly BmotionAnimationEngine _engine;
    private readonly IBmotionInterop _interop;
    private readonly BmotionOptions _options;
    private readonly ILogger? _logger;
    // The rejections Warn mode has reported, so a target animated again and again is reported once, not once per call.
    private readonly BmotionCssWarnings _cssWarned = new();

    /// <summary>
    /// The service on <paramref name="engine"/>, following the library-wide options the engine was created with: an
    /// engine from the application's container brings its <see cref="BmotionOptions.CssSafeMode"/>, and Warn mode
    /// logs to the engine's logger.
    /// </summary>
    public BmotionAnimateService(BmotionAnimationEngine engine, IBmotionInterop interop)
        : this(engine, interop, engine?.Options!, engine?.Logger)
    {
    }

    /// <summary>
    /// The service with library-wide options: <see cref="BmotionOptions.CssSafeMode"/> checks the targets it
    /// animates as it does a <see cref="Bmotion"/> element's, logging to <paramref name="logger"/> in Warn mode.
    /// </summary>
    public BmotionAnimateService(BmotionAnimationEngine engine, IBmotionInterop interop, BmotionOptions options, ILogger<BmotionAnimateService>? logger = null)
        : this(engine, interop, options, (ILogger?)logger)
    {
    }

    private BmotionAnimateService(BmotionAnimationEngine engine, IBmotionInterop interop, BmotionOptions options, ILogger? logger)
    {
        ArgumentNullException.ThrowIfNull(engine);
        ArgumentNullException.ThrowIfNull(interop);
        ArgumentNullException.ThrowIfNull(options);
        _engine = engine;
        _interop = interop;
        _options = options;
        _logger = logger;
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
    /// Optional transition configuration (easing, duration, spring parameters, etc.); the engine's default when
    /// omitted. A <see cref="BmotionConfig"/> in the markup has no say over this call: under reduced motion
    /// (<see cref="BmotionOptions.ReducedMotion"/>) transforms, layout and sizes land at once, as on an element.
    /// </param>
    /// <param name="stagger">
    /// Optional per-element start-delay generator (see <see cref="BmotionStagger"/>) applied in
    /// document order across the matched elements.
    /// </param>
    /// <param name="cancellationToken">Cancelling stops the animation and resolves the controls.</param>
    /// <returns>
    /// An <see cref="BmotionAnimationControls"/> that can be <c>await</c>ed or stopped early.
    /// </returns>
    public async ValueTask<BmotionAnimationControls> AnimateAsync(
        string selector,
        BmotionProps keyframes,
        BmotionTransition? transition = null,
        BmotionStagger? stagger = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(selector))
            throw new ArgumentException("Selector must not be null or whitespace.", nameof(selector));
        ArgumentNullException.ThrowIfNull(keyframes);
        // Skip interop + engine registration when already cancelled (see Bmotion.AnimateAsync).
        if (cancellationToken.IsCancellationRequested) return CancelledControls();
        // Checked before any element is registered, so a rejection leaves none behind.
        var values = CheckedValues(keyframes, selector);
        var (config, reduced) = await AsPlayedAsync((keyframes.Transition ?? transition)?.ToConfig());
        var ids = await ResolveAsync(selector);
        // Reduced, a staggered list appears at once rather than trickling in, as a Bmotion element's children do.
        return WithCancellation(await StartAnimationsAsync(ids, values, config, reduced ? null : stagger), cancellationToken);
    }

    // The transition as it plays, and whether motion is reduced: by BmotionOptions.ReducedMotion, the rule a Bmotion
    // element follows (Bmotion.IsMotionReduced) - with no <BmotionConfig> around a call to scope it - so transforms,
    // layout and sizes land at once while fades and colours still animate.
    private async ValueTask<(BmotionTransitionConfig? Config, bool Reduced)> AsPlayedAsync(BmotionTransitionConfig? normal)
    {
        if (Bmotion.OsPreferenceMatters(_options.ReducedMotion)) await _engine.EnsureReducedMotionDetectedAsync();
        return Bmotion.IsMotionReduced(_options.ReducedMotion, _engine.OsPrefersReducedMotion)
            ? (Bmotion.BuildReducedTransition(normal), true)
            : (normal, false);
    }

    // The elements a selector names. One the engine has never addressed starts from what it shows, not from the values
    // kept for the element that had its id before it - the same page, left and opened again, renders a new element.
    private async ValueTask<string[]> ResolveAsync(string selector)
    {
        var (ids, newIds) = await _interop.ResolveBySelectorAsync(selector);
        foreach (var id in newIds) _engine.ForgetRetained(id);
        return ids;
    }

    // CSS safe mode, as on a Bmotion element, and the values the engine is handed, in one synchronous step: what is
    // animated is exactly what was checked, however the target changes while the elements are resolved.
    private Dictionary<string, object?> CheckedValues(BmotionProps target, string? name)
    {
        BmotionCssValidator.Enforce(target, _options.CssSafeMode, _logger, name, _cssWarned);
        return target.ToJsDictionary();
    }

    // A resolved, no-op controls for a pre-cancelled call: no elements registered, already settled.
    private BmotionAnimationControls CancelledControls() => BmotionAnimationControls.Stopped(_engine);

    // Wires a CancellationToken to an in-flight animation: cancelling stops it (and resolves the
    // controls). The registration is disposed once the animation settles so it doesn't leak.
    private static BmotionAnimationControls WithCancellation(BmotionAnimationControls controls, CancellationToken cancellationToken)
    {
        if (!cancellationToken.CanBeCanceled) return controls;
        if (cancellationToken.IsCancellationRequested) { controls.Stop(); return controls; }
        // On the caller's context - the renderer's, on Blazor Server - however the token is cancelled: the engine is
        // single-threaded, and CancelAfter's timer cancels from the thread pool.
        var registration = cancellationToken.RegisterOnCallerContext(controls.Stop);
        controls.WhenCompleteAsync().OnCallerContext(_ => registration.Dispose());
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
    /// An <see cref="BmotionAnimationControls"/> that can be <c>await</c>ed or stopped early.
    /// </returns>
    public async ValueTask<BmotionAnimationControls> AnimateAsync(
        ElementReference elementReference,
        BmotionProps keyframes,
        BmotionTransition? transition = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(keyframes);
        if (cancellationToken.IsCancellationRequested) return CancelledControls();
        var values = CheckedValues(keyframes, elementReference.Id);
        var (config, _) = await AsPlayedAsync((keyframes.Transition ?? transition)?.ToConfig());
        var (id, isNew) = await _interop.ResolveByRefAsync(elementReference);
        if (isNew) _engine.ForgetRetained(id);
        return WithCancellation(await StartAnimationsAsync([id], values, config, stagger: null), cancellationToken);
    }

    /// <summary>
    /// Animates a motion value to a target, like motion.dev's <c>animate(x, 200)</c>.
    /// A spring transition inherits the value's current velocity for seamless interruption.
    /// The returned task completes when the animation settles (false result semantics: it
    /// resolves even when superseded by a newer animation on the same value).
    /// </summary>
    public async Task AnimateAsync(BmotionValue<double> value, double to, BmotionTransition? transition = null)
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
    public async Task AnimateAsync(double from, double to, Action<double> onUpdate, BmotionTransition? transition = null)
    {
        ArgumentNullException.ThrowIfNull(onUpdate);
        onUpdate(from);
        var value = Bm.Value(from);
        using var subscription = value.Subscribe(onUpdate);
        await AnimateAsync(value, to, transition);
    }

    /// <summary>
    /// Creates a derived motion value that spring-follows <paramref name="source"/> -
    /// motion.dev's <c>useSpring</c>. Dispose the returned value to detach it from its source (a spring
    /// still moving when it is disposed finishes on its own).
    /// </summary>
    public BmotionValue<double> Spring(BmotionValue<double> source, BmotionSpring? spring = null)
    {
        ArgumentNullException.ThrowIfNull(source);
        var template = spring ?? new BmotionSpring();
        var follower = Bm.Value(source.Value);
        IBmotionAnimationDriver? following = null;   // the spring this follower last started

        var subscription = source.Subscribe(target =>
        {
            // No frame loop (Blazor Server): the follower tracks the source directly.
            if (!_engine.SupportsFrameLoop)
            {
                follower.SetSync(target);
                return;
            }

            // A spring still chasing the last target is pointed at this one: it keeps its position, its momentum and its
            // clock, so a source that changes several times between two frames still lets it move on every frame.
            if (!_engine.TryRetargetDetached(follower, following, target))
            {
                var config = template.ToConfig();
                // Carry momentum into the new spring, unless the caller set the velocity it starts with (as AnimateAsync).
                if (config.Velocity == 0) config.Velocity = follower.GetVelocity();
                following = _engine.StartDetached(follower, follower.Value, target, config, v => follower.SetSync(v));
            }
            // Fire-and-forget loop start on every change, a retarget's included, so a start that failed is tried again
            // by the next change. A failure here surfaces on the next awaited animation.
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
    /// Runs a multi-step <see cref="BmotionSequence"/> timeline. Segments start at their computed
    /// timeline positions; the returned controls can await, stop, pause or speed up the whole run.
    /// </summary>
    public async ValueTask<BmotionAnimationControls> RunAsync(BmotionSequence sequence, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(sequence);
        if (cancellationToken.IsCancellationRequested) return CancelledControls();

        // Every segment checked (CSS safe mode) and serialized before any is resolved: a rejected one leaves no element
        // registered, and each plays exactly what was checked, however the sequence changes while they are resolved.
        var segments = sequence.Segments
            .Select(segment => (segment.Selector, Values: CheckedValues(segment.Target, segment.Selector), segment.Transition, segment.Start))
            .ToArray();

        // Resolve every segment's targets up front so registration/refcounting is symmetric.
        var starts = new List<(string[] ids, Dictionary<string, object?> values, BmotionTransitionConfig config, double start)>();
        foreach (var segment in segments)
        {
            var ids = await ResolveAsync(segment.Selector);
            if (ids.Length == 0) continue;
            var (config, _) = await AsPlayedAsync((segment.Transition ?? new BmotionTween()).ToConfig());
            starts.Add((ids, segment.Values, config!, segment.Start));
        }

        // One refcount per (segment, element) occurrence, released exactly once by the controls.
        var allIds = starts.SelectMany(s => s.ids).ToArray();
        foreach (var id in allIds)
            _engine.RegisterElement(id, null, programmatic: true);

        // Segment starts are scheduled rather than expressed as driver delays: a delayed driver
        // would immediately supersede an earlier segment animating the same property (drivers for
        // a key cancel their predecessor at creation), which would break sequential steps.
        //
        // Where a frame loop exists the schedule rides the engine's animation clock, so the
        // timeline's playback rate governs the gaps between segments as well as the segments
        // themselves - pausing a sequence really holds it, and doubling its speed halves the whole
        // run. On Blazor Server there is no frame loop, so the gaps fall back to wall-clock timers
        // (playback control degrades along with everything else that needs the loop).
        bool onEngineClock = _engine.SupportsFrameLoop;

        // Where every element starts from (all the keys any segment animates on it): what a seek puts it back to
        // before playing the timeline again from part-way in.
        var elementIds = allIds.Distinct().ToArray();
        Dictionary<string, object?>[] startValues;
        try
        {
            startValues = await Task.WhenAll(elementIds.Select(id => _engine.CaptureStartAsync(id,
                starts.Where(s => s.ids.Contains(id)).SelectMany(s => s.values.Keys).Distinct().ToArray()).AsTask()));
        }
        catch
        {
            foreach (var id in allIds)
                _engine.UnregisterElement(id);
            throw;
        }

        // One run of the timeline: its scheduler, its wall-clock timers, and one completion source per (segment,
        // element) the scheduler hands out - what the caller's `await controls` is actually waiting on. A seek
        // replaces the run with one starting part-way in.
        BmotionSequenceScheduler? scheduler = null;
        CancellationTokenSource? cts = null;
        var segmentCompletions = new List<TaskCompletionSource<bool>>();
        double timelineRate = 1;

        async Task AwaitSegmentAsync(string id, Dictionary<string, object?> values,
            BmotionTransitionConfig config, TaskCompletionSource<bool> tcs)
        {
            try
            {
                await _engine.AnimateToAwaitAsync(id, values, config);
                tcs.TrySetResult(true);
            }
            catch (Exception ex)
            {
                tcs.TrySetException(ex);
            }
        }

        async Task RunSegmentOnWallClockAsync(string id, Dictionary<string, object?> values,
            BmotionTransitionConfig config, double start, CancellationToken token)
        {
            try
            {
                if (start > 0) await Task.Delay(TimeSpan.FromSeconds(start), token);
            }
            catch (OperationCanceledException)
            {
                return; // sequence stopped/completed (or sought) before this segment started
            }
            await _engine.AnimateToAwaitAsync(id, values, config);
        }

        // Plays the timeline from `offset` seconds in. Segments over by then land on their end values (in order, so
        // a later one on the same property wins), a segment under way starts part-way through, and the rest are
        // scheduled for when the playhead reaches them.
        async Task PlayFromAsync(double offset)
        {
            var run = new CancellationTokenSource();
            cts = run;
            var scheduledStarts = new List<(double StartSeconds, Action Fire)>();
            var completionTasks = new List<Task>();
            foreach (var (ids, values, config, start) in starts.OrderBy(s => s.start))
                foreach (var id in ids)
                {
                    if (start + config.TotalSeconds() <= offset)
                    {
                        var rest = new Dictionary<string, object?>();
                        foreach (var (key, value) in values)
                            if (value is not null && BmotionElementAnimationState.RestingValueOf(value) is { } resting) rest[key] = resting;
                        _engine.SetInstant(id, rest);
                        continue;
                    }
                    var c = config;
                    double wait = start - offset;
                    if (wait < 0)
                    {
                        c = ShiftedBy(config, wait); // started -wait seconds ago
                        wait = 0;
                    }
                    if (!onEngineClock)
                    {
                        completionTasks.Add(RunSegmentOnWallClockAsync(id, values, c, wait, run.Token));
                        continue;
                    }
                    var tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
                    segmentCompletions.Add(tcs);
                    // Fire-and-forget by design: the segment's own completion flows through `tcs`, and the
                    // scheduler tick (inside the rAF frame) must not await anything.
                    scheduledStarts.Add((wait, () => _ = AwaitSegmentAsync(id, values, c, tcs)));
                    completionTasks.Add(tcs.Task);
                }

            if (onEngineClock && scheduledStarts.Count > 0)
            {
                scheduler = new BmotionSequenceScheduler(scheduledStarts) { Rate = timelineRate };
                _engine.AddScheduler(scheduler);
                await _engine.EnsureLoopRunningAsync();
            }
            await Task.WhenAll(completionTasks);
        }

        // Ends the current run: drops its not-yet-fired segments first, so nothing races into starting, and settles
        // the completions of segments it will now never fire (false = superseded/stopped, not a natural finish).
        void EndRun()
        {
            if (scheduler != null) _engine.RemoveScheduler(scheduler);
            scheduler = null;
            cts?.Cancel();
            cts?.Dispose();
            cts = null;
            foreach (var pending in segmentCompletions) pending.TrySetResult(false);
            segmentCompletions.Clear();
        }

        var completion = PlayFromAsync(0);

        var released = false;
        void Release()
        {
            if (released) return;
            released = true;
            EndRun();
            foreach (var id in allIds)
                _engine.UnregisterElement(id);
        }

        var playback = new BmotionPlayback
        {
            Duration = starts.Count == 0 ? 0 : starts.Max(s => s.start + s.config.TotalSeconds()),
            PlayFrom = offset =>
            {
                EndRun();
                for (int i = 0; i < elementIds.Length; i++) _engine.ResetForSeek(elementIds[i], startValues[i]);
                return PlayFromAsync(offset);
            },
            Rearm = () =>
            {
                released = false;
                foreach (var id in allIds)
                    _engine.RegisterElement(id, null, programmatic: true);
            },
        };

        var controls = new BmotionAnimationControls(allIds, _engine, completion, Release,
            setTimelineRate: rate =>
            {
                timelineRate = rate;
                if (scheduler != null) scheduler.Rate = rate;
            },
            playback: playback);

        return WithCancellation(controls, cancellationToken);
    }

    // ────────────────────────────────────────────────────────────────────────────

    // A copy of `config` starting `seconds` later (earlier, when negative: a negative delay starts it part-way in). Its
    // per-property overrides move with it, as they are what plays a property that has one - the reduced-motion
    // transition keeps the properties still animating there, under an instant root.
    private static BmotionTransitionConfig ShiftedBy(BmotionTransitionConfig config, double seconds)
    {
        var shifted = config.Clone();
        shifted.Delay += seconds;
        if (shifted.Properties is { } overrides)
            foreach (var own in overrides.Values) own.Delay += seconds;
        return shifted;
    }

    // `values` and `config` are the target as checked and lowered before the elements were resolved. The config is the
    // target's embedded transition, else the explicit argument (the component's resolution order), lowered once and
    // shared across all target elements (drivers never mutate the config).
    private async ValueTask<BmotionAnimationControls> StartAnimationsAsync(
        string[] elementIds,
        Dictionary<string, object?> values,
        BmotionTransitionConfig? config,
        BmotionStagger? stagger)
    {
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
            _engine.RegisterElement(id, null, programmatic: true);

        // Start all animations concurrently; collect their completion tasks. Each runs that many seconds in: 0 at
        // first, the seek point when a seek plays it again (a negative delay starts an animation part-way through).
        Task PlayFrom(double seconds) => Task.WhenAll(elementIds.Select((id, index) =>
        {
            var c = ConfigFor(index);
            if (seconds > 0) c = ShiftedBy(c, -seconds);
            return _engine.AnimateToAwaitAsync(id, values, c).AsTask();
        }));

        // Where every element starts from - what a seek puts it back to before playing it again - and the first run.
        // If either throws (e.g. a driver rejects the keyframes), release every element we already registered so
        // they don't leak a refcount before the exception propagates.
        Dictionary<string, object?>[] starts;
        Task completion;
        try
        {
            starts = await Task.WhenAll(elementIds.Select(id => _engine.CaptureStartAsync(id, values.Keys).AsTask()));
            completion = PlayFrom(0);
        }
        catch
        {
            foreach (var id in elementIds)
                _engine.UnregisterElement(id);
            throw;
        }

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

        var playback = new BmotionPlayback
        {
            Duration = elementIds.Length == 0 ? 0 : elementIds.Select((_, index) => ConfigFor(index).TotalSeconds()).Max(),
            // Every element's browser animations moved in place, or (anything else playing, or one already over) none.
            TrySeekInPlace = seconds => Array.TrueForAll(elementIds, id => _engine.TrySeekBrowserAnimations(id, seconds * 1000)),
            PlayFrom = seconds =>
            {
                for (int i = 0; i < elementIds.Length; i++) _engine.ResetForSeek(elementIds[i], starts[i]);
                return PlayFrom(seconds);
            },
            Rearm = () =>
            {
                released = false;
                foreach (var id in elementIds)
                    _engine.RegisterElement(id, null, programmatic: true);
            },
        };

        return new BmotionAnimationControls(elementIds, _engine, completion, Release, playback: playback);
    }
}
