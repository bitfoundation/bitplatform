namespace Bit.Bmotion;

/// <summary>
/// A read-only snapshot of one element's live animation state, produced by
/// <see cref="BmotionAnimationEngine.GetDiagnostics"/> and rendered by <c>&lt;BmotionInspector&gt;</c>.
/// </summary>
/// <param name="Id">The element's engine id.</param>
/// <param name="ActiveDriverCount">Number of per-property drivers currently animating the element.</param>
/// <param name="HasActiveAnimations">Whether the element is animating or being dragged.</param>
/// <param name="IsDragging">Whether the element is mid-drag.</param>
/// <param name="ActiveProperties">The property keys with an active driver this frame.</param>
/// <param name="Transforms">Current transform-component values (x, y, scale, rotate, …).</param>
/// <param name="NumericValues">Current non-transform numeric values (opacity, pathLength, …).</param>
/// <param name="StringValues">Current string values (colors, dimensions, filter, …).</param>
public sealed record BmotionElementDiagnostics(
    string Id,
    int ActiveDriverCount,
    bool HasActiveAnimations,
    bool IsDragging,
    IReadOnlyList<string> ActiveProperties,
    IReadOnlyDictionary<string, double> Transforms,
    IReadOnlyDictionary<string, double> NumericValues,
    IReadOnlyDictionary<string, string> StringValues);
