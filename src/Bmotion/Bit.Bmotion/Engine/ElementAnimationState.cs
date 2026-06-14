using Bit.Bmotion.Models;

namespace Bit.Bmotion.Engine;

/// <summary>
/// Per-element animation state - the C# equivalent of the JS <c>ElementState</c> class.
/// Holds current transform / numeric / color values, active animation drivers,
/// and gesture-layer bookkeeping. Called by <see cref="AnimationEngine.ComputeFrame"/>
/// every rAF tick.
/// </summary>
internal sealed class ElementAnimationState
{
    // ── Live CSS values ───────────────────────────────────────────────────────

    /// <summary>Current values of transform components (x, y, scale, rotate, …).</summary>
    internal readonly Dictionary<string, double> Transforms = new();

    /// <summary>Current values of numeric non-transform properties (opacity, pathLength, …).</summary>
    internal readonly Dictionary<string, double> NumericValues = new();

    /// <summary>Current values of color / string properties (backgroundColor, color, …).</summary>
    internal readonly Dictionary<string, string> StringValues = new();

    // ── Active animations ─────────────────────────────────────────────────────
    private readonly Dictionary<string, IAnimationDriver> _activeAnims = new();

    // ── Gesture layer stack ────────────────────────────────────────────────────
    private static readonly string[] GesturePriority = ["drag", "focus", "tap", "hover", "inview"];
    private readonly Dictionary<string, GestureLayer> _gestureLayers = new();
    private Dictionary<string, object?>? _baseValues;
    private TransitionConfig? _baseTransition;

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

        // Advance all drivers
        var completed = new List<string>(_activeAnims.Count);
        foreach (var (key, driver) in _activeAnims)
        {
            if (driver.Tick(timestamp))
                completed.Add(key);
        }

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
            updates["transform"] = TransformComposer.Build(Transforms);

        foreach (var prop in _dirtyProps)
        {
            if (prop == "pathLength")
            {
                double v = NumericValues.GetValueOrDefault("pathLength", 1.0);
                double clamped = Math.Max(0, Math.Min(1, v));
                updates["strokeDasharray"] = "1 1";
                updates["strokeDashoffset"] = (1 - clamped).ToString("G6");
            }
            else if (prop == "pathOffset")
            {
                updates["strokeDashoffset"] = (-NumericValues.GetValueOrDefault("pathOffset", 0)).ToString("G6");
            }
            else if (prop.StartsWith("--"))
            {
                if (NumericValues.TryGetValue(prop, out double nv))
                    updates[prop] = nv.ToString("G6");
                else if (StringValues.TryGetValue(prop, out string? sv))
                    updates[prop] = sv;
            }
            else if (NumericValues.TryGetValue(prop, out double numVal))
            {
                updates[prop] = numVal.ToString("G6");
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
        TransitionConfig? transition,
        TaskCompletionSource? completionSource = null)
    {
        var entries = values.Where(kv => kv.Value != null).ToList();
        if (entries.Count == 0) { completionSource?.TrySetResult(); return; }

        // Complete any previously-pending awaiter so callers aren't stranded when a
        // new animation supersedes the old one.
        if (_completionSource != null && !ReferenceEquals(_completionSource, completionSource))
            _completionSource.TrySetResult();
        _completionSource = completionSource;

        foreach (var (key, value) in entries)
        {
            var perKey = transition?.Properties?.GetValueOrDefault(key) ?? transition ?? new TransitionConfig();
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
                CreateNumericDriver(key, Convert.ToDouble(value), perKey);
        }
    }

    public void SetInstant(Dictionary<string, object?> values)
    {
        foreach (var (key, value) in values)
        {
            if (value == null) continue;
            if (TransformComposer.IsTransformProp(key))
            {
                Transforms[key] = Convert.ToDouble(value);
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
                NumericValues[key] = Convert.ToDouble(value);
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

    public void SetBaseAnimation(Dictionary<string, object?> values, TransitionConfig? transition)
    {
        _baseValues = values;
        _baseTransition = transition;
    }

    public void ActivateGestureLayer(string gesture, Dictionary<string, object?> values, TransitionConfig? transition)
    {
        _gestureLayers[gesture] = new GestureLayer(values, transition);
        AnimateTo(values, transition);
    }

    public void DeactivateGestureLayer(string gesture)
    {
        _gestureLayers.Remove(gesture);
        // Revert to the highest-priority remaining gesture or base
        foreach (var priority in GesturePriority)
        {
            if (_gestureLayers.TryGetValue(priority, out var remaining))
            {
                AnimateTo(remaining.Values, remaining.Transition);
                return;
            }
        }
        if (_baseValues != null)
            AnimateTo(_baseValues, _baseTransition);
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

    private void CreateNumericDriver(string key, double toValue, TransitionConfig config)
    {
        bool isTransform = TransformComposer.IsTransformProp(key);
        double from = isTransform
            ? Transforms.GetValueOrDefault(key, DefaultTransformValue(key))
            : NumericValues.GetValueOrDefault(key, DefaultNumericValue(key));

        Action<double> apply = isTransform
            ? v => ApplyTransform(key, v)
            : v => ApplyNumeric(key, v);

        IAnimationDriver driver = config.Type switch
        {
            TransitionType.Spring  => new SpringDriver(from, toValue, config, apply),
            TransitionType.Inertia => new InertiaDriver(from, config, apply),
            _                      => new TweenDriver(from, toValue, config, apply),
        };

        _activeAnims[key] = driver;
    }

    private void CreateColorDriver(string key, string toValue, TransitionConfig config)
    {
        string from = StringValues.GetValueOrDefault(key, "rgba(0,0,0,0)");
        _activeAnims[key] = new ColorTweenDriver(from, toValue, config, v => ApplyString(key, v));
    }

    private void CreateNumericKeyframesDriver(string key, double[] frames, TransitionConfig config)
    {
        bool isTransform = TransformComposer.IsTransformProp(key);
        Action<double> apply = isTransform
            ? v => ApplyTransform(key, v)
            : v => ApplyNumeric(key, v);
        _activeAnims[key] = new NumericKeyframesDriver(frames, config, apply);
    }

    private void CreateColorKeyframesDriver(string key, string[] frames, TransitionConfig config)
    {
        _activeAnims[key] = new ColorKeyframesDriver(frames, config, v => ApplyString(key, v));
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
        => _colorProps.Contains(key) || key.Contains("color", StringComparison.OrdinalIgnoreCase);

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
            result = oa.Select(x => Convert.ToDouble(x)).ToArray();
            return true;
        }
        return false;
    }

    private void CreateCssDimensionDriver(string key, string toValue, TransitionConfig config)
    {
        // If both from and to are the same unit, interpolate numerically.
        // Otherwise just snap to the new value immediately.
        string fromRaw = StringValues.GetValueOrDefault(key, "");
        if (TryParseCssDimension(toValue, out double toNum, out string toUnit) &&
            TryParseCssDimension(fromRaw, out double fromNum, out string fromUnit) &&
            string.Equals(fromUnit, toUnit, StringComparison.OrdinalIgnoreCase))
        {
            _activeAnims[key] = new TweenDriver(fromNum, toNum, config,
                v => ApplyString(key, v.ToString("G6") + toUnit));
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

    private sealed record GestureLayer(Dictionary<string, object?> Values, TransitionConfig? Transition);
}
