
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
            if (driver.Tick(timestamp))
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
            // Superseding an in-flight driver for this key interrupts any completion batch that
            // owned it (resolves that batch with false once its remaining keys settle).
            CancelProp(key);
            activeBefore = _activeAnims.Count;

            if (TryGetDoubleArray(value, out double[]? doubleFrames))
            {
                // Keyframe drivers require at least two frames (they build n-1 segments and
                // divide by n-1 when distributing times). Degenerate arrays would otherwise
                // throw and (via ComputeFrame) stall the whole loop, so handle them here:
                //   0 frames -> nothing to do; 1 frame -> snap to that single value.
                if (doubleFrames!.Length >= 2)
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
                // Non-colour string keyframes (e.g. dimension arrays) have no interpolating driver;
                // snap to the final frame so the value still lands on its destination.
                StringValues[key] = otherFrames[^1];
                NumericValues.Remove(key); // keep numeric/string stores mutually exclusive
                _dirtyProps.Add(key);
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
        // If both from and to are the same unit, interpolate numerically.
        // Otherwise just snap to the new value immediately.
        string fromRaw = StringValues.GetValueOrDefault(key, "");
        if (TryParseCssDimension(toValue, out double toNum, out string toUnit) &&
            TryParseCssDimension(fromRaw, out double fromNum, out string fromUnit) &&
            string.Equals(fromUnit, toUnit, StringComparison.OrdinalIgnoreCase))
        {
            _activeAnims[key] = new BmotionTweenDriver(fromNum, toNum, config,
                v => ApplyString(key, BmotionCssFormat.Num(v) + toUnit));
        }
        else
        {
            // Snap and mark dirty - no interpolation possible across different units.
            StringValues[key] = toValue;
            NumericValues.Remove(key); // keep numeric/string stores mutually exclusive
            _dirtyProps.Add(key);
        }
    }

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
}
