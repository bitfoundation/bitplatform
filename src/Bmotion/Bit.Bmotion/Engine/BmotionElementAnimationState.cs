
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
    /// Number of live owners holding this element (a wrapping &lt;Bmotion&gt;, controllers, and
    /// in-flight AnimateAsync calls). The engine only tears the element down when this hits zero.
    /// </summary>
    internal int RefCount;

    // ── Active animations ─────────────────────────────────────────────────────
    private readonly Dictionary<string, IBmotionAnimationDriver> _activeAnims = new();

    // ── Gesture layer stack ────────────────────────────────────────────────────
    private static readonly string[] GesturePriority = ["drag", "focus", "tap", "hover", "inview"];
    private readonly Dictionary<string, GestureLayer> _gestureLayers = new();
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

    // Transform writes are paused while a WAAPI FLIP layout animation owns the element's
    // transform, so the rAF engine doesn't fight (and visually tear) the layout animation.
    // The window is measured against Tick timestamps so no extra timer/interop is needed.
    private double _transformSuspendMs;
    private double _transformSuspendStart = -1;

    // Reused across frames to avoid allocating a fresh CSS-update dictionary every rAF tick.
    // Safe because the synchronous JS interop marshals the returned dictionary before the next
    // Tick runs (single-threaded Blazor WASM), so the buffer is never read after it is cleared.
    private readonly Dictionary<string, string> _updateBuffer = new();

    public bool HasActiveAnimations => _activeAnims.Count > 0 || _isDragging;

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

    // ═══════════════════════════════════════════════════════════════════════════
    // Tick - called every rAF frame
    // ═══════════════════════════════════════════════════════════════════════════

    public Dictionary<string, string>? Tick(double timestamp)
    {
        // Nothing to do only when there are no drivers, no drag, and no pending
        // instant (SetInstant) changes still waiting to be emitted.
        if (_activeAnims.Count == 0 && !_isDragging && !_transformDirty && _dirtyProps.Count == 0)
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
                NotePropFinished(key, interrupted: false); // natural completion
            }

        if (!_transformDirty && _dirtyProps.Count == 0) return null;

        // ── Build CSS style update dict (reused buffer) ────────────────────────
        var updates = _updateBuffer;
        updates.Clear();

        // While a FLIP layout animation owns the transform, hold transform writes back so the two
        // animators don't fight; the dirty flag is preserved so the latest transform is flushed
        // the moment the suspension window ends.
        bool transformSuspended = IsTransformSuspended(timestamp);
        if (_transformDirty && !transformSuspended)
            updates["transform"] = BmotionTransformComposer.Build(Transforms);

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
        if (durationMs <= 0) return;
        _transformSuspendMs = durationMs;
        _transformSuspendStart = -1; // armed on the next tick
    }

    private bool IsTransformSuspended(double timestamp)
    {
        if (_transformSuspendMs <= 0) return false;
        if (_transformSuspendStart < 0) _transformSuspendStart = timestamp;
        if (timestamp - _transformSuspendStart < _transformSuspendMs) return true;
        // Window elapsed - clear so transforms resume.
        _transformSuspendMs = 0;
        _transformSuspendStart = -1;
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
            var tr = BmotionTransformComposer.Build(Transforms);
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

        // Track which keys actually get an active driver so the completion batch only waits on
        // animations that can finish (keys that snap instantly resolve immediately).
        HashSet<string>? driverKeys = completionSource != null ? new HashSet<string>() : null;
        int activeBefore;

        foreach (var (key, value) in values)
        {
            if (value == null) continue;
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
                NotePropFinished(key, interrupted: false); // snapped to end value = natural completion
            }
        }
    }

    internal void CancelProp(string key)
    {
        if (_activeAnims.TryGetValue(key, out var driver))
        {
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
    }

    public void ActivateGestureLayer(string gesture, Dictionary<string, object?> values, BmotionTransitionConfig? transition)
    {
        _gestureLayers[gesture] = new GestureLayer(values, transition);

        // Respect gesture priority: don't let a lower-priority gesture animate over a
        // higher-priority one that is already active (mirrors DeactivateGestureLayer).
        int newPriority = Array.IndexOf(GesturePriority, gesture);
        if (newPriority >= 0)
        {
            foreach (var other in _gestureLayers.Keys)
            {
                if (other == gesture) continue;
                int otherPriority = Array.IndexOf(GesturePriority, other);
                if (otherPriority >= 0 && otherPriority < newPriority) return; // higher-priority layer wins
            }
        }

        AnimateTo(values, transition);
    }

    public void DeactivateGestureLayer(string gesture)
    {
        if (!_gestureLayers.Remove(gesture, out var removed))
            return;

        // Build the target the element should revert to: the base animation overlaid with
        // every still-active gesture layer (lowest priority first so higher priority wins).
        var target = new Dictionary<string, object?>();
        BmotionTransitionConfig? transition = _baseTransition;

        if (_baseValues != null)
            foreach (var kv in _baseValues)
                target[kv.Key] = kv.Value;

        for (int i = GesturePriority.Length - 1; i >= 0; i--)
        {
            if (_gestureLayers.TryGetValue(GesturePriority[i], out var layer))
            {
                foreach (var kv in layer.Values)
                    target[kv.Key] = kv.Value;
                transition = layer.Transition; // highest-priority remaining layer wins the transition
            }
        }

        // Any property the removed layer set but no remaining layer/base defines must animate
        // back to its identity value, otherwise it would stay stuck at the gesture value.
        foreach (var key in removed.Values.Keys)
        {
            if (target.ContainsKey(key)) continue;
            if (BmotionTransformComposer.IsTransformProp(key))
                target[key] = DefaultTransformValue(key);
            else if (!IsColorProp(key)) // colours have no safe identity to revert to
                target[key] = DefaultNumericValue(key);
        }

        if (target.Count > 0)
            AnimateTo(target, transition);
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

    public void EndDrag() => _isDragging = false;

    public (double x, double y) GetCurrentXY()
        => (Transforms.GetValueOrDefault("x"), Transforms.GetValueOrDefault("y"));

    // ═══════════════════════════════════════════════════════════════════════════
    // Driver factory helpers
    // ═══════════════════════════════════════════════════════════════════════════

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
            if (BmotionStringMixer.TryCreateMix(frames[i], frames[i + 1]) is not { } mix)
                return false;
            mixes[i] = mix;
        }

        var indexTrack = new double[frames.Length];
        for (int i = 0; i < frames.Length; i++) indexTrack[i] = i;

        _activeAnims[key] = new BmotionNumericKeyframesDriver(indexTrack, config, v =>
        {
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
    }

    private void ApplyNumeric(string key, double value)
    {
        NumericValues[key] = value;
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

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static readonly HashSet<string> _colorProps = new(StringComparer.OrdinalIgnoreCase)
    {
        "backgroundColor", "color", "borderColor", "outlineColor", "fill", "stroke",
        "caretColor", "columnRuleColor", "textDecorationColor",
    };

    private static bool IsColorProp(string key)
        => _colorProps.Contains(key) || key.EndsWith("color", StringComparison.OrdinalIgnoreCase);

    private static double DefaultTransformValue(string key) =>
        key is "scale" or "scaleX" or "scaleY" ? 1.0 : 0.0;

    private static double DefaultNumericValue(string key) =>
        key is "opacity" or "pathLength" ? 1.0 : 0.0;

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
        // Complex strings with a matching shape ("blur(0px)" → "blur(8px)", multi-part shadows,
        // matching gradients) interpolate token-wise via the string mixer, driven by an eased /
        // sprung progress track.
        else if (BmotionStringMixer.TryCreateMix(fromRaw, toValue) is { } mix)
        {
            _activeAnims[key] = CreateProgressDriver(config, p => ApplyString(key, mix(p)));
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

    private static bool TryParseCssDimension(string value, out double number, out string unit)
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

    private sealed record GestureLayer(Dictionary<string, object?> Values, BmotionTransitionConfig? Transition);

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
        public required long StartMs { get; init; }         // Environment.TickCount64 at start
        public required double DelayMs { get; init; }
        public required double DurationMs { get; init; }    // one iteration
        public required double[] Progress { get; init; }    // eased progress samples, uniform over DurationMs
        public required Dictionary<string, (double From, double To)> Values { get; init; }
        public required int Iterations { get; init; }       // -1 = infinite
        public required bool Mirror { get; init; }          // alternate direction per iteration

        public bool AnimatesTransform
        {
            get
            {
                foreach (var key in Values.Keys)
                    if (BmotionTransformComposer.IsTransformProp(key)) return true;
                return false;
            }
        }

        /// <summary>Eased progress (may exceed [0,1] for springs) and velocity factor at elapsed wall time.</summary>
        public (double Progress, double VelocityPerMs) SampleAt(long nowMs)
        {
            double elapsed = nowMs - StartMs - DelayMs;
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

    internal void AddWaapiPlan(WaapiPlan plan)
    {
        // Starting a compositor animation supersedes any rAF drivers on the same keys.
        foreach (var key in plan.Values.Keys)
            CancelProp(key);
        _waapiPlans.Add(plan);
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
    internal List<int>? RealizeWaapiPlans(IReadOnlyCollection<string>? keys)
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
        }
        return tokens;

        static bool PlanTouchesAny(WaapiPlan plan, IReadOnlyCollection<string> keys)
        {
            foreach (var key in keys)
                if (plan.Values.ContainsKey(key)) return true;
            return false;
        }
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
                WriteRealizedValue(key, to, markDirty: true);
            tokens.Add(plan.Token);
        }
        _waapiPlans.Clear();
        return tokens;
    }

    /// <summary>
    /// Completes a WAAPI plan that finished naturally: state settles on the target values.
    /// Returns false when the plan is gone already (superseded by an interruption).
    /// </summary>
    internal bool TryCompleteWaapiPlan(int token)
    {
        for (int i = 0; i < _waapiPlans.Count; i++)
        {
            if (_waapiPlans[i].Token != token) continue;
            var plan = _waapiPlans[i];
            _waapiPlans.RemoveAt(i);
            foreach (var (key, (_, to)) in plan.Values)
                WriteRealizedValue(key, to, markDirty: false); // commitStyles already wrote the DOM
            return true;
        }
        return false;
    }

    /// <summary>Removes a plan realized at its current values (used when playback failed/cancelled).</summary>
    internal bool TryRealizeWaapiPlan(int token, out double elapsedMs)
    {
        elapsedMs = 0;
        for (int i = 0; i < _waapiPlans.Count; i++)
        {
            if (_waapiPlans[i].Token != token) continue;
            var plan = _waapiPlans[i];
            long now = Environment.TickCount64;
            elapsedMs = now - plan.StartMs;
            RealizePlanValues(plan, now);
            _waapiPlans.RemoveAt(i);
            return true;
        }
        return false;
    }

    private void RealizePlanValues(WaapiPlan plan, long nowMs)
    {
        var (progress, _) = plan.SampleAt(nowMs);
        foreach (var (key, (from, to)) in plan.Values)
            WriteRealizedValue(key, from + (to - from) * progress, markDirty: true);
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
