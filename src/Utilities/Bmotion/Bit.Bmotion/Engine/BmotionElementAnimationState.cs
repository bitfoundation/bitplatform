
namespace Bit.Bmotion;
/// <summary>
/// Per-element animation state - the C# equivalent of the JS <c>ElementState</c> class.
/// Holds current transform / numeric / color values, active animation drivers,
/// and gesture-layer bookkeeping. Called by <see cref="BmotionAnimationEngine.ComputeFrame"/>
/// every rAF tick.
/// </summary>
internal sealed class BmotionElementAnimationState
{
    // ── Live CSS values ───────────────────────────────────────────────────────

    /// <summary>Current values of transform components (x, y, scale, rotate, …).</summary>
    // Case-insensitive so keys accepted by BmotionTransformComposer.IsTransformProp (which compares
    // OrdinalIgnoreCase) match the canonical lowercase keys the composer reads when emitting.
    internal readonly Dictionary<string, double> Transforms = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Current values of numeric non-transform properties (opacity, pathLength, …).</summary>
    internal readonly Dictionary<string, double> NumericValues = new();

    /// <summary>Current values of color / string properties (backgroundColor, color, …).</summary>
    internal readonly Dictionary<string, string> StringValues = new();

    /// <summary>
    /// Optional consumer rewrite of the composed transform string (see <see cref="BmTransformTemplate"/>).
    /// Every path that writes this element's transform goes through <see cref="ComposeTransform"/>,
    /// so the template can't be bypassed by one of them.
    /// </summary>
    internal BmTransformTemplate? TransformTemplate;

    /// <summary>
    /// Composes <paramref name="components"/> into a CSS transform string and applies the
    /// element's <see cref="TransformTemplate"/>, if any. A template that throws or returns null is
    /// ignored rather than allowed to break the frame - this runs inside the rAF tick, where an
    /// exception would evict the element from the engine.
    /// </summary>
    internal string ComposeTransform(Dictionary<string, double> components)
    {
        var generated = BmotionTransformComposer.Build(components);
        if (TransformTemplate is null) return generated;
        try { return TransformTemplate(components, generated) ?? generated; }
        catch { return generated; }
    }

    /// <summary>
    /// Number of live owners holding this element (a wrapping &lt;Bmotion&gt;, controllers, and
    /// in-flight AnimateAsync calls). The engine only tears the element down when this hits zero.
    /// </summary>
    internal int RefCount;

    /// <summary>Whether a <c>&lt;Bmotion&gt;</c> ever owned the element (as opposed to programmatic animations only).</summary>
    internal bool OwnedByComponent;

    /// <summary>When the engine retained this state after its last programmatic owner released it (eviction order).</summary>
    internal long RetainedAt;

    // ── Active animations ─────────────────────────────────────────────────────
    private readonly Dictionary<string, IBmotionAnimationDriver> _activeAnims = new();

    // ── Gesture layer stack ────────────────────────────────────────────────────
    // Highest first, as in motion.dev: a press shows over the hover it happens under, and both over a focus - which
    // a keyboard tap always has, so a focus ranked above the press would hide WhileTap from every keyboard user.
    private static readonly string[] GesturePriority = ["drag", "tap", "hover", "focus", "inview"];
    private readonly Dictionary<string, GestureLayer> _gestureLayers = new();
    // What each property a gesture took over showed before the first gesture touched it, for the
    // properties the resting target doesn't define (an Initial-only value, say): it is what they
    // come back to when the last gesture lets go, rather than an identity value the element never had.
    private readonly Dictionary<string, object> _gestureOrigins = new(StringComparer.Ordinal);
    private Dictionary<string, object?>? _baseValues;
    private BmotionTransitionConfig? _baseTransition;

    // ── Animation completion tracking ─────────────────────────────────────────
    // Each AnimateTo call that carries a TaskCompletionSource registers a "batch": the set of
    // property keys that call is animating. The batch resolves true only when every one of its
    // keys finishes naturally; it resolves false if any of its keys is superseded or cancelled.
    // Tracking per-batch (rather than a single per-element source) means overlapping animations on
    // different properties of the same element no longer resolve each other prematurely.
    private sealed class CompletionBatch
    {
        public required TaskCompletionSource<bool> Source { get; init; }
        public required HashSet<string> Keys { get; init; }
        public bool Interrupted { get; set; }
    }
    private readonly List<CompletionBatch> _batches = new();

    // ── Drag state ────────────────────────────────────────────────────────────
    private bool _isDragging;

    // ── Dirty flags for CSS build ─────────────────────────────────────────────
    private bool _transformDirty;
    private readonly HashSet<string> _dirtyProps = new();
    // Properties whose inline declaration is removed on the next flush - values handed back to the stylesheet.
    private readonly HashSet<string> _clearedProps = new();

    // ── Velocity (for retargeting a spring mid-flight) ────────────────────────
    // The last two values each numeric key showed, stamped with the element's clock: what a key's velocity is read
    // from when a new animation takes it over mid-flight.
    private readonly Dictionary<string, (double T0, double V0, double T1, double V1)> _samples = new(StringComparer.OrdinalIgnoreCase);
    // Velocities (units/s) an interrupted animation handed over, stamped with when (Environment.TickCount64): the
    // animation that replaces it inherits them, a much later one doesn't.
    private readonly Dictionary<string, (double Velocity, long AtMs)> _handoff = new(StringComparer.OrdinalIgnoreCase);
    private const long HandoffWindowMs = 100;

    // ── Values borrowed from the stylesheet ───────────────────────────────────
    // Keys whose value was read off the computed style rather than set by a target: a gesture that takes one hands
    // it back to the stylesheet when it lets go (see GestureRequest.StylesheetKeys).
    private readonly HashSet<string> _borrowed = new(StringComparer.Ordinal);
    // Gesture origins that were borrowed when the first gesture captured them.
    private readonly HashSet<string> _stylesheetOrigins = new(StringComparer.Ordinal);

    // Transform writes are paused while a WAAPI FLIP layout animation owns the element's
    // transform, so the rAF engine doesn't fight (and visually tear) the layout animation.
    // The window is measured against Tick timestamps so no extra timer/interop is needed.
    // Until when (Environment.TickCount64) a FLIP owns the transform. A wall-clock deadline fixed
    // when the FLIP starts: a window armed on the element's next tick would open whenever the element
    // next animated - possibly long after the FLIP ended - and freeze that animation instead.
    private long _transformSuspendUntil;

    // Reused across frames to avoid allocating a fresh CSS-update dictionary every rAF tick.
    // Safe because the synchronous JS interop marshals the returned dictionary before the next
    // Tick runs (single-threaded Blazor WASM), so the buffer is never read after it is cleared.
    private readonly Dictionary<string, string> _updateBuffer = new();

    public bool HasActiveAnimations => _activeAnims.Count > 0 || _isDragging;

    // Read-only diagnostics surface (consumed by BmotionInspector via the engine).
    internal int ActiveDriverCount => _activeAnims.Count;
    internal IReadOnlyCollection<string> ActiveDriverKeys => _activeAnims.Keys;
    internal bool IsDragging => _isDragging;

    /// <summary>
    /// Optional per-frame callback invoked with the CSS declarations flushed this frame
    /// (the <c>Bmotion.OnUpdate</c> parameter).
    /// </summary>
    internal Action<IReadOnlyDictionary<string, string>>? OnFrame;

    // ── Playback clock ────────────────────────────────────────────────────────
    // Drivers anchor to the timestamps they receive, so pausing / changing speed is implemented
    // by feeding them a virtual clock that advances at PlaybackRate × real time. Rate 0 = paused
    // (the clock freezes, drivers hold their current values); rate 2 = twice as fast.
    private double _clock;
    private double _lastRealTs = -1;

    /// <summary>Playback rate for this element's animations. 1 = realtime, 0 = paused.</summary>
    internal double PlaybackRate = 1;

    /// <summary>
    /// When set (Blazor Server / no synchronous interop), every animation collapses to a
    /// zero-duration tween so a single flush tick settles the element on its target values.
    /// </summary>
    internal bool ForceInstant;

    /// <summary>
    /// When set (a fork recording an animation, see <see cref="BmotionFrameRecorder"/>), Tick advances the drivers but
    /// writes nothing: the recorder reads the values off the state and composes only the frames it keeps.
    /// </summary>
    internal bool Silent;

    // ═══════════════════════════════════════════════════════════════════════════
    // Tick - called every rAF frame
    // ═══════════════════════════════════════════════════════════════════════════

