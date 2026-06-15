
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
    internal readonly Dictionary<string, double> Transforms = new();

    /// <summary>Current values of numeric non-transform properties (opacity, pathLength, …).</summary>
    internal readonly Dictionary<string, double> NumericValues = new();

    /// <summary>Current values of color / string properties (backgroundColor, color, …).</summary>
    internal readonly Dictionary<string, string> StringValues = new();

    // ── Active animations ─────────────────────────────────────────────────────
    private readonly Dictionary<string, IBmotionAnimationDriver> _activeAnims = new();

    // ── Gesture layer stack ────────────────────────────────────────────────────
    private static readonly string[] GesturePriority = ["drag", "focus", "tap", "hover", "inview"];
    private readonly Dictionary<string, GestureLayer> _gestureLayers = new();
    private Dictionary<string, object?>? _baseValues;
    private BmotionTransitionConfig? _baseTransition;

    // ── Animation completion tracking ─────────────────────────────────────────
    private TaskCompletionSource? _completionSource;

    // ── Drag state ────────────────────────────────────────────────────────────
    private bool _isDragging;

    // ── Dirty flags for CSS build ─────────────────────────────────────────────
    private bool _transformDirty;
    private readonly HashSet<string> _dirtyProps = new();

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

        // Advance all drivers. Only allocate the "completed" list when something finishes.
        List<string>? completed = null;
        foreach (var (key, driver) in _activeAnims)
        {
            if (driver.Tick(timestamp))
                (completed ??= new List<string>()).Add(key);
        }

        if (completed != null)
            foreach (var key in completed)
                _activeAnims.Remove(key);

        // Signal awaiter if all finished
        if (_completionSource != null && _activeAnims.Count == 0)
        {
            _completionSource.TrySetResult();
            _completionSource = null;
        }

        if (!_transformDirty && _dirtyProps.Count == 0) return null;

        // ── Build CSS style update dict ────────────────────────────────────────
        var updates = new Dictionary<string, string>(_dirtyProps.Count + 1);

        if (_transformDirty)
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

        // Reset dirty flags now that this frame's changes have been emitted.
        _transformDirty = false;
        _dirtyProps.Clear();

        return updates.Count > 0 ? updates : null;
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // Animation control
    // ═══════════════════════════════════════════════════════════════════════════

    public void AnimateTo(
        Dictionary<string, object?> values,
        BmotionTransitionConfig? transition,
        TaskCompletionSource? completionSource = null)
    {
        // Cheap scan for any non-null target (no allocation).
        bool any = false;
        foreach (var v in values.Values)
            if (v != null) { any = true; break; }
        if (!any) { completionSource?.TrySetResult(); return; }

        // Complete any previously-pending awaiter so callers aren't stranded when a
        // new animation supersedes the old one.
        if (_completionSource != null && !ReferenceEquals(_completionSource, completionSource))
            _completionSource.TrySetResult();
        _completionSource = completionSource;

        foreach (var (key, value) in values)
        {
            if (value == null) continue;
            var perKey = transition?.Properties?.GetValueOrDefault(key) ?? transition ?? new BmotionTransitionConfig();
            CancelProp(key);

            if (TryGetDoubleArray(value, out double[]? doubleFrames))
                CreateNumericKeyframesDriver(key, doubleFrames!, perKey);
            else if (TryGetStringArray(value, out string[]? strFrames))
                CreateColorKeyframesDriver(key, strFrames!, perKey);
            else if (IsColorProp(key) && value is string colorStr)
                CreateColorDriver(key, colorStr, perKey);
            else if (value is string dimStr)
                CreateCssDimensionDriver(key, dimStr, perKey);
            else
                CreateNumericDriver(key, Convert.ToDouble(value, System.Globalization.CultureInfo.InvariantCulture), perKey);
        }
    }

    public void SetInstant(Dictionary<string, object?> values)
    {
        foreach (var (key, value) in values)
        {
            if (value == null) continue;
            if (BmotionTransformComposer.IsTransformProp(key))
            {
                Transforms[key] = Convert.ToDouble(value, System.Globalization.CultureInfo.InvariantCulture);
                _transformDirty = true;
            }
            else if (IsColorProp(key) && value is string colorStr)
            {
                StringValues[key] = colorStr;
                _dirtyProps.Add(key);
            }
            else if (value is string dimStr)
            {
                StringValues[key] = dimStr;
                _dirtyProps.Add(key);
            }
            else
            {
                NumericValues[key] = Convert.ToDouble(value, System.Globalization.CultureInfo.InvariantCulture);
                _dirtyProps.Add(key);
            }
        }
    }

    public void Cancel(string[]? properties)
    {
        if (properties == null || properties.Length == 0)
            CancelAll();
        else
            foreach (var p in properties)
                CancelProp(p);
    }

    internal void CancelAll()
    {
        foreach (var driver in _activeAnims.Values)
            driver.Cancel();
        _activeAnims.Clear();
        _completionSource?.TrySetResult();
        _completionSource = null;
    }

    /// <summary>
    /// Finish all running animations immediately, snapping every property to its target
    /// (end) value. Unlike <see cref="CancelAll"/> (which freezes in place), this applies
    /// the final frame so the element settles on the destination state.
    /// </summary>
    internal void CompleteAll()
    {
        foreach (var driver in _activeAnims.Values)
            driver.Complete();
        _activeAnims.Clear();
        _completionSource?.TrySetResult();
        _completionSource = null;
    }

    internal void CancelProp(string key)
    {
        if (_activeAnims.TryGetValue(key, out var driver))
        {
            driver.Cancel();
            _activeAnims.Remove(key);
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
        _dirtyProps.Add(key);
    }

    private void ApplyString(string key, string value)
    {
        StringValues[key] = value;
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
            result = oa.Select(x => Convert.ToDouble(x, System.Globalization.CultureInfo.InvariantCulture)).ToArray();
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
        if (value is string[] sa) { result = sa; return true; }
        if (value is object[] oa && oa.Length > 0 && oa[0] is string)
        {
            result = oa.Cast<string>().ToArray();
            return true;
        }
        return false;
    }

    private sealed record GestureLayer(Dictionary<string, object?> Values, BmotionTransitionConfig? Transition);
}