    public Dictionary<string, string>? Tick(double timestamp)
    {
        // Nothing to do only when there are no drivers, no drag, and no pending
        // instant (SetInstant) changes or released declarations still waiting to be emitted.
        if (_activeAnims.Count == 0 && !_isDragging && !_transformDirty && _dirtyProps.Count == 0 && _clearedProps.Count == 0)
            return null;

        if (_isDragging) _transformDirty = true; // drag always refreshes transform

        // Advance the element's virtual playback clock. At rate 1 it tracks the real timestamps
        // exactly; at rate 0 it freezes (pause); other rates scale elapsed time.
        if (_lastRealTs < 0) _clock = timestamp;
        else _clock += (timestamp - _lastRealTs) * PlaybackRate;
        _lastRealTs = timestamp;

        // Advance all drivers. Iterate over a snapshot because driver.Tick can invoke a user
        // OnUpdate callback that re-enters and mutates _activeAnims (e.g. starts/cancels an
        // animation on this same element), which would otherwise corrupt the enumeration.
        List<string>? completed = null;
        foreach (var (key, driver) in _activeAnims.ToArray())
        {
            // The driver may have been removed - or replaced with a different driver - by a
            // re-entrant callback earlier in this loop. Skip unless the live driver for this key
            // is still the exact one captured in the snapshot, so a stale driver can't tick and
            // later evict the replacement at the removal step below.
            if (!_activeAnims.TryGetValue(key, out var current) || !ReferenceEquals(current, driver)) continue;
            if (driver.Tick(_clock))
                (completed ??= new List<string>()).Add(key);
        }

        if (completed != null)
            foreach (var key in completed)
            {
                _activeAnims.Remove(key);
                SettleAuto(key);
                NotePropFinished(key, interrupted: false); // natural completion
            }

        if (Silent)
        {
            _transformDirty = false;
            _dirtyProps.Clear();
            _clearedProps.Clear();
            return null;
        }

        if (!_transformDirty && _dirtyProps.Count == 0 && _clearedProps.Count == 0) return null;

        // ── Build CSS style update dict (reused buffer) ────────────────────────
        var updates = _updateBuffer;
        updates.Clear();

        // An empty value removes the inline declaration, handing the property back to the stylesheet.
        foreach (var prop in _clearedProps) updates[prop] = string.Empty;
        _clearedProps.Clear();

        // While a FLIP layout animation owns the transform, hold transform writes back so the two
        // animators don't fight; the dirty flag is preserved so the latest transform is flushed
        // the moment the suspension window ends.
        bool transformSuspended = IsTransformSuspended(timestamp);
        if (_transformDirty && !transformSuspended)
            updates["transform"] = ComposeTransform(Transforms);

        foreach (var prop in _dirtyProps)
        {
            if (prop is "pathLength" or "pathSpacing")
            {
                // Compose strokeDasharray from the normalized pathLength + pathSpacing pair.
                double len = Math.Clamp(NumericValues.GetValueOrDefault("pathLength", 1.0), 0, 1);
                double spacing = NumericValues.GetValueOrDefault("pathSpacing", 1.0);
                double offset = NumericValues.GetValueOrDefault("pathOffset", 0.0);
                updates["strokeDasharray"] = BmotionCssFormat.Num(len) + " " + BmotionCssFormat.Num(spacing);
                // Offset combines the "draw from end" baseline (1 - len) with any explicit pathOffset.
                updates["strokeDashoffset"] = BmotionCssFormat.Num(1 - len - offset);
            }
            else if (prop == "pathOffset")
            {
                double len = Math.Clamp(NumericValues.GetValueOrDefault("pathLength", 1.0), 0, 1);
                double offset = NumericValues.GetValueOrDefault("pathOffset", 0.0);
                updates["strokeDashoffset"] = BmotionCssFormat.Num(1 - len - offset);
            }
            else if (prop.StartsWith("--"))
            {
                if (NumericValues.TryGetValue(prop, out double nv))
                    updates[prop] = BmotionCssFormat.Num(nv);
                else if (StringValues.TryGetValue(prop, out string? sv))
                    updates[prop] = sv;
            }
            else if (NumericValues.TryGetValue(prop, out double numVal))
            {
                updates[prop] = BmotionCssFormat.Num(numVal);
            }
            else if (StringValues.TryGetValue(prop, out string? strVal))
            {
                updates[prop] = strVal;
                _showsAuto?.Remove(prop);
            }
        }

        // Reset dirty flags now that this frame's changes have been emitted. The transform flag is
        // kept set while suspended so the pending transform flushes once the FLIP window ends.
        if (!transformSuspended) _transformDirty = false;
        _dirtyProps.Clear();

        if (updates.Count > 0 && OnFrame is { } onFrame)
        {
            // Guard the user callback: it runs inside the rAF tick, where an exception would
            // otherwise evict this element from the engine (see ComputeFrame's fault handling).
            try { onFrame(updates); } catch { /* user callback failures must not break the loop */ }
        }

        return updates.Count > 0 ? updates : null;
    }

    // ── Transform suspension (used by FLIP layout animations) ─────────────────

    /// <summary>Pause rAF transform writes for <paramref name="durationMs"/> (measured from the next tick).</summary>
    internal void SuspendTransformWrites(double durationMs)
    {
        if (!(durationMs > 0)) return;
        _transformSuspendUntil = Environment.TickCount64 + (long)Math.Ceiling(durationMs);
    }

    private bool IsTransformSuspended(double timestamp)
    {
        if (_transformSuspendUntil == 0) return false;
        if (Environment.TickCount64 < _transformSuspendUntil) return true;
        // Window elapsed - clear so transforms resume.
        _transformSuspendUntil = 0;
        return false;
    }

    /// <summary>
    /// Builds a full snapshot of the element's current CSS (transform + numeric + string + path
    /// values), regardless of dirty flags. Used to re-flush live styles to the DOM after a Blazor
    /// re-render rewrites the element's <c>style</c> attribute.
    /// </summary>
    internal Dictionary<string, string>? BuildSnapshotStyles()
    {
        var d = new Dictionary<string, string>();

        if (Transforms.Count > 0)
        {
            var tr = ComposeTransform(Transforms);
            if (!string.IsNullOrEmpty(tr)) d["transform"] = tr;
        }

        bool hasPath = NumericValues.ContainsKey("pathLength")
                    || NumericValues.ContainsKey("pathSpacing")
                    || NumericValues.ContainsKey("pathOffset");
        if (hasPath)
        {
            double len = Math.Clamp(NumericValues.GetValueOrDefault("pathLength", 1.0), 0, 1);
            double spacing = NumericValues.GetValueOrDefault("pathSpacing", 1.0);
            double offset = NumericValues.GetValueOrDefault("pathOffset", 0.0);
            d["strokeDasharray"] = BmotionCssFormat.Num(len) + " " + BmotionCssFormat.Num(spacing);
            d["strokeDashoffset"] = BmotionCssFormat.Num(1 - len - offset);
        }

        foreach (var (prop, value) in NumericValues)
        {
            if (prop is "pathLength" or "pathSpacing" or "pathOffset") continue;
            d[prop] = BmotionCssFormat.Num(value);
        }
        foreach (var (prop, value) in StringValues)
            d[prop] = value;

        return d.Count > 0 ? d : null;
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // Animation control
    // ═══════════════════════════════════════════════════════════════════════════

    public void AnimateTo(
        Dictionary<string, object?> values,
        BmotionTransitionConfig? transition,
        TaskCompletionSource<bool>? completionSource = null)
    {
        // Cheap scan for any non-null target (no allocation).
        bool any = false;
        foreach (var v in values.Values)
            if (v != null) { any = true; break; }
        if (!any) { completionSource?.TrySetResult(true); return; }

        // Without a frame loop an inertia can't coast, but it still has a resting place: land there
        // at once (snapped and clamped) rather than staying wherever it was released.
        if (ForceInstant) values = ProjectInertiaRests(values, transition);

        // Track which keys actually get an active driver so the completion batch only waits on
        // animations that can finish (keys that snap instantly resolve immediately).
        HashSet<string>? driverKeys = completionSource != null ? new HashSet<string>() : null;
        int activeBefore;

        // An arc couples x and y, so it has to be resolved before the per-key loop and those keys
        // then skipped - two independent drivers can only describe the straight line between the
        // endpoints. Returns the keys the arc took ownership of.
        var arcKeys = TryCreateArcDriver(values, transition, driverKeys);

        foreach (var (key, value) in values)
        {
            if (value == null) continue;
            if (arcKeys is not null && arcKeys.Contains(key)) continue;
            var perKey = transition?.Properties?.GetValueOrDefault(key) ?? transition ?? new BmotionTransitionConfig();
            // Blazor Server fallback: no rAF loop exists, so every animation must settle in one
            // flush tick. Collapse to a zero-duration tween, but keep the (possibly per-key)
            // OnUpdate callback so motion-value tracking still observes the snap.
            if (ForceInstant)
                perKey = new BmotionTransitionConfig
                    { Type = BmotionTransitionType.Tween, Duration = 0, Delay = 0, OnUpdate = perKey.OnUpdate };
            // Superseding an in-flight driver for this key interrupts any completion batch that
            // owned it (resolves that batch with false once its remaining keys settle).
            CancelProp(key);
            activeBefore = _activeAnims.Count;

            if (TryGetDoubleArray(value, out double[]? doubleFrames))
            {
                // Bm.Current wildcard frames (NaN) resolve to the element's current value now,
                // at animation start, so interrupted animations continue from where they are.
                doubleFrames = ResolveWildcardFrames(key, doubleFrames!);
                // Keyframe drivers require at least two frames (they build n-1 segments and
                // divide by n-1 when distributing times). Degenerate arrays would otherwise
                // throw and (via ComputeFrame) stall the whole loop, so handle them here:
                //   0 frames -> nothing to do; 1 frame -> snap to that single value.
                if (doubleFrames.Length >= 2)
                    CreateNumericKeyframesDriver(key, doubleFrames, perKey);
                else if (doubleFrames.Length == 1)
                    CreateNumericDriver(key, doubleFrames[0], perKey);
            }
            else if (IsColorProp(key) && TryGetStringArray(value, out string[]? strFrames))
            {
                if (strFrames!.Length >= 2)
                    CreateColorKeyframesDriver(key, strFrames, perKey);
                else if (strFrames.Length == 1)
                    CreateColorDriver(key, strFrames[0], perKey);
            }
            else if (IsColorProp(key) && value is string colorStr)
                CreateColorDriver(key, colorStr, perKey);
            else if (value is string dimStr)
                CreateCssDimensionDriver(key, dimStr, perKey);
            else if (TryGetStringArray(value, out string[]? otherFrames) && otherFrames!.Length > 0)
            {
                // Non-colour string keyframes ("100px" → "50%" arrays, filter sequences, …):
                // animate through per-segment string mixers when every adjacent pair has a
                // matching shape; otherwise snap to the final frame so the value still lands.
                if (otherFrames.Length >= 2 && TryCreateStringKeyframesDriver(key, otherFrames, perKey))
                {
                    // driver registered by the helper
                }
                else
                {
                    StringValues[key] = otherFrames[^1];
                    NumericValues.Remove(key); // keep numeric/string stores mutually exclusive
                    _dirtyProps.Add(key);
                }
            }
            else if (TryConvertToDouble(value, out double numeric))
                CreateNumericDriver(key, numeric, perKey);
            // else: an unconvertible value (e.g. an arbitrary object in a user Keyframes dict) is
            // skipped rather than throwing - a bad value can't take down the init / event path.

            // Record the key if this iteration created a live driver for the completion batch.
            if (driverKeys != null && _activeAnims.Count > activeBefore && _activeAnims.ContainsKey(key))
                driverKeys.Add(key);
        }

        // Register the completion batch (if any): it resolves once every key that got a driver
        // finishes. If nothing animates (all snapped instantly), resolve immediately.
        if (completionSource != null)
        {
            if (driverKeys is { Count: > 0 })
                _batches.Add(new CompletionBatch { Source = completionSource, Keys = driverKeys });
            else
                completionSource.TrySetResult(true);
        }
    }

    public void SetInstant(Dictionary<string, object?> values)
    {
        foreach (var (key, value) in values)
        {
            if (value == null) continue;
            // Cancel any in-flight driver for this property so the instant value is authoritative
            // and isn't overwritten on the next tick by an ongoing animation.
            CancelProp(key);
            ForgetAuto(key);
            if (BmotionTransformComposer.IsTransformProp(key))
            {
                if (TryConvertToDouble(value, out double tv))
                {
                    Transforms[key] = tv;
                    _transformDirty = true;
                }
            }
            else if (IsColorProp(key) && value is string colorStr)
            {
                StringValues[key] = colorStr;
                NumericValues.Remove(key); // keep numeric/string stores mutually exclusive
                _dirtyProps.Add(key);
            }
            else if (value is string dimStr)
            {
                StringValues[key] = dimStr;
                NumericValues.Remove(key); // keep numeric/string stores mutually exclusive
                _dirtyProps.Add(key);
            }
            else if (TryConvertToDouble(value, out double nv))
            {
                NumericValues[key] = nv;
                StringValues.Remove(key); // keep numeric/string stores mutually exclusive
                _dirtyProps.Add(key);
            }
        }
    }

    public void Cancel(string[]? properties)
    {
        if (properties == null || properties.Length == 0)
            CancelAll();
        else
        {
            // CancelProp interrupts any completion batch that owns the property, so callers of
            // AnimateToAwaitAsync resolve (with false) instead of hanging forever.
            foreach (var p in properties)
                CancelProp(p);
        }
    }

    internal void CancelAll()
    {
        foreach (var driver in _activeAnims.Values)
            driver.Cancel();
        _activeAnims.Clear();
        _autoFinals?.Clear();
        ResolveAllBatches(false); // cancelled, not completed
    }

    /// <summary>
    /// Finish all running animations immediately, snapping every property to its target
    /// (end) value. Unlike <see cref="CancelAll"/> (which freezes in place), this applies
    /// the final frame so the element settles on the destination state.
    /// </summary>
    internal void CompleteAll()
    {
        // Snapshot the drivers before iterating: driver.Complete() applies the final value, which
        // can invoke a user OnUpdate callback that re-enters and mutates _activeAnims/_batches
        // (e.g. starts a new animation on this element). Only finish the animations captured here
        // and let NotePropFinished resolve their batches - blanket-clearing the live collections
        // would wipe (and prematurely resolve) any re-entrant animations the callbacks started.
        foreach (var (key, driver) in _activeAnims.ToArray())
        {
            // Skip if a re-entrant callback earlier in this loop already removed or replaced the
            // driver for this key, so a stale driver can't evict its replacement below.
            if (!_activeAnims.TryGetValue(key, out var current) || !ReferenceEquals(current, driver)) continue;
            driver.Complete();
            // Re-check: Complete()'s callback may itself have removed/replaced this key.
            if (_activeAnims.TryGetValue(key, out current) && ReferenceEquals(current, driver))
            {
                _activeAnims.Remove(key);
                SettleAuto(key);
                NotePropFinished(key, interrupted: false); // snapped to end value = natural completion
            }
        }
    }

    internal void CancelProp(string key)
    {
        if (_activeAnims.TryGetValue(key, out var driver))
        {
            ForgetAuto(key); // it stops short of the measured size, or something else takes it from there
            // Whatever replaces this animation (a spring retargeted mid-flight) carries on with its velocity.
            if (TrackedVelocity(key) is double velocity and not 0) _handoff[key] = (velocity, Environment.TickCount64);
            driver.Cancel();
            _activeAnims.Remove(key);
            NotePropFinished(key, interrupted: true); // cancelled / superseded
        }
    }

    // ── Completion-batch bookkeeping ──────────────────────────────────────────

    /// <summary>
    /// Records that <paramref name="key"/> is no longer animating. Removes it from any pending
    /// completion batch; a batch resolves once all its keys are gone (true only if none of them
    /// were interrupted - i.e. every key finished naturally).
    /// </summary>
    private void NotePropFinished(string key, bool interrupted)
    {
        for (int i = _batches.Count - 1; i >= 0; i--)
        {
            var b = _batches[i];
            if (!b.Keys.Remove(key)) continue;
            if (interrupted) b.Interrupted = true;
            if (b.Keys.Count == 0)
            {
                b.Source.TrySetResult(!b.Interrupted);
                _batches.RemoveAt(i);
            }
        }
    }

    private void ResolveAllBatches(bool result)
    {
        foreach (var b in _batches)
            b.Source.TrySetResult(result);
        _batches.Clear();
    }

    /// <summary>
    /// Replaces <see cref="Bm.Current"/> wildcard frames (NaN) with the element's current value
    /// for the property. Returns the original array untouched when no wildcard is present.
    /// </summary>
    private double[] ResolveWildcardFrames(string key, double[] frames)
    {
        bool hasWildcard = false;
        foreach (var f in frames)
            if (double.IsNaN(f)) { hasWildcard = true; break; }
        if (!hasWildcard) return frames;

        double current = BmotionTransformComposer.IsTransformProp(key)
            ? Transforms.GetValueOrDefault(key, DefaultTransformValue(key))
            : NumericValues.GetValueOrDefault(key, DefaultNumericValue(key));

        // Clone: the source array may be caller-owned (or reused by a repeating target).
        var resolved = (double[])frames.Clone();
        for (int i = 0; i < resolved.Length; i++)
            if (double.IsNaN(resolved[i])) resolved[i] = current;
        return resolved;
    }

    private static bool TryConvertToDouble(object value, out double result)
    {
        try
        {
            result = Convert.ToDouble(value, System.Globalization.CultureInfo.InvariantCulture);
            // Convert.ToDouble happily returns NaN/±Infinity for those inputs; reject them so
            // non-finite values never propagate into state, driver math or CSS output.
            if (!double.IsFinite(result))
            {
                result = 0;
                return false;
            }
            return true;
        }
        catch (Exception e) when (e is FormatException or InvalidCastException or OverflowException)
        {
            result = 0;
            return false;
        }
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // Gesture layer management
    // ═══════════════════════════════════════════════════════════════════════════

    public void SetBaseAnimation(Dictionary<string, object?> values, BmotionTransitionConfig? transition)
    {
        _baseValues = values;
        _baseTransition = transition;
        // A resting target owns what it names: nothing it sets goes back to the stylesheet.
        Claim(values.Keys);
    }

    /// <summary>
    /// An animation the state asks the engine to play - a gesture taking its properties, or letting them go.
    /// <paramref name="StylesheetKeys"/> are properties going back to a value borrowed from the stylesheet: the
    /// engine re-reads what the stylesheet gives them now as their target, and hands them back to it (removing
    /// their inline declaration) once the animation has finished.
    /// </summary>
    internal sealed record AnimationRequest(
        Dictionary<string, object?> Values, BmotionTransitionConfig? Transition, IReadOnlyCollection<string>? StylesheetKeys = null);

    /// <summary>
    /// Activates a gesture layer and returns the animation that applies it, or <c>null</c> when a higher-priority
    /// gesture holds the element (the layer applies once that one lets go).
    /// </summary>
    public AnimationRequest? ActivateGestureLayer(string gesture, Dictionary<string, object?> values, BmotionTransitionConfig? transition)
    {
        var added = new GestureLayer(values, transition);
        _gestureLayers[gesture] = added;
        foreach (var key in values.Keys)
        {
            if (_baseValues?.ContainsKey(key) == true || _gestureOrigins.ContainsKey(key) || _stylesheetOrigins.Contains(key)) continue;
            // A value nothing has set yet is the stylesheet's: the gesture gives it back to the stylesheet (whatever
            // it says by then) rather than to a captured copy.
            if (CurrentValue(key) is not { } origin)
            {
                if (!BmotionTransformComposer.IsTransformProp(key) && !IsSyntheticKey(key)) _stylesheetOrigins.Add(key);
                continue;
            }
            _gestureOrigins[key] = origin;
            if (_borrowed.Contains(key)) _stylesheetOrigins.Add(key);
        }

        // Respect gesture priority: don't let a lower-priority gesture animate over a
        // higher-priority one that is already active (mirrors DeactivateGestureLayer).
        int newPriority = Array.IndexOf(GesturePriority, gesture);
        if (newPriority >= 0)
        {
            foreach (var other in _gestureLayers.Keys)
            {
                if (other == gesture) continue;
                int otherPriority = Array.IndexOf(GesturePriority, other);
                if (otherPriority >= 0 && otherPriority < newPriority) return null; // higher-priority layer wins
            }
        }

        added.Applied = true;
        return new AnimationRequest(values, transition);
    }

    /// <summary>Deactivates a gesture layer and returns the animation back to what remains, if anything moves.</summary>
    public AnimationRequest? DeactivateGestureLayer(string gesture)
    {
        if (!_gestureLayers.Remove(gesture, out var removed))
            return null;

        // Only what the gesture changed moves back (motion.dev semantics): the properties the removed
        // layer set, plus those of any remaining layer a higher-priority one had kept from applying.
        // Everything else is already where it belongs - re-running it would restart an in-flight
        // enter animation (and lose its completion) or replay a keyframe sequence from frame one.
        var keys = new HashSet<string>(removed.Values.Keys, StringComparer.Ordinal);
        foreach (var layer in _gestureLayers.Values)
        {
            if (layer.Applied) continue;
            keys.UnionWith(layer.Values.Keys);
        }

        // Build the target the element should revert to: the base animation (each sequence at its
        // resting frame) overlaid with every still-active gesture layer (lowest priority first so
        // higher priority wins).
        var target = new Dictionary<string, object?>();
        // Settling on a resting frame plays once: a finite repeat would replay the way back.
        BmotionTransitionConfig? transition = _baseTransition;
        if (transition is { IsInfiniteRepeat: false, Repeat: > 0 })
        {
            transition = transition.Clone();
            transition.Repeat = 0;
            transition.RepeatDelay = 0;
        }

        if (_baseValues != null)
            foreach (var kv in _baseValues)
                if (keys.Contains(kv.Key))
                    target[kv.Key] = RestingValue(kv.Key, kv.Value, _baseTransition);

        for (int i = GesturePriority.Length - 1; i >= 0; i--)
        {
            if (_gestureLayers.TryGetValue(GesturePriority[i], out var layer))
            {
                foreach (var kv in layer.Values)
                    if (keys.Contains(kv.Key))
                        target[kv.Key] = kv.Value;
                layer.Applied = true;
                transition = layer.Transition; // highest-priority remaining layer wins the transition
            }
        }

        // Any property the removed layer set but no remaining layer/base defines must animate
        // back to what it showed before the gesture - or, when that was never known, its identity
        // value - otherwise it would stay stuck at the gesture value. One the stylesheet gave it goes
        // back to the stylesheet (the origin is only the fallback if the stylesheet can't be read).
        HashSet<string>? stylesheet = null;
        foreach (var key in removed.Values.Keys)
        {
            if (target.ContainsKey(key)) continue;
            if (_stylesheetOrigins.Remove(key))
            {
                // The engine fills in what the stylesheet gives the key now; the captured origin (if any) is only
                // the fallback for when that can't be read.
                target[key] = _gestureOrigins.Remove(key, out var fallback) ? fallback : null;
                (stylesheet ??= new HashSet<string>(StringComparer.Ordinal)).Add(key);
            }
            else if (_gestureOrigins.Remove(key, out var origin))
                target[key] = origin;
            else if (BmotionTransformComposer.IsTransformProp(key))
                target[key] = DefaultTransformValue(key);
            else if (!IsColorProp(key)) // colours have no safe identity to revert to
                target[key] = DefaultNumericValue(key);
        }

        return target.Count > 0 || stylesheet is not null ? new AnimationRequest(target, transition, stylesheet) : null;
    }

    /// <summary>Whether any gesture layer is still active for <paramref name="key"/>.</summary>
    internal bool GestureHolds(string key)
    {
        foreach (var layer in _gestureLayers.Values)
            if (layer.Values.ContainsKey(key)) return true;
        return false;
    }

    // ── Values borrowed from the stylesheet ───────────────────────────────────

    // Path drawing is composed from these into stroke-dash* - they have no computed value of their own to read.
    private static bool IsSyntheticKey(string key) => key is "pathLength" or "pathOffset" or "pathSpacing";

    /// <summary>
    /// The keys among <paramref name="keys"/> whose starting value the engine has never seen - neither a target
    /// set them nor did it read them - so an animation of them must first read them off the computed style rather
    /// than start from a guess. Transform components are left out: the engine owns the element's transform.
    /// </summary>
    internal List<string>? UnknownKeys(IEnumerable<string> keys)
    {
        List<string>? unknown = null;
        foreach (var key in keys)
        {
            if (BmotionTransformComposer.IsTransformProp(key) || IsSyntheticKey(key)) continue;
            if (CurrentValue(key) is null) (unknown ??= new List<string>()).Add(key);
        }
        return unknown;
    }

    /// <summary>
    /// Seeds the value the stylesheet gives <paramref name="key"/> - read off the computed style - as its current
    /// value, without writing it (the element already shows it). The key stays borrowed until a target claims it.
    /// </summary>
    internal void SeedFromStylesheet(string key, string value)
    {
        if (CurrentValue(key) is not null || string.IsNullOrWhiteSpace(value)) return;
        if (key == "opacity")
        {
            if (!BmotionCssFormat.TryParse(value, out double number)) return;
            NumericValues[key] = number;
        }
        else
        {
            StringValues[key] = value.Trim();
        }
        _borrowed.Add(key);
    }

    /// <summary>Marks <paramref name="keys"/> as set by a target rather than borrowed from the stylesheet.</summary>
    internal void Claim(IEnumerable<string> keys)
    {
        if (_borrowed.Count == 0) return;
        foreach (var key in keys) _borrowed.Remove(key);
    }

    /// <summary>
    /// Hands <paramref name="keys"/> back to the stylesheet once a gesture has let go of them: the engine forgets
    /// its value (to read it afresh next time) and the inline declaration is removed on the next flush, so a later
    /// stylesheet change applies. A key something has taken again in the meantime is left alone.
    /// </summary>
    internal bool ReleaseToStylesheet(IEnumerable<string> keys)
    {
        bool released = false;
        foreach (var key in keys)
        {
            if (_activeAnims.ContainsKey(key) || GestureHolds(key) || PlanOwns(key)) continue;
            if (_baseValues?.ContainsKey(key) == true) continue;
            StringValues.Remove(key);
            NumericValues.Remove(key);
            _borrowed.Remove(key);
            _dirtyProps.Remove(key);
            _clearedProps.Add(key);
            released = true;
        }
        return released;
    }

    // ── Sizes that land on "auto" ─────────────────────────────────────────────

    // The width/height keys whose running animation plays to a measured natural size and then lands on "auto"
    // itself, so the element follows its content again (as motion.dev does).
    private HashSet<string>? _autoFinals;

    /// <summary>Whether <paramref name="key"/> is a size that can animate to and from <c>"auto"</c>.</summary>
    internal static bool IsAutoSizeKey(string key) => key is "width" or "height";

    /// <summary>The <paramref name="keys"/> that are <c>"auto"</c> now: their size has to be measured to animate from it.</summary>
    internal List<string>? AutoKeys(IEnumerable<string> keys)
    {
        List<string>? auto = null;
        foreach (var key in keys)
            if (IsAutoSizeKey(key) && StringValues.TryGetValue(key, out var value) && IsAuto(value))
                (auto ??= new List<string>()).Add(key);
        return auto;
    }

    internal static bool IsAuto(string? value) => string.Equals(value?.Trim(), "auto", StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Takes the measured size of a key that is <c>"auto"</c> as its current value, without writing it (the element
    /// already shows it), so an animation from it starts where the element is.
    /// </summary>
    internal void SeedMeasured(string key, string value)
    {
        if (!TryParseCssDimension(value.Trim(), out _, out _)) return;
        StringValues[key] = value.Trim();
        NumericValues.Remove(key);
        (_showsAuto ??= new HashSet<string>(StringComparer.Ordinal)).Add(key);
    }

    // Sizes the page still shows as "auto" although the state holds their measured size: a browser animation can't
    // start from what the page shows there (auto doesn't interpolate), so it is given the measured start.
    private HashSet<string>? _showsAuto;

    /// <summary>Whether the page shows <paramref name="key"/> as <c>"auto"</c> while the state holds its measured size.</summary>
    internal bool ShowsAuto(string key) => _showsAuto?.Contains(key) == true;

    /// <summary>The page is being given a value for <paramref name="key"/> (a browser animation of it started).</summary>
    internal void StartWritten(string key) => _showsAuto?.Remove(key);

    /// <summary>Lands <paramref name="key"/> on <c>"auto"</c> once its running animation arrives at the measured size.</summary>
    internal void LandOnAuto(string key) => (_autoFinals ??= new HashSet<string>(StringComparer.Ordinal)).Add(key);

    /// <summary>Forgets that <paramref name="key"/> was to land on <c>"auto"</c>: something else has taken it.</summary>
    internal void ForgetAuto(string key) => _autoFinals?.Remove(key);

    private void ForgetAuto(IEnumerable<string> keys)
    {
        if (_autoFinals is null) return;
        foreach (var key in keys) _autoFinals.Remove(key);
    }

    // The animation of key arrived: when it was played to a natural size, "auto" is written over the measured one.
    private bool SettleAuto(string key)
    {
        if (_autoFinals?.Remove(key) != true) return false;
        StringValues[key] = "auto";
        NumericValues.Remove(key);
        _dirtyProps.Add(key);
        return true;
    }

    /// <summary>
    /// A copy of this element's values and pending velocity handoffs - the state an animation is recorded against,
    /// tick by tick, without touching the element itself (see <see cref="BmotionFrameRecorder"/>).
    /// </summary>
    internal BmotionElementAnimationState ForkForRecording()
    {
        var fork = new BmotionElementAnimationState { TransformTemplate = TransformTemplate };
        foreach (var (key, value) in Transforms) fork.Transforms[key] = value;
        foreach (var (key, value) in NumericValues) fork.NumericValues[key] = value;
        foreach (var (key, value) in StringValues) fork.StringValues[key] = value;
        foreach (var (key, value) in _handoff) fork._handoff[key] = value;
        return fork;
    }

    /// <summary>The value the element shows for <paramref name="key"/> now, when the engine knows it.</summary>
    internal object? CurrentValue(string key)
    {
        if (BmotionTransformComposer.IsTransformProp(key))
            return Transforms.TryGetValue(key, out double t) ? t : DefaultTransformValue(key);
        if (NumericValues.TryGetValue(key, out double n)) return n;
        if (StringValues.TryGetValue(key, out string? s)) return s;
        return null;
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // Drag position (updated synchronously from JS pointer events)
    // ═══════════════════════════════════════════════════════════════════════════

    public void SetDragPosition(double x, double y)
    {
        Transforms["x"] = x;
        Transforms["y"] = y;
        _isDragging = true;
        _transformDirty = true;
    }

    /// <summary>A drag starts: it owns the transform from the press on, before its first move arrives.</summary>
    public void BeginDrag()
    {
        _isDragging = true;
        _transformDirty = true;
        // A layout animation that paused the transform writes has been cut short by the press (the bridge ends it):
        // the drag's writes must reach the element from the first move, not once the FLIP would have ended.
        _transformSuspendUntil = 0;
    }

    public void EndDrag() => _isDragging = false;

    public (double x, double y) GetCurrentXY()
        => (Transforms.GetValueOrDefault("x"), Transforms.GetValueOrDefault("y"));

    // ═══════════════════════════════════════════════════════════════════════════
    // Driver factory helpers
    // ═══════════════════════════════════════════════════════════════════════════

    // The keys an arc owns, registered under each so a later animation on x (or y) supersedes the
    // whole arc rather than leaving half of it running.
    private static readonly string[] _arcKeys = ["x", "y"];
    private static readonly string[] _arcKeysWithRotation = ["x", "y", "rotate"];

    /// <summary>
    /// Creates the coupled arc driver when this target qualifies, returning the keys it claimed
    /// (or <c>null</c> when the animation should take the ordinary straight-line path).
    /// <para>
    /// It qualifies only when a path is configured and both <c>x</c> and <c>y</c> are present as
    /// single finite numbers: a curve is defined by two endpoints, so a keyframe sequence on either
    /// axis is already describing its own path and must be left alone.
    /// </para>
    /// </summary>
    private HashSet<string>? TryCreateArcDriver(
        Dictionary<string, object?> values, BmotionTransitionConfig? transition, HashSet<string>? driverKeys)
    {
        if (transition?.Path is not { } arc) return null;
        // Blazor Server has no frame loop, so everything snaps; there is no journey to curve.
        if (ForceInstant) return null;
        // Same predicate the compositor offload consults, so the two can never disagree about
        // whether this animation is curved.
        if (!BmotionArcTargets.Applies(values)) return null;
        if (!TryConvertToDouble(values["x"]!, out double toX) || !TryConvertToDouble(values["y"]!, out double toY))
            return null;

        double fromX = Transforms.GetValueOrDefault("x", DefaultTransformValue("x"));
        double fromY = Transforms.GetValueOrDefault("y", DefaultTransformValue("y"));
        var curve = arc.BuildCurve(fromX, fromY, toX, toY);
        bool rotates = arc.FollowsTangent;

        // Supersede anything already animating the keys the arc is about to own, exactly as the
        // per-key path does - including a rotate the arc is taking over.
        var claimed = new HashSet<string>(rotates ? _arcKeysWithRotation : _arcKeys, StringComparer.Ordinal);
        foreach (var key in claimed) CancelProp(key);

        var driver = new BmotionArcDriver(curve, transition, (x, y, rotation) =>
        {
            ApplyTransform("x", x);
            ApplyTransform("y", y);
            if (rotation.HasValue) ApplyTransform("rotate", rotation.Value);
        });

        // One driver instance registered under every key it owns: cancelling or superseding any one
        // of them tears down the whole arc, which is the only coherent outcome for a coupled motion.
        foreach (var key in claimed)
        {
            _activeAnims[key] = driver;
            driverKeys?.Add(key);
        }
        return claimed;
    }

    private void CreateNumericDriver(string key, double toValue, BmotionTransitionConfig config)
    {
        bool isTransform = BmotionTransformComposer.IsTransformProp(key);
        double from = isTransform
            ? Transforms.GetValueOrDefault(key, DefaultTransformValue(key))
            : NumericValues.GetValueOrDefault(key, DefaultNumericValue(key));

        Action<double> apply = isTransform
            ? v => ApplyTransform(key, v)
            : v => ApplyNumeric(key, v);

        // Wire the optional per-frame OnUpdate callback (single-value numeric animations).
        if (config.OnUpdate is { } onUpdate)
        {
            var inner = apply;
            apply = v => { inner(v); onUpdate(v); };
        }

        config = WithInheritedVelocity(key, config);
        IBmotionAnimationDriver driver = config.Type switch
        {
            BmotionTransitionType.Spring  => new BmotionSpringDriver(from, toValue, config, apply),
            BmotionTransitionType.Inertia => new BmotionInertiaDriver(from, config, apply),
            _                      => new BmotionTweenDriver(from, toValue, config, apply),
        };

        _activeAnims[key] = driver;
    }

    private void CreateColorDriver(string key, string toValue, BmotionTransitionConfig config)
    {
        string from = StringValues.GetValueOrDefault(key, "rgba(0,0,0,0)");
        if (config.Type == BmotionTransitionType.Spring
            && BmotionColorInterpolator.Parse(from) is { } fromChannels
            && BmotionColorInterpolator.Parse(toValue) is { } toChannels)
        {
            // A spring shapes the colour's progress like any other value's (rather than falling back to a tween
            // of the spring's unrelated Duration); at rest it lands on the authored colour itself.
            var space = config.ColorSpace;
            _activeAnims[key] = new BmotionSpringDriver(0, 1, config, p =>
                ApplyString(key, p == 1 ? toValue : BmotionColorInterpolator.Lerp(fromChannels, toChannels, Math.Clamp(p, 0, 1), space)));
            return;
        }
        _activeAnims[key] = new BmotionColorTweenDriver(from, toValue, config, v => ApplyString(key, v));
    }

    private void CreateNumericKeyframesDriver(string key, double[] frames, BmotionTransitionConfig config)
    {
        bool isTransform = BmotionTransformComposer.IsTransformProp(key);
        Action<double> apply = isTransform
            ? v => ApplyTransform(key, v)
            : v => ApplyNumeric(key, v);
        _activeAnims[key] = new BmotionNumericKeyframesDriver(frames, config, apply);
    }

    private void CreateColorKeyframesDriver(string key, string[] frames, BmotionTransitionConfig config)
    {
        _activeAnims[key] = new BmotionColorKeyframesDriver(frames, config, v => ApplyString(key, v));
    }

    /// <summary>
    /// Registers a keyframes driver for generic string frames when every adjacent pair is
    /// mixable. The numeric keyframes driver runs an index track (0, 1, …, n-1) - inheriting
    /// times, per-segment eases and repeat behavior - and each in-between index value selects a
    /// segment mixer and its local progress.
    /// </summary>
    private bool TryCreateStringKeyframesDriver(string key, string[] frames, BmotionTransitionConfig config)
    {
        var mixes = new Func<double, string>[frames.Length - 1];
        for (int i = 0; i < frames.Length - 1; i++)
        {
            var (from, to) = BmotionStringMixer.MixEnds(key, frames[i], frames[i + 1]);
            if (BmotionStringMixer.TryCreateMix(from, to, key) is not { } mix)
                return false;
            mixes[i] = mix;
        }

        var indexTrack = new double[frames.Length];
        for (int i = 0; i < frames.Length; i++) indexTrack[i] = i;

        _activeAnims[key] = new BmotionNumericKeyframesDriver(indexTrack, config, v =>
        {
            // On a frame, the authored frame itself ("none" rather than the neutral values it plays through).
            if (v == Math.Floor(v) && v >= 0 && v <= frames.Length - 1)
            {
                ApplyString(key, frames[(int)v]);
                return;
            }
            int seg = Math.Clamp((int)Math.Floor(v), 0, mixes.Length - 1);
            ApplyString(key, mixes[seg](v - seg));
        });
        return true;
    }

    // ── Value apply callbacks (mark dirty) ────────────────────────────────────

    private void ApplyTransform(string key, double value)
    {
        Transforms[key] = value;
        _transformDirty = true;
        Sample(key, value);
    }

    private void ApplyNumeric(string key, double value)
    {
        NumericValues[key] = value;
        Sample(key, value);
        // Keep the numeric/string stores mutually exclusive: Tick emits a prop from NumericValues
        // first, so a stale string entry for the same key would otherwise be masked (and vice versa).
        StringValues.Remove(key);
        _dirtyProps.Add(key);
    }

    private void ApplyString(string key, string value)
    {
        StringValues[key] = value;
        // Mutually exclusive with NumericValues (see ApplyNumeric): a stale numeric entry for this
        // key would mask the string update during Tick emission.
        NumericValues.Remove(key);
        _dirtyProps.Add(key);
    }

    // ── Velocity ──────────────────────────────────────────────────────────────

    // Records a value a driver applied, on the element's clock, keeping the last two distinct instants.
    private void Sample(string key, double value)
    {
        if (_samples.TryGetValue(key, out var s))
            _samples[key] = _clock > s.T1 ? (s.T1, s.V1, _clock, value) : (s.T0, s.V0, s.T1, value);
        else
            _samples[key] = (_clock, value, _clock, value);
    }

    /// <summary>
    /// The velocity (units per second of animation time) of the animation driving <paramref name="key"/> now, from
    /// its last two frames; <c>null</c> when nothing animates the key.
    /// </summary>
    internal double? TrackedVelocity(string key)
    {
        if (!_activeAnims.ContainsKey(key) || !_samples.TryGetValue(key, out var s) || s.T1 <= s.T0) return null;
        double velocity = (s.V1 - s.V0) / (s.T1 - s.T0) * 1000;
        return double.IsFinite(velocity) ? velocity : null;
    }

    /// <summary>
    /// The velocity the animation <paramref name="key"/> was just taken from handed over - an interrupted driver or
    /// compositor animation - or 0 when there is none, or it was handed over too long ago to still be moving.
    /// </summary>
    internal double HandoffVelocity(string key)
        => _handoff.TryGetValue(key, out var h) && Environment.TickCount64 - h.AtMs <= HandoffWindowMs ? h.Velocity : 0;

    /// <summary>
    /// What a spring starting on <paramref name="key"/> plays with: an authored velocity wins; otherwise one that
    /// doesn't repeat inherits the velocity of the animation it replaces (motion.dev semantics), so retargeting a
    /// moving element bends its path instead of stopping it dead.
    /// </summary>
    internal BmotionTransitionConfig WithInheritedVelocity(string key, BmotionTransitionConfig config)
    {
        if (config.Type != BmotionTransitionType.Spring || config.Velocity != 0) return config;
        if (config.IsInfiniteRepeat || config.Repeat > 0) return config;
        double velocity = HandoffVelocity(key);
        if (velocity == 0) return config;
        var inherited = config.Clone();
        inherited.Velocity = velocity;
        return inherited;
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static readonly HashSet<string> _colorProps = new(StringComparer.OrdinalIgnoreCase)
    {
        "backgroundColor", "color", "borderColor", "outlineColor", "fill", "stroke",
        "caretColor", "columnRuleColor", "textDecorationColor",
    };

    internal static bool IsColorProp(string key)
        => _colorProps.Contains(key) || key.EndsWith("color", StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// The value an animation to <paramref name="value"/> comes to rest on: a keyframe sequence's last frame (a
    /// <see cref="Bm.Current"/> wildcard there is skipped), a single value itself. Null when there is none.
    /// </summary>
    internal static object? RestingValueOf(object value) => value switch
    {
        double[] numbers => numbers.LastOrDefault(double.IsFinite) is var last && numbers.Any(double.IsFinite) ? last : null,
        string[] strings => strings.Length > 0 ? strings[^1] : null,
        _ => value,
    };

    internal static double DefaultTransformValue(string key) =>
        key is "scale" or "scaleX" or "scaleY" ? 1.0 : 0.0;

    internal static double DefaultNumericValue(string key) =>
        key is "opacity" or "pathLength" ? 1.0 : 0.0;

    private static Dictionary<string, object?> ProjectInertiaRests(
        Dictionary<string, object?> values, BmotionTransitionConfig? transition)
    {
        Dictionary<string, object?>? projected = null;
        foreach (var (key, value) in values)
        {
            var perKey = transition?.Properties?.GetValueOrDefault(key) ?? transition;
            if (perKey is not { Type: BmotionTransitionType.Inertia } || value is null) continue;
            if (!TryConvertToDouble(value, out double from)) continue;
            projected ??= new Dictionary<string, object?>(values);
            projected[key] = BmotionInertiaDriver.ProjectRest(from, perKey);
        }
        return projected ?? values;
    }

    private static bool TryGetDoubleArray(object? value, out double[]? result)
    {
        result = null;
        if (value is double[] da) { result = da; return true; }
        if (value is IEnumerable<double> de) { result = de.ToArray(); return true; }
        if (value is object[] oa && oa.Length > 0 && oa[0] is double or float or int or long)
        {
            // Convert each element defensively: a mixed array like [0, "bad"] must not throw.
            var arr = new double[oa.Length];
            for (int i = 0; i < oa.Length; i++)
            {
                if (oa[i] is null) return false;
                try { arr[i] = Convert.ToDouble(oa[i], System.Globalization.CultureInfo.InvariantCulture); }
                catch (Exception e) when (e is FormatException or InvalidCastException or OverflowException)
                { return false; }
            }
            result = arr;
            return true;
        }
        // Any other numeric sequence (int[], float[], List<int>, …). Strings are excluded so
        // colour keyframes still fall through to TryGetStringArray.
        if (value is System.Collections.IEnumerable seq && value is not string)
        {
            var list = new List<double>();
            foreach (var item in seq)
            {
                if (item is string || item is null) return false;
                try { list.Add(Convert.ToDouble(item, System.Globalization.CultureInfo.InvariantCulture)); }
                catch { return false; }
            }
            if (list.Count > 0) { result = list.ToArray(); return true; }
        }
        return false;
    }

    private void CreateCssDimensionDriver(string key, string toValue, BmotionTransitionConfig config)
    {
        // Simple same-unit dimensions ("100px" → "240px") interpolate numerically.
        string fromRaw = StringValues.GetValueOrDefault(key, "");
        if (TryParseCssDimension(toValue, out double toNum, out string toUnit) &&
            TryParseCssDimension(fromRaw, out double fromNum, out string fromUnit) &&
            string.Equals(fromUnit, toUnit, StringComparison.OrdinalIgnoreCase))
        {
            _activeAnims[key] = new BmotionTweenDriver(fromNum, toNum, config,
                v => ApplyString(key, BmotionCssFormat.Num(v) + toUnit));
        }
        // A length whose ends are in different units ("100px" → "50%") goes through calc(), as CSS interpolates it.
        else if (MixedLengthMix(fromRaw, toValue) is { } lengths)
        {
            _activeAnims[key] = CreateProgressDriver(config, p => ApplyString(key, lengths(p)));
        }
        // Complex strings with a matching shape ("blur(0px)" → "blur(8px)", multi-part shadows,
        // matching gradients) interpolate token-wise via the string mixer, driven by an eased /
        // sprung progress track.
        else if (BmotionStringMixer.TryCreateMix(BmotionStringMixer.MixEnds(key, fromRaw, toValue), key) is { } mix)
        {
            // At the end the authored string itself, not the mixer's re-serialisation of it.
            _activeAnims[key] = CreateProgressDriver(config, p => ApplyString(key, p == 1 ? toValue : mix(p)));
        }
        else
        {
            // Snap and mark dirty - no interpolation possible between these shapes.
            StringValues[key] = toValue;
            NumericValues.Remove(key); // keep numeric/string stores mutually exclusive
            _dirtyProps.Add(key);
        }
    }

    /// <summary>
    /// A 0 → 1 progress driver used to animate mixed (complex string) values: springs keep their
    /// physics (overshoot flows into the mixer's number extrapolation); everything else tweens.
    /// Inertia has no meaningful string semantics, so it also falls back to a tween.
    /// </summary>
    private IBmotionAnimationDriver CreateProgressDriver(BmotionTransitionConfig config, Action<double> apply)
        => config.Type == BmotionTransitionType.Spring
            ? new BmotionSpringDriver(0, 1, config, apply)
            : new BmotionTweenDriver(0, 1, config, apply);

    // The units of a CSS <length-percentage>: what a length in one of them can interpolate with a length in another.
    private static readonly HashSet<string> _lengthUnits = new(StringComparer.OrdinalIgnoreCase)
    {
        "px", "%", "em", "rem", "ex", "rex", "ch", "rch", "cap", "rcap", "ic", "ric", "lh", "rlh",
        "vw", "vh", "vi", "vb", "vmin", "vmax", "svw", "svh", "svi", "svb", "svmin", "svmax",
        "lvw", "lvh", "lvi", "lvb", "lvmin", "lvmax", "dvw", "dvh", "dvi", "dvb", "dvmin", "dvmax",
        "cqw", "cqh", "cqi", "cqb", "cqmin", "cqmax", "cm", "mm", "q", "in", "pt", "pc",
    };

    /// <summary>
    /// A length whose two ends are in different units (<c>"100px"</c> → <c>"50%"</c>, <c>"2rem"</c> →
    /// <c>"40vw"</c>), interpolated the way CSS does: <c>calc()</c> of both ends weighted by progress - exact in any
    /// unit, with nothing to measure. The ends themselves are the authored strings; a spring's overshoot carries on
    /// past them. <c>null</c> unless both ends are lengths or percentages (a bare <c>0</c> counts) in different units.
    /// </summary>
    internal static Func<double, string>? MixedLengthMix(string from, string to)
    {
        if (!TryParseLength(from, out double a, out string aUnit) || !TryParseLength(to, out double b, out string bUnit)) return null;
        if (string.Equals(aUnit, bUnit, StringComparison.OrdinalIgnoreCase)) return null;
        return p => p == 0 ? from
            : p == 1 ? to
            : $"calc({BmotionCssFormat.Num(a * (1 - p))}{aUnit} + {BmotionCssFormat.Num(b * p)}{bUnit})";

        static bool TryParseLength(string value, out double number, out string unit)
        {
            if (!TryParseCssDimension(value.Trim(), out number, out unit) || !double.IsFinite(number)) return false;
            if (unit.Length == 0 && number == 0) unit = "px";
            return _lengthUnits.Contains(unit);
        }
    }

    internal static bool TryParseCssDimension(string value, out double number, out string unit)
    {
        if (string.IsNullOrEmpty(value)) { number = 0; unit = ""; return false; }
        // Find the split between leading numeric part and trailing unit.
        int i = 0;
        if (i < value.Length && (value[i] == '-' || value[i] == '+')) i++;
        while (i < value.Length && (char.IsDigit(value[i]) || value[i] == '.')) i++;
        if (i == 0 || (i == 1 && (value[0] == '-' || value[0] == '+')))
        { number = 0; unit = ""; return false; }
        unit = value[i..];
        return double.TryParse(value[..i], System.Globalization.NumberStyles.Float,
            System.Globalization.CultureInfo.InvariantCulture, out number);
    }

    private static bool TryGetStringArray(object? value, out string[]? result)
    {
        result = null;
        if (value is string) return false; // a single string is not a keyframe array
        if (value is string[] sa) { result = sa; return true; }
        if (value is IEnumerable<string> se) { result = se.ToArray(); return true; }
        if (value is object[] oa && oa.Length > 0 && oa.All(x => x is string))
        {
            result = oa.Cast<string>().ToArray();
            return true;
        }
        return false;
    }

    private sealed record GestureLayer(Dictionary<string, object?> Values, BmotionTransitionConfig? Transition)
    {
        /// <summary>Whether the layer's values were animated to (false while a higher-priority layer held it back).</summary>
        public bool Applied { get; set; }
    }

    /// <summary>
    /// The value a base target property comes back to when a gesture releases it: a keyframe
    /// sequence settles on the frame it rests on rather than replaying from its first, unless it
    /// loops forever - then there is no resting frame and the loop restarts.
    /// </summary>
    private static object? RestingValue(string key, object? value, BmotionTransitionConfig? transition)
    {
        var config = transition?.Properties is { } perKey && perKey.TryGetValue(key, out var own) ? own : transition;
        if (config?.IsInfiniteRepeat == true) return value;
        bool mirrorsBack = config is { RepeatType: BmRepeatType.Mirror or BmRepeatType.Reverse } && config.Repeat % 2 == 1;
        return value switch
        {
            double[] { Length: > 0 } numbers when double.IsFinite(mirrorsBack ? numbers[0] : numbers[^1]) => mirrorsBack ? numbers[0] : numbers[^1],
            string[] { Length: > 0 } strings => mirrorsBack ? strings[0] : strings[^1],
            _ => value,
        };
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // WAAPI (compositor) offload bookkeeping
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// A compositor-offloaded animation the browser plays via the Web Animations API.
    /// The engine keeps this record so it can compute the element's current values (and
    /// velocity) at any moment WITHOUT reading the DOM - the sample table is the same
    /// curve the browser is playing.
    /// </summary>
    internal sealed class WaapiPlan
    {
        public required int Token { get; init; }
        // Environment.TickCount64 at start; moved back when the animation is played from the instant it took over.
        public required long StartMs { get; set; }
        public required double DelayMs { get; init; }
        public required double DurationMs { get; init; }    // one iteration
        public required double[] Progress { get; init; }    // eased progress samples, uniform over DurationMs
        // The exact curve, when the browser plays one the samples can't stand in for: a steps() staircase,
        // which interpolating between samples would turn into ramps the browser never shows.
        public Func<double, double>? Ease { get; init; }
        public required Dictionary<string, (double From, double To)> Values { get; init; }
        public required int Iterations { get; init; }       // -1 = infinite
        public required bool Mirror { get; init; }          // alternate direction per iteration

        // Per-key keyframe values (uniform offsets, one entry per animated key) when the offload is a
        // multi-keyframe animation. Null for a simple two-endpoint tween/spring. WAAPI warps the
        // timeline by the eased Progress and interpolates linearly between keyframes, so the C# mirror
        // reproduces the same value with KeyframeLerp(frames, easedProgress) - see RealizePlanValues.
        // A recorded (sampled) animation is the same shape with a linear Progress: one frame per tick.
        public Dictionary<string, double[]>? Frames { get; init; }

        /// <summary>
        /// The string-valued keys (colours, lengths, filters, ...): the value each shows at a progress (the same
        /// eased progress the numeric keys are sampled at), and the authored value it ends on.
        /// </summary>
        public Dictionary<string, StringTrack>? Strings { get; init; }

        /// <summary>One string-valued key of a plan: <paramref name="At"/> maps progress to its value.</summary>
        internal sealed record StringTrack(Func<double, string> At, string Final)
        {
            /// <summary>Whether the browser ends on something other than <see cref="Final"/> (a re-encoded colour), so it is written afterwards.</summary>
            public bool RewriteFinal { get; init; }
        }

        /// <summary>Every key the plan animates, numeric and string.</summary>
        public IEnumerable<string> Keys => Strings is null ? Values.Keys : Values.Keys.Concat(Strings.Keys);

        /// <summary>Whether the plan animates <paramref name="key"/>.</summary>
        public bool Owns(string key) => Values.ContainsKey(key) || Strings?.ContainsKey(key) == true;

        /// <summary>The value string key <paramref name="key"/> shows <paramref name="playedMs"/> into the animation.</summary>
        public string StringAt(string key, double playedMs)
        {
            var track = Strings![key];
            if (!double.IsInfinity(TotalMs) && playedMs >= TotalMs) return track.Final;
            return track.At(SampleAtPlayed(playedMs).Progress);
        }

        /// <summary>The velocity (units/s of animation time) numeric key <paramref name="key"/> moves at <paramref name="playedMs"/> in.</summary>
        public double VelocityAt(string key, double playedMs)
        {
            const double h = 2;
            double before = Math.Max(0, playedMs - h);
            double span = playedMs + h - before;
            double velocity = (ValueAt(key, playedMs + h) - ValueAt(key, before)) / span * 1000;
            return double.IsFinite(velocity) ? velocity : 0;
        }

        /// <summary>Linear interpolation across uniformly-spaced keyframes at eased progress <paramref name="p"/>.</summary>
        internal static double KeyframeLerp(double[] frames, double p)
        {
            if (frames.Length == 1) return frames[0];
            double pos = Math.Clamp(p, 0, 1) * (frames.Length - 1);
            int i = (int)pos;
            if (i >= frames.Length - 1) return frames[^1];
            return frames[i] + (frames[i + 1] - frames[i]) * (pos - i);
        }

        public bool AnimatesTransform
        {
            get
            {
                foreach (var key in Values.Keys)
                    if (BmotionTransformComposer.IsTransformProp(key)) return true;
                return false;
            }
        }

        // Playback-rate bookkeeping, mirroring the Web Animation's own: how much of the animation had
        // played when the rate last changed, since when, and at what rate it has played since.
        private double _playedBeforeMs;
        private long _rateSinceMs = -1;

        /// <summary>The rate the browser plays the animation at (1 = realtime, 0 = paused).</summary>
        public double Rate { get; private set; } = 1;

        /// <summary>The completion the animation's starter awaits; handed to its continuation drivers if interrupted.</summary>
        public TaskCompletionSource<bool>? Completion { get; set; }

        /// <summary>Set when an interruption handed the plan's remaining keys (and <see cref="Completion"/>) to rAF drivers.</summary>
        public bool HandedOff { get; set; }

        /// <summary>How far into the animation (delay included) playback is, in ms of animation time.</summary>
        public double PlayedMs(long nowMs)
        {
            if (_rateSinceMs < 0) _rateSinceMs = StartMs;
            return _playedBeforeMs + (Math.Max(0, nowMs - _rateSinceMs) * Rate);
        }

        public void SetRate(long nowMs, double rate)
        {
            _playedBeforeMs = PlayedMs(nowMs);
            _rateSinceMs = nowMs;
            Rate = rate;
        }

        /// <summary>Puts the playhead at <paramref name="playedMs"/> of animation time as of <paramref name="nowMs"/>.</summary>
        public void SeekTo(long nowMs, double playedMs)
        {
            _playedBeforeMs = playedMs;
            _rateSinceMs = nowMs;
        }

        /// <summary>The whole animation's length in ms of animation time, delay included; infinity when it repeats forever.</summary>
        public double TotalMs => Iterations < 0 ? double.PositiveInfinity : DelayMs + (Math.Max(DurationMs, 1) * (Iterations + 1));

        /// <summary>The value <paramref name="key"/> shows <paramref name="playedMs"/> into the animation.</summary>
        public double ValueAt(string key, double playedMs)
        {
            var (from, to) = Values[key];
            var (progress, _) = SampleAtPlayed(playedMs);
            return Frames != null && Frames.TryGetValue(key, out var frames)
                ? KeyframeLerp(frames, progress)
                : from + ((to - from) * progress);
        }

        /// <summary>Eased progress (may exceed [0,1] for springs) and velocity factor at elapsed wall time.</summary>
        public (double Progress, double VelocityPerMs) SampleAt(long nowMs) => SampleAtPlayed(PlayedMs(nowMs));

        internal (double Progress, double VelocityPerMs) SampleAtPlayed(double playedMs)
        {
            double elapsed = playedMs - DelayMs;
            if (elapsed <= 0) return (Progress[0], 0);

            double cycles = elapsed / Math.Max(DurationMs, 1);
            if (Iterations >= 0 && cycles >= Iterations + 1)
                return (EndProgress, 0); // finished

            double local = cycles - Math.Floor(cycles);
            bool reversed = Mirror && ((long)Math.Floor(cycles) % 2 == 1);
            if (reversed) local = 1 - local;

            double p = Sample(local);
            // Central-difference velocity in progress-per-ms (sign flips on mirrored passes).
            const double eps = 0.001;
            double v = (Sample(Math.Min(local + eps, 1)) - Sample(Math.Max(local - eps, 0)))
                       / (2 * eps * Math.Max(DurationMs, 1));
            if (reversed) v = -v;
            return (p, v);
        }

        private double EndProgress => Mirror && Iterations >= 0 && Iterations % 2 == 1 ? Progress[0] : Progress[^1];

        private double Sample(double local)
        {
            if (Ease is not null) return Ease(Math.Clamp(local, 0, 1));
            if (Progress.Length == 1) return Progress[0];
            double pos = Math.Clamp(local, 0, 1) * (Progress.Length - 1);
            int i = (int)pos;
            if (i >= Progress.Length - 1) return Progress[^1];
            double frac = pos - i;
            return Progress[i] + (Progress[i + 1] - Progress[i]) * frac;
        }
    }

    private readonly List<WaapiPlan> _waapiPlans = new();

    internal bool HasWaapiPlans => _waapiPlans.Count > 0;

    /// <summary>Moves every browser animation's mirror to <paramref name="playedMs"/> of animation time (a seek).</summary>
    internal void SeekWaapiPlans(long nowMs, double playedMs)
    {
        foreach (var plan in _waapiPlans) plan.SeekTo(nowMs, playedMs);
    }

    /// <summary>
    /// Forgets the velocities interrupted animations left behind, so what starts next starts from rest - an animation
    /// replayed from part-way in (a seek) must not inherit the speed of the one it replaces.
    /// </summary>
    internal void ForgetVelocity()
    {
        _handoff.Clear();
        _samples.Clear();
    }

    /// <summary>Every key a browser-played animation currently owns (diagnostics).</summary>
    internal IEnumerable<string> WaapiPlanKeys => _waapiPlans.SelectMany(p => p.Keys).Distinct(StringComparer.OrdinalIgnoreCase);

    internal void AddWaapiPlan(WaapiPlan plan)
    {
        // Starting a compositor animation supersedes any rAF drivers on the same keys.
        foreach (var key in plan.Keys)
            CancelProp(key);
        _waapiPlans.Add(plan);
    }

    /// <summary>Whether a browser-played animation currently owns <paramref name="key"/>.</summary>
    internal bool PlanOwns(string key)
    {
        foreach (var plan in _waapiPlans)
            if (plan.Owns(key)) return true;
        return false;
    }

    /// <summary>True when any active rAF driver animates a transform component.</summary>
    internal bool HasActiveTransformDriver()
    {
        foreach (var key in _activeAnims.Keys)
            if (BmotionTransformComposer.IsTransformProp(key)) return true;
        return false;
    }

    /// <summary>
    /// Realizes (writes current sampled values into state, marks dirty) and removes every WAAPI
    /// plan that overlaps <paramref name="keys"/>. Because <c>transform</c> is a single CSS
    /// property, a plan touching any transform component overlaps a key set touching any other.
    /// Pass <c>null</c> to realize all plans. Returns the removed plans' tokens (for JS cancel).
    /// </summary>
    internal List<int>? RealizeWaapiPlans(IReadOnlyCollection<string>? keys, bool continueOthers = false)
    {
        if (_waapiPlans.Count == 0) return null;

        bool keysTouchTransform = false;
        if (keys != null)
            foreach (var key in keys)
                if (BmotionTransformComposer.IsTransformProp(key)) { keysTouchTransform = true; break; }

        List<int>? tokens = null;
        long now = Environment.TickCount64;
        for (int i = _waapiPlans.Count - 1; i >= 0; i--)
        {
            var plan = _waapiPlans[i];
            bool overlaps = keys == null
                || (keysTouchTransform && plan.AnimatesTransform)
                || PlanTouchesAny(plan, keys);
            if (!overlaps) continue;

            RealizePlanValues(plan, now);
            _waapiPlans.RemoveAt(i);
            (tokens ??= new List<int>()).Add(plan.Token);
            // The plan was cancelled for some of its keys only (transform is one CSS property, so
            // animating y takes the compositor's x with it): the rest carry on where they were.
            if (continueOthers && keys != null) ContinuePlan(plan, keys, now);
            else ForgetAuto(plan.Keys);
            if (keys != null) ForgetAuto(keys);
        }
        return tokens;

        static bool PlanTouchesAny(WaapiPlan plan, IReadOnlyCollection<string> keys)
        {
            foreach (var key in keys)
                if (plan.Owns(key)) return true;
            return false;
        }
    }

    /// <summary>
    /// Hands the keys of an interrupted compositor plan that the interruption doesn't animate to
    /// drivers that carry on along the very same curve from where the compositor left them - with
    /// the plan's completion, so whoever awaits it still hears when it is over. It was partly
    /// superseded, so it settles as superseded (false), as a batch with a superseded key does.
    /// Without a frame loop nothing can carry them, so they land on their targets at once instead.
    /// </summary>
    private void ContinuePlan(WaapiPlan plan, IReadOnlyCollection<string> interrupted, long nowMs)
    {
        double played = plan.PlayedMs(nowMs);
        HashSet<string>? continued = null;
        foreach (var key in plan.Keys)
        {
            if (interrupted.Contains(key)) continue;
            bool isString = plan.Strings?.ContainsKey(key) == true;
            if (ForceInstant || double.IsInfinity(plan.TotalMs) is false && played >= plan.TotalMs)
            {
                if (isString) WriteRealizedString(key, plan.Strings![key].Final, markDirty: true);
                else WriteRealizedValue(key, plan.ValueAt(key, double.MaxValue / 4), markDirty: true);
                SettleAuto(key);
                continue;
            }
            var k = key;
            if (isString)
                _activeAnims[key] = new BmotionWaapiContinuationDriver(plan, key, played, applyString: v => ApplyString(k, v));
            else
            {
                bool isTransform = BmotionTransformComposer.IsTransformProp(key);
                Action<double> apply = isTransform ? v => ApplyTransform(k, v) : v => ApplyNumeric(k, v);
                _activeAnims[key] = new BmotionWaapiContinuationDriver(plan, key, played, apply);
            }
            (continued ??= new HashSet<string>()).Add(key);
        }

        if (continued is null || plan.Completion is not { } completion) return;
        plan.HandedOff = true;
        var carried = new TaskCompletionSource<bool>();
        carried.Task.ContinueWith(_ => completion.TrySetResult(false), TaskContinuationOptions.ExecuteSynchronously);
        _batches.Add(new CompletionBatch { Source = carried, Keys = continued });
    }

    /// <summary>
    /// The transform components as they will be <paramref name="afterMs"/> from now, with every
    /// compositor plan's transform keys sampled that far ahead (at their resting value once they are
    /// over). What a FLIP that masks those plans while it plays must end on, so that when it lifts
    /// they show through at the very frame it left them at.
    /// </summary>
    internal Dictionary<string, double> TransformsAfter(double afterMs)
    {
        if (_waapiPlans.Count == 0) return Transforms;
        long now = Environment.TickCount64;
        var target = new Dictionary<string, double>(Transforms, StringComparer.OrdinalIgnoreCase);
        foreach (var plan in _waapiPlans)
            foreach (var key in plan.Values.Keys)
                if (BmotionTransformComposer.IsTransformProp(key))
                    // The plan's clock runs at its rate; afterMs of wall time is afterMs × rate of animation time.
                    target[key] = plan.ValueAt(key, plan.PlayedMs(now) + (afterMs * plan.Rate));
        return target;
    }

    /// <summary>Applies a playback rate to the compositor plans' bookkeeping (the bridge applies it to the animations).</summary>
    internal void SetWaapiPlansRate(double rate)
    {
        long now = Environment.TickCount64;
        foreach (var plan in _waapiPlans) plan.SetRate(now, rate);
    }

    /// <summary>
    /// Snaps every WAAPI plan to its target values (used by <c>Complete</c>) and removes them.
    /// Returns the removed plans' tokens (for JS cancellation), or null when none existed.
    /// </summary>
    internal List<int>? CompleteWaapiPlans()
    {
        if (_waapiPlans.Count == 0) return null;
        var tokens = new List<int>(_waapiPlans.Count);
        foreach (var plan in _waapiPlans)
        {
            foreach (var (key, (_, to)) in plan.Values)
                WriteRealizedValue(key, plan.Frames?.ContainsKey(key) == true ? plan.ValueAt(key, double.MaxValue / 4) : to, markDirty: true);
            if (plan.Strings is { } strings)
                foreach (var (key, track) in strings)
                    WriteRealizedString(key, track.Final, markDirty: true);
            tokens.Add(plan.Token);
        }
        _waapiPlans.Clear();
        return tokens;
    }

    /// <summary>
    /// Completes a WAAPI plan that finished naturally: state settles on the target values.
    /// Returns false when the plan is gone already (superseded by an interruption).
    /// </summary>
    internal bool TryCompleteWaapiPlan(int token) => TryCompleteWaapiPlan(token, out _);

    /// <summary>
    /// <see cref="TryCompleteWaapiPlan(int)"/>; <paramref name="rewrite"/> tells whether the state now differs from
    /// what the browser committed - an authored value it re-encoded, a size that lands on <c>"auto"</c> - so it
    /// has to be flushed.
    /// </summary>
    internal bool TryCompleteWaapiPlan(int token, out bool rewrite)
    {
        rewrite = false;
        for (int i = 0; i < _waapiPlans.Count; i++)
        {
            if (_waapiPlans[i].Token != token) continue;
            var plan = _waapiPlans[i];
            _waapiPlans.RemoveAt(i);
            foreach (var (key, (_, to)) in plan.Values)
                WriteRealizedValue(key, to, markDirty: false); // commitStyles already wrote the DOM
            if (plan.Strings is { } strings)
                foreach (var (key, track) in strings)
                {
                    // The browser committed what it interpolated; where that is a re-encoding of the authored
                    // value (an oklch() target played as rgba()), the authored value is written over it.
                    WriteRealizedString(key, track.Final, markDirty: track.RewriteFinal);
                    rewrite |= track.RewriteFinal;
                }
            // A size played to its measured natural size lands on "auto" itself.
            foreach (var key in plan.Keys) rewrite |= SettleAuto(key);
            return true;
        }
        return false;
    }

    /// <summary>Removes a plan realized at its current values (used when playback failed/cancelled).</summary>
    internal bool TryRealizeWaapiPlan(int token, out double elapsedMs, double neverStartedWithinMs = 0)
    {
        elapsedMs = 0;
        for (int i = 0; i < _waapiPlans.Count; i++)
        {
            if (_waapiPlans[i].Token != token) continue;
            var plan = _waapiPlans[i];
            long now = Environment.TickCount64;
            elapsedMs = now - plan.StartMs;
            // Rejected before it could ever have played (within neverStartedWithinMs): nothing was shown, so the state
            // goes back to the plan's first frame - not to where it would have got to by now, nor, for one that starts
            // part-way in (a negative delay), to that point - and the replay that follows starts from there.
            RealizePlanValues(plan, now, elapsedMs < neverStartedWithinMs ? plan.DelayMs : null);
            _waapiPlans.RemoveAt(i);
            return true;
        }
        return false;
    }

    private void RealizePlanValues(WaapiPlan plan, long nowMs, double? atPlayedMs = null)
    {
        double played = atPlayedMs ?? plan.PlayedMs(nowMs);
        var (progress, _) = plan.SampleAtPlayed(played);
        foreach (var (key, (from, to)) in plan.Values)
        {
            double value = plan.Frames != null && plan.Frames.TryGetValue(key, out var frames)
                ? WaapiPlan.KeyframeLerp(frames, progress)
                : from + (to - from) * progress;
            WriteRealizedValue(key, value, markDirty: true);
            // Whatever takes the key over next (a retargeted spring) carries on at the speed the browser had it at -
            // in animation time, the clock the replacing driver runs on too.
            double velocity = plan.VelocityAt(key, played);
            if (velocity != 0 && atPlayedMs is null) _handoff[key] = (velocity, nowMs); // one never shown left no motion
        }
        if (plan.Strings is { } strings)
            foreach (var key in strings.Keys)
                WriteRealizedString(key, plan.StringAt(key, played), markDirty: true);
    }

    private void WriteRealizedString(string key, string value, bool markDirty)
    {
        StringValues[key] = value;
        NumericValues.Remove(key);
        if (markDirty) _dirtyProps.Add(key);
    }

    private void WriteRealizedValue(string key, double value, bool markDirty)
    {
        if (BmotionTransformComposer.IsTransformProp(key))
        {
            Transforms[key] = value;
            if (markDirty) _transformDirty = true;
        }
        else
        {
            NumericValues[key] = value;
            StringValues.Remove(key);
            if (markDirty) _dirtyProps.Add(key);
        }
    }
}
